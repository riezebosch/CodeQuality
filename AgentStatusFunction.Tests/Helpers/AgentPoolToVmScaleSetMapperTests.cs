using AgentStatusFunction.Helpers;
using FluentAssertions;
using Xunit;

namespace AgentStatusFunction.Tests.Helpers
{
    public static class AgentPoolToVmScaleSetMapperTests
    {
        [Fact]
        public static void ParseVirtualMachineInformation()
        {
            var mapper = new AgentPoolToVmScaleSetMapper();
            var result = mapper.ParseVirtualMachineInformation(
                "Some-Build-Azure-Linux-Canary", 
                "linux-agent-canary-1-84432-2296-000000-1");
            
            result.InstanceId.Should().Be(0); 
            result.ResourceGroup.Should().Be("rg-m01-prd-linuxcanary-01");
        }
    }
}