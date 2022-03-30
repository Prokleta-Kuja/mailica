using System;
using System.Collections.Generic;
using mailica.Enums;
using mailica.Services;

namespace mailica.Entities
{
    public class Credential
    {
        Credential()
        {
            Host = null!;
            Username = null!;
        }
        internal Credential(CredentialType type, string host, int port, string username)
        {
            Type = type;
            Host = host;
            Port = port;
            Username = username;
            IsValid = true;
        }

        public int CredentialId { get; set; }
        public Guid AliasId { get; set; }
        public CredentialType Type { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string? Password { get; set; }
        public bool IsValid { get; set; }
        public DateTime? Disabled { get; set; }
        public Job? Job { get; set; }

        public virtual ICollection<Job> CatchAllJobs { get; set; } = new HashSet<Job>();
        public virtual ICollection<OutgoingRule> OutgoingRules { get; set; } = new HashSet<OutgoingRule>();
        public Credential SetPassword(string newPassword, IPasswordHasher hasher)
        {
            throw new NotImplementedException("Must be stored with reversible method");
            // if (string.IsNullOrWhiteSpace(newPassword))
            //     throw new ArgumentNullException(newPassword);

            // Password = hasher.HashPassword(newPassword);
            // return this;
        }
    }
}