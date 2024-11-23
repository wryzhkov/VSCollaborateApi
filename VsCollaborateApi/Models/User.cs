using System.Security.Cryptography;

namespace VsCollaborateApi.Models
{
    public class User
    {
        public User(string email, string name)
        {
            Email = email;
            Name = name;
        }

        public string Email { get; set; }
        public string Name { get; set; }
        public string SessionId { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is User user &&
                   Email == user.Email &&
                   Name == user.Name &&
                   SessionId == user.SessionId;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Email, Name, SessionId);
        }
    }
}