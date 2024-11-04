namespace VsCollaborateApi.Services
{
    public class IdentityService : IIdentityService
    {
        public User Authenticate(HttpContext context)
        {
            var user = TryAuthenticate(context);
            if (user == null)
            {
                throw new UnauthorizedAccessException();
            }
            return user;
        }

        public User? TryAuthenticate(HttpContext context)
        {
            var header = context.Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrEmpty(header))
            {
                header = "Bearer ";
                //return null;
            }
            if (!header.StartsWith("Bearer "))
            {
                //return null;
            }
            var token = header.Substring(7);

            var tokenParts = token.Split(" ");
            if (tokenParts.Length >= 2)
            {
                return new User(tokenParts[0], tokenParts[1]);
            }
            // TODO add jwt here
            return new User("test@skrynia.com", "Yasos Biba");
        }
    }

    public class User
    {
        public User(string email, string name)
        {
            Email = email;
            Name = name;
        }

        public string Email { get; set; }
        public string Name { get; set; }
    }
}