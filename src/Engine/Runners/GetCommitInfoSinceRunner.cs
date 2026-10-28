using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeployItSmart.Engine.ArgumentParser;
using DeployItSmart.Model.Configuration.Runner;
using DeployItSmart.Model.DataStructure;
using LibGit2Sharp;

namespace DeployItSmart.Engine.Runner;

public class GetCommitInfoSinceRunner : BaseRunner
{
    private JsonSerializerOptions jsonSerializerOption = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles

    };
    public GetCommitInfoSinceConf Configuration { get; init; }
    private CommitFilter gitCommitFilterOldestFirst = new CommitFilter()
    {
        SortBy = CommitSortStrategies.Reverse
    };
    public GetCommitInfoSinceRunner(SimpleArgumentParser simpleArgument)
    {
        Configuration = new GetCommitInfoSinceConf(simpleArgument);
    }

    public override async Task RunAsync()
    {
        var commits = await GetHistory(Configuration.SinceTag);
        string json = JsonSerializer.Serialize(commits, jsonSerializerOption);
        Console.WriteLine(json);
    }
    private static void FetchRemoteTags(Repository repo)
    {
        // Find the default remote (usually "origin")
        Remote remote = repo.Network.Remotes["origin"] ?? repo.Network.Remotes.FirstOrDefault() ?? throw new Exception("No remote repository configured, cannot pull tags.");
        var fetchOptions = new FetchOptions
        {
            TagFetchMode = TagFetchMode.All
        };
        Commands.Fetch(repo, remote.Name, remote.FetchRefSpecs.Select(r => r.Specification), fetchOptions, null);
    }
    public async Task<List<string>> GetTagHistory()
    {
        using var repo = new Repository(Environment.CurrentDirectory);
        FetchRemoteTags(repo);
        var tags = repo.Tags.Select(p => p.FriendlyName).Reverse();
        System.Console.Error.WriteLine($"Found Tags: {string.Join(',', tags)}");
        return tags.ToList();
    }
    public Task<List<CommitInfoStructure>> GetHistory(string? sinceTag)
    {
        using var repo = new Repository(Environment.CurrentDirectory);
        FetchRemoteTags(repo);
        string targetSha = repo.Commits.QueryBy(gitCommitFilterOldestFirst).FirstOrDefault()?.Sha ?? throw new Exception("Cannot find first commit.");
        bool includeTarget = true;
        if (sinceTag != default)
        {
            var tag = repo.Tags.FirstOrDefault(p => p.FriendlyName.Equals($"v{sinceTag}", StringComparison.OrdinalIgnoreCase));
            if (tag == default)
                throw new Exception($"Tag: {sinceTag} Not Found Among Tags: {string.Join(',', repo.Tags.Select(p => p.FriendlyName))}");
            targetSha = tag.PeeledTarget.Sha;
            includeTarget = false;
        }
        List<CommitInfoStructure> targetedExtraction = [];
        foreach (Commit commit in repo.Commits)
        {
            if (!includeTarget && commit.Id.Sha == targetSha)
                break;
            Console.Error.WriteLine($"{commit.Id.Sha.Substring(0, 7)} | {commit.MessageShort}");
            Tree? parentTree = commit.Parents.FirstOrDefault()?.Tree;
            TreeChanges changes = repo.Diff.Compare<TreeChanges>(parentTree, commit.Tree);
            List<string> relativePaths = changes.Select(c => c.Path).ToList();
            var data = new CommitInfoStructure()
            {
                Author = commit.Author.Name,
                CommitId = commit.Id.Sha,
                CommitMessage = commit.Message,
                AffectedFiles = relativePaths
            };
            targetedExtraction.Add(data);
            if (commit.Id.Sha == targetSha)
                break;
        }

        if (targetedExtraction.Count <= 0)
            throw new Exception($"No Commit Found on Branch '{repo.Head.FriendlyName}'");
        return Task.FromResult(targetedExtraction);
    }
}
