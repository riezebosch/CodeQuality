using System;
using System.Collections.Generic;
using System.Linq;
using AgentClient;
using AgentStatusFunction.Helpers;
using AgentStatusFunction.LogItems;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace AgentStatusFunction
{
    public class AgentPoolScanFunction
    {
        private readonly ILogAnalyticsClient _logAnalyticsClient;
        private readonly IRestClient _client;
        private readonly IAgentPoolToVmScaleSetMapper _mapper;

        public AgentPoolScanFunction(ILogAnalyticsClient logAnalyticsClient,
            IRestClient client,
            IAgentPoolToVmScaleSetMapper mapper)
        {
            _logAnalyticsClient = logAnalyticsClient;
            _client = client;
            _mapper = mapper;
        }

        [FunctionName(nameof(AgentPoolScanFunction))]
        public async System.Threading.Tasks.Task Run(
            [TimerTrigger("0 */5 * * * *", RunOnStartup = true)] TimerInfo timerInfo,
            ILogger log)
        {
            if (log == null) throw new ArgumentNullException(nameof(log));

            var pools = _client
                .Get(new EnumerableRequest<AgentPoolInfo>())
                .Where(x => _mapper.IsWellKnown(x.Name));
            var list = new List<LogAnalyticsAgentStatus>();

            foreach (var pool in pools)
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