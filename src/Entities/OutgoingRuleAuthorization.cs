namespace mailica.Entities
{
    public class OutgoingRuleAuthorization
    {
        public int OutgoingRuleId { get; set; }
        public int AccountId { get; set; }

        public OutgoingRule? OutgoingRule { get; set; }
        public Account? Account { get; set; }
    }
}