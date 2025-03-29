using Certes.Acme.Resource;
using CertOps.Model;
using CertOps.Services;
using Dignus.Collections;
using Dignus.Coroutine;
using Dignus.DependencyInjection.Attributes;
using Dignus.Log;
using Dignus.Utils.Extensions;
using System.Collections;

namespace CertOps.Handler
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Singleton)]
    class ScheduleHandler(Config config, AcmeService acmeService, AzureDnsService azureService)
    {
        public CoroutineHandler _coroutineHandler = new();

        public void Start(CancellationToken cancellationToken)
        {
            acmeService.InitAcmeContext();

            _coroutineHandler.Start(Start());
            while (cancellationToken.IsCancellationRequested == false)
            {
                Task.Delay(config.ScheduleDelayMilliseconds, cancellationToken).GetResult();
                _coroutineHandler.UpdateCoroutines((float)config.ScheduleDelayMilliseconds / 1000);
            }
        }
        private IEnumerator Start()
        {
            while (true)
            {
                HandleExpiringDomainsAsync().GetResult();

                yield return new DelayInMilliseconds(config.ScheduleDelayMilliseconds);
            }
        }
        private async Task HandleExpiringDomainsAsync()
        {
            try
            {
                var domainsToRenew = acmeService.GetExpiringDomains();

                if (domainsToRenew.Count == 0)
                {
                    return;
                }

                foreach (var item in domainsToRenew)
                {
                    LogHelper.Info($"expiring domain detected : {item}");
                }

                var challengeResult = await acmeService.GetDns01ChallengesAsync([.. domainsToRenew]);
                var dnsChallenges = challengeResult.ChallengesByRecord;

                foreach (var challengeEntry in dnsChallenges)
                {
                    var recordName = challengeEntry.Key;
                    var challengeList = challengeEntry.Value;

                    var txtValues = new ArrayQueue<string>();
                    foreach (var dnsChallenge in challengeList)
                    {
                        txtValues.Add(acmeService.GetDnsTxtValue(dnsChallenge));
                    }
                    await azureService.ClearAzureDnsTxtRecordAsync(recordName);

                    await azureService.AddAzureDnsTxtRecordsAsync(recordName, txtValues);
                    bool isValid = true;
                    for (int attempt = 0; attempt < 10; ++attempt)
                    {
                        isValid = true;

                        var statuses = new ArrayQueue<ChallengeStatus?>();

                        foreach (var dnsChallenge in challengeList)
                        {
                            var validateResponse = await dnsChallenge.Validate();

                            var resource = await dnsChallenge.Resource();

                            statuses.Add(resource.Status);
                        }

                        foreach (var status in statuses)
                        {
                            if (status.HasValue == false || status.Value == ChallengeStatus.Invalid)
                            {
                                isValid = false;
                                break;
                            }
                        }

                        if (isValid == true)
                        {
                            break;
                        }
                        await Task.Delay(1000);
                    }

                    if (isValid)
                    {
                        var order = challengeResult.Order;
                        var mainDomain = domainsToRenew.First();
                        await acmeService.IssueCertificateAsync(order, mainDomain);
                        LogHelper.Info($"certificate issued for: {mainDomain}");
                    }
                    else
                    {
                        LogHelper.Error($"challenge failed for record: {recordName}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Fatal(ex);
            }
        }
    }
}
