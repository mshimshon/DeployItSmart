using System;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks.Dataflow;
using DeployItSmart.Engine.ArgumentParser;
using DeployItSmart.Model.Configuration.Runner;
using DeployItSmart.Model.DataStructure;
using DeployItSmart.Model.DataStructure.Enums;
using LibGit2Sharp;

namespace DeployItSmart.Engine.Runner;

public class GetTrackCommitMessagesRunner : BaseRunner
{
    private JsonSerializerOptions jsonSerializerOption = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles

    };
    public GetTrackCommitMessagesConf Configuration { get; init; }
    public GetCommitInfoSinceRunner GetCommitInfoSince { get; init; }
    public MessageSearchTool MessageTool { get; init; }
    private CommitFilter gitCommitFilterOldestFirst = new CommitFilter()
    {
        SortBy = CommitSortStrategies.Reverse
    };


    public GetTrackCommitMessagesRunner(SimpleArgumentParser simpleArgument)
    {

        Configuration = new GetTrackCommitMessagesConf(simpleArgument);
        GetCommitInfoSince = new GetCommitInfoSinceRunner(simpleArgument);
        MessageTool = new MessageSearchTool();
    }

    public override async Task RunAsync()
    {
        var commits = await GetHistory(GetCommitInfoSince.Configuration.SinceTag);
        string json = JsonSerializer.Serialize(commits, jsonSerializerOption);
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
            [TrackerReleaseType.Breaking] = new List<string>(),
            [TrackerReleaseType.Feature] = new List<string>(),
            [TrackerReleaseType.Fix] = new List<string>(),
            [TrackerReleaseType.Note] = new List<string>(),
            [TrackerReleaseType.Sponsor] = new List<string>(),
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
            ProcessDuplicateTrackItem(TrackerReleaseType.Breaking, uncleanItem.TokenMessages, ref processedTokens, ref cleanedTokenMessages);
            ProcessDuplicateTrackItem(TrackerReleaseType.Feature, uncleanItem.TokenMessages, ref processedTokens, ref cleanedTokenMessages);
            ProcessDuplicateTrackItem(TrackerReleaseType.Fix, uncleanItem.TokenMessages, ref processedTokens, ref cleanedTokenMessages);
            ProcessDuplicateTrackItem(TrackerReleaseType.Note, uncleanItem.TokenMessages, ref processedTokens, ref cleanedTokenMessages);
            ProcessDuplicateTrackItem(TrackerReleaseType.Thanks, uncleanItem.TokenMessages, ref processedTokens, ref cleanedTokenMessages);
            ProcessDuplicateTrackItem(TrackerReleaseType.Sponsor, uncleanItem.TokenMessages, ref processedTokens, ref cleanedTokenMessages);
            cleanedData.Add(cleanedItem with
            {
                TokenMessages = cleanedTokenMessages
            });
        }
        return cleanedData;
    }
    public async Task<List<TrackedCommitMessageStructure>> GetHistory(string? sinceTag)
    {
        var result = await GetCommitInfoSince.GetHistory(sinceTag);
        List<TrackedCommitMessageStructure> uncleanData = new();
        foreach (CommitInfoStructure item in result)
        {
            var entry = new TrackedCommitMessageStructure()
            {
                TokenMessages = MessageTool.FindReleaseTokens(item.CommitMessage),
                TokenDirtyMessages = MessageTool.FindDirtyTokens(item.CommitMessage),
                AffectedFiles = item.AffectedFiles,
                Author = item.Author,
                CommitId = item.CommitId,
                OriginalMessage = item.CommitMessage
            };
            uncleanData.Add(entry);
        }

        return Configuration.KeepDuplicate ? uncleanData : RemoveDuplicatesMessages(uncleanData);
    }
}
