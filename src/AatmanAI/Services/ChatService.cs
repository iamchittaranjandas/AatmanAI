using System.Runtime.CompilerServices;
using System.Text;
using AatmanAI.Core.Constants;
using AatmanAI.Data.Database;
using AatmanAI.Data.Models;
using AatmanAI.Services.Interfaces;

namespace AatmanAI.Services;

public class ChatService : IChatService
{
    private readonly AppDatabase _db;
    private readonly IInferenceService _inference;
    private readonly IModelService _modelService;
    private readonly ICustomPromptService _promptService;
    private readonly IVaultService _vaultService;

    public ChatService(AppDatabase db, IInferenceService inference, IModelService modelService, ICustomPromptService promptService, IVaultService vaultService)
    {
        _db = db;
        _inference = inference;
        _modelService = modelService;
        _promptService = promptService;
        _vaultService = vaultService;
    }

    public async Task<Conversation> CreateConversationAsync(string? title = null, string? promptFolderId = null, string? promptId = null)
    {
        var conversation = new Conversation
        {
            Title = title ?? "New Chat",
            CustomPromptFolderId = promptFolderId,
            CustomPromptId = promptId,
            ModelId = _inference.LoadedModelId
        };

        var conn = await _db.GetConnectionAsync();
        await conn.InsertAsync(conversation);
        return conversation;
    }

    public async Task<List<Conversation>> GetConversationsAsync()
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.Table<Conversation>()
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();
    }

    public async Task<Conversation?> GetConversationAsync(string conversationId)
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.FindAsync<Conversation>(conversationId);
    }

    public async Task<List<ChatMessage>> GetMessagesAsync(string conversationId)
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.Table<ChatMessage>()
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async IAsyncEnumerable<string> SendMessageAsync(
        string conversationId, string content,
        InferenceParams? overrides = null, bool isVoiceMode = false,
        string? customPromptId = null, string? language = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var conn = await _db.GetConnectionAsync();

        // Save user message
        var userMessage = new ChatMessage
        {
            ConversationId = conversationId,
            ContentText = content,
            Role = "user",
            ModelId = _inference.LoadedModelId
        };
        await conn.InsertAsync(userMessage);

        // Update conversation
        var conversation = await conn.FindAsync<Conversation>(conversationId);
        if (conversation is not null)
        {
            conversation.MessageCount++;
            conversation.UpdatedAt = DateTime.UtcNow;
            if (conversation.MessageCount == 1)
                conversation.Title = content.Length > 40 ? content[..40] + "..." : content;
            await conn.UpdateAsync(conversation);
        }

        // Check deterministic policy response
        var policyResponse = GetDeterministicPolicyResponse(content);
        if (policyResponse is not null)
        {
            var policyMsg = new ChatMessage
            {
                ConversationId = conversationId,
                ContentText = policyResponse,
                Role = "assistant",
                ModelId = _inference.LoadedModelId
            };
            await conn.InsertAsync(policyMsg);
            if (conversation is not null)
            {
                conversation.MessageCount++;
                await conn.UpdateAsync(conversation);
            }
            yield return policyResponse;
            yield break;
        }

        // Build system prompt with vault RAG context
        var systemPrompt = await BuildSystemPromptAsync(conversation, content, isVoiceMode, customPromptId, language);

        // Get full conversation history — InferenceService.TrimHistoryToFit handles context budget
        var messages = await GetMessagesAsync(conversationId);
        var history = messages
            .Where(m => m.Role is "user" or "assistant")
            .SkipLast(1) // exclude the current user message we just saved
            .Select(m => (m.Role, m.ContentText))
            .ToList();

        // Stream response from inference
        var responseBuilder = new StringBuilder();
        var startTime = DateTime.UtcNow;
        var tokenCount = 0;

        await foreach (var token in _inference.GenerateAsync(content, systemPrompt, history, overrides, ct))
        {
            var sanitized = SanitizeToken(token);
            if (string.IsNullOrEmpty(sanitized)) continue;

            // Check for template stop markers
            if (IsTemplateStopMarker(sanitized))
                break;

            responseBuilder.Append(sanitized);
            tokenCount++;
            yield return sanitized;
        }

        // Save assistant message
        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
        var assistantMessage = new ChatMessage
        {
            ConversationId = conversationId,
            ContentText = responseBuilder.ToString(),
            Role = "assistant",
            ModelId = _inference.LoadedModelId,
            TokenCount = tokenCount,
            GenerationTimeMs = (int)elapsed,
            TokensPerSecond = elapsed > 0 ? tokenCount / (elapsed / 1000.0) : 0
        };
        await conn.InsertAsync(assistantMessage);

        if (conversation is not null)
        {
            conversation.MessageCount++;
            await conn.UpdateAsync(conversation);
        }
    }

    public async IAsyncEnumerable<string> RegenerateLastMessageAsync(
        string conversationId, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var conn = await _db.GetConnectionAsync();
        var messages = await GetMessagesAsync(conversationId);

        // Remove last assistant message
        var lastAssistant = messages.LastOrDefault(m => m.Role == "assistant");
        if (lastAssistant is not null)
            await conn.DeleteAsync(lastAssistant);

        // Get last user message
        var lastUser = messages.LastOrDefault(m => m.Role == "user");
        if (lastUser is null) yield break;

        // Regenerate
        // Decrement message count for the removed assistant message
        var conversation = await conn.FindAsync<Conversation>(conversationId);
        if (conversation is not null)
        {
            conversation.MessageCount = Math.Max(0, conversation.MessageCount - 1);
            await conn.UpdateAsync(conversation);
        }

        await foreach (var token in SendMessageAsync(conversationId, lastUser.ContentText, ct: ct))
        {
            yield return token;
        }
    }

    public async Task StopGenerationAsync()
    {
        await _inference.StopGenerationAsync();
    }

    public async Task DeleteConversationAsync(string conversationId)
    {
        var conn = await _db.GetConnectionAsync();
        await conn.ExecuteAsync("DELETE FROM ChatMessages WHERE ConversationId = ?", conversationId);
        await conn.ExecuteAsync("DELETE FROM Conversations WHERE Id = ?", conversationId);
    }

    public async Task DeleteMessageAsync(string messageId)
    {
        var conn = await _db.GetConnectionAsync();
        await conn.ExecuteAsync("DELETE FROM ChatMessages WHERE Id = ?", messageId);
    }

    public async Task<string> ExportAsMarkdownAsync(string conversationId)
    {
        var conversation = await GetConversationAsync(conversationId);
        var messages = await GetMessagesAsync(conversationId);

        var sb = new StringBuilder();
        sb.AppendLine($"# {conversation?.Title ?? "Chat"}");
        sb.AppendLine($"*Exported from Aatman AI*\n");

        foreach (var msg in messages.Where(m => m.Role is "user" or "assistant"))
        {
            var label = msg.Role == "user" ? "You" : "Aatman AI";
            sb.AppendLine($"**{label}:** {msg.ContentText}\n");
        }

        return sb.ToString();
    }

    public async Task UpdateConversationTitleAsync(string conversationId, string title)
    {
        var conn = await _db.GetConnectionAsync();
        var conversation = await conn.FindAsync<Conversation>(conversationId);
        if (conversation is not null)
        {
            conversation.Title = title;
            await conn.UpdateAsync(conversation);
        }
    }

    private async Task<string> BuildSystemPromptAsync(Conversation? conversation, string userMessage, bool isVoiceMode, string? customPromptId = null, string? language = null)
    {
        var customPromptText = string.Empty;

        // Use caller-supplied promptId first, fall back to conversation's saved one
        var effectivePromptId = customPromptId ?? conversation?.CustomPromptId;
        if (effectivePromptId is not null)
        {
            var customPrompt = await _promptService.GetPromptAsync(effectivePromptId);
            if (customPrompt is not null)
                customPromptText = $"CUSTOM INSTRUCTION: {customPrompt.Prompt}";
        }

        // Language instruction — prepended so the model always sees it first
        if (!string.IsNullOrWhiteSpace(language) && language != "English")
            customPromptText = $"IMPORTANT: Respond only in {language}. " + customPromptText;

        // Search vault for relevant document chunks (RAG)
        var vaultContext = string.Empty;
        try
        {
            // Auto-initialize vault if needed
            if (!_vaultService.IsInitialized)
                await _vaultService.InitializeAsync();

            if (_vaultService.TotalDocuments > 0)
            {
                var results = await _vaultService.SearchAsync(userMessage, topK: 5);

                // Fallback: if keyword search found nothing, grab first chunks from each doc
                if (results.Count == 0)
                    results = await _vaultService.GetTopChunksAsync(3);

                if (results.Count > 0)
                    vaultContext = _vaultService.BuildContext(results);
            }
        }
        catch
        {
            // Vault search failure should not block chat
        }

        // Build user profile context so the AI knows who it's talking to
        var profileName = await _db.GetSettingAsync(AppConstants.KeyProfileName);
        var profileGoal = await _db.GetSettingAsync(AppConstants.KeyProfileLifeGoal);
        var profileInterests = await _db.GetSettingAsync(AppConstants.KeyProfileInterests);

        var userProfile = string.Empty;
        if (!string.IsNullOrWhiteSpace(profileName))
        {
            var sb = new System.Text.StringBuilder("USER PROFILE (use this to personalize your responses):");
            sb.Append($" The user's name is {profileName}.");
            if (!string.IsNullOrWhiteSpace(profileGoal))
                sb.Append($" Their life goal is: {profileGoal}.");
            if (!string.IsNullOrWhiteSpace(profileInterests))
                sb.Append($" Their interests include: {profileInterests}.");
            sb.Append(" Address the user by name occasionally and tailor responses to their background and goals.");
            userProfile = sb.ToString();
        }

        var prompt = AppConstants.UnifiedSystemPromptTemplate
            .Replace("{INTERACTION_MODE}", isVoiceMode ? "VOICE" : "TEXT")
            .Replace("{MODEL_PROFILE}", "MOBILE_ONLY")
            .Replace("{CUSTOM_PROMPT}", customPromptText)
            .Replace("{VAULT_CONTEXT}", vaultContext)
            .Replace("{USER_PROFILE}", userProfile);

        return prompt;
    }

    private static string? GetDeterministicPolicyResponse(string input)
    {
        var lower = input.Trim().ToLowerInvariant();

        if (lower.Contains("who built you") || lower.Contains("who made you") || lower.Contains("who created you"))
            return "AATMAN AI made by sritcreations.com, origin in India for the world.";

        if (lower.Contains("what is your name") || lower.Contains("who are you") || lower.Contains("what model"))
            return "I am AATMAN AI.";

        if (lower.Contains("is my data") && (lower.Contains("safe") || lower.Contains("private") || lower.Contains("secure")))
            return "AATMAN AI is actively being improved to become more flexible and useful. Your data is not stored on servers. Processing runs on your mobile processor with lightweight on-device models. Data is stored locally in encrypted form.";

        return null;
    }

    private static string SanitizeToken(string token)
    {
        // Remove common template artifacts
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
