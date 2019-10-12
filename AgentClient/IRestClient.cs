using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgentClient
{
    public interface IRestClient
    {
        Task<TResponse> GetAsync<TResponse>(IRequest<TResponse> request) where TResponse : new();
        IEnumerable<TResponse> Get<TResponse>(IEnumerableRequest<TResponse> request);
    }
}