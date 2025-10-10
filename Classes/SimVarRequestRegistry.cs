using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace MSFSPlugin.Classes
{
    public enum REQUEST
    {
        Dummy = 0,
    };
    public enum DEFINITION
    {
        Dummy = 0,
    };
    internal enum Events
    {
        EVENT_FLIGHT_LOAD ,
        EVENT_RECUR_FRAME ,
        EVENT_CRASHED,
        EVENT_CRASH_RESET,
        EVENT_PAUSE,
    }

    internal static class EventExtensions
    {
        public static string ToEventName(this Events e) => e switch
        {
            Events.EVENT_FLIGHT_LOAD => "FlightLoaded",
            Events.EVENT_RECUR_FRAME => "Frame",
            Events.EVENT_CRASHED => "Crashed",
            Events.EVENT_CRASH_RESET => "CrashReset",
            Events.EVENT_PAUSE => "Pause",
            _ => throw new ArgumentOutOfRangeException(nameof(e), e, null)
        };
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct SimVarStringStruct
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Value;
    }
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
    public class SimVarRequestRegistry
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
            }
        }
        public void Clear()
        {
            requests.Clear();
            requestIdMap.Clear();
        }
    }

    public class SimConnectDataRegistry
    {
        private readonly Dictionary<DEFINITION, List<string>> _definitions = new();
        private readonly Dictionary<string, DEFINITION> _reverseLookup = new();

        public void Add(DEFINITION definitionId, string simVarName)
        {
            if (!_definitions.ContainsKey(definitionId))
                _definitions[definitionId] = new List<string>();

            _definitions[definitionId].Add(simVarName);
            _reverseLookup[simVarName] = definitionId;
        }

        public IReadOnlyDictionary<DEFINITION, List<string>> GetAll() => _definitions;

        public DEFINITION? GetByName(string simVarName)
        {
            return _reverseLookup.TryGetValue(simVarName, out var defId) ? defId : null;
        }
    }


}

