namespace PartnerBot.Core.Entities
{
    /// <summary>
    /// A set of arguemtns related to the Partner Sender
    /// </summary>
    public class PartnerSenderArguments
    {
        public int DonorRun { get; set; } = 0;
        public bool IgnoreOwnerMatch { get; set; } = false;
        public bool IgnoreCacheMatch { get; set; } = false;
        public bool DevelopmentStressTest { get; set; } = false;

        public override string ToString()
        {
            return $"SenderArgs:[DonorRun: {this.DonorRun}," +
                $" IgnoreOwnerMatch: {this.IgnoreOwnerMatch}," +
                $" IgnoreCacheMatch: {this.IgnoreCacheMatch}," +
                $" DevelopmentStressTest: {this.DevelopmentStressTest}]";
        }
    }
}
