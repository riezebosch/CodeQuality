﻿using AgentStatusFunction.Activities;
using AgentStatusFunction.Data;
using ExpectedObjects;
using Microsoft.Extensions.Logging;
using Moq;
using SecurePipelineScan.VstsService;
using System.Threading.Tasks;
using Xunit;

namespace AgentStatusFunction.Tests.Activities
{
    public class SendStartMessageActivityTests
    {
        [Fact]
        public async Task SendTaskStartedMessage()
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
                Result = null,
                Name = "TaskStarted",
                JobId = poolInfo.JobId,
                TaskId = poolInfo.TaskInstanceId
            }.ToExpectedObject();

            var rest = new Mock<IVstsRestClient>(MockBehavior.Strict);
            rest
                .Setup(x => x.PostAsync(It.IsAny<AzureDevOpsCallbackRequest<MessageBody>>(),
                    It.Is<MessageBody>(m => expected.Matches(m))))
                .ReturnsAsync(new object())
                .Verifiable();

            // Act
            var function = new SendStartMessageActivity(rest.Object);
            await function.Run(poolInfo, new Mock<ILogger>().Object);

            // Assert
            rest.VerifyAll();
        }
    }
}