

using Microsoft.Extensions.Configuration;

namespace MSFSPlugin.Classes
{
    public class Helpers
    {
        static public string GetConfiguration(IConfiguration configuration, string key, string def )
        {
            var value = configuration.GetValue<string>( key ) ?? string.Empty;
            if (string.IsNullOrEmpty(value))
            {
                value = def;
                configuration[key] = value;
            }

            return value;
        }

        public static bool ValidateRequest(string request)
        {
            string trimmedRequest;
            string trimmedIndex;

            if (request?.Split(":").Length < 2)
            {
                // If no index is provided, default to "0"
                trimmedRequest = request ?? "";
                trimmedIndex = "1";
            }
            else
            {
                trimmedRequest = request?.Split(":")[0] ?? "";
                trimmedIndex = request?.Split(":")[1] ?? "1";
            }

            if (!int.TryParse(trimmedIndex, out int index)) return false;

            if (index < 1 || index > 10) return false;

            return !string.IsNullOrWhiteSpace(request) && SimVars.Names.Contains(trimmedRequest);
        }
    }
}
