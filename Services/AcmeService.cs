using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using CertOps.Model;
using Dignus.Collections;
using Dignus.DependencyInjection.Attributes;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace CertOps.Services
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Transient)]
    class AcmeService
    {
        private readonly AcmeContext _acmeContext;
        private readonly ArrayQueue<string> _domains = [];
        public AcmeService(AcmeConfig config, IKey key)
        {
            _acmeContext = new AcmeContext(WellKnownServers.LetsEncryptV2, key);
            _domains.AddRange(config.Domains);
        }

        public async Task<ArrayQueue<string>> GetExpiringDomainsAsync()
        {
            var expiringDomains = new ArrayQueue<string>();

            foreach (var domain in _domains)
            {
                var orderContext = _acmeContext.Order(new Uri(domain));
                var order = await orderContext.Resource();

                if (order.Status != OrderStatus.Valid)
                {
                    continue;
                }

                var expires = order.Expires;
                if (expires.HasValue && expires.Value - DateTime.UtcNow < TimeSpan.FromDays(30))
                {
                    expiringDomains.Add(domain);
                }
            }
            return expiringDomains;
        }

        public async Task<Dictionary<string, string>> GetDns01ChallengesAsync(IList<string> domains)
        {
            var order = await _acmeContext.NewOrder(domains);
            var authorizations = await order.Authorizations();

            var dnsChallengeRecords = new Dictionary<string, string>();

            foreach (var authorization in authorizations)
            {
                var dnsChallenge = await authorization.Dns();
                var dnsTxtRecordName = $"_acme-challenge.{authorization.Location.Host}";
                var keyAuth = _acmeContext.AccountKey.DnsTxt(dnsChallenge.Token);
                dnsChallengeRecords[dnsTxtRecordName] = keyAuth;
            }
            return dnsChallengeRecords;
        }
    }
}
