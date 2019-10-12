using AgentStatusFunction.Activities;
using AgentStatusFunction.Data;
using ExpectedObjects;
using Microsoft.Extensions.Logging;
using Moq;
using SecurePipelineScan.VstsService;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AgentStatusFunction.Tests.Activities
{
    public class SendFailedMessageActivityTests
    {
        [Fact]
        public async Task SendTaskFailedMessage()
        {
            // Arrange
            var poolInfo = new AgentPoolInfo
            {
                HubName = "hubname",
                JobId = "jobid",
                PlanId = "planid",
                PlanUrl = "https://planuri/",
                PoolId = "0",
                ProjectId = "projectid",
                TaskInstanceId = "taskid"
            };

            var expected = new MessageBody
            {
                Result = "failed",
                Name = "TaskCompleted",
                JobId = poolInfo.JobId,
                TaskId = poolInfo.TaskInstanceId
            }.ToExpectedObject();

            var rest = new Mock<IVstsRestClient>();
            rest
                .Setup(x => x.PostAsync(
                    It.Is<AzureDevOpsCallbackRequest<MessageBody>>(r => r.PlanUrl == new Uri(poolInfo.PlanUrl)),
                    It.Is<MessageBody>(m => expected.Matches(m))))
                .ReturnsAsync(new object())
                .Verifiable();

            // Act
            var function = new SendFailedMessageActivity(rest.Object);
            await function.Run(poolInfo, new Mock<ILogger>().Object);

            // Assert
            rest.VerifyAll();
        }
    }
}