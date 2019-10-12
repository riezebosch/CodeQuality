using System;
using System.Threading.Tasks;
using AgentClient;

namespace AgentStatusFunction
{
    internal class DummyLogAnalyticsClient : ILogAnalyticsClient
    {
        public Task AddCustomLogJsonAsync(string log, object input, string timeField) => throw new NotImplementedException();
    }
}