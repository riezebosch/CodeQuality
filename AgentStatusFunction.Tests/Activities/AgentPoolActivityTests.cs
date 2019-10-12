using AgentStatusFunction.Activities;
using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Extensions.Logging;
using Moq;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using Xunit;
using AgentPoolInfo = AgentStatusFunction.Data.AgentPoolInfo;

namespace AgentStatusFunction.Tests.Activities
{
    public class AgentPoolActivityTests
    {
        [Fact]
        public void IfNotEnabledAgentsNotIdleResultIsNotZero()
        {
            //Arrange
            var fixture = new Fixture();
            fixture.Customize<AgentStatus>(x => x.With(p => p.Enabled, false));

            var rest = new Mock<IVstsRestClient>();
            rest.Setup(x => x.Get(It.IsAny<IEnumerableRequest<AgentStatus>>())).ReturnsUsingFixture(fixture);

            //Act
            var function = new AgentPoolCheckActivity(rest.Object);
            var result = function.Run(new AgentPoolInfo { PoolId = "1" }, new Mock<ILogger>().Object);

            //Assert
            Assert.NotEqual(0, result);
        }
        
        [Fact]
        public void IfAgentsIdle_ThenResultIsZero()
        {
            //Arrange
            var fixture = new Fixture();
            fixture.Customize<AgentStatus>(x => x
                .Without(p => p.AssignedRequest));

            var rest = new Mock<IVstsRestClient>();
            rest.Setup(x => x.Get(It.IsAny<IEnumerableRequest<AgentStatus>>())).ReturnsUsingFixture(fixture);

            //Act
            var function = new AgentPoolCheckActivity(rest.Object);
            var result = function.Run(new AgentPoolInfo { PoolId = "1" }, new Mock<ILogger>().Object);

            //Assert
            Assert.Equal(0, result);
        }
        
        [Fact]
        public void IgnoreActiveEnabledAgentsWhen()
        {
            //Arrange
            var fixture = new Fixture();
            fixture.Customize<AgentStatus>(x => x
                .With(p => p.Enabled, true));
            
            var rest = new Mock<IVstsRestClient>();
            rest.Setup(x => x.Get(It.IsAny<IEnumerableRequest<AgentStatus>>())).ReturnsUsingFixture(fixture);

            //Act
            var function = new AgentPoolCheckActivity(rest.Object);
            var result = function.Run(new AgentPoolInfo { PoolId = "1" }, new Mock<ILogger>().Object);

            //Assert
            Assert.Equal(0, result);
        }
    }
}