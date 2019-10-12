using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgentClient
{
    public class RestClient : IRestClient
    {
        public Task<TResponse> GetAsync<TResponse>(IRequest<TResponse> request) where TResponse : new() => throw new NotImplementedException();

        public IEnumerable<TResponse> Get<TResponse>(IEnumerableRequest<TResponse> request) => throw new NotImplementedException();
    }
}