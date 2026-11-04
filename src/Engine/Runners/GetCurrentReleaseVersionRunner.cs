using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using DeployItSmart.Engine.ArgumentParser;
using DeployItSmart.Engine.Runner;
using LibGit2Sharp;

namespace DeployItSmart.Engine.Runners;

public class GetCurrentReleaseVersionRunner : BaseRunner
{
    private JsonSerializerOptions jsonSerializerOption = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles

    };
    public GetCommitInfoSinceRunner GetCommitInfoSince { get; init; }
    public MessageSearchTool MessageTool { get; init; }
    private CommitFilter gitCommitFilterOldestFirst = new CommitFilter()
    {
        SortBy = CommitSortStrategies.Reverse
    };


    public GetCurrentReleaseVersionRunner(SimpleArgumentParser simpleArgument)
    {
        GetCommitInfoSince = new GetCommitInfoSinceRunner(simpleArgument);
        MessageTool = new MessageSearchTool();
    }

    public override async Task RunAsync()
    {
        var current = await GetCurrentTag();
        Console.WriteLine(current?.ToString(3) ?? "");
    }
    private readonly string _regexSemVer = @"^v\d+\.\d+\.\d+$";
    public async Task<List<System.Version>> GetVersions()
    {
        var tags = await GetCommitInfoSince.GetTagHistory();
        return tags.Where(p => Regex.IsMatch(p, _regexSemVer))
            .Select(p => new System.Version(p.Substring(1)))
            .OrderBy(v => v)
            .ToList();
    }
    public async Task<System.Version?> GetCurrentTag()
    {
        var versions = await GetVersions();
        System.Version? v = versions.LastOrDefault();


        return v;
    }
}
