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

public class GetCurrentPreReleaseVersionRunner : IRunner
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

    public static IRunner Create() => new GetCurrentPreReleaseVersionRunner();
    public static string GetCommandName() => nameof(GetCurrentPreReleaseVersionRunner);
    public GetCurrentPreReleaseVersionRunner()
    {
        _getCommitInfoSince = new GetCommitInfoSinceRunner();
        _messageTool = new MessageSearchTool();
        if (!string.IsNullOrWhiteSpace(VersionRunnerConfig.PreviewMark))
            _regexPreviewSemVer = @$"^v\d+\.\d+\.\d+-{VersionRunnerConfig.PreviewMark}\.\d+$";
    }

    public async Task RunAsync()
    {
        var current = await GetCurrentPreviewTag();
        if (VersionRunnerConfig.GithubOutput)
            GithubOutput("version_current_preview", current?.ToString() ?? throw new Exception("Current Version Null Required due to --ghOutput"));
        Console.WriteLine(current?.ToString() ?? "");
    }
    private readonly string _regexPreviewSemVer = @"^v\d+\.\d+\.\d+-preview\.\d+$";
    public async Task<List<NuGetVersion>> GetPreviewVersions()
    {
        var tags = await _getCommitInfoSince.GetTagHistory();
        return tags.Where(p => Regex.IsMatch(p, _regexPreviewSemVer))
            .Select(p => NuGetVersion.Parse(p.Substring(1)))
            .OrderBy(v => v)
            .ToList();
    }

    public async Task<NuGetVersion?> GetCurrentPreviewTag()
    {
        var versions = await GetPreviewVersions();
        NuGetVersion? v = versions.LastOrDefault(p => p.IsPrerelease);
        return v;
    }
}
