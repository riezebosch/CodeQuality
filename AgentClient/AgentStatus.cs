namespace AgentClient
{
    public class AgentStatus
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public string Status { get; set; }
        public AssignedRequest AssignedRequest { get; set; }
        public bool Enabled { get; set; }
        public string Version { get; set; }
    }
}