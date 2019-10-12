using AgentStatusFunction.Data;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.VstsService;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgentStatusFunction.Activities
{
    public class SendTaskCompletedMessageActivity
    {
        private readonly IVstsRestClient _rest;

        public SendTaskCompletedMessageActivity(IVstsRestClient rest)
        {
            _rest = rest;
        }

        [FunctionName(nameof(SendTaskCompletedMessageActivity))]
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
                Name = "TaskCompleted",
                TaskId = info.TaskInstanceId,
                JobId = info.JobId,
                Result = "succeeded"
            };

            logger.LogInformation(
                $"Sending TaskCompleted(succeeded) message to {uri} for task id {body.TaskId} and job id {body.JobId}");

            return _rest.PostAsync(new AzureDevOpsCallbackRequest<MessageBody>(info.PlanUrl, uri, parameters), body);
        }
    }
}