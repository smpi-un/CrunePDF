using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CrunePDF
{
    internal class AppSettingsLoader
    {
        public class Config
        {
            public string Language { get; set; } = "";
            public double RaitoThreshold { get; set; } = 0;
            public double ScoreThreshold { get; set; } = 0;
            public string OutputDirectory { get; set; } = "";
        }
        public static Config? LoadConfig()
        {
            var jsonSerOpt = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                 WriteIndented = true,
                  ReadCommentHandling = JsonCommentHandling.Skip,
            };
            var programPath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            var configFileName = "config.json";
            var configFilePath = Path.Join(programPath, configFileName);
            var currentLanguage = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            Config? config = null;
            if (File.Exists(configFilePath))
            {
                string configJsonText = File.ReadAllText(configFilePath);
                config = JsonSerializer.Deserialize<Config>(configJsonText, jsonSerOpt);
            }
            else
            {
                using var stream = File.CreateText(configFilePath);
                config = new Config
                {
                    Language = currentLanguage,
                    ScoreThreshold = 10,
                    RaitoThreshold = 1.25,
                    OutputDirectory = "",
                };
                stream.Write(JsonSerializer.Serialize(config, jsonSerOpt));
            }
            return config;
        }
    }
}
