using AatmanAI.Services.Interfaces;

namespace AatmanAI.Services;

/// <summary>Phase 3 stub - Auth service</summary>
public class AuthService : IAuthService
{
    public bool IsAuthenticated => true; // Skip auth for MVP
    public Task<bool> LoginAsync(string username, string password) => Task.FromResult(true);
    public Task<bool> RegisterAsync(string username, string password) => Task.FromResult(true);
    public Task LogoutAsync() => Task.CompletedTask;
}
