using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SecurePipelineScan.VstsService;
using System;
using System.Net.Http;
using LogAnalytics.Client;
using Microsoft.Azure.Services.AppAuthentication;
using Unmockable;

[assembly: WebJobsStartup(typeof(AgentStatusFunction.Startup))]

namespace AgentStatusFunction
{
    public class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            RegisterServices(builder.Services);
        }

        private static void RegisterServices(IServiceCollection services)
        {
            var tenantId = GetEnvironmentVariable("tenantId");
            var clientId = GetEnvironmentVariable("clientId");
            var clientSecret = GetEnvironmentVariable("clientSecret");

            var workspaceId = GetEnvironmentVariable("logAnalyticsWorkspaceId");
            var key = GetEnvironmentVariable("logAnalyticsKey");
            services.AddSingleton<ILogAnalyticsClient>(new LogAnalyticsClient(workspaceId, key,
                new AzureTokenProvider(tenantId, clientId, clientSecret)));
            
            services.AddSingleton<IVstsRestClient>(new VstsRestClient(GetEnvironmentVariable("organization"), GetEnvironmentVariable("vstsPat")));

            services.AddSingleton(new HttpClient());
            services.AddSingleton(new AzureServiceTokenProvider().Wrap());
        }
        
        private static string GetEnvironmentVariable(string variableName)
        {
            return Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.Process)
                   ?? throw new ArgumentNullException(variableName,
                       $"Please provide a valid value for environment variable '{variableName}'");
        }
    }
}
