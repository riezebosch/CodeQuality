using AgentStatusFunction.Data;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.VstsService;
using System.Linq;
using System.Threading.Tasks;
using SecurePipelineScan.VstsService.Requests;

namespace AgentStatusFunction.Activities
{
    public class AgentPoolCheckActivity
    {
        private readonly IVstsRestClient _rest;

        public AgentPoolCheckActivity(IVstsRestClient rest)
        {
            _rest = rest;
        }

        [FunctionName(nameof(AgentPoolCheckActivity))]
        public int Run([ActivityTrigger] AgentPoolInfo info, ILogger log) => 
            AllInactiveAgentsIdle(info, log);

        private int AllInactiveAgentsIdle(AgentPoolInfo info, ILogger log)
        {
            var agents = _rest.Get(DistributedTask.AgentPoolStatus(int.Parse(info.PoolId)));
            var active = agents
                .Where(a => !a.Enabled)
                .Count(a => a.AssignedRequest != null);

            log.LogInformation($"Azure DevOps reported {active} active agents.");
            return active;
        }
    }
}