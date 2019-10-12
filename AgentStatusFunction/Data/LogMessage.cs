using System;

namespace AgentStatusFunction.Data
{
    public class LogMessage
    {
        public AgentPoolInfo AgentPoolInfo { get; }
        public string Message { get; }

        public LogMessage(AgentPoolInfo agentPoolInfo, string message)
        {
            AgentPoolInfo = agentPoolInfo ?? throw new ArgumentNullException(nameof(agentPoolInfo));
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }
    }
}