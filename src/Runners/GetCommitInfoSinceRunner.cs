using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeployItSmart.Engine.ArgumentParser;
using DeployItSmart.Engine.Configuration;
using DeployItSmart.Engine.Utils;
using DeployItSmart.Model.Configuration;
using DeployItSmart.Model.DataStructure;
using LibGit2Sharp;
using static DeployItSmart.Engine.Configuration.ConfigurationRunner;

namespace DeployItSmart.Engine.Runner;

public class GetCommitInfoSinceRunner : IRunner
{
    private readonly JsonSerializerOptions _jsonSerializerOption = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles

    };
    private ToolConf ToolConfig => GetToolConfiguration();

    private CommitSearchParameters CommitSearchConfig => GetConfigurationFor<CommitSearchParameters>();
    public GetCommitInfoSinceRunner()
    {
    }
    public static IRunner Create() => new GetCommitInfoSinceRunner();
    public static string GetCommandName() => nameof(GetCommitInfoSinceRunner);
    public async Task RunAsync()
    {
        var commits = await GetHistory(CommitSearchConfig.SinceTag);
        string json = JsonSerializer.Serialize(commits, _jsonSerializerOption);
        Console.WriteLine(json);
    }

    public Task<List<string>> GetTagHistory()
        => ServiceAccessor.Use<IGitSmart>().GetTagHistory();
    public Task<List<CommitInfoStructure>> GetHistory(string? sinceTag)
        => ServiceAccessor.Use<IGitSmart>().GetHistory(sinceTag);
}
