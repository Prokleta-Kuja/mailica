using System;
using System.Collections.Generic;

namespace mailica.Entities
{
    public class IncomingRule
    {
        IncomingRule()
        {
            Filter = null!;
        }
        internal IncomingRule(int jobId, string filter)
        {
            JobId = jobId;
            Filter = filter;
        }

        public int IncomingRuleId { get; set; }
        public Guid AliasId { get; set; }
        public int JobId { get; set; }
        public string Filter { get; set; }
        public bool IsPlain { get; set; }
        public DateTime? Disabled { get; set; }

        public Job? Job { get; set; }
        public virtual ICollection<IncomingRuleCredential> IncomingRuleCredentials { get; set; } = new HashSet<IncomingRuleCredential>();
    }
}