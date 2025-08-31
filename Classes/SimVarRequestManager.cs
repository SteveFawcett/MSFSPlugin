using MSFSPlugin.Models;
using System.Collections.Concurrent;

namespace MSFSPlugin.Classes
{
    public class SimVarRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string DataTypeName { get; set; } = "STRING256";
        public REQUEST RequestId { get; set; }
        public DEFINITION DefinitionId { get; set; }
        public object? Value { get; set; }
        public bool IsString => true;
    }
    public class SimvarRequestManager
    {
        private readonly ConcurrentDictionary<string, SimVarRequest> requests = new();
        private readonly ConcurrentDictionary<REQUEST, SimVarRequest> requestIdMap = new();

        public event Action<SimVarRequest>? RequestUpdated;

        public void RegisterRequest(SimVarRequest request)
        {
            var key = request.Name.ToLowerInvariant();
            requests.TryAdd(key, request);
            requestIdMap.TryAdd(request.RequestId, request);
        }

        public bool TryGetRequest(string name, out SimVarRequest? request) =>
            requests.TryGetValue(name.ToLowerInvariant(), out request);

        public bool TryGetByRequestId(REQUEST id, out SimVarRequest? request) =>
            requestIdMap.TryGetValue(id, out request);

        public IEnumerable<SimVarRequest> GetAllRequests() => requests.Values;

        public void UpdateValue(string name, object value)
        {
            if (TryGetRequest(name, out var request))
            {
                if (request is not null)
                {
                    request.Value = value;
                    RequestUpdated?.Invoke(request);
                }
                ;
            }
        }

        public void Clear()
        {
            requests.Clear();
            requestIdMap.Clear();
        }
    }
}

