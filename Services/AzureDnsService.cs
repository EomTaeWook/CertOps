using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Dns;
using Azure.ResourceManager.Dns.Models;
using CertOps.Model;
using Dignus.Collections;
using Dignus.DependencyInjection.Attributes;
using Dignus.Log;

namespace CertOps.Services
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Singleton)]
    class AzureDnsService
    {
        private readonly AzureConfig _azureConfig;
        private readonly ArmClient _armClient;

        private DnsZoneResource _dnsZone;
        public AzureDnsService(AzureConfig config)
        {
            _azureConfig = config;

            var credential = new ClientSecretCredential(config.TenantId, config.ClientId, config.ClientSecret);
            _armClient = new ArmClient(credential);

            var dnsZoneId = DnsZoneResource.CreateResourceIdentifier(
                _azureConfig.AzureSubscriptionId,
                _azureConfig.AzureResourceGroup,
                _azureConfig.AzureDnsZone);

            _dnsZone = _armClient.GetDnsZoneResource(dnsZoneId);
        }

        public async Task<ArrayQueue<string>> GetAzureDnsTxtRecordAsync(string recordName)
        {

            var recordValues = new ArrayQueue<string>();
            var recordSetResponse = await _dnsZone.GetDnsTxtRecordAsync(recordName);
            if (recordSetResponse.GetRawResponse().Status == 404)
            {
                return recordValues;
            }
            var dnsTxtRecordData = recordSetResponse.Value.Data;
            foreach (var recordInfo in dnsTxtRecordData.DnsTxtRecords)
            {
                foreach (var value in recordInfo.Values)
                {
                    recordValues.Add(value);
                }
            }

            return recordValues;
        }

        public async Task<bool> ClearAzureDnsTxtRecordAsync(string recordName)
        {
            var recordSetResponse = await _dnsZone.GetDnsTxtRecordAsync(recordName);
            if (recordSetResponse.GetRawResponse().Status == 404)
            {
                return true;
            }
            var dnsTxtRecordData = recordSetResponse.Value.Data;
            foreach (var recordInfo in dnsTxtRecordData.DnsTxtRecords)
            {
                foreach (var value in recordInfo.Values)
                {
                    LogHelper.Info($"delete record value : {value}");
                }
            }
            var deleteResponse = await recordSetResponse.Value.DeleteAsync(Azure.WaitUntil.Completed);

            return deleteResponse.GetRawResponse().IsError == false;

        }
        public async Task<bool> RemoveAzureDnsTxtRecordAsync(string recordName, string recordValue)
        {
            var recordSetResponse = await _dnsZone.GetDnsTxtRecordAsync(recordName);

            if(recordSetResponse.GetRawResponse().Status  == 404)
            {
                return true;
            }

            if(recordSetResponse.Value.HasData == false)
            {
                return true;
            }

            var originalRecords = recordSetResponse.Value.Data.DnsTxtRecords;

            var updatedRecords = new List<DnsTxtRecordInfo>();

            foreach (var recordInfo in originalRecords)
            {
                var filteredValues = recordInfo.Values.Where(v => v != recordValue).ToList();

                if(filteredValues.Count > 0)
                {
                    var newRecordInfo = new DnsTxtRecordInfo();
                    foreach(var item in filteredValues)
                    {
                        newRecordInfo.Values.Add(item);
                    }
                    updatedRecords.Add(newRecordInfo);
                }
            }

            if (updatedRecords.Count == originalRecords.Count)
            {
                return true;
            }

            var updateDnsTxtRecordData = new DnsTxtRecordData();
            foreach(var item in updatedRecords)
            {
                updateDnsTxtRecordData.DnsTxtRecords.Add(item);
            }

            var updateResponse = await recordSetResponse.Value.UpdateAsync(updateDnsTxtRecordData);

            return updateResponse.GetRawResponse().IsError == false;
        }
        public async Task<bool> AddAzureDnsTxtRecordAsync(string recordName, string recordValue)
        {
            var recordSetResponse = await _dnsZone.GetDnsTxtRecordAsync(recordName);

            DnsTxtRecordResource dnsTxtRecordResource = recordSetResponse.Value;

            var recordInfo = new DnsTxtRecordInfo();
            recordInfo.Values.Add(recordValue);

            DnsTxtRecordData dnsTxtRecordData = null;
            if (dnsTxtRecordResource != null)
            {
                dnsTxtRecordData = dnsTxtRecordResource.Data;
            }
            else
            {
                dnsTxtRecordData = new DnsTxtRecordData()
                {
                    TtlInSeconds = 60,
                };
            }
            dnsTxtRecordData.DnsTxtRecords.Add(recordInfo);

            var updateResponse = await recordSetResponse.Value.UpdateAsync(dnsTxtRecordData);

            return updateResponse.GetRawResponse().IsError == false;
        }
    }
}
