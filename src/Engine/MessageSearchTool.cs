using System;
using System.Text.RegularExpressions;
using DeployItSmart.Model.Configuration.Runner;
using DeployItSmart.Model.DataStructure.Enums;

namespace DeployItSmart.Engine;

public class MessageSearchTool
{
    public ToolConfigLoader ToolConfig { get; init; }
    public List<string> BreakSearchPatterns { get; init; }
    public List<string> FeatureSearchPatterns { get; init; }
    public List<string> ThanksSearchPatterns { get; init; }
    public List<string> SponsorSearchPatterns { get; init; }
    public List<string> FixSearchPatterns { get; init; }
    public List<string> NoteSearchPatterns { get; init; }
    public List<string> WipSearchPatterns { get; init; }
    public List<string> TraceSearchPatterns { get; init; }
    public List<string> TraceNoTriggerSearchPatterns { get; init; }

    public MessageSearchTool()
    {
        ToolConfig = new ToolConfigLoader();
        BreakSearchPatterns = GenerateTokenMatchPatterns(TrackerReleaseType.Breaking);
        FeatureSearchPatterns = GenerateTokenMatchPatterns(TrackerReleaseType.Feature);
        FixSearchPatterns = GenerateTokenMatchPatterns(TrackerReleaseType.Fix);
        ThanksSearchPatterns = GenerateTokenMatchPatterns(TrackerReleaseType.Thanks);
        SponsorSearchPatterns = GenerateTokenMatchPatterns(TrackerReleaseType.Sponsor);
        NoteSearchPatterns = GenerateTokenMatchPatterns(TrackerReleaseType.Note);
        WipSearchPatterns = GenerateTokenMatchPatterns(TrackerDirtyType.WorkInProgress);
        TraceSearchPatterns = GenerateTokenMatchPatterns(TrackerDirtyType.Trace);
        TraceNoTriggerSearchPatterns = GenerateTokenMatchPatterns(TrackerDirtyType.TraceNoTrigger);
    }

    private List<string> GenerateTokenMatchPatterns(TrackerReleaseType releaseType)
    {
        string[] tokens = [];
        switch (releaseType)
        {
            case TrackerReleaseType.Breaking:
                if (BreakSearchPatterns != default) return BreakSearchPatterns;
                tokens = ToolConfig.Configuration.ReleaseBreakingTokens.ToArray();
                break;
            case TrackerReleaseType.Feature:
                if (FeatureSearchPatterns != default) return FeatureSearchPatterns;
                tokens = ToolConfig.Configuration.ReleaseFeatureTokens.ToArray();
                break;
            case TrackerReleaseType.Fix:
                if (FixSearchPatterns != default) return FixSearchPatterns;
                tokens = ToolConfig.Configuration.ReleaseFixTokens.ToArray();
                break;
            case TrackerReleaseType.Sponsor:
                if (SponsorSearchPatterns != default) return SponsorSearchPatterns;
                tokens = ToolConfig.Configuration.ReleaseSponsorTokens.ToArray();
                break;
            case TrackerReleaseType.Thanks:
                if (ThanksSearchPatterns != default) return ThanksSearchPatterns;
                tokens = ToolConfig.Configuration.ReleaseThanksTokens.ToArray();
                break;
            default:
                if (NoteSearchPatterns != default) return NoteSearchPatterns;
                tokens = ToolConfig.Configuration.ReleaseNoteTokens.ToArray();
                break;
        }
        List<string> tokenPatterns = new();
        foreach (var item in tokens)
            tokenPatterns.Add(GenerateTokenMatchPattern(item));
        return tokenPatterns;
    }

    private List<string> GenerateTokenMatchPatterns(TrackerDirtyType trackerDirty)
    {
        string[] tokens = [];
        switch (trackerDirty)
        {
            case TrackerDirtyType.Trace:
                if (TraceSearchPatterns != default) return TraceSearchPatterns;
                tokens = ToolConfig.Configuration.ReleaseNoLogTokens.ToArray();
                break;
            case TrackerDirtyType.TraceNoTrigger:
                if (TraceNoTriggerSearchPatterns != default) return TraceNoTriggerSearchPatterns;
                tokens = ToolConfig.Configuration.ReleaseNoTriggerTokens.ToArray();
                break;
            default:
                if (WipSearchPatterns != default) return WipSearchPatterns;
                tokens = ToolConfig.Configuration.ReleaseDirtyTokens.ToArray();
                break;
        }
        List<string> tokenPatterns = new();
        foreach (var item in tokens)
            tokenPatterns.Add(GenerateTokenMatchPattern(item));
        return tokenPatterns;
    }
    private string GenerateTokenMatchPattern(string token)
    {
        string pattern = string.Empty;
        bool rightOnly = ToolConfig.Configuration.ReleaseRightSymbol != string.Empty && ToolConfig.Configuration.ReleaseLeftSymbol == string.Empty;
        bool leftOnly = ToolConfig.Configuration.ReleaseRightSymbol == string.Empty && ToolConfig.Configuration.ReleaseLeftSymbol != string.Empty;
        bool wrapper = ToolConfig.Configuration.ReleaseRightSymbol != string.Empty && ToolConfig.Configuration.ReleaseLeftSymbol != string.Empty;
        string right = Regex.Escape(ToolConfig.Configuration.ReleaseRightSymbol);
        string left = Regex.Escape(ToolConfig.Configuration.ReleaseLeftSymbol);
        token = Regex.Escape(token);
        if (rightOnly)
            pattern = @$"\b({token})\s*{right}\s*((?:[^\n]|(?!\b{token}\s*:))*)";
        else if (leftOnly)
            pattern = @$"{left}({token})\s+((?:(?!-{token}\s)[^\n])*)";
        else if (wrapper)
            pattern = @$"{right}({token}){left}\s*((?:(?!\[Trace\])[^\n])*)";
        else
            throw new Exception("Misconfigured ReleaseLeftSymbol or ReleaseRightSymbol.");
        return pattern;
    }

    private List<string> GetMatches(string pattern, string target)
    {
        var matches = Regex.Matches(target, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        if (matches.Count <= 0) return [];
        return matches.Select(p => p.Groups[2].Value).ToList();
    }

    private KeyValuePair<TrackerDirtyType, List<string>> ExtractDirtyTypeFor(TrackerDirtyType trackerType, string message)
    {
        List<string> patterns = GenerateTokenMatchPatterns(trackerType);
        var result = new KeyValuePair<TrackerDirtyType, List<string>>(trackerType, new());
        foreach (var pattern in patterns)
            result.Value.AddRange(GetMatches(pattern, message));

        return result;
    }

    private KeyValuePair<TrackerReleaseType, List<string>> ExtractReleaseTypeFor(TrackerReleaseType trackerType, string message)
    {
        List<string> patterns = GenerateTokenMatchPatterns(trackerType);
        var result = new KeyValuePair<TrackerReleaseType, List<string>>(trackerType, new());
        foreach (var pattern in patterns)
            result.Value.AddRange(GetMatches(pattern, message));
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
