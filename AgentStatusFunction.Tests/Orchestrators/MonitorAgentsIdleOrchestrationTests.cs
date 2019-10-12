using AgentStatusFunction.Activities;
using AgentStatusFunction.Data;
using AgentStatusFunction.Orchestrators;
using AutoFixture;
using Microsoft.Azure.WebJobs;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AgentStatusFunction.Tests.Orchestrators
{
    public class MonitorAgentsIdleOrchestrationTests : IDisposable
    {
        private readonly MockRepository _mockRepository = new MockRepository(MockBehavior.Strict);
        private readonly Mock<DurableOrchestrationContextBase> _context;
        private readonly MonitorAgentsIdleOrchestration _function = new MonitorAgentsIdleOrchestration();

        private readonly AgentPoolInfo _agentPoolInfo;

        // we keep time constant as that makes verifying timestamps much easier
        private readonly DateTime _dateTime = new DateTime(2019, 7, 4, 0, 0, 0, DateTimeKind.Utc);

        public MonitorAgentsIdleOrchestrationTests()
        {
            _context = _mockRepository.Create<DurableOrchestrationContextBase>();

            var fixture = new Fixture();
            _agentPoolInfo = fixture.Create<AgentPoolInfo>();

            _context.Setup(x => x.GetInput<AgentPoolInfo>()).Returns(_agentPoolInfo);
            _context.Setup(x => x.CurrentUtcDateTime).Returns(_dateTime);
        }

        public void Dispose()
        {
            _mockRepository.VerifyAll();
        }

        [Fact]
        public async Task IfAgentsIdleThenTimerNotCreatedAndSuccessMessageIsSent()
        {
            // Arrange
            MockAgentPoolCheckActivityCall();
            MockSendTaskCompletedMessageActivityCall();
            MockSendStartMessageActivityCall();
            MockLogMessageActivity();

            // Act
            await RunFunctionAsync();

            // Assert
            VerifySendLogMessageActivityCalled(0);
        }

        [Fact]
        public async Task IfAgentsNotIdleThenTimerCreated()
        {
            // Arrange
            const int callCount = 5;
            MockAgentPoolCheckActivityCalls(callCount);
            MockCreateTimer(_dateTime.AddMinutes(1));
            MockSendTaskCompletedMessageActivityCall();
            MockSendStartMessageActivityCall();
            MockLogMessageActivity();

            // Act
            await RunFunctionAsync();

            // Assert
            VerifyMockAgentPoolCheckActivityCalls(callCount);
            VerifyTimersCreated(_dateTime.AddMinutes(1), CancellationToken.None, callCount - 1);

            VerifySendLogMessageActivityCalled(4);
            VerifySendLogMessageActivityCalled(0);
        }

        [Fact]
        public async Task IfAgentsAreNotIdleAfterOneHourFailedMessageIsSent()
        {
            // Arrange
            _context.SetupSequence(x => x.CurrentUtcDateTime)
                .Returns(_dateTime)
                .Returns(_dateTime.AddHours(1));

            MockSendStartMessageActivityCall();
            MockSendFailedMessageActivityCall();

            // Act
            await RunFunctionAsync();

            //Assert
        }

        [Fact]
        public async Task IfExceptionOccursThenTaskFailedMessageIsSent()
        {
            // Arrange
            MockSendStartMessageActivityCall();
            MockSendFailedMessageActivityCall();

            _context
                .Setup(x => x.CallActivityAsync<int>(nameof(AgentPoolCheckActivity), _agentPoolInfo))
                .Throws(new Exception());

            // Act
            Func<Task> act = () => RunFunctionAsync();

            // Assert: in Dispose()
            await Assert.ThrowsAsync<Exception>(act);
        }

        private void MockSendFailedMessageActivityCall()
        {
            _context
                .Setup(x => x.CallActivityAsync(nameof(SendFailedMessageActivity), _agentPoolInfo))
                .Returns(() => Task.CompletedTask);
        }

        private void MockSendStartMessageActivityCall()
        {
            _context
                .Setup(x => x.CallActivityAsync(nameof(SendStartMessageActivity), _agentPoolInfo))
                .Returns(() => Task.CompletedTask);
        }

        private void MockAgentPoolCheckActivityCall()
        {
            MockAgentPoolCheckActivityCalls(1);
        }

        private void MockAgentPoolCheckActivityCalls(int callCount)
        {
            var i = 0;
            _context
                .Setup(x => x.CallActivityAsync<int>(nameof(AgentPoolCheckActivity), _agentPoolInfo))
                .ReturnsAsync(() => callCount - ++i);
        }

        private void MockSendTaskCompletedMessageActivityCall()
        {
            _context
                .Setup(x => x.CallActivityAsync(nameof(SendTaskCompletedMessageActivity), _agentPoolInfo))
                .Returns(() => Task.CompletedTask);
        }

        private void MockCreateTimer(DateTime fireAt) =>
            _context.Setup(x => x.CreateTimer(fireAt, CancellationToken.None)).Returns(Task.CompletedTask);

        private void MockLogMessageActivity() =>
            _context.Setup(c => c.CallActivityAsync(nameof(SendLogMessageActivity), It.IsAny<LogMessage>()))
                .Returns(Task.CompletedTask);

        private Task RunFunctionAsync() =>
            _function.Run(_context.Object);

        private void VerifyMockAgentPoolCheckActivityCalls(int callCount) =>
            _context.Verify(x => x.CallActivityAsync<int>(nameof(AgentPoolCheckActivity), _agentPoolInfo),
                Times.Exactly(callCount));

        private void VerifyTimersCreated(DateTime firedAt, CancellationToken cancellationToken, int count) =>
            _context.Verify(x => x.CreateTimer(firedAt, cancellationToken), Times.Exactly(count));

        private void VerifySendLogMessageActivityCalled(int agentsActive) =>
            _context.Verify(c => c.CallActivityAsync(nameof(SendLogMessageActivity),
                It.Is<LogMessage>(m => m.Message == $"{agentsActive} agents still working.")));
    }
}