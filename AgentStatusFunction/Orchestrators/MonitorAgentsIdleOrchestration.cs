using AgentStatusFunction.Activities;
using AgentStatusFunction.Data;
using Microsoft.Azure.WebJobs;
using System.Threading;
using System.Threading.Tasks;

namespace AgentStatusFunction.Orchestrators
{
    public class MonitorAgentsIdleOrchestration
    {
        [FunctionName(nameof(MonitorAgentsIdleOrchestration))]
        public async Task Run([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var info = context.GetInput<AgentPoolInfo>();
            var expiryTime = context.CurrentUtcDateTime.AddHours(1);

            try
            {
                await WaitForAgentsIdleAsync(context, info, expiryTime);
            }
            catch
            {
                await context.CallActivityAsync(nameof(SendFailedMessageActivity), info);
                throw;
            }
        }

        private static async Task WaitForAgentsIdleAsync(DurableOrchestrationContextBase context, AgentPoolInfo info,
            System.DateTime expiryTime)
        {
            await context.CallActivityAsync(nameof(SendStartMessageActivity), info);

            while (context.CurrentUtcDateTime < expiryTime)
            {
                var agentsWorking = await context.CallActivityAsync<int>(nameof(AgentPoolCheckActivity), info);
                await context.CallActivityAsync(nameof(SendLogMessageActivity),
                    new LogMessage(info, $"{agentsWorking} agents still working."));

                if (agentsWorking == 0)
                {
                    await context.CallActivityAsync(nameof(SendTaskCompletedMessageActivity), info);
                    return;
                }

                // Orchestration sleeps until this time.
                var nextCheck = context.CurrentUtcDateTime.AddSeconds(60);
                await context.CreateTimer(nextCheck, CancellationToken.None);
            }

            await context.CallActivityAsync(nameof(SendFailedMessageActivity), info);
        }
    }
}