namespace CertOps.Model
{
    public class AcmeConfig
    {
        public string OutCertificatePath { get; set; }

        public List<string> Domains { get; set; }

        public AcemeAccountConfig AccountConfig { get; set; }
    }

    public class AcemeAccountConfig
    {
        public string Email { get; set; }

        public string AccountKeyPath { get; set; }
    }
}
