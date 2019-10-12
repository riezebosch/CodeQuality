using AgentStatusFunction.Data;
using AgentStatusFunction.Orchestrators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AgentStatusFunction.Starter
{
    public class MonitorAgentsIdleStarter
    {
        [FunctionName(nameof(MonitorAgentsIdleStarter))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "PrepareAgentsForDeploymentFunction")]
            HttpRequest req,
            [OrchestrationClient] DurableOrchestrationClientBase orchestrationClient,
            ILogger log)
        {
            // Sample from: https://github.com/Microsoft/azure-pipelines-extensions/blob/master/ServerTaskHelper/HttpRequestSampleWithoutHandler/MyTaskController.cs
            var pool = new AgentPoolInfo
            {
                ProjectId = req.Headers["ProjectId"],
                PlanId = req.Headers["PlanId"],
                JobId = req.Headers["JobId"],
                TimelineId = req.Headers["TimelineId"],
                TaskInstanceId = req.Headers["TaskInstanceId"],
                HubName = req.Headers["HubName"],
                TaskInstanceName = req.Headers["TaskInstanceName"],
                PlanUrl = req.Headers["PlanUrl"],
                AuthToken = req.Headers["AuthToken"],
                PoolId = req.Headers["PoolId"]
            };

            await orchestrationClient.StartNewAsync(nameof(MonitorAgentsIdleOrchestration), pool);
            return new AcceptedResult();
        }
    }
}