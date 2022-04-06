using System;
using System.Collections.Generic;
using System.Diagnostics;
using mailica.Enums;

namespace mailica.Entities
{
    [DebuggerDisplay("Id={CredentialId} User={Username}")]
    public class Credential
    {
        Credential()
        {
            Host = null!;
            Username = null!;
            Password = null!;
        }
        internal Credential(CredentialType type, string host, int port, string username, string protectedPass)
        {
            Type = type;
            Host = host;
            Port = port;
            Username = username;
            Password = protectedPass;
            IsValid = true;
        }

        public int CredentialId { get; set; }
        public Guid AliasId { get; set; }
        public CredentialType Type { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IsValid { get; set; }
        public DateTime? Disabled { get; set; }
        public Job? Job { get; set; }

        public virtual ICollection<Job> CatchAllJobs { get; set; } = new HashSet<Job>();
        public virtual ICollection<IncomingRuleCredential> IncomingRuleCredentials { get; set; } = new HashSet<IncomingRuleCredential>();
        public virtual ICollection<OutgoingRule> OutgoingRules { get; set; } = new HashSet<OutgoingRule>();
    }
}