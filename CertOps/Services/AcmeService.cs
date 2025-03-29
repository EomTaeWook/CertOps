using Certes;
using Certes.Acme;
using CertOps.Model;
using Dignus.Collections;
using Dignus.DependencyInjection.Attributes;
using Dignus.Log;
using Dignus.Utils.Extensions;
using System.Security.Cryptography.X509Certificates;

namespace CertOps.Services
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Transient)]
    class AcmeService
    {
        private AcmeContext _acmeContext;
        private readonly AcmeConfig _config;
        public AcmeService(AcmeConfig config)
        {
            _config = config;
        }
        public void InitAcmeContext()
        {
            var basePath = Path.Combine(_config.AccountConfig.AccountKeyPath, "accounts");
            var path = Path.Combine(basePath, "account-key.pem");
            if (File.Exists(path))
            {
                var key = KeyFactory.FromPem(File.ReadAllText(path));
                _acmeContext = new AcmeContext(WellKnownServers.LetsEncryptV2, key);
            }
            else
            {
                var key = KeyFactory.NewKey(KeyAlgorithm.ES256);
                _acmeContext = new AcmeContext(WellKnownServers.LetsEncryptV2, key);
                var accountContext = _acmeContext.NewAccount(_config.AccountConfig.Email, true).GetResult();
                var accountUrl = accountContext.Location;
                File.WriteAllText(Path.Combine(basePath, "account-url.txt"), accountUrl.ToString());
                File.WriteAllText(path, key.ToPem());
            }
        }
        public async Task IssueCertificateAsync(IOrderContext order, string domain)
        {
            var key = KeyFactory.NewKey(KeyAlgorithm.RS256);
            var csrInfo = new CsrInfo
            {
                CommonName = domain,
            };
            var cert = await order.Generate(csrInfo, key);

            var certPem = cert.ToPem();
            var keyPem = key.ToPem();
            if (System.IO.Directory.Exists(_config.OutCertificatePath) == false)
            {
                System.IO.Directory.CreateDirectory(_config.OutCertificatePath);
            }
            var pemPath = Path.Combine(_config.OutCertificatePath, domain);
            try
            {
                {
                    var path = Path.Combine(pemPath, "fullchain.pem");
                    using var writer = new StreamWriter(File.Open(path, FileMode.OpenOrCreate));

                    writer.AutoFlush = true;
                    await writer.WriteAsync(certPem);
                }

                {
                    var path = Path.Combine(pemPath, "privkey.pem");
                    using var writer = new StreamWriter(File.Open(path, FileMode.OpenOrCreate));

                    writer.AutoFlush = true;
                    await writer.WriteAsync(certPem);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
                throw;
            }
            LogHelper.Info("certificate issued and saved successfully.");
        }

        public ArrayQueue<string> GetExpiringDomains()
        {
            var expiringDomains = new ArrayQueue<string>();
            LogHelper.Debug($"checking certificate at: {_config.OutCertificatePath}");

            foreach (var domain in _config.Domains)
            {
                var certFolderName = domain.TrimStart('*', '.');

                var pemPath = Path.Combine(_config.OutCertificatePath, certFolderName, "fullchain.pem");
                if (File.Exists(pemPath) == false)
                {
                    LogHelper.Error($"certificate for '{domain}' not found. marking as expired.");
                    expiringDomains.Add(domain);
                    continue;
                }

                var cert = new X509Certificate2(pemPath);
                if (cert.NotAfter - DateTime.UtcNow < TimeSpan.FromDays(30))
                {
                    expiringDomains.Add(domain);
                }
            }
            return expiringDomains;
        }
        public string GetDnsTxtValue(IChallengeContext challenge)
        {
            return _acmeContext.AccountKey.DnsTxt(challenge.Token);
        }

        public async Task<DnsChallengeResult> GetDns01ChallengesAsync(IList<string> domains)
        {
            var dnsChallengeResult = new DnsChallengeResult();

            var order = await _acmeContext.NewOrder(domains);
            dnsChallengeResult.Order = order;

            var authorizations = await order.Authorizations();

            var challengesByRecordName = new Dictionary<string, ArrayQueue<IChallengeContext>>();

            foreach (var authorization in authorizations)
            {
                var authDetails = await authorization.Resource();
                var domain = authDetails.Identifier.Value;

                var recordName = $"_acme-challenge.{domain.TrimStart('*', '.')}";

                if (challengesByRecordName.TryGetValue(recordName, out var dnsChallengeList) == false)
                {
                    dnsChallengeList = [];
                    challengesByRecordName.Add(recordName, dnsChallengeList);
                }
                var challenge = await authorization.Dns();
                dnsChallengeList.Add(challenge);
            }

            dnsChallengeResult.ChallengesByRecord = challengesByRecordName;
            return dnsChallengeResult;
        }
    }
}
