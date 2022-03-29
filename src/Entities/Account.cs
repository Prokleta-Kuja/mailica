using System;
using mailica.Enums;

namespace mailica.Entities
{
    public class Account
    {
        public int AccountId { get; set; }
        public Guid AliasId { get; set; }
        public AccountType Type { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IsValid { get; set; }
    }
}