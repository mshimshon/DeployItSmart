using System;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeployItSmart.Engine.ArgumentParser;
using DeployItSmart.Engine.Configuration;
using DeployItSmart.Model.DataStructure;
using LibGit2Sharp;
using static DeployItSmart.Engine.Configuration.ConfigurationRunner;

namespace DeployItSmart.Engine.Runner;

public class GenerateCustomConfigRunner : IRunner
{
    private readonly JsonSerializerOptions _jsonSerializerOption = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        Encoder = JavaScriptEncoder.Default
    };
    private ToolConf ToolConfig => GetToolConfiguration();

    public GenerateCustomConfigRunner()
    {
    }
    public static IRunner Create() => new GenerateCustomConfigRunner();
    public static string GetCommandName() => nameof(GenerateCustomConfigRunner);
    public async Task RunAsync()
    {

        string path = Path.Combine(Environment.CurrentDirectory, ".github", "config", "deploy_it_smart.json");
        if (!Directory.Exists(Path.GetDirectoryName(path)))
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        if (File.Exists(path))
            System.Console.Error.WriteLine($"'{path}' already exist, updating to latest schema.");
        ToolConfig.Projects = ToolConfig.Projects.ToDictionary(p => p.Key == "_" ? p.Key : Path.GetRelativePath(Environment.CurrentDirectory, p.Key), path => path.Value);
        ToolConfig.CustomScriptLocation = Path.GetRelativePath(Environment.CurrentDirectory, ToolConfig.CustomScriptLocation);
        string json = JsonSerializer.Serialize(ToolConfig, _jsonSerializerOption);
        File.WriteAllText(path, json, Encoding.UTF8);
    }
}
