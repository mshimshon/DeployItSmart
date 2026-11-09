using DeployItSmart.Model.DataStructure;
using System;
using LibGit2Sharp;
using static DeployItSmart.Engine.Configuration.ConfigurationRunner;
using DeployItSmart.Engine.Configuration;

namespace DeployItSmart.Engine.Utils;

public class GitSmart : IGitSmart, IDisposable
{
    private readonly Repository _repository;
    private readonly ToolConf _toolConf = GetToolConfiguration();
    private bool _disposedValue;
    private readonly string _masterBranch;
    private readonly string _currentBranch;
    private readonly CommitFilter _gitCommitFilterOldestFirst = new CommitFilter()
    {
        SortBy = CommitSortStrategies.Reverse
    };

    public GitSmart()
    {
        _repository = new Repository(Environment.CurrentDirectory);
        _currentBranch = _repository.Head.FriendlyName;
        _masterBranch = _toolConf.ReleasePromotionFlow.First().Key;
        FetchRemoteTags();

    }
    private void FetchRemoteTags()
    {
        // Find the default remote (usually "origin")
        Remote remote = _repository.Network.Remotes["origin"] ?? _repository.Network.Remotes.FirstOrDefault() ?? throw new Exception("No remote repository configured, cannot pull tags.");
        var fetchOptions = new FetchOptions
        {
            TagFetchMode = TagFetchMode.All
        };
        Commands.Fetch(_repository, remote.Name, remote.FetchRefSpecs.Select(r => r.Specification), fetchOptions, null);
    }
    public string? GetTagCommitId(string targetTag)
    {
        var tag = _repository.Tags.FirstOrDefault(p => p.FriendlyName.Equals($"v{targetTag}", StringComparison.OrdinalIgnoreCase));
        return tag?.PeeledTarget.Sha;
    }
    public Task<List<CommitInfoStructure>> GetHistory(string? sinceTag)
    {
        string targetSha = _repository.Commits.QueryBy(_gitCommitFilterOldestFirst).FirstOrDefault()?.Sha ?? throw new Exception("Cannot find first commit.");
        bool includeTarget = true;
        if (sinceTag != default)
        {
            var tag = _repository.Tags.FirstOrDefault(p => p.FriendlyName.Equals($"v{sinceTag}", StringComparison.OrdinalIgnoreCase));
            if (tag == default)
                throw new Exception($"Tag: {sinceTag} Not Found Among Tags: {string.Join(',', _repository.Tags.Select(p => p.FriendlyName))}");
            targetSha = tag.PeeledTarget.Sha;
            includeTarget = false;
            bool confirmTargetInHistory = _repository.Commits.Any(p => p.Id.Sha == targetSha);
            if (!confirmTargetInHistory && _masterBranch == _currentBranch)
                throw new Exception($"Tag: {sinceTag} is not in the history of {_repository.Head.CanonicalName} this is most likely due to misconfiguration of repository should enable rebase merge for PR.");
        }

        List<CommitInfoStructure> targetedExtraction = [];
        foreach (Commit commit in _repository.Commits)
        {
            if (!includeTarget && commit.Id.Sha == targetSha)
                break;
            Console.Error.WriteLine($"{commit.Id.Sha.Substring(0, 7)} | {commit.MessageShort}");
            Tree? parentTree = commit.Parents.FirstOrDefault()?.Tree;
            TreeChanges changes = _repository.Diff.Compare<TreeChanges>(parentTree, commit.Tree);
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
            throw new Exception($"No Commit Found on Branch '{_repository.Head.FriendlyName}'");
        return Task.FromResult(targetedExtraction);

    }
    public async Task<List<string>> GetTagHistory()
    {
        FetchRemoteTags();
        var tags = _repository.Tags.Select(p => p.FriendlyName).Reverse();
        System.Console.Error.WriteLine($"Found Tags: {string.Join(',', tags)}");
        return tags.ToList();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _repository.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

}
