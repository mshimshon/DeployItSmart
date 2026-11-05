using System;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeployItSmart.Engine.ArgumentParser;
using DeployItSmart.Engine.Configuration;
using DeployItSmart.Engine.Utils;
using DeployItSmart.Engine.Runner;
using DeployItSmart.Model.DataStructure;
using DeployItSmart.Model.DataStructure.Enums;
using LibGit2Sharp;
using static DeployItSmart.Engine.Configuration.ConfigurationRunner;

namespace DeployItSmart.Runners;

public class LocalTrashCommitCleanerRunner : IRunner
{
    private ToolConf ToolConfig => GetToolConfiguration();
    private readonly GetCurrentReleaseVersionRunner _currentReleaseVersion;
    private readonly GetTrackCommitMessagesRunner _getTrackCommitMessages;
    private readonly MessageSearchTool _messageTool;
    public LocalTrashCommitCleanerRunner()
    {
        _currentReleaseVersion = new GetCurrentReleaseVersionRunner();
        _getTrackCommitMessages = new GetTrackCommitMessagesRunner();
        _messageTool = new MessageSearchTool();
    }
    public static IRunner Create() => new LocalTrashCommitCleanerRunner();
    public static string GetCommandName() => nameof(LocalTrashCommitCleanerRunner);
    public async Task RunAsync()
    {
        await CleanCommits();
    }

    public async Task CleanCommits()
    {
        var currentVersion = await _currentReleaseVersion.GetCurrentTag();
        string? sinceTag = currentVersion != default ? currentVersion.ToString(3) : null;
        var history = await _getTrackCommitMessages.GetHistory(sinceTag);
        if (history.Count <= 1) return;
        var lastCommit = history.First();
        // if (lastCommit.CountDirtyToken() > 0 && lastCommit.CountReleaseToken() <= 0)
        // {
        //     System.Console.Error.WriteLine($"Last Commit is not Release Token");
        //     return;
        // }
        Dictionary<TrackerReleaseType, List<string>> tokenMessages = new()
        {
            [TrackerReleaseType.SecurityBreaking] = new List<string>(),
            [TrackerReleaseType.FeatureBreaking] = new List<string>(),
            [TrackerReleaseType.FixBreaking] = new List<string>(),
            [TrackerReleaseType.NoteBreaking] = new List<string>(),
            [TrackerReleaseType.SecurityImportant] = new List<string>(),
            [TrackerReleaseType.FeatureImportant] = new List<string>(),
            [TrackerReleaseType.FixImportant] = new List<string>(),
            [TrackerReleaseType.NoteImportant] = new List<string>(),
            [TrackerReleaseType.Feature] = new List<string>(),
            [TrackerReleaseType.Security] = new List<string>(),
            [TrackerReleaseType.Fix] = new List<string>(),
            [TrackerReleaseType.Note] = new List<string>(),
            [TrackerReleaseType.SponsorImportant] = new List<string>(),
            [TrackerReleaseType.Sponsor] = new List<string>(),
            [TrackerReleaseType.ThanksImportant] = new List<string>(),
            [TrackerReleaseType.Thanks] = new List<string>(),
        };

        Dictionary<TrackerDirtyType, List<string>> tokenDirtyMessages = new()
        {
            [TrackerDirtyType.TraceNoTrigger] = new List<string>(),
            [TrackerDirtyType.WorkInProgress] = new List<string>(),
        };

        TrackedCommitMessageStructure? resetPoint = default;
        var commits = history.ToList();
        foreach (var item in commits) // ss
        {
            if (item != lastCommit)
                if (item.CountReleaseToken() <= 0 || item.CountDirtyToken() > 0)
                    resetPoint = item;
                else
                    break;

            foreach (var itemTokens in item.TokenMessages)
            {
                var toAdd = itemTokens.Value.Where(p => !tokenMessages[itemTokens.Key].Contains(p, StringComparer.OrdinalIgnoreCase))
                    .Select(p => AddReleaseTokenToMessage(itemTokens.Key, p));
                tokenMessages[itemTokens.Key].AddRange(toAdd);
            }
            foreach (var itemTokens in item.TokenDirtyMessages)
            {
                var toAdd = itemTokens.Value.Where(p => !tokenDirtyMessages[itemTokens.Key].Contains(p, StringComparer.OrdinalIgnoreCase))
                    .Select(p => AddDirtyTokenToMessage(itemTokens.Key, p));
                tokenDirtyMessages[itemTokens.Key].AddRange(toAdd);
            }

        }
        if (resetPoint == default || resetPoint == lastCommit)
        {
            System.Console.Error.WriteLine("No Reset Required Yet.");
            return;
        }

        var repo = new Repository(Path.Combine(Environment.CurrentDirectory));
        var commit = repo.Lookup<Commit>(resetPoint.CommitId); ;

        var nMessages = tokenMessages.SelectMany(p => p.Value).ToList();
        if (lastCommit.TokenDirtyMessages[TrackerDirtyType.WorkInProgress].Count > 0)
            foreach (var wip in lastCommit.TokenDirtyMessages[TrackerDirtyType.WorkInProgress])
                nMessages.Add(AddDirtyTokenToMessage(TrackerDirtyType.WorkInProgress, wip));

        if (lastCommit.TokenDirtyMessages[TrackerDirtyType.TraceNoTrigger].Count > 0)
            nMessages.Add(AddDirtyTokenToMessage(TrackerDirtyType.TraceNoTrigger, lastCommit.TokenDirtyMessages[TrackerDirtyType.TraceNoTrigger].First()));

        var nCommitMessage = string.Join("\n", nMessages);
        if (string.IsNullOrWhiteSpace(nCommitMessage))
            throw new Exception("Couldn't extract messages.");


        repo.Reset(ResetMode.Mixed, commit);
        Commands.Stage(repo, "*");
        var committer = repo.Config.BuildSignature(DateTimeOffset.Now);
        var cleanCommit = repo.Commit(nCommitMessage, committer, committer, new()
        {
            AllowEmptyCommit = true,
            AmendPreviousCommit = true,
            PrettifyMessage = true,
        });


        // System.Console.WriteLine($"New Commit with work  {cleanCommit.Message}");
        // System.Console.WriteLine($"New Commit SHA-1 ID: {cleanCommit.Id.Sha}");
        // System.Console.WriteLine($"Committed by: {cleanCommit.Committer.Name} at {cleanCommit.Committer.When}");
    }

    private string AddReleaseTokenToMessage(TrackerReleaseType tokenType, string message)
    {
        string token = string.Empty;
        if (tokenType == TrackerReleaseType.SecurityBreaking)
            token = $"{ToolConfig.ReleaseSecurityTokens[0]}!";
        else if (tokenType == TrackerReleaseType.SecurityImportant)
            token = $"{ToolConfig.ReleaseSecurityTokens[0]}~";
        else if (tokenType == TrackerReleaseType.Security)
            token = ToolConfig.ReleaseSecurityTokens[0];
        else if (tokenType == TrackerReleaseType.FeatureBreaking)
            token = $"{ToolConfig.ReleaseFeatureTokens[0]}!";
        else if (tokenType == TrackerReleaseType.FeatureImportant)
            token = $"{ToolConfig.ReleaseFeatureTokens[0]}~";
        else if (tokenType == TrackerReleaseType.Feature)
            token = ToolConfig.ReleaseFeatureTokens[0];
        else if (tokenType == TrackerReleaseType.Fix)
            token = ToolConfig.ReleaseFixTokens[0];
        else if (tokenType == TrackerReleaseType.FixBreaking)
            token = $"{ToolConfig.ReleaseFixTokens[0]}!";
        else if (tokenType == TrackerReleaseType.FixImportant)
            token = $"{ToolConfig.ReleaseFixTokens[0]}~";
        else if (tokenType == TrackerReleaseType.Sponsor)
            token = ToolConfig.ReleaseSponsorTokens[0];
        else if (tokenType == TrackerReleaseType.SponsorImportant)
            token = $"{ToolConfig.ReleaseSponsorTokens[0]}~";
        else if (tokenType == TrackerReleaseType.Thanks)
            token = ToolConfig.ReleaseThanksTokens[0];
        else if (tokenType == TrackerReleaseType.ThanksImportant)
            token = $"{ToolConfig.ReleaseThanksTokens[0]}~";
        else if (tokenType == TrackerReleaseType.NoteBreaking)
            token = $"{ToolConfig.ReleaseNoteTokens[0]}!";
        else if (tokenType == TrackerReleaseType.NoteImportant)
            token = $"{ToolConfig.ReleaseNoteTokens[0]}~";
        else
            token = ToolConfig.ReleaseNoteTokens[0];
        return AddTokenKeyToMessage(token, message);
    }
    private string AddDirtyTokenToMessage(TrackerDirtyType tokenType, string message)
    {
        string token = string.Empty;
        if (tokenType == TrackerDirtyType.TraceNoTrigger)
            token = ToolConfig.ReleaseNoTriggerTokens[0];
        else
            token = ToolConfig.ReleaseDirtyTokens[0];
        return AddTokenKeyToMessage(token, message);
    }
    private string AddTokenKeyToMessage(string tokenKey, string message)
    {
        string finalOut = string.Empty;
        string token = tokenKey;

        finalOut += $"{token}: {message}";
        return finalOut;
    }


}
