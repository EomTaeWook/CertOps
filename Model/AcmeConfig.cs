using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CertOps.Model
{
    public class AcmeConfig
    {
        public string AccountKeyPath { get; set; }

        public List<string> Domains { get; set; }
    }
}
