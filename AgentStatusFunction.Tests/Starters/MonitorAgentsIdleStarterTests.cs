using AgentStatusFunction.Data;
using AgentStatusFunction.Orchestrators;
using AgentStatusFunction.Starter;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AgentStatusFunction.Tests.Starters
{
    public class MonitorAgentsIdleStarterTests
    {
        [Fact]
        public async void FunctionShouldStartMonitorAndReturnOK()
        {
            //Arrange
            var log = new Mock<ILogger>();
            var orchestration = new Mock<DurableOrchestrationClientBase>();

            var req = new Mock<HttpRequest>();
            req.Setup(x => x.Headers["HubName"]).Returns("hubname");
            req.Setup(x => x.Headers["JobId"]).Returns("jobId");
            req.Setup(x => x.Headers["PlanUrl"]).Returns("https://planuri/");
            req.Setup(x => x.Headers["PlanId"]).Returns("planId");
            req.Setup(x => x.Headers["PoolId"]).Returns("poolId");
            req.Setup(x => x.Headers["ProjectId"]).Returns("projectId");
            req.Setup(x => x.Headers["TaskInstanceId"]).Returns("taskId");

            //Act
            var function = new MonitorAgentsIdleStarter();
            var result = await function.Run(req.Object, orchestration.Object, log.Object);

            //Assert
            orchestration.Verify(x => x.StartNewAsync(nameof(MonitorAgentsIdleOrchestration), It.Is((AgentPoolInfo a) =>
                a.HubName == "hubname" &&
                a.JobId == "jobId" &&
                a.PlanId == "planId" &&
                a.PlanUrl == "https://planuri/" &&
                a.PoolId == "poolId" &&
                a.ProjectId == "projectId" &&
                a.TaskInstanceId == "taskId")));

            Assert.IsType<AcceptedResult>(result);
        }
    }
}