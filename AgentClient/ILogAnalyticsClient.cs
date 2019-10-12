using System.Threading.Tasks;

namespace AgentClient
{
    public interface ILogAnalyticsClient
    {
        Task AddCustomLogJsonAsync(string log, object input, string timeField);
    }
}