using CertOps.Model;
using CertOps.Services;
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
        public CoroutineHandler _coroutineHandler;


        public void Start(CancellationToken cancellationToken)
        {
            _coroutineHandler.Start(Start());
            while (cancellationToken.IsCancellationRequested == false)
            {
                var startDateTime = DateTime.Now.Ticks;
                Task.Delay(33, cancellationToken).GetResult();
                var elapsedTime = TimeSpan.FromTicks(DateTime.Now.Ticks - startDateTime);
                _coroutineHandler.UpdateCoroutines((float)elapsedTime.TotalMilliseconds);
            }
        }
        private IEnumerator Start()
        {
            while(true)
            {
                


                yield return new DelayInMilliseconds(config.ScheduleDelayMilliseconds);
            }
        }
        private async Task HandleExpiringDomainsAsync()
        {
            try
            {
                var expiringDomains = await acmeService.GetExpiringDomainsAsync();

                if (expiringDomains.Count == 0)
                {
                    return;
                }

                foreach (var item in expiringDomains)
                {
                    LogHelper.Info($"expiring domain detected : {item}");
                }

                var tt = await acmeService.GetDns01ChallengesAsync([.. expiringDomains]);

                foreach(var item in tt)
                {
                    await azureService.AddAzureDnsTxtRecordAsync("_acme-challenge", item.Value);
                }
                



            }
            catch(Exception ex)
            {
                LogHelper.Fatal(ex);
            }
           
        }
    }
}
