using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using DeployItSmart.Engine.ArgumentParser;
using DeployItSmart.Engine.Configuration;
using DeployItSmart.Engine.Utils;
using DeployItSmart.Engine.Runner;
using LibGit2Sharp;
using static DeployItSmart.Engine.Configuration.ConfigurationRunner;
using NuGet.Versioning;
using DeployItSmart.Model.Configuration;
using static DeployItSmart.Engine.Utils.GithubWriter;

namespace DeployItSmart.Runners;

public class GetCurrentReleaseVersionRunner : IRunner
{
    private readonly JsonSerializerOptions _jsonSerializerOption = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles

    };
    private ToolConf ToolConfig => GetToolConfiguration();
    private VersionRunnerParameters VersionRunnerConfig => GetConfigurationFor<VersionRunnerParameters>();
    private GetCommitInfoSinceRunner _getCommitInfoSince;
    private readonly MessageSearchTool _messageTool;
    private readonly CommitFilter _gitCommitFilterOldestFirst = new CommitFilter()
    {
        SortBy = CommitSortStrategies.Reverse
    };

    public static IRunner Create() => new GetCurrentReleaseVersionRunner();
    public static string GetCommandName() => nameof(GetCurrentReleaseVersionRunner);
    public GetCurrentReleaseVersionRunner()
    {
        _getCommitInfoSince = new GetCommitInfoSinceRunner();
        _messageTool = new MessageSearchTool();
    }

    public async Task RunAsync()
    {
        var current = await GetCurrentTag();
        if (VersionRunnerConfig.GithubOutput)
            if (current != default)
                GithubOutput("version_current", current.ToString());
            else if (VersionRunnerConfig.IsFirstRelease)
                GithubOutput("version_current", $"0.0.0");
            else
                throw new Exception("Current Version Null Required due to --ghOutput and no --isFirstRelease");
        Console.WriteLine(current?.ToString() ?? "");
    }
    private readonly string _regexSemVer = @"^v\d+\.\d+\.\d+$";
    public async Task<List<NuGetVersion>> GetVersions()
    {
        var tags = await _getCommitInfoSince.GetTagHistory();
        return tags.Where(p => Regex.IsMatch(p, _regexSemVer))
            .Select(p => NuGetVersion.Parse(p.Substring(1)))
            .OrderBy(v => v)
            .ToList();
    }
    public async Task<NuGetVersion?> GetCurrentTag()
    {
        var versions = await GetVersions();
        NuGetVersion? v = versions.LastOrDefault(p => !p.IsPrerelease);
        return v;
    }
}
