using System;

namespace mailica.Entities
{
    public class Job
    {
        public int JobId { get; set; }
        public Guid AliasId { get; set; }
        public string Title { get; set; }
        public int FromAccountId { get; set; }
        public int? ToAccountId { get; set; }
        public string CronJson { get; set; }
    }
}