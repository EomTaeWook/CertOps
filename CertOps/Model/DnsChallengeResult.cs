using Certes.Acme;
using Dignus.Collections;

namespace CertOps.Model
{
    internal class DnsChallengeResult
    {
        public IOrderContext Order { get; set; }
        public Dictionary<string, ArrayQueue<IChallengeContext>> ChallengesByRecord { get; set; }
    }
}
