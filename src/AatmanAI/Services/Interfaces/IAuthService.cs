namespace AatmanAI.Services.Interfaces;

public interface IAuthService
{
    bool IsAuthenticated { get; }
    Task<bool> LoginAsync(string username, string password);
    Task<bool> RegisterAsync(string username, string password);
    Task LogoutAsync();
}
