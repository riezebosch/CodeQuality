using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AgentStatusFunction.Model;
using AutoFixture;
using FluentAssertions;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;
using Unmockable;
using Xunit;

namespace AgentStatusFunction.Tests
{
    public class ReImageAgentsFunctionTests
    {
        [Fact]
        public async Task AgentStillReImagingShouldNotReImageAgain()
        {
            //Arrange
            var http = new MockHttpMessageHandler();

            var status = http
                .When(HttpMethod.Get, "https://management.azure.com/subscriptions/*/resourceGroups/*/providers/Microsoft.Compute/virtualMachineScaleSets/*/virtualmachines/*/instanceView?api-version=2018-06-01")
                .Respond("application/json", "{ \"placementGroupId\": \"f79e82f0-3480-4eb3-a893-5cf9bd74daad\", \"platformUpdateDomain\": 0, \"platformFaultDomain\": 0, \"computerName\": \"agents2q3000000\", \"osName\": \"ubuntu\", \"osVersion\": \"18.04\", \"vmAgent\": { \"vmAgentVersion\": \"2.2.36\", \"statuses\": [ { \"code\": \"ProvisioningState/succeeded\", \"level\": \"Info\", \"displayStatus\": \"Ready\", \"message\": \"Guest Agent is running\", \"time\": \"2019-02-22T08:17:12+00:00\" } ], \"extensionHandlers\": [] }, \"disks\": [ { \"name\": \"agents_agents_0_OsDisk_1_d0e2afd2252041e98796d5ccdcf329d0\", \"statuses\": [ { \"code\": \"ProvisioningState/succeeded\", \"level\": \"Info\", \"displayStatus\": \"Provisioning succeeded\", \"time\": \"2019-02-22T08:16:59.1325957+00:00\" } ] }, { \"name\": \"agents_agents_0_OsDisk_1_3009fa8e43e144029be77cd72065f6df\", \"statuses\": [ { \"code\": \"ProvisioningState/deleting\", \"level\": \"Info\", \"displayStatus\": \"Deleting\" } ] } ], \"statuses\": [ { \"code\": \"ProvisioningState/updating\", \"level\": \"Info\", \"displayStatus\": \"Updating\" }, { \"code\": \"PowerState/running\", \"level\": \"Info\", \"displayStatus\": \"VM running\" } ] }");

            var reImage = http
                .When(HttpMethod.Post, "https://management.azure.com/subscriptions/*/resourceGroups/*/providers/Microsoft.Compute/virtualMachineScaleSets/*/virtualmachines/*/reimage?api-version=2018-06-01")
                .Respond(HttpStatusCode.OK);

            var fixture = new Fixture();
            var agentInfo = fixture.Create<VirtualMachineInformation>();

            var tokenProvider = new Intercept<AzureServiceTokenProvider>();
            tokenProvider
                .Setup(x => x.GetAccessTokenAsync(Arg.Ignore<string>(), null))
                .Returns(string.Empty);

            //Act
            await ReImageAgentsFunction.ReImageAgent(agentInfo, tokenProvider, http.ToHttpClient(), new Mock<ILogger>().Object);

            //Assert
            http.GetMatchCount(status).Should().Be(1);
            http.GetMatchCount(reImage).Should().Be(0);
        }

        [Fact]
        public async Task AgentOfflineShouldCheckStatusAndReImage()
        {

            //Arrange
            var log = new Mock<ILogger>();

            var http = new MockHttpMessageHandler();
            var status = http
                .When(HttpMethod.Get, "https://management.azure.com/subscriptions/*/resourceGroups/*/providers/Microsoft.Compute/virtualMachineScaleSets/*/virtualmachines/*/instanceView?api-version=2018-06-01")
                .Respond("application/json", "{ \"placementGroupId\": \"f79e82f0-3480-4eb3-a893-5cf9bd74daad\", \"platformUpdateDomain\": 0, \"platformFaultDomain\": 0, \"computerName\": \"agents2q3000000\", \"osName\": \"ubuntu\", \"osVersion\": \"18.04\", \"vmAgent\": { \"vmAgentVersion\": \"2.2.36\", \"statuses\": [ { \"code\": \"ProvisioningState/succeeded\", \"level\": \"Info\", \"displayStatus\": \"Ready\", \"message\": \"Guest Agent is running\", \"time\": \"2019-02-22T08:15:48+00:00\" } ], \"extensionHandlers\": [] }, \"disks\": [ { \"name\": \"agents_agents_0_OsDisk_1_3009fa8e43e144029be77cd72065f6df\", \"statuses\": [ { \"code\": \"ProvisioningState/succeeded\", \"level\": \"Info\", \"displayStatus\": \"Provisioning succeeded\", \"time\": \"2019-02-06T11:45:35.5975265+00:00\" } ] } ], \"statuses\": [ { \"code\": \"ProvisioningState/succeeded\", \"level\": \"Info\", \"displayStatus\": \"Provisioning succeeded\", \"time\": \"2019-02-06T11:46:58.0511995+00:00\" }, { \"code\": \"PowerState/running\", \"level\": \"Info\", \"displayStatus\": \"VM running\" } ] } ");

            var reImage = http
                .When(HttpMethod.Post, "https://management.azure.com/subscriptions/*/resourceGroups/*/providers/Microsoft.Compute/virtualMachineScaleSets/*/virtualmachines/*/reimage?api-version=2018-06-01")
                .Respond(HttpStatusCode.OK);

            var fixture = new Fixture();
            var agentInfo = fixture.Create<VirtualMachineInformation>();

            var tokenProvider = new Intercept<AzureServiceTokenProvider>();
            tokenProvider
                .Setup(x => x.GetAccessTokenAsync(Arg.Ignore<string>(), null))
                .Returns(string.Empty);

            //Act
            await ReImageAgentsFunction.ReImageAgent(agentInfo, tokenProvider, http.ToHttpClient(), log.Object);

            //Assert
            http.GetMatchCount(status).Should().Be(1);
            http.GetMatchCount(reImage).Should().Be(1);
        }
    }
}