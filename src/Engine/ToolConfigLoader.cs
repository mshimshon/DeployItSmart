using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeployItSmart.Model.Configuration;

namespace DeployItSmart.Engine;

public sealed class ToolConfigLoader
{
    public ToolConfiguration Configuration { get; private set; }
    public string ConfigPathFile { get; init; }
    private JsonSerializerOptions jsonSerializerOption = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    public ToolConfigLoader()
    {
        ConfigPathFile = Path.Combine(Environment.CurrentDirectory, ".github", "config", "deploy_it_smart.json");
        if (File.Exists(ConfigPathFile))
        {
            string json = File.ReadAllText(ConfigPathFile);
            ToolConfiguration? config = JsonSerializer.Deserialize<ToolConfiguration>(json, jsonSerializerOption);
            if (config != default)
            {
                Dictionary<string, string> nameMapResolver = new();
                foreach (var item in config.Projects)
                {
                    if (item.Key == "_")
                    {
                        nameMapResolver.Add(item.Key, item.Value);
                        continue;
                    }

                    string path = Path.IsPathFullyQualified(item.Key) ? Path.GetFullPath(item.Key) : Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, item.Key));
                    nameMapResolver.Add(path, item.Value);
                }
                System.Console.Error.WriteLine($"Using 'deploy_it_smart.json' user defined configuration");
                Configuration = config with
                {
                    Projects = nameMapResolver
                };
            }
        }
        if (Configuration == default)
            Configuration = new();
    }
}
