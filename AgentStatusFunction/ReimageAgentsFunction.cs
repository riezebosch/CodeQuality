using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AgentClient;
using AgentStatusFunction.Helpers;
using AgentStatusFunction.Model;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Unmockable;

namespace AgentStatusFunction
{
    public class ReImageAgentsFunction
    {
        private readonly IRestClient _client;
        private readonly HttpClient _http;
        private readonly IUnmockable<AzureServiceTokenProvider> _tokenProvider;
        private readonly IAgentPoolToVmScaleSetMapper _mapper;

        public ReImageAgentsFunction(IRestClient client,
            IAgentPoolToVmScaleSetMapper mapper,
            HttpClient http,
            IUnmockable<AzureServiceTokenProvider> tokenProvider)
        {
            _client = client;
            _http = http;
            _tokenProvider = tokenProvider;
            _mapper = mapper;
        }

        [FunctionName(nameof(ReImageAgentsFunction))]
        public async Task Run(
            [TimerTrigger("0 30 * * * *", RunOnStartup = true)] TimerInfo timerInfo,
            ILogger log)
        {
            if (log == null) throw new ArgumentNullException(nameof(log));

            var pools = _client
                .Get(new EnumerableRequest<AgentPoolInfo>())
                .Where(x => _mapper.IsWellKnown(x.Name));
            
            foreach (var pool in pools)
            {
                var agents = _client.Get(new EnumerableRequest<AgentStatus>(pool.Id));
                foreach (var agent in agents.Where(x => x.Status != "online"))
                {
                    var agentInfo = _mapper.ParseVirtualMachineInformation(pool.Name, agent.Name);
                    await ReImageAgent(agentInfo, _tokenProvider, _http, log);
                }
            }
        }

        public static async Task ReImageAgent(VirtualMachineInformation info,
            IUnmockable<AzureServiceTokenProvider> tokenProvider, HttpClient client, ILogger log)
        {
            await PrepareAuthorization(client, tokenProvider);
            if (await IsReImaging(client, info))
            {
                log.LogInformation($"Agent already being re-imaged: {info.ResourceGroup} - {info.InstanceId}");
                return;
            }

            log.LogInformation($"Re-image agent: {info.ResourceGroup} - {info.InstanceId}");
            var result = await client.PostAsync($"https://management.azure.com/subscriptions/{info.Subscription}/resourceGroups/{info.ResourceGroup}/providers/Microsoft.Compute/virtualMachineScaleSets/{info.ScaleSet}/virtualmachines/{info.InstanceId}/reimage?api-version=2018-06-01", new StringContent(""));
            
            if (!result.IsSuccessStatusCode)
            {
                throw new Exception($"Error re-imaging agent: {info.ResourceGroup} - {info.InstanceId}");
            }
        }

        private static async Task PrepareAuthorization(HttpClient client,
            IUnmockable<AzureServiceTokenProvider> tokenProvider)
        {
            var token = await tokenProvider.Execute(x => x.GetAccessTokenAsync("https://management.azure.com/", null)).ConfigureAwait(false);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        private static async Task<dynamic> IsReImaging(HttpClient client, VirtualMachineInformation info)
        {
            var response = await client.GetStringAsync($"https://management.azure.com/subscriptions/{info.Subscription}/resourceGroups/{info.ResourceGroup}/providers/Microsoft.Compute/virtualMachineScaleSets/{info.ScaleSet}/virtualmachines/{info.InstanceId}/instanceView?api-version=2018-06-01");
            var status = (dynamic) JObject.Parse(response);
            
            return status.statuses[0].code == "ProvisioningState/updating";
        }
    }
}