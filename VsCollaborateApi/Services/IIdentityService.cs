
namespace VsCollaborateApi.Services
{
    public interface IIdentityService
    {
        User Authenticate(HttpContext context);
        User? TryAuthenticate(HttpContext context);
    }
}