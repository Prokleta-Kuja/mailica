namespace mailica.Entities
{
    public class IncomingRuleCredential
    {
        public int IncomingRuleId { get; set; }
        public int CredentialId { get; set; }

        public IncomingRule? IncomingRule { get; set; }
        public Credential? Credential { get; set; }
    }
}