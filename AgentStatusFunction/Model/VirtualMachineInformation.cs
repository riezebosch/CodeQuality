namespace AgentStatusFunction.Model
{
    public class VirtualMachineInformation
    {
        public string ResourceGroup { get; set; }
        public int InstanceId { get; set; }
        public string Subscription { get; set; }
        public string ScaleSet { get; set; }
    }
}