using Microsoft.Extensions.Primitives;

namespace AgentStatusFunction.Data
{
    public class AgentPoolInfo
    {
        public string PlanUrl { get; set; }
        public string ProjectId { get; set; }
        public string HubName { get; set; }
        public string PlanId { get; set; }
        public string JobId { get; set; }
        public string PoolId { get; set; }
        public string TimelineId { get; set; }
        public string TaskInstanceId { get; set; }
        public string TaskInstanceName { get; set; }
        public StringValues AuthToken { get; set; }
    }
}