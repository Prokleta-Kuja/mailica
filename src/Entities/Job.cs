using System;
using System.Collections.Generic;

namespace mailica.Entities
{
    public class Job
    {
        Job() { }

        internal Job(int credentialId, bool markAsRead, TimeSpan syncEvery)
        {
            CredentialId = credentialId;
            MarkAsRead = true;
            SyncEvery = syncEvery;
        }

        public int JobId { get; set; }
        public Guid AliasId { get; set; }
        public int CredentialId { get; set; } // one-on-one
        public bool LeaveCopy { get; set; }
        public bool MarkAsRead { get; set; }
        public TimeSpan SyncEvery { get; set; }
        public int? CatchAllCredentialId { get; set; }

        public Credential? Credential { get; set; } // one-on-one
        public Credential? CatchAllCredential { get; set; }
        public virtual ICollection<IncomingRule> IncomingRules { get; set; } = new HashSet<IncomingRule>();
    }
}