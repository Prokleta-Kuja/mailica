using System;
using System.Collections.Generic;

namespace mailica.Entities
{
    public class OutgoingRule
    {
        public OutgoingRule(int credentialId, int order, string filter)
        {
            CredentialId = credentialId;
            Order = order;
            Filter = filter;
        }

        public int OutgoingRuleId { get; set; }
        public Guid AliasId { get; set; }
        public int CredentialId { get; set; }
        public int Order { get; set; }
        public string Filter { get; set; }
        public bool IsPlain { get; set; }
        public DateTime? Disabled { get; set; }

        public Credential? Credential { get; set; }

        public virtual ICollection<OutgoingRuleAuthorization> Authorizations { get; set; } = new HashSet<OutgoingRuleAuthorization>();
    }
}