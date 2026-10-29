using System;
using System.Text.RegularExpressions;
using DeployItSmart.Model.Configuration.Runner;
using DeployItSmart.Model.DataStructure.Enums;

namespace DeployItSmart.Engine;

public class MessageSearchTool
{
    public ToolConfigLoader ToolConfig { get; init; }
    private string _tokenMatchPattern = @"([^\s:]+)\s*:\s*([\s\S]*?)(?=\s+[^\s:]+\s*:|$)";
    public MessageSearchTool()
    {
        ToolConfig = new ToolConfigLoader();
    }

    private string[] GetTokenMatchPatterns(TrackerReleaseType releaseType)
    {
        string[] tokens = [];
        switch (releaseType)
        {
            case TrackerReleaseType.Breaking:
                return ToolConfig.Configuration.ReleaseBreakingTokens.ToArray();
            case TrackerReleaseType.Feature:
                return ToolConfig.Configuration.ReleaseFeatureTokens.ToArray();
            case TrackerReleaseType.Fix:
                return ToolConfig.Configuration.ReleaseFixTokens.ToArray();
            case TrackerReleaseType.Sponsor:
                return ToolConfig.Configuration.ReleaseSponsorTokens.ToArray();
            case TrackerReleaseType.Thanks:
                return ToolConfig.Configuration.ReleaseThanksTokens.ToArray();
            default:
                return ToolConfig.Configuration.ReleaseNoteTokens.ToArray();
        }
    }

    private string[] GetTokenMatchPatterns(TrackerDirtyType trackerDirty)
    {
        string[] tokens = [];
        switch (trackerDirty)
        {
            case TrackerDirtyType.Trace:
                return ToolConfig.Configuration.ReleaseNoLogTokens.ToArray();
            case TrackerDirtyType.TraceNoTrigger:
                return ToolConfig.Configuration.ReleaseNoTriggerTokens.ToArray();
            default:
                return ToolConfig.Configuration.ReleaseDirtyTokens.ToArray();
        }
    }


    private List<string> GetMatches(string key, string target)
    {
        var matches = Regex.Matches(target, _tokenMatchPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        if (matches.Count <= 0) return [];
        return matches.Where(p => key.Equals(p.Groups[1].Value, StringComparison.OrdinalIgnoreCase)).Select(p => p.Groups[2].Value).ToList();
    }

    private KeyValuePair<TrackerDirtyType, List<string>> ExtractDirtyTypeFor(TrackerDirtyType trackerType, string message)
    {
        var result = new KeyValuePair<TrackerDirtyType, List<string>>(trackerType, new());
        foreach (var token in GetTokenMatchPatterns(trackerType))
            result.Value.AddRange(GetMatches(token, message));
        return result;
    }

    private KeyValuePair<TrackerReleaseType, List<string>> ExtractReleaseTypeFor(TrackerReleaseType trackerType, string message)
    {
        var result = new KeyValuePair<TrackerReleaseType, List<string>>(trackerType, new());
        foreach (var token in GetTokenMatchPatterns(trackerType))
            result.Value.AddRange(GetMatches(token, message));
        return result;
    }
    public Dictionary<TrackerDirtyType, List<string>> FindDirtyTokens(string message)
    {
        List<KeyValuePair<TrackerDirtyType, List<string>>> dirtyMessages = [
            ExtractDirtyTypeFor(TrackerDirtyType.WorkInProgress, message),
            ExtractDirtyTypeFor(TrackerDirtyType.TraceNoTrigger, message),
            ExtractDirtyTypeFor(TrackerDirtyType.Trace, message),
        ];
        return dirtyMessages.ToDictionary(p => p.Key, p => p.Value);
    }

    public Dictionary<TrackerReleaseType, List<string>> FindReleaseTokens(string message)
    {
        List<KeyValuePair<TrackerReleaseType, List<string>>> result = [
            ExtractReleaseTypeFor(TrackerReleaseType.Breaking, message),
            ExtractReleaseTypeFor(TrackerReleaseType.Feature, message),
            ExtractReleaseTypeFor(TrackerReleaseType.Fix, message),
            ExtractReleaseTypeFor(TrackerReleaseType.Note, message),
            ExtractReleaseTypeFor(TrackerReleaseType.Sponsor, message),
            ExtractReleaseTypeFor(TrackerReleaseType.Thanks, message),
        ];
        return result.ToDictionary(p => p.Key, p => p.Value);
    }
}
