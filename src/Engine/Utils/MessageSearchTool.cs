using System;
using System.Text.RegularExpressions;
using DeployItSmart.Engine.Configuration;
using DeployItSmart.Model.DataStructure.Enums;
using static DeployItSmart.Engine.Configuration.ConfigurationRunner;
namespace DeployItSmart.Engine.Utils;

public class MessageSearchTool
{
    private ToolConf ToolConfig => GetToolConfiguration();
    private string _tokenMatchPattern = @"([^\s:]+)\s*:\s*([\s\S]*?)(?=\s+[^\s:]+\s*:|$)";
    public MessageSearchTool()
    {
    }

    private string[] GetTokenMatchPatterns(TrackerReleaseType releaseType)
    {
        string[] tokens = [];
        switch (releaseType)
        {
            case TrackerReleaseType.SecurityBreaking:
                return ToolConfig.ReleaseSecurityTokens.Select(p => $"{p}!").ToArray();
            case TrackerReleaseType.SecurityImportant:
                return ToolConfig.ReleaseSecurityTokens.Select(p => $"{p}~").ToArray();
            case TrackerReleaseType.Security:
                return ToolConfig.ReleaseSecurityTokens.ToArray();
            case TrackerReleaseType.FeatureBreaking:
                return ToolConfig.ReleaseFeatureTokens.Select(p => $"{p}!").ToArray();
            case TrackerReleaseType.FeatureImportant:
                return ToolConfig.ReleaseFeatureTokens.Select(p => $"{p}~").ToArray();
            case TrackerReleaseType.Feature:
                return ToolConfig.ReleaseFeatureTokens.ToArray();
            case TrackerReleaseType.Fix:
                return ToolConfig.ReleaseFixTokens.ToArray();
            case TrackerReleaseType.FixBreaking:
                return ToolConfig.ReleaseFixTokens.Select(p => $"{p}!").ToArray();
            case TrackerReleaseType.FixImportant:
                return ToolConfig.ReleaseFixTokens.Select(p => $"{p}~").ToArray();
            case TrackerReleaseType.Sponsor:
                return ToolConfig.ReleaseSponsorTokens.ToArray();
            case TrackerReleaseType.SponsorImportant:
                return ToolConfig.ReleaseSponsorTokens.Select(p => $"{p}~").ToArray();
            case TrackerReleaseType.Thanks:
                return ToolConfig.ReleaseThanksTokens.ToArray();
            case TrackerReleaseType.ThanksImportant:
                return ToolConfig.ReleaseThanksTokens.Select(p => $"{p}~").ToArray();
            case TrackerReleaseType.NoteBreaking:
                return ToolConfig.ReleaseNoteTokens.Select(p => $"{p}!").ToArray();
            case TrackerReleaseType.NoteImportant:
                return ToolConfig.ReleaseNoteTokens.Select(p => $"{p}~").ToArray();
            default:
                return ToolConfig.ReleaseNoteTokens.ToArray();
        }
    }

    private string[] GetTokenMatchPatterns(TrackerDirtyType trackerDirty)
    {
        string[] tokens = [];
        switch (trackerDirty)
        {
            case TrackerDirtyType.TraceNoTrigger:
                return ToolConfig.ReleaseNoTriggerTokens.ToArray();
            default:
                return ToolConfig.ReleaseDirtyTokens.ToArray();
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
        ];
        return dirtyMessages.ToDictionary(p => p.Key, p => p.Value);
    }

    public Dictionary<TrackerReleaseType, List<string>> FindReleaseTokens(string message)
    {
        List<KeyValuePair<TrackerReleaseType, List<string>>> result = [
            ExtractReleaseTypeFor(TrackerReleaseType.SecurityBreaking, message),
            ExtractReleaseTypeFor(TrackerReleaseType.FeatureBreaking, message),
            ExtractReleaseTypeFor(TrackerReleaseType.FixBreaking, message),
            ExtractReleaseTypeFor(TrackerReleaseType.NoteBreaking, message),

            ExtractReleaseTypeFor(TrackerReleaseType.SecurityImportant, message),
            ExtractReleaseTypeFor(TrackerReleaseType.FeatureImportant, message),
            ExtractReleaseTypeFor(TrackerReleaseType.FixImportant, message),
            ExtractReleaseTypeFor(TrackerReleaseType.NoteImportant, message),

            ExtractReleaseTypeFor(TrackerReleaseType.Feature, message),
            ExtractReleaseTypeFor(TrackerReleaseType.Security, message),
            ExtractReleaseTypeFor(TrackerReleaseType.Fix, message),
            ExtractReleaseTypeFor(TrackerReleaseType.Note, message),

            ExtractReleaseTypeFor(TrackerReleaseType.SponsorImportant, message),
            ExtractReleaseTypeFor(TrackerReleaseType.Sponsor, message),

            ExtractReleaseTypeFor(TrackerReleaseType.ThanksImportant, message),
            ExtractReleaseTypeFor(TrackerReleaseType.Thanks, message),
        ];
        return result.ToDictionary(p => p.Key, p => p.Value);
    }
}
