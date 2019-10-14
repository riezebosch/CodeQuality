using System.Collections.Generic;
using AgentClient;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AzureFunctions.TestHelpers;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Xunit;

namespace AgentStatusFunction.IntegrationTests
{
    public static class StarterTests
    {
        [Fact]
        public static async void IntegrationTest()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization{ ConfigureMembers = true });
            var rest = fixture.Create<IRestClient>();
            var analytics = fixture.Create<ILogAnalyticsClient>();
            
            
            using (var host = new HostBuilder()
                .ConfigureWebJobs(builder => builder
                    .AddTimers() // enable time-triggered functions
                    .AddDurableTaskInTestHub() // enable durable functions (aka orchestrations)
                    .AddAzureStorageCoreServices() // enable storage for state of orchestrations
                    .UseWebJobsStartup<Startup>() // register default services
                    .ConfigureServices(services => services // replace some services with mocks
                        .AddSingleton(rest)
                        .AddSingleton(analytics)))
                .Build())
            {
                await host.StartAsync();
                var jobs = host.Services.GetService<IJobHost>();
                
                // Act
                await jobs.CallAsync(nameof(AgentPoolScanFunction), new Dictionary<string, object>
                {
                    ["timerInfo"] = fixture.Create<TimerInfo>() // dummy data for starter function
                });

                // Assert
                rest.ReceivedCalls();
                analytics.ReceivedCalls();
            }
        }    
    }
}
