namespace CertOps.Model
{
    public class Config
    {
        public int ScheduleDelayMilliseconds { get; set; }
        
        public AcmeConfig AcmeConfig { get; set; }

        public AzureConfig AzureConfig { get; set; }
    }
}
