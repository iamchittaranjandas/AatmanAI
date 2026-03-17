using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using AatmanAI.Core.Constants;
using AatmanAI.Core.Enums;
using AatmanAI.Services.Interfaces;
using AatmanAI.Services.Native;
using static AatmanAI.Services.Native.LlamaNative;

namespace AatmanAI.Services;

/// <summary>
/// Inference service for on-device LLM inference using llama.cpp via P/Invoke.
/// </summary>
public class InferenceService : IInferenceService
{
    private InferenceState _state = InferenceState.Idle;
    private CancellationTokenSource? _generationCts;
    private PowerMode _powerMode = PowerMode.Auto;
    private double _temperature = AppConstants.DefaultTemperature;
    private double _topP = AppConstants.DefaultTopP;
    private string? _loadedModelPath;

    // Native handles
    private IntPtr _model = IntPtr.Zero;
    private IntPtr _context = IntPtr.Zero;
    private IntPtr _vocab = IntPtr.Zero;
    private static bool _backendInitialized;

    public InferenceState State
    {
        get => _state;
        private set
        {
            if (_state == value) return;
            _state = value;
            StateChanged?.Invoke(this, value);
        }
    }

    public string? LoadedModelId { get; private set; }
    public double TokensPerSecond { get; private set; }
    public int TotalTokensGenerated { get; private set; }

    public event EventHandler<InferenceState>? StateChanged;

    public async Task LoadModelAsync(string modelId, string modelPath, CancellationToken ct = default)
    {
        if (State == InferenceState.Loading) return;

        State = InferenceState.Loading;
        try
        {
            await Task.Run(() =>
            {
                // Initialize backend once
                if (!_backendInitialized)
                {
                    llama_backend_init();
                    _backendInitialized = true;
                }

                // Unload previous model if any
                UnloadNative();

                // Verify file exists and is readable
                if (!File.Exists(modelPath))
                    throw new InvalidOperationException($"Model file not found: {modelPath}");

                Console.WriteLine($"[AatmanAI] Loading model: {modelPath} size={new FileInfo(modelPath).Length}");

                // Load model via wrapper (avoids struct-by-value ABI issues)
                _model = LlamaNative.wrapper_model_load(modelPath, 0);
                Console.WriteLine($"[AatmanAI] wrapper_model_load returned: {_model}");
                if (_model == IntPtr.Zero)
                    throw new InvalidOperationException($"Failed to load model from {modelPath}");

                _vocab = llama_model_get_vocab(_model);

                // Create context via wrapper
                var threads = GetThreadCount();
                _context = LlamaNative.wrapper_context_create(
                    _model,
                    (uint)AppConstants.DefaultContextLength,
                    512, 512,
                    threads, threads);
                if (_context == IntPtr.Zero)
                {
                    llama_model_free(_model);
                    _model = IntPtr.Zero;
                    throw new InvalidOperationException("Failed to create context");
                }
            }, ct);

            _loadedModelPath = modelPath;
            LoadedModelId = modelId;
            State = InferenceState.Loaded;
        }
        catch (Exception ex)
        {
            State = InferenceState.Error;
            Console.WriteLine($"[AatmanAI] LoadModelAsync error: {ex.Message}");
            // Don't re-throw — let callers check State == Error
        }
    }

    public async Task UnloadModelAsync()
    {
        await StopGenerationAsync();
        await Task.Run(UnloadNative);
        _loadedModelPath = null;
        LoadedModelId = null;
        State = InferenceState.Idle;
    }

    public async IAsyncEnumerable<string> GenerateAsync(
        string prompt,
        string systemPrompt,
        List<(string role, string content)>? history,
        InferenceParams? parameters,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (State != InferenceState.Loaded || _context == IntPtr.Zero)
            throw new InvalidOperationException("Model not loaded");

        State = InferenceState.Generating;
        _generationCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var cts = _generationCts;
        var startTime = DateTime.UtcNow;
        var tokenCount = 0;

        // Unbounded channel: background thread writes tokens, this method streams them out.
        // Eliminates per-token Task.Run overhead (was 2 dispatches × 256 tokens = 512 thread hops).
        var channel = Channel.CreateUnbounded<string>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });

        _ = Task.Run(() =>
        {
            try
            {
                RunGenerationLoop(prompt, systemPrompt, history, parameters, channel.Writer, cts.Token);
                channel.Writer.TryComplete();
            }
            catch (OperationCanceledException)
            {
                channel.Writer.TryComplete();
            }
            catch (Exception ex)
            {
                channel.Writer.TryComplete(ex);
            }
        }, cts.Token);

        try
        {
            await foreach (var token in channel.Reader.ReadAllAsync(cts.Token))
            {
                tokenCount++;
                TotalTokensGenerated++;
                var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
                TokensPerSecond = elapsed > 0 ? tokenCount / elapsed : 0;
                yield return token;
            }
        }
        finally
        {
            State = InferenceState.Loaded;
            _generationCts = null;
        }
    }

    private void RunGenerationLoop(
        string prompt,
        string systemPrompt,
        List<(string role, string content)>? history,
        InferenceParams? parameters,
        ChannelWriter<string> writer,
        CancellationToken ct)
    {
        var temp = (float)(parameters?.Temperature ?? _temperature);
        var topP = (float)(parameters?.TopP ?? _topP);
        var maxTokens = parameters?.MaxTokens ?? AppConstants.DefaultMaxTokens;

        // Trim history to keep prompt within context budget (reserve 200 tokens for new reply)
        var trimmedHistory = TrimHistoryToFit(history, AppConstants.DefaultContextLength - 200);
        var fullPrompt = BuildChatPrompt(systemPrompt, trimmedHistory, prompt);

        var tokens = LlamaNative.Tokenize(_vocab, fullPrompt, true, true);

        // Recreate context for a clean KV cache — memory-clear APIs crash on this build
        llama_free(_context);
        var threads = GetThreadCount();
        _context = LlamaNative.wrapper_context_create(
            _model,
            (uint)AppConstants.DefaultContextLength,
            512, 512,
            threads, threads);
        if (_context == IntPtr.Zero)
            throw new InvalidOperationException("Failed to recreate inference context");

        const int nBatch = 512;
        var batch = LlamaNative.wrapper_batch_init(nBatch, 0, 1);
        try
        {
            // Decode prompt in n_batch-sized chunks
            for (int batchStart = 0; batchStart < tokens.Length; batchStart += nBatch)
            {
                ct.ThrowIfCancellationRequested();
                LlamaNative.wrapper_batch_reset(batch);
                int batchEnd = Math.Min(batchStart + nBatch, tokens.Length);
                bool isLastBatch = batchEnd == tokens.Length;
                for (int i = batchStart; i < batchEnd; i++)
                {
                    bool isLastToken = isLastBatch && (i == batchEnd - 1);
                    LlamaNative.wrapper_batch_add(batch, tokens[i], i, 0, isLastToken ? 1 : 0);
                }
                if (LlamaNative.wrapper_decode(_context, batch) != 0)
                    throw new InvalidOperationException($"llama_decode failed for prompt batch at {batchStart}");
            }

            var sampler = LlamaNative.wrapper_sampler_chain_create();
            try
            {
                llama_sampler_chain_add(sampler, llama_sampler_init_top_k(20));
                llama_sampler_chain_add(sampler, llama_sampler_init_top_p(topP, 1));
                llama_sampler_chain_add(sampler, llama_sampler_init_temp(temp));
                llama_sampler_chain_add(sampler, llama_sampler_init_dist((uint)Random.Shared.Next()));

                var nCur = tokens.Length;

                for (int i = 0; i < maxTokens; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    var newTokenId = LlamaNative.wrapper_sampler_sample(sampler, _context, -1);

                    if (LlamaNative.wrapper_vocab_is_eog(_vocab, newTokenId) != 0)
                        break;

                    var piece = LlamaNative.TokenToPiece(_vocab, newTokenId);
                    var sanitized = SanitizeToken(piece);
                    if (IsTemplateStopMarker(sanitized))
                        break;

                    if (!string.IsNullOrEmpty(sanitized))
                        writer.TryWrite(sanitized);

                    LlamaNative.wrapper_batch_reset(batch);
                    LlamaNative.wrapper_batch_add(batch, newTokenId, nCur, 0, 1);
                    nCur++;

                    if (LlamaNative.wrapper_decode(_context, batch) != 0)
                        break;
                }
            }
            finally
            {
                llama_sampler_free(sampler);
            }
        }
        finally
        {
            LlamaNative.wrapper_batch_free(batch);
        }
    }

    public async Task StopGenerationAsync()
    {
        if (_generationCts is not null)
        {
            await _generationCts.CancelAsync();
            _generationCts = null;
        }
    }

    public void SetPowerMode(PowerMode mode)
    {
        _powerMode = mode;
    }

    public void UpdateSamplingParams(double? temperature, double? topP)
    {
        if (temperature.HasValue) _temperature = temperature.Value;
        if (topP.HasValue) _topP = topP.Value;
    }

    private void UnloadNative()
    {
        if (_context != IntPtr.Zero)
        {
            llama_free(_context);
            _context = IntPtr.Zero;
        }
        if (_model != IntPtr.Zero)
        {
            llama_model_free(_model);
            _model = IntPtr.Zero;
        }
        _vocab = IntPtr.Zero;
    }

    private int GetThreadCount()
    {
        return _powerMode switch
        {
            PowerMode.Efficient => 2,
            PowerMode.Performance => Math.Max(6, Environment.ProcessorCount - 1),
            _ => Math.Max(4, Environment.ProcessorCount - 2) // Auto: use most cores
        };
    }

    private static List<(string role, string content)>? TrimHistoryToFit(
        List<(string role, string content)>? history, int maxTokenBudget)
    {
        if (history is null || history.Count == 0) return history;
        // Rough estimate: 1 token ≈ 4 characters. Keep pairs from most recent.
        const int charsPerToken = 4;
        int budget = maxTokenBudget * charsPerToken;
        int totalChars = 0;
        var kept = new List<(string, string)>(history.Count);
        for (int i = history.Count - 1; i >= 0; i--)
        {
            totalChars += history[i].content.Length;
            if (totalChars > budget) break;
            kept.Insert(0, history[i]);
        }
        return kept.Count > 0 ? kept : null;
    }

    private static string BuildChatPrompt(string systemPrompt, List<(string role, string content)>? history, string userPrompt)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"<|im_start|>system\n{systemPrompt}<|im_end|>");

        if (history is not null)
        {
            foreach (var (role, content) in history)
            {
                sb.AppendLine($"<|im_start|>{role}\n{content}<|im_end|>");
            }
        }

        sb.AppendLine($"<|im_start|>user\n{userPrompt}<|im_end|>");
        sb.Append("<|im_start|>assistant\n");
        return sb.ToString();
    }

    private static string SanitizeToken(string token)
    {
        return token
            .Replace("<|im_end|>", "")
            .Replace("<|im_start|>", "")
            .Replace("<|endoftext|>", "")
            .Replace("<|end|>", "")
            .Replace("</s>", "")
            .Replace("<s>", "");
    }

    private static bool IsTemplateStopMarker(string token)
    {
        var trimmed = token.Trim();
        return trimmed is "<|im_end|>" or "<|endoftext|>" or "<|end|>" or "</s>"
            or "<|eot_id|>" or "<end_of_turn>";
    }
}
