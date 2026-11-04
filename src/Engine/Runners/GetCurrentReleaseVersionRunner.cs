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
    public async Task<System.Version?> GetCurrentTag()
    {
        var tags = await GetCommitInfoSince.GetTagHistory();
        var versions = tags.Where(p => Regex.IsMatch(p, _regexSemVer))
            .Select(p => new System.Version(p.Substring(1)))
            .OrderBy(v => v)
            .ToList();
        var v = versions.LastOrDefault();
        System.Console.Error.WriteLine(v == default ? "Current Version Tag Not Found" : v.ToString(3));
        return v;
    }
}
