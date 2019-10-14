using System;
using System.Collections.Generic;
using AgentStatusFunction.Model;

namespace AgentStatusFunction.Helpers
{
    public class AgentPoolToVmScaleSetMapper : IAgentPoolToVmScaleSetMapper
    {
        private readonly IDictionary<string, string> _agentPoolResourceGroupPrefix = new Dictionary<string, string>
        {
            ["Some-Build-Azure-Linux"] = "rg-m01-prd-linuxagents-0",
            ["Some-Build-Azure-Linux-Canary"] = "rg-m01-prd-linuxcanary-0",
            ["Some-Build-Azure-Linux-Fallback"] = "rg-m01-prd-linuxfallback-0",
            ["Some-Build-Azure-Linux-Preview"] = "rg-m01-prd-linuxpreview-0",
            ["Some-Build-Azure-Windows"] = "rg-m01-prd-winagents-0",
            ["Some-Build-Azure-Windows-Canary"] = "rg-m01-prd-wincanary-0",
            ["Some-Build-Azure-Windows-Canary-2"] = "rg-m01-prd-wincanary-0",
            ["Some-Build-Azure-Windows-Fallback"] = "rg-m01-prd-winfallback-0",
            ["Some-Build-Azure-Windows-Preview"] = "rg-m01-prd-winpreview-0",
        };

        public bool IsWellKnown(string pool) => _agentPoolResourceGroupPrefix.ContainsKey(pool);

        public VirtualMachineInformation ParseVirtualMachineInformation(string pool, string agent)
        {
            var prefix = _agentPoolResourceGroupPrefix[pool];
            var parts = agent.Split('-');

            if (parts.Length != 8)
            {
                throw new ArgumentException($"Illegal agent name: {agent}");
            }

            return new VirtualMachineInformation
            {
                ResourceGroup = $"{prefix}{parts[3]}", 
                InstanceId = int.Parse(parts[6]),
                Subscription = "f13f81f8-7578-4ca8-83f3-0a845fad3cb5",
                ScaleSet = "agents"
            };
        }
    }
}