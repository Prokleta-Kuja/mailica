using System;
using System.Collections.Generic;

namespace mailica.Entities
{
    public class Job
    {
        Job() { }

        internal Job(int credentialId, bool leaveCopy, TimeSpan syncEvery)
        {
            CredentialId = credentialId;
            MarkAsRead = true;
            LeaveCopy = leaveCopy;
            SyncEvery = syncEvery;
        }

        public int JobId { get; set; }
        public Guid AliasId { get; set; }
        public int CredentialId { get; set; } // one-on-one
        public bool LeaveCopy { get; set; }
        public bool MarkAsRead { get; set; }
        public TimeSpan SyncEvery { get; set; }
        public int? CatchAllCredentialId { get; set; }
        public string? CatchAllFolder { get; set; }
        public DateTime? Disabled { get; set; }

        public Credential? Credential { get; set; } // one-on-one
        public Credential? CatchAllCredential { get; set; }
        public virtual ICollection<IncomingRule> IncomingRules { get; set; } = new HashSet<IncomingRule>();
    }
}