

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
    }
}
