using System.Threading.Tasks;
using AgentClient;
using AgentStatusFunction.Helpers;
using AutoFixture;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
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

            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var client = new Mock<IRestClient>();

            client
                .Setup(x => x.Get(It.IsAny<IEnumerableRequest<AgentPoolInfo>>()))
                .Returns(fixture.CreateMany<AgentPoolInfo>());

            client
                .Setup(x => x.Get(It.IsAny<IEnumerableRequest<AgentStatus>>()))
                .Returns(fixture.CreateMany<AgentStatus>());

            var mapper = new Mock<IAgentPoolToVmScaleSetMapper>();
            mapper
                .Setup(x => x.IsWellKnown(It.IsAny<string>()))
                .Returns(true);

            // Act
            var function = new AgentPoolScanFunction(logAnalyticsClient.Object, client.Object, mapper.Object);
            await function.Run(new TimerInfo(null, null), new Mock<ILogger>().Object);

            // Assert
            client.Verify(x => 
                x.Get(It.IsAny<IEnumerableRequest<AgentPoolInfo>>()),
                Times.Exactly(1));

            client.Verify(x => 
                x.Get(It.IsAny<IEnumerableRequest<AgentStatus>>()));

            logAnalyticsClient.Verify(x => 
                x.AddCustomLogJsonAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()),
                Times.Exactly(1));
        }
        
        [Fact]
        public async Task AgentPoolScanIgnoresUnobservedAgentPools()
        {
            // Arrange
            var fixture = new Fixture();

            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var client = new Mock<IRestClient>();

            client
                .Setup(x => x.Get(It.IsAny<IEnumerableRequest<AgentPoolInfo>>()))
                .Returns(fixture.CreateMany<AgentPoolInfo>());

            var mapper = new Mock<IAgentPoolToVmScaleSetMapper>();
            mapper
                .Setup(x => x.IsWellKnown(It.IsAny<string>()))
                .Returns(false);

            // Act
            var function = new AgentPoolScanFunction(logAnalyticsClient.Object, client.Object, mapper.Object);
            await function.Run(new TimerInfo(null, null), new Mock<ILogger>().Object);

            // Assert
            client.Verify(x => 
                x.Get(It.IsAny<IEnumerableRequest<AgentStatus>>()), 
                Times.Never);
        }
    }
}