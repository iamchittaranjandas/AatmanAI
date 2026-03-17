using AatmanAI.Data.Models;

namespace AatmanAI.Services.Interfaces;

public interface IChatService
{
    Task<Conversation> CreateConversationAsync(string? title = null, string? promptFolderId = null, string? promptId = null);
    Task<List<Conversation>> GetConversationsAsync();
    Task<Conversation?> GetConversationAsync(string conversationId);
    Task<List<ChatMessage>> GetMessagesAsync(string conversationId);
    IAsyncEnumerable<string> SendMessageAsync(string conversationId, string content, InferenceParams? overrides = null, bool isVoiceMode = false, string? customPromptId = null, string? language = null, CancellationToken ct = default);
    IAsyncEnumerable<string> RegenerateLastMessageAsync(string conversationId, CancellationToken ct = default);
    Task StopGenerationAsync();
    Task DeleteConversationAsync(string conversationId);
    Task DeleteMessageAsync(string messageId);
    Task<string> ExportAsMarkdownAsync(string conversationId);
    Task UpdateConversationTitleAsync(string conversationId, string title);
}
