using System.Runtime.InteropServices;

namespace AatmanAI.Services.Native;

/// <summary>
/// P/Invoke bindings for llama.cpp native library.
/// Targets the latest llama.cpp API (2025).
/// </summary>
public static unsafe class LlamaNative
{
    private const string LibName = "llama";

    // ===== Backend =====
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void llama_backend_init();

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void llama_backend_free();

    // ===== Model =====
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void llama_model_free(IntPtr model);

    // ===== Context =====

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void llama_free(IntPtr ctx);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint llama_n_ctx(IntPtr ctx);

    // ===== Vocab =====
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr llama_model_get_vocab(IntPtr model);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int llama_vocab_n_tokens(IntPtr vocab);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int llama_vocab_bos(IntPtr vocab);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int llama_vocab_eos(IntPtr vocab);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool llama_vocab_is_eog(IntPtr vocab, int token);

    // ===== Tokenize =====
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int llama_tokenize(
        IntPtr vocab,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string text,
        int text_len,
        int* tokens,
        int n_tokens_max,
        [MarshalAs(UnmanagedType.I1)] bool add_special,
        [MarshalAs(UnmanagedType.I1)] bool parse_special);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int llama_token_to_piece(
        IntPtr vocab,
        int token,
        byte* buf,
        int length,
        int lstrip,
        [MarshalAs(UnmanagedType.I1)] bool special);

    // ===== Batch =====
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern LlamaBatch llama_batch_init(int n_tokens, int embd, int n_seq_max);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void llama_batch_free(LlamaBatch batch);

    // ===== Decode =====
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int llama_decode(IntPtr ctx, LlamaBatch batch);

    // ===== KV Cache =====
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void llama_memory_clear(IntPtr ctx, [MarshalAs(UnmanagedType.I1)] bool data);

    // ===== Logits =====
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern float* llama_get_logits_ith(IntPtr ctx, int i);

    // ===== Sampler =====

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void llama_sampler_chain_add(IntPtr chain, IntPtr smpl);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr llama_sampler_init_temp(float t);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr llama_sampler_init_top_p(float p, nint min_keep);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr llama_sampler_init_top_k(int k);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr llama_sampler_init_greedy();

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr llama_sampler_init_dist(uint seed);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int llama_sampler_sample(IntPtr smpl, IntPtr ctx, int idx);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void llama_sampler_free(IntPtr smpl);

    // ===== Wrapper functions (avoid struct-by-value ABI issues on ARM64) =====
    private const string WrapperLib = "llama_wrapper";

    [DllImport(WrapperLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr wrapper_model_load(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        int n_gpu_layers);

    [DllImport(WrapperLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr wrapper_context_create(
        IntPtr model,
        uint n_ctx,
        uint n_batch,
        uint n_ubatch,
        int n_threads,
        int n_threads_batch);

    [DllImport(WrapperLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr wrapper_sampler_chain_create();

    [DllImport(WrapperLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int wrapper_model_default_params_size();

    [DllImport(WrapperLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int wrapper_context_default_params_size();

    // ===== Wrapper2: batch/decode via pointer to avoid struct-by-value SIGSEGV on ARM64 =====
    private const string Wrapper2Lib = "llama_wrapper2";

    [DllImport(Wrapper2Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr wrapper_batch_init(int n_tokens, int embd, int n_seq_max);

    [DllImport(Wrapper2Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void wrapper_batch_free(IntPtr batch);

    [DllImport(Wrapper2Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void wrapper_batch_reset(IntPtr batch);

    [DllImport(Wrapper2Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void wrapper_batch_add(IntPtr batch, int token, int pos, int seqId, int logits);

    [DllImport(Wrapper2Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int wrapper_decode(IntPtr ctx, IntPtr batch);

    [DllImport(Wrapper2Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int wrapper_batch_get_n_tokens(IntPtr batch);

    [DllImport(Wrapper2Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void wrapper_memory_clear(IntPtr ctx, int data);

    [DllImport(Wrapper2Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int wrapper_tokenize(
        IntPtr vocab,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string text,
        int text_len,
        int* tokens,
        int n_tokens_max,
        int add_special,
        int parse_special);

    [DllImport(Wrapper2Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int wrapper_sampler_sample(IntPtr smpl, IntPtr ctx, int idx);

    [DllImport(Wrapper2Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int wrapper_vocab_is_eog(IntPtr vocab, int token);

    [DllImport(Wrapper2Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int wrapper_token_to_piece(
        IntPtr vocab, int token, byte* buf, int length, int lstrip, int special);

    // ===== Structs =====

    [StructLayout(LayoutKind.Sequential)]
    public struct LlamaModelParams
    {
        public IntPtr devices;                  // ggml_backend_dev_t *
        public IntPtr tensor_buft_overrides;    // const llama_model_tensor_buft_override *
        public int n_gpu_layers;                // int32_t
        public int split_mode;                  // enum llama_split_mode
        public int main_gpu;                    // int32_t
        public IntPtr tensor_split;             // const float *
        public IntPtr progress_callback;        // llama_progress_callback
        public IntPtr progress_callback_user_data; // void *
        public IntPtr kv_overrides;             // const llama_model_kv_override *
        [MarshalAs(UnmanagedType.I1)]
        public bool vocab_only;
        [MarshalAs(UnmanagedType.I1)]
        public bool use_mmap;
        [MarshalAs(UnmanagedType.I1)]
        public bool use_direct_io;
        [MarshalAs(UnmanagedType.I1)]
        public bool use_mlock;
        [MarshalAs(UnmanagedType.I1)]
        public bool check_tensors;
        [MarshalAs(UnmanagedType.I1)]
        public bool use_extra_bufts;
        [MarshalAs(UnmanagedType.I1)]
        public bool no_host;
        [MarshalAs(UnmanagedType.I1)]
        public bool no_alloc;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LlamaContextParams
    {
        public uint n_ctx;
        public uint n_batch;
        public uint n_ubatch;
        public uint n_seq_max;
        public int n_threads;
        public int n_threads_batch;
        public int rope_scaling_type;
        public int pooling_type;
        public int attention_type;
        public int flash_attn_type;             // enum llama_flash_attn_type
        public float rope_freq_base;
        public float rope_freq_scale;
        public float yarn_ext_factor;
        public float yarn_attn_factor;
        public float yarn_beta_fast;
        public float yarn_beta_slow;
        public uint yarn_orig_ctx;
        public float defrag_thold;
        public IntPtr cb_eval;                  // ggml_backend_sched_eval_callback
        public IntPtr cb_eval_user_data;        // void *
        public int type_k;                      // enum ggml_type
        public int type_v;                      // enum ggml_type
        public IntPtr abort_callback;           // ggml_abort_callback
        public IntPtr abort_callback_data;      // void *
        [MarshalAs(UnmanagedType.I1)]
        public bool embeddings;
        [MarshalAs(UnmanagedType.I1)]
        public bool offload_kqv;
        [MarshalAs(UnmanagedType.I1)]
        public bool no_perf;
        [MarshalAs(UnmanagedType.I1)]
        public bool op_offload;
        [MarshalAs(UnmanagedType.I1)]
        public bool swa_full;
        [MarshalAs(UnmanagedType.I1)]
        public bool kv_unified;
        public IntPtr samplers;                 // llama_sampler_seq_config *
        public nuint n_samplers;                // size_t
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LlamaBatch
    {
        public int n_tokens;
        public int* token;
        public IntPtr embd;
        public int* pos;
        public int* n_seq_id;
        public int** seq_id;
        public sbyte* logits;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LlamaSamplerChainParams
    {
        [MarshalAs(UnmanagedType.I1)]
        public bool no_perf;
    }

    // ===== Helper Methods =====

    /// <summary>
    /// Tokenize text and return token array.
    /// </summary>
    public static int[] Tokenize(IntPtr vocab, string text, bool addSpecial, bool parseSpecial)
    {
        // Use UTF-8 byte count for text_len (llama.cpp expects UTF-8 byte length)
        var utf8Len = System.Text.Encoding.UTF8.GetByteCount(text);

        // First call to get required buffer size (via wrapper2 for logging/safety)
        var nTokens = wrapper_tokenize(vocab, text, utf8Len, null, 0, addSpecial ? 1 : 0, parseSpecial ? 1 : 0);
        nTokens = Math.Abs(nTokens);

        var tokens = new int[nTokens];
        fixed (int* ptr = tokens)
        {
            wrapper_tokenize(vocab, text, utf8Len, ptr, nTokens, addSpecial ? 1 : 0, parseSpecial ? 1 : 0);
        }
        return tokens;
    }

    /// <summary>
    /// Convert a token to its text piece.
    /// </summary>
    public static string TokenToPiece(IntPtr vocab, int token)
    {
        var buf = new byte[256];
        fixed (byte* ptr = buf)
        {
            var len = wrapper_token_to_piece(vocab, token, ptr, buf.Length, 0, 0);
            if (len < 0) len = 0;
            return System.Text.Encoding.UTF8.GetString(buf, 0, len);
        }
    }

    /// <summary>
    /// Add a token to a batch at the given position.
    /// </summary>
    public static void BatchAdd(ref LlamaBatch batch, int token, int pos, int seqId, bool logits)
    {
        var i = batch.n_tokens;
        batch.token[i] = token;
        batch.pos[i] = pos;
        batch.n_seq_id[i] = 1;
        batch.seq_id[i][0] = seqId;
        batch.logits[i] = logits ? (sbyte)1 : (sbyte)0;
        batch.n_tokens++;
    }
}
