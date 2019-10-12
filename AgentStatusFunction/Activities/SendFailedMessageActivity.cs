using AgentStatusFunction.Data;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.VstsService;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgentStatusFunction.Activities
{
    public class SendFailedMessageActivity
    {
        private readonly IVstsRestClient _rest;

        public SendFailedMessageActivity(IVstsRestClient rest)
        {
            _rest = rest;
        }

        [FunctionName(nameof(SendFailedMessageActivity))]
        public Task Run([ActivityTrigger] AgentPoolInfo info, ILogger logger)
        {
            var uri =
                $"{info.ProjectId}/_apis/distributedtask/hubs/{info.HubName}/plans/{info.PlanId}/events";
            var parameters = new Dictionary<string, object>
            {
                ["api-version"] = "2.0-preview.1"
            };

            var body = new MessageBody
            {
                Result = "failed",
                Name = "TaskCompleted",
                TaskId = info.TaskInstanceId,
                JobId = info.JobId
            };

            logger.LogInformation(
                $"Sending TaskCompleted(failed) message to {uri} for task id {body.TaskId} and job id {body.JobId}");

            return _rest.PostAsync(new AzureDevOpsCallbackRequest<MessageBody>(info.PlanUrl, uri, parameters), body);
        }
    }
}