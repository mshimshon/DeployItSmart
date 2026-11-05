using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeployItSmart.Engine.ArgumentParser;
using DeployItSmart.Model.Configuration;

namespace DeployItSmart.Engine.Configuration;

public static class ConfigurationRunner
{
    private static ToolConf _toolConfiguration = default!;
    private static Dictionary<Type, object> _configuration = new();
    private static JsonSerializerOptions _jsonSerializerOption = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };
    public static T GetConfigurationFor<T>() where T : BaseConfigurator
    {
        Type type = typeof(T);
        if (_configuration.ContainsKey(type))
            return (T)_configuration[type];

        T instance = (T)Activator.CreateInstance(type)!;
        _configuration[type] = instance;
        return instance;
    }

    public static ToolConf GetToolConfiguration()
    {
        if (_toolConfiguration == default)
            LoadToolConfiguration();
        return _toolConfiguration!;
    }

    private static void LoadToolConfiguration()
    {
        var path = Path.Combine(Environment.CurrentDirectory, ".github", "config", "deploy_it_smart.json");
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            ToolConf? config = JsonSerializer.Deserialize<ToolConf>(json, _jsonSerializerOption);
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

                    string projectPath = Path.IsPathFullyQualified(item.Key) ? Path.GetFullPath(item.Key) : Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, item.Key));
                    nameMapResolver.Add(projectPath, item.Value);
                }
                string customScriptLocation = config.CustomScriptLocation;
                config.CustomScriptLocation = Path.IsPathFullyQualified(customScriptLocation) ? Path.GetFullPath(customScriptLocation) : Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, customScriptLocation));
                System.Console.Error.WriteLine($"Using 'deploy_it_smart.json' user defined configuration");
                _toolConfiguration = config with
                {
                    Projects = nameMapResolver
                };
            }
        }
        if (_toolConfiguration == default)
            _toolConfiguration = new();
    }
}
