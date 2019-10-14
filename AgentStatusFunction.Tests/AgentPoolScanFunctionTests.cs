﻿using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AgentClient;
using AgentStatusFunction.Model;
using AutoFixture;
using FluentAssertions;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;
using Unmockable;
using Xunit;

namespace AgentStatusFunction.Tests
{
    public class AgentPoolScanFunctionTests
    {

        [Fact]
        public async Task AgentPoolScanTest()
        {
            // Arrange
            var fixture = new Fixture();
            IEnumerable<AgentPoolInfo> pools = new[] {
            new AgentPoolInfo {Name = "Some-Build-Azure-Linux", Id = 1},
            new AgentPoolInfo {Name = "Some-Build-Azure-Linux-Canary", Id = 2},
            new AgentPoolInfo {Name = "Some-Build-Azure-Linux-Fallback", Id = 3},
            new AgentPoolInfo {Name = "Some-Build-Azure-Linux-Preview", Id = 4},
            new AgentPoolInfo {Name = "Some-Build-Azure-Windows", Id = 5},
            new AgentPoolInfo {Name = "Some-Build-Azure-Windows-Canary", Id = 6},
            new AgentPoolInfo {Name = "Some-Build-Azure-Windows-Fallback", Id = 7},
            new AgentPoolInfo {Name = "Some-Build-Azure-Windows-Preview", Id = 8},
            new AgentPoolInfo {Name = "Some-Build-Azure-Windows-NOT-OBSERVED", Id = 9}
        };

            fixture.Customize<AgentStatus>(a => a.With(agent => agent.Status, "online"));

            var mockHttp = new MockHttpMessageHandler();

            mockHttp.When(HttpMethod.Get, "https://management.azure.com/subscriptions/f13f81f8-7578-4ca8-83f3-0a845fad3cb5/resourceGroups/*/providers/Microsoft.Compute/virtualMachineScaleSets/agents/virtualmachines/*/instanceView?api-version=2018-06-01")
                .Respond("application/json", "{ \"placementGroupId\": \"f79e82f0-3480-4eb3-a893-5cf9bd74daad\", \"platformUpdateDomain\": 0, \"platformFaultDomain\": 0, \"computerName\": \"agents2q3000000\", \"osName\": \"ubuntu\", \"osVersion\": \"18.04\", \"vmAgent\": { \"vmAgentVersion\": \"2.2.36\", \"statuses\": [ { \"code\": \"ProvisioningState/succeeded\", \"level\": \"Info\", \"displayStatus\": \"Ready\", \"message\": \"Guest Agent is running\", \"time\": \"2019-02-22T08:17:12+00:00\" } ], \"extensionHandlers\": [] }, \"disks\": [ { \"name\": \"agents_agents_0_OsDisk_1_d0e2afd2252041e98796d5ccdcf329d0\", \"statuses\": [ { \"code\": \"ProvisioningState/succeeded\", \"level\": \"Info\", \"displayStatus\": \"Provisioning succeeded\", \"time\": \"2019-02-22T08:16:59.1325957+00:00\" } ] }, { \"name\": \"agents_agents_0_OsDisk_1_3009fa8e43e144029be77cd72065f6df\", \"statuses\": [ { \"code\": \"ProvisioningState/deleting\", \"level\": \"Info\", \"displayStatus\": \"Deleting\" } ] } ], \"statuses\": [ { \"code\": \"ProvisioningState/updating\", \"level\": \"Info\", \"displayStatus\": \"Updating\" }, { \"code\": \"PowerState/running\", \"level\": \"Info\", \"displayStatus\": \"VM running\" } ] }");

            mockHttp.When(HttpMethod.Post, "https://management.azure.com/subscriptions/f13f81f8-7578-4ca8-83f3-0a845fad3cb5/resourceGroups/*/providers/Microsoft.Compute/virtualMachineScaleSets/agents/virtualmachines/*/reimage?api-version=2018-06-01")
                .Respond(HttpStatusCode.OK);

            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var tokenProvider = new Intercept<AzureServiceTokenProvider>();
            var client = new Mock<IRestClient>();

            client.Setup(x => x.Get(It.IsAny<IEnumerableRequest<AgentPoolInfo>>()))
                .Returns(pools);

            client.Setup(x => x.Get(It.IsAny<IEnumerableRequest<AgentStatus>>()))
                .Returns(fixture.CreateMany<AgentStatus>());


            // Act
            var function = new AgentPoolScanFunction(logAnalyticsClient.Object, client.Object);
            await function.Run(new TimerInfo(null, null), new Mock<ILogger>().Object);

            // Assert
            client
                .Verify(v => v.Get(It.IsAny<IEnumerableRequest<AgentPoolInfo>>()),
                    Times.Exactly(1));

            client
                .Verify(v => v.Get(It.IsAny<IEnumerableRequest<AgentStatus>>()),
                    Times.Exactly(8));

            logAnalyticsClient
                .Verify(x => x.AddCustomLogJsonAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()),
                    Times.Exactly(1));
        }
    }
}