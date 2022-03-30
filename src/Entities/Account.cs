using System;
using System.Collections.Generic;
using mailica.Services;

namespace mailica.Entities
{
    public class Account
    {
        Account()
        {
            Username = null!;
        }
        internal Account(string username)
        {
            Username = username;
        }

        public int AccountId { get; set; }
        public string Username { get; set; }
        public string? Password { get; set; }
        public string? LdapBindDn { get; set; }
        public DateTime? Disabled { get; set; }

        public virtual ICollection<OutgoingRuleAuthorization> Authorizations { get; set; } = new HashSet<OutgoingRuleAuthorization>();
        // TODO: limit to countries

        public Account SetPassword(string newPassword, IPasswordHasher hasher)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
                throw new ArgumentNullException(newPassword);

            Password = hasher.HashPassword(newPassword);
            return this;
        }
    }
}