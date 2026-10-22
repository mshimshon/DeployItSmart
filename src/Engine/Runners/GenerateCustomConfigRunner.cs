using System;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeployItSmart.Engine.ArgumentParser;
using DeployItSmart.Model.Configuration.Runner;
using DeployItSmart.Model.DataStructure;
using LibGit2Sharp;

namespace DeployItSmart.Engine.Runner;

public class GenerateCustomConfigRunner : BaseRunner
{
    private JsonSerializerOptions jsonSerializerOption = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        Encoder = JavaScriptEncoder.Default
    };
    private ToolConfigLoader ToolConfig { get; init; }
    public GenerateCustomConfigRunner(SimpleArgumentParser simpleArgument)
    {
        ToolConfig = new ToolConfigLoader();
    }

    public override async Task RunAsync()
    {
        string path = Path.Combine(Environment.CurrentDirectory, ".github", "config", "deploy_it_smart.json");
        if (File.Exists(ToolConfig.ConfigPathFile))
            throw new Exception($"'{ToolConfig.ConfigPathFile}' already exist.");
        if (!Directory.Exists(Path.GetDirectoryName(path)))
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string json = JsonSerializer.Serialize(ToolConfig.Configuration, jsonSerializerOption);
        File.WriteAllText(path, json, Encoding.UTF8);
    }
}
