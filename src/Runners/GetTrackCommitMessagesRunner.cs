using System;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks.Dataflow;
using DeployItSmart.Engine.ArgumentParser;
using DeployItSmart.Engine.Configuration;
using DeployItSmart.Model.Configuration;
using DeployItSmart.Engine.Utils;
using DeployItSmart.Model.DataStructure;
using DeployItSmart.Model.DataStructure.Enums;
using LibGit2Sharp;
using static DeployItSmart.Engine.Configuration.ConfigurationRunner;

namespace DeployItSmart.Engine.Runner;

public class GetTrackCommitMessagesRunner : IRunner
{
    private readonly JsonSerializerOptions _jsonSerializerOption = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles

    };
    private ToolConf ToolConfig => GetToolConfiguration();

    private CommitTrackerParameters CommitTrackerConfig => GetConfigurationFor<CommitTrackerParameters>();
    private CommitSearchParameters CommitSearchConfig => GetConfigurationFor<CommitSearchParameters>();
    private readonly GetCommitInfoSinceRunner _getCommitInfoSince;
    private readonly MessageSearchTool _messageTool;
    private readonly CommitFilter _gitCommitFilterOldestFirst = new CommitFilter()
    {
        SortBy = CommitSortStrategies.Reverse
    };


    public GetTrackCommitMessagesRunner()
    {
        _getCommitInfoSince = new GetCommitInfoSinceRunner();
        _messageTool = new MessageSearchTool();
    }
    public static IRunner Create() => new GetTrackCommitMessagesRunner();
    public static string GetCommandName() => nameof(GetTrackCommitMessagesRunner);
    public async Task RunAsync()
    {
        var commits = await GetHistory(CommitSearchConfig.SinceTag);
        string json = JsonSerializer.Serialize(commits, _jsonSerializerOption);
        Console.WriteLine(json);
    }
    private void ProcessDuplicateTrackItem(
        TrackerReleaseType target,
        Dictionary<TrackerReleaseType, List<string>> uncleanTokenMessages,
        ref Dictionary<TrackerReleaseType, List<string>> processedTokens,
        ref Dictionary<TrackerReleaseType, List<string>> cleanedTokenMessages)
    {
        foreach (var trackItem in uncleanTokenMessages[target] ?? [])
            if (!processedTokens[target].Contains(trackItem, StringComparer.OrdinalIgnoreCase))
            {
                processedTokens[target].Add(trackItem);
                cleanedTokenMessages[target].Add(trackItem);
            }
    }
    private List<TrackedCommitMessageStructure> RemoveDuplicatesMessages(List<TrackedCommitMessageStructure> uncleanedList)
    {
        List<TrackedCommitMessageStructure> cleanedData = new();
        Dictionary<TrackerReleaseType, List<string>> processedTokens = new()
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
        foreach (var uncleanItem in uncleanedList)
        {
            var cleanedItem = new TrackedCommitMessageStructure()
            {
                AffectedFiles = uncleanItem.AffectedFiles,
                Author = uncleanItem.Author,
                CommitId = uncleanItem.CommitId,
                TokenDirtyMessages = uncleanItem.TokenDirtyMessages,
                OriginalMessage = uncleanItem.OriginalMessage
            };
            Dictionary<TrackerReleaseType, List<string>> cleanedTokenMessages = cleanedItem.TokenMessages;
            // 1. MAJOR / BREAKING CHANGES (Highest Priority)
            ProcessDuplicateTrackItem(TrackerReleaseType.SecurityBreaking, uncleanItem.TokenMessages, ref processedTokens, ref cleanedTokenMessages);
            ProcessDuplicateTrackItem(TrackerReleaseType.FeatureBreaking, uncleanItem.TokenMessages, ref processedTokens, ref cleanedTokenMessages);
            ProcessDuplicateTrackItem(TrackerReleaseType.FixBreaking, uncleanItem.TokenMessages, ref processedTokens, ref cleanedTokenMessages);
            ProcessDuplicateTrackItem(TrackerReleaseType.NoteBreaking, uncleanItem.TokenMessages, ref processedTokens, ref cleanedTokenMessages);

            // 2. MINOR / IMPORTANT CHANGES (Medium Priority)
            ProcessDuplicateTrackItem(TrackerReleaseType.SecurityImportant, uncleanItem.TokenMessages, ref processedTokens, ref cleanedTokenMessages);
            ProcessDuplicateTrackItem(TrackerReleaseType.FeatureImportant, uncleanItem.TokenMessages, ref processedTokens, ref cleanedTokenMessages);
            ProcessDuplicateTrackItem(TrackerReleaseType.FixImportant, uncleanItem.TokenMessages, ref processedTokens, ref cleanedTokenMessages);
            ProcessDuplicateTrackItem(TrackerReleaseType.NoteImportant, uncleanItem.TokenMessages, ref processedTokens, ref cleanedTokenMessages);
            ProcessDuplicateTrackItem(TrackerReleaseType.Feature, uncleanItem.TokenMessages, ref processedTokens, ref cleanedTokenMessages);

            // 3. PATCH / STANDARD CHANGES (Standard Priority)
            ProcessDuplicateTrackItem(TrackerReleaseType.Security, uncleanItem.TokenMessages, ref processedTokens, ref cleanedTokenMessages);
            ProcessDuplicateTrackItem(TrackerReleaseType.Fix, uncleanItem.TokenMessages, ref processedTokens, ref cleanedTokenMessages);
            ProcessDuplicateTrackItem(TrackerReleaseType.Note, uncleanItem.TokenMessages, ref processedTokens, ref cleanedTokenMessages);

            // 4. METADATA & COMMUNITY (Lowest Priority)
            ProcessDuplicateTrackItem(TrackerReleaseType.SponsorImportant, uncleanItem.TokenMessages, ref processedTokens, ref cleanedTokenMessages);
            ProcessDuplicateTrackItem(TrackerReleaseType.Sponsor, uncleanItem.TokenMessages, ref processedTokens, ref cleanedTokenMessages);
            ProcessDuplicateTrackItem(TrackerReleaseType.ThanksImportant, uncleanItem.TokenMessages, ref processedTokens, ref cleanedTokenMessages);
            ProcessDuplicateTrackItem(TrackerReleaseType.Thanks, uncleanItem.TokenMessages, ref processedTokens, ref cleanedTokenMessages);

            cleanedData.Add(cleanedItem with
            {
                TokenMessages = cleanedTokenMessages
            });
        }
        return cleanedData;
    }
    public async Task<List<TrackedCommitMessageStructure>> GetHistory(string? sinceTag)
    {
        var result = await _getCommitInfoSince.GetHistory(sinceTag);
        List<TrackedCommitMessageStructure> uncleanData = new();
        foreach (CommitInfoStructure item in result)
        {
            var entry = new TrackedCommitMessageStructure()
            {
                TokenMessages = _messageTool.FindReleaseTokens(item.CommitMessage),
                TokenDirtyMessages = _messageTool.FindDirtyTokens(item.CommitMessage),
                AffectedFiles = item.AffectedFiles,
                Author = item.Author,
                CommitId = item.CommitId,
                OriginalMessage = item.CommitMessage
            };
            uncleanData.Add(entry);
        }

        return CommitTrackerConfig.KeepDuplicate ? uncleanData : RemoveDuplicatesMessages(uncleanData);
    }
}
