using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using AgentClient;
using AgentStatusFunction.LogItems;
using AgentStatusFunction.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace AgentStatusFunction
{
    public class AgentPoolScanFunction
    {
        private readonly ILogAnalyticsClient _logAnalyticsClient;
        private readonly IRestClient _client;

        public AgentPoolScanFunction(ILogAnalyticsClient logAnalyticsClient,
            IRestClient client)
        {
            _logAnalyticsClient = logAnalyticsClient;
            _client = client;
        }

        [FunctionName(nameof(AgentPoolScanFunction))]
        public async System.Threading.Tasks.Task Run(
            [TimerTrigger("0 */5 * * * *", RunOnStartup = true)] TimerInfo timerInfo,
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

            var orgPools = _client.Get(new EnumerableRequest<AgentPoolInfo>());
            var poolsToObserve = orgPools.Where(x => observedPools.Any(p => p.PoolName == x.Name));
            var list = new List<LogAnalyticsAgentStatus>();

            foreach (var pool in poolsToObserve)
            {
                var agents = _client.Get(new EnumerableRequest<AgentStatus>(pool.Id)); 
                foreach (var agent in agents)
                {
                    var assignedTask = (agent.Status != "online") ? "Offline" : ((agent.AssignedRequest == null) ? "Idle" : agent.AssignedRequest.PlanType);
                    int statusCode;
                    switch (assignedTask)
                    {
                        case "Idle": statusCode = 1; break;
                        case "Build": statusCode = 2; break;
                        case "Release": statusCode = 3; break;
                        default: statusCode = 0; break;
                    }

                    list.Add(new LogAnalyticsAgentStatus
                    {
                        Name = agent.Name,
                        Id = agent.Id,
                        Enabled = agent.Enabled,
                        Status = agent.Status,
                        StatusCode = statusCode,
                        Version = agent.Version,
                        AssignedTask = assignedTask,
                        Pool = pool.Name,
                        Date = DateTime.UtcNow,
                    });
                }
            }

            log.LogInformation("Done retrieving poolstatus information. Send to log analytics");
            await _logAnalyticsClient.AddCustomLogJsonAsync("AgentStatus", list, "Date");
        }
    }
}