using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CertOps.Model
{
    public class AzureConfig
    {
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string AzureSubscriptionId { get; set; }

        public string AzureResourceGroup { get; set; }

        public string AzureDnsZone { get; set; }
    }
}
