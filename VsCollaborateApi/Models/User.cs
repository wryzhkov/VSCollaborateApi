﻿namespace VsCollaborateApi.Models
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
    }
}