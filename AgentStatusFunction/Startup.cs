using System.Net.Http;
using AgentClient;
using AgentStatusFunction.Helpers;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Unmockable;

[assembly: WebJobsStartup(typeof(AgentStatusFunction.Startup))]

namespace AgentStatusFunction
{
    public class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder) => RegisterServices(builder.Services);

        private static void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton<ILogAnalyticsClient>(new DummyLogAnalyticsClient());
            services.AddSingleton<IRestClient>(new RestClient());
            services.AddSingleton(new HttpClient());
            services.AddSingleton(new AzureServiceTokenProvider().Wrap());
            services.AddSingleton<IAgentPoolToVmScaleSetMapper, AgentPoolToVmScaleSetMapper>();
        }
    }
}
