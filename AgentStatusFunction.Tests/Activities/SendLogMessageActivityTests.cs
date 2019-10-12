using AgentStatusFunction.Activities;
using AgentStatusFunction.Data;
using AutoFixture;
using ExpectedObjects;
using Microsoft.Extensions.Logging;
using Moq;
using SecurePipelineScan.VstsService;
using System.Threading.Tasks;
using Xunit;

namespace AgentStatusFunction.Tests.Activities
{
    public class SendLogMessageActivityTests
    {
        [Fact]
        public async Task ShouldSendMessage()
        {
            // Arrange
            var fixture = new Fixture();
            fixture.Customize<AgentPoolInfo>(x => x.With(y => y.PlanUrl, "https://kjhkjh.jhgkjh.nl"));
            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);
            var activity = new SendLogMessageActivity(client.Object);
            const string message = "This is a test";

            var agentPoolInfo = fixture.Create<AgentPoolInfo>();
            var logMessage = new LogMessage(agentPoolInfo, message);
            var body = new MultipleWithCount<string>
            {
                Value = new[] {message}
            };
            var expectedBody = body.ToExpectedObject();

            client.Setup(c => c.PostAsync(It.IsAny<AzureDevOpsCallbackRequest<MultipleWithCount<string>>>(),
                    It.Is<MultipleWithCount<string>>(b => expectedBody.Matches(b))))
                .ReturnsAsync(new object());

            // Act
            await activity.Run(logMessage, new Mock<ILogger>().Object);

            // Assert
            client.VerifyAll();
        }
    }
}