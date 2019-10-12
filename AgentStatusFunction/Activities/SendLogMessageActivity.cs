using AgentStatusFunction.Data;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.VstsService;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgentStatusFunction.Activities
{
    public class SendLogMessageActivity
    {
        private readonly IVstsRestClient _client;

        public SendLogMessageActivity(IVstsRestClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        [FunctionName(nameof(SendLogMessageActivity))]
        public Task Run([ActivityTrigger] LogMessage logMessage, ILogger logger)
        {
            var info = logMessage.AgentPoolInfo;
            var uri = $@"{info.ProjectId}/_apis/distributedtask/hubs/{info.HubName}/plans/{info.PlanId}/timelines/{
                    info.TimelineId
                }/records/{info.JobId}/feed";
            var parameters = new Dictionary<string, object>
            {
                ["api-version"] = "4.1"
            };
            var body = new MultipleWithCount<string>
            {
                Value = new[] {logMessage.Message}
            };

            logger.LogInformation($"Logging '{logMessage.Message}' to {uri}");

            return _client.PostAsync(
                new AzureDevOpsCallbackRequest<MultipleWithCount<string>>(info.PlanUrl, uri, parameters),
                body);
        }
    }
}