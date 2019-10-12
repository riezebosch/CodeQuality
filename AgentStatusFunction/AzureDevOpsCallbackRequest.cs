using SecurePipelineScan.VstsService;
using System;
using System.Collections.Generic;

namespace AgentStatusFunction
{
    public class AzureDevOpsCallbackRequest<TBody> : VstsRequest<TBody, object> where TBody : new()
    {
        public AzureDevOpsCallbackRequest(string planUrl, string resource, IDictionary<string, object> queryParams)
            : base(resource, queryParams)
        {
            if (planUrl == null) throw new ArgumentNullException(nameof(planUrl));

            PlanUrl = new Uri(planUrl);
        }

        public override Uri BaseUri(string organization) => PlanUrl;

        public Uri PlanUrl { get; }
    }
}