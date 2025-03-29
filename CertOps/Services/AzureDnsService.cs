using Azure;
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
            var relativeRecordName = GetRelativeRecordName(recordName);
            var recordSets = _dnsZone.GetDnsTxtRecords();

            DnsTxtRecordData dnsTxtRecordData = new DnsTxtRecordData
            {
                TtlInSeconds = 60
            };

            try
            {
                var existingRecord = await recordSets.GetAsync(relativeRecordName);
                await existingRecord.Value.DeleteAsync(Azure.WaitUntil.Completed);
            }
            catch (RequestFailedException requestFailedException)
            {
                if (requestFailedException.Status != 404)
                {
                    throw;
                }
            }
            return true;
        }

        private string GetRelativeRecordName(string fullRecordName)
        {
            if (fullRecordName.EndsWith(_dnsZone.Id.Name, StringComparison.OrdinalIgnoreCase))
            {
                var relative = fullRecordName[..^_dnsZone.Id.Name.Length];
                return relative.TrimEnd('.');
            }

            return fullRecordName;
        }
        public async Task<bool> AddAzureDnsTxtRecordsAsync(string recordName, ArrayQueue<string> recordValues)
        {
            var relativeRecordName = GetRelativeRecordName(recordName);
            var recordSets = _dnsZone.GetDnsTxtRecords();
            DnsTxtRecordData dnsTxtRecordData = new DnsTxtRecordData
            {
                TtlInSeconds = 60
            };

            try
            {
                var existingRecord = await recordSets.GetAsync(relativeRecordName);
                dnsTxtRecordData = existingRecord.Value.Data;
            }
            catch (RequestFailedException requestFailedException)
            {
                if (requestFailedException.Status != 404)
                {
                    throw;
                }
            }

            var dnsTxtRecords = dnsTxtRecordData.DnsTxtRecords;
            var existingValues = dnsTxtRecords.SelectMany(r => r.Values).ToHashSet();

            foreach (var record in recordValues)
            {
                if (existingValues.Contains(record) == false)
                {
                    var item = new DnsTxtRecordInfo();
                    item.Values.Add(record);
                    dnsTxtRecordData.DnsTxtRecords.Add(item);
                }
            }

            try
            {
                await recordSets.CreateOrUpdateAsync(Azure.WaitUntil.Completed, relativeRecordName, dnsTxtRecordData);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
                return false;
            }
            return true;
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
