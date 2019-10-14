using AgentStatusFunction.Model;

namespace AgentStatusFunction.Helpers
{
    public interface IAgentPoolToVmScaleSetMapper
    {
        bool IsWellKnown(string pool);
        VirtualMachineInformation ParseVirtualMachineInformation(string pool, string agent);
    }
}