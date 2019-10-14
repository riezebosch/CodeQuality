using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using AgentClient;
using AgentStatusFunction.Model;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Unmockable;

namespace AgentStatusFunction
{
    public class ReimageAgentsFunction
    {
        private readonly IRestClient _client;
        private readonly HttpClient _http;
        private readonly IUnmockable<AzureServiceTokenProvider> _tokenProvider;

        public ReimageAgentsFunction(IRestClient client,
            HttpClient http,
            IUnmockable<AzureServiceTokenProvider> tokenProvider)
        {
            _client = client;
            _http = http;
            _tokenProvider = tokenProvider;
        }

        [FunctionName(nameof(ReimageAgentsFunction))]
        public async System.Threading.Tasks.Task Run(
            [TimerTrigger("0 30 * * * *", RunOnStartup = true)] TimerInfo timerInfo,
            ILogger log)
        {
            if (log == null) throw new ArgumentNullException(nameof(log));

            log.LogInformation("Time trigger function to check Azure DevOps agent status");
            var observedPools = new[]
            {
                new AgentPoolInformation {PoolName = "Some-Build-Azure-Linux", ResourceGroupPrefix = "rg-m01-prd-linuxagents-0"},
                new AgentPoolInformation {PoolName = "Some-Build-Azure-Linux-Canary", ResourceGroupPrefix = "rg-m01-prd-linuxcanary-0"},
                new AgentPoolInformation {PoolName = "Some-Build-Azure-Linux-Fallback", ResourceGroupPrefix = "rg-m01-prd-linuxfallback-0"},
                new AgentPoolInformation {PoolName = "Some-Build-Azure-Linux-Preview", ResourceGroupPrefix = "rg-m01-prd-linuxpreview-0"},
                new AgentPoolInformation {PoolName = "Some-Build-Azure-Windows", ResourceGroupPrefix = "rg-m01-prd-winagents-0"},
                new AgentPoolInformation {PoolName = "Some-Build-Azure-Windows-Canary", ResourceGroupPrefix = "rg-m01-prd-wincanary-0"},
                new AgentPoolInformation {PoolName = "Some-Build-Azure-Windows-Canary-2", ResourceGroupPrefix = "rg-m01-prd-wincanary-0"},
                new AgentPoolInformation {PoolName = "Some-Build-Azure-Windows-Fallback", ResourceGroupPrefix = "rg-m01-prd-winfallback-0"},
                new AgentPoolInformation {PoolName = "Some-Build-Azure-Windows-Preview", ResourceGroupPrefix = "rg-m01-prd-winpreview-0"},
            };

            var pools = _client
                .Get(new EnumerableRequest<AgentPoolInfo>())
                .Where(x => observedPools.Any(p => p.PoolName == x.Name));
            
            foreach (var pool in pools)
            {
                var agents = _client.Get(new EnumerableRequest<AgentStatus>(pool.Id));
                foreach (var agent in agents)
                {
                    if (agent.Status != "online")
                    {
                        var agentInfo = GetAgentInfoFromName(agent, pool, observedPools);
                        await ReImageAgent(log, agentInfo, _http, _tokenProvider);
                    }
                }
            }
        }

        public static async System.Threading.Tasks.Task ReImageAgent(ILogger log, AgentInformation agentInfo, HttpClient client, IUnmockable<AzureServiceTokenProvider> tokenProvider)
        {
            var accessToken = await tokenProvider.Execute(x => x.GetAccessTokenAsync("https://management.azure.com/", null)).ConfigureAwait(false);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var agentStatusJson = await client.GetStringAsync($"https://management.azure.com/subscriptions/f13f81f8-7578-4ca8-83f3-0a845fad3cb5/resourceGroups/{agentInfo.ResourceGroup}/providers/Microsoft.Compute/virtualMachineScaleSets/agents/virtualmachines/{agentInfo.InstanceId}/instanceView?api-version=2018-06-01");
            dynamic status = JObject.Parse(agentStatusJson);

            if (status.statuses[0].code == "ProvisioningState/updating")
            {
                log.LogInformation($"Agent already being re-imaged: {agentInfo.ResourceGroup} - {agentInfo.InstanceId}");
                return;
            }

            log.LogInformation($"Re-image agent: {agentInfo.ResourceGroup} - {agentInfo.InstanceId}");
            var result = await client.PostAsync($"https://management.azure.com/subscriptions/f13f81f8-7578-4ca8-83f3-0a845fad3cb5/resourceGroups/{agentInfo.ResourceGroup}/providers/Microsoft.Compute/virtualMachineScaleSets/agents/virtualmachines/{agentInfo.InstanceId}/reimage?api-version=2018-06-01", new StringContent(""));
            if (!result.IsSuccessStatusCode)
            {
                throw new Exception($"Error Re-imaging agent: {agentInfo.ResourceGroup} - {agentInfo.InstanceId}");
            }
        }

        public static AgentInformation GetAgentInfoFromName(AgentStatus agent, AgentPoolInfo pool, IEnumerable<AgentPoolInformation> observedPools)
        {
            //re-image agent
            var rgPrefix = observedPools.FirstOrDefault(op => op.PoolName == pool.Name);
            var spit = agent.Name.Split('-');

            if (rgPrefix == null || spit.Length != 8)
            {
                throw new Exception($"Agent with illegal name detected. cannot re-image: {agent.Name}");
            }

            return new AgentInformation($"{rgPrefix.ResourceGroupPrefix}{spit[3]}", int.Parse(spit[6]));
        }
    }
}