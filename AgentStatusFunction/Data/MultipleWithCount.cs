using SecurePipelineScan.VstsService.Response;

namespace AgentStatusFunction.Data
{
    public class MultipleWithCount<T> : Multiple<T>
    {
        public int Count => Value.Length;
    }
}