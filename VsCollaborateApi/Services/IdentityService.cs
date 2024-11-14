using JWT.Algorithms;
using JWT;
using JWT.Builder;
using JWT.Serializers;
using Microsoft.AspNetCore.Components.RenderTree;
using VsCollaborateApi.Models;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.OpenApi.Validations;

namespace VsCollaborateApi.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly IDatabaseClient _databaseClient;

        private readonly string JWT_KEY = "AoN1mHIc5XkVHgcRqSFNU8JyX0R2nINGTeVR3iPRnxx0izJFyR/3KjmYt2dMGVlfiKmdH6+DGWrq3EeeXg/f+naTfLDudXQP3eulNLvHgEz85JrMKhFebjtBAX+f3vKAf4il4DDWllUXP8WRC364vRxoyT8Ln4+PY0RJ67T2TCixzY/ZlAxhBEB8B0JgFt2KR4Ls1Z6+UT8VD8LiI9N/JkF3LDXl08v6AqNjbaMMwSRqn9ua7WUhZYrQTGqOlZu+0LlcW59EWGCty2jMkgiM+qXFQLZm+3JOv0wGd66ufCjkFJCRP4Wu4PdkNYq9BipBG9WwlCoUJXP4IW2FRiYjw6zmWBygNUhqBlUQQpn2GYBE++CfvJMePeEBJeI+vP1nuzaXHpK8jA0Kp5xJLKdCiiXllbGmNss7AFak5N/dN9euV9S2Qw0FOcMCALb1gnjAWA0ewYhpgnLmzyHxV44/nM3gRu74pXeYiJBTcrinVDfWOr5SOyM2w8v9DD2WUsHmSXzLBSmtxlOoXH014VJTgQ3NqSRvWoUBYcpFYmXm9iuFgPZK18vwWfJcmuLijPELFFuBAOHjK+WIPYQ5nPl7LVV5URG5Fe3aa2FaadM3935ZJ7zbHp3gHHaf5frr073+uRyhXEq4pQYQWbp07/gwboZ/qylviMBLIo2kSfKvG0o=";

        public IdentityService(IDatabaseClient databaseClient)

        {
            _databaseClient = databaseClient;
        }

        public async Task<User> AuthenticateAsync(HttpContext context)
        {
            var user = await TryAuthenticateAsync(context);
            if (user == null)
            {
                throw new UnauthorizedAccessException();
            }
            return user;
        }

        public async Task<User?> TryAuthenticateAsync(HttpContext context)
        {
            var header = context.Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrEmpty(header))
            {
                return null;
            }
            if (!header.StartsWith("Bearer "))
            {
                return null;
            }
            var token = header.Substring(7);

            var json = JwtBuilder.Create()
                                 .WithAlgorithm(new HMACSHA512Algorithm())
                                 .WithSecret(JWT_KEY)
                                 .MustVerifySignature()
                                 .Decode<IDictionary<string, object>>(token);

            var userEmail = json["user"].ToString();
            var user = await _databaseClient.FindUserAsync(userEmail);
            return user;
        }

        public async Task<User?> CreateUserAsync(User user, string password)
        {
            var existingUser = await _databaseClient.FindUserAsync(user.Email);
            if (existingUser != null)
            {
                throw new ArgumentException("User already exists");
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            if (await _databaseClient.CreateUser(user, passwordHash))
            {
                return await _databaseClient.FindUserAsync(user.Email);
            }
            return null;
        }

        public async Task<string?> LoginAsync(string email, string password)
        {
            var existingUser = await _databaseClient.FindUserAsync(email);
            if (existingUser == null)
            {
                throw new ArgumentException("User doesn't exists");
            }

            var passwordHash = await _databaseClient.GetPassword(existingUser);

            if (!BCrypt.Net.BCrypt.Verify(password, passwordHash))
            {
                throw new UnauthorizedAccessException("Password is not correct");
            }

            string key = JWT_KEY;
            var token = JwtBuilder.Create()
                                  .WithAlgorithm(new HMACSHA512Algorithm())
                                  .WithSecret(key)
                                  .AddClaim("exp", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds())
                                  .AddClaim("user", email)
                                  .AddClaim("sessionId", Guid.NewGuid())
                                  .Encode();
            return token;
        }

        public string RefreshToken(User user)
        {
            var payload = new Dictionary<string, object>
            {
                { "email", user.Email},
                { "sessionId", Guid.NewGuid()}
            };

            string key = JWT_KEY;
            var token = JwtBuilder.Create()
                                  .WithAlgorithm(new HMACSHA512Algorithm())
                                  .WithSecret(key)
                                  .AddClaim("exp", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds())
                                  .AddClaim("user", user.Email)
                                  .AddClaim("sessionId", Guid.NewGuid())
                                  .Encode();
            return token;
        }
    }
}