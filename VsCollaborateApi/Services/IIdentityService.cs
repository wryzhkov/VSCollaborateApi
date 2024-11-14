using VsCollaborateApi.Models;

namespace VsCollaborateApi.Services
{
    public interface IIdentityService
    {
        Task<User> AuthenticateAsync(HttpContext context);

        Task<User?> CreateUserAsync(User user, string password);

        Task<string?> LoginAsync(string email, string password);

        string RefreshToken(User user);

        Task<User?> TryAuthenticateAsync(HttpContext context);
    }
}