using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace B2C_ms_graph.Models
{
    public class AppSettingsFile
    {
        public AppSettings AppSettings { get; set; }

        public static AppSettings ReadFromJsonFile()
        {
            IConfigurationRoot Configuration;

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            Configuration = builder.Build();
            return Configuration.Get<AppSettingsFile>().AppSettings;
        }
    }

    public class AppSettings
    {
        [JsonPropertyName("TenantId")]
        public string TenantId { get; set; }

        [JsonPropertyName("AppId")]
        public string AppId { get; set; }

        [JsonPropertyName("ClientSecret")]
        public string ClientSecret { get; set; }

        [JsonPropertyName("B2cExtensionAppClientId")]
        public string B2cExtensionAppClientId { get; set; }

        [JsonPropertyName("UsersFileName")]
        public string UsersFileName { get; set; }

    }
}
