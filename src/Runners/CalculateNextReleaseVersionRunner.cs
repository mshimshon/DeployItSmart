using DeployItSmart.Engine.Utils;
using DeployItSmart.Engine.Configuration;
using DeployItSmart.Engine.Runner;
using DeployItSmart.Model.Configuration;
using static DeployItSmart.Engine.Configuration.ConfigurationRunner;
using DeployItSmart.Model.DataStructure;
using DeployItSmart.Model.DataStructure.Enums;
using NuGet.Versioning;

namespace DeployItSmart.Runners;

public class CalculateNextReleaseVersionRunner : IRunner
{
    private ToolConf ToolConfig => GetToolConfiguration();
    private CommitSearchParameters CommitSearchConfig => GetConfigurationFor<CommitSearchParameters>();

    private readonly GetCurrentReleaseVersionRunner _currentReleaseVersion;
    private readonly GetCurrentPreReleaseVersionRunner _getCurrentPreReleaseVersion;
    private readonly GetTrackCommitMessagesRunner _getTrackCommitMessages;
    private readonly MessageSearchTool _messageTool;
    public CalculateNextReleaseVersionRunner()
    {
        _getCurrentPreReleaseVersion = new GetCurrentPreReleaseVersionRunner();
        _currentReleaseVersion = new GetCurrentReleaseVersionRunner();
        _getTrackCommitMessages = new GetTrackCommitMessagesRunner();
        _messageTool = new MessageSearchTool();
    }

    public async Task RunAsync()
    {
        var version = await GetNextVersion();
        Console.WriteLine(version);
    }

    public async Task<NuGetVersion> GetNextVersion()
    {
        var latestStable = await _currentReleaseVersion.GetCurrentTag();
        var latestPreview = await _getCurrentPreReleaseVersion.GetCurrentPreviewTag();
        var IsFirstRelease = CommitSearchConfig.IsFirstRelease;
        var IsAllowingUnsafe = CommitSearchConfig.AllowUnsafeVersionCalculation;
        var tagSince = CommitSearchConfig.SinceTag;
        if (tagSince != default && IsAllowingUnsafe)
            latestStable = NuGetVersion.Parse(tagSince);

        if (latestStable == default && !IsFirstRelease)
            throw new Exception("--IsFirstRelease not active for protection we block continuing.");
        if (latestStable == default)
            return NuGetVersion.Parse(ToolConfig.InitialVersion);
        var commits = await _getTrackCommitMessages.GetHistory(latestStable.ToString());
        int major = 0, minor = 0;
        if (HasAnyBreakingChanges(commits)) major = 1;
        else
        {
            int patch = CountPatchChanges(commits);
            minor = CountMinorChanges(commits);
            int earnedMinors = patch / ToolConfig.PatchRolloverMinor;
            int combinedMinors = minor + earnedMinors;
            int earnedMajors = combinedMinors / ToolConfig.MinorRolloverMajor;
            minor = combinedMinors - (earnedMajors * ToolConfig.MinorRolloverMajor);
            major = major + earnedMajors;
        }



        NuGetVersion? nextVersion;
        if (major > 0)
            nextVersion = new(latestStable.Major + 1, 0, 0);
        else if (minor > 0)
            nextVersion = new(latestStable.Major, latestStable.Minor + 1, 0);
        else
            nextVersion = new(latestStable.Major, latestStable.Minor, latestStable.Patch + 1);

        /*
            If this hit it mean someone change the git history on the main tracking branch effectively destroyed the version calculation
        */
        if (latestPreview != default && latestPreview > nextVersion)
            throw new Exception($"Last Preview is greater than next logical release this should be impossible unless the git tag history got changed manually cannot proceed next stable = {nextVersion} vs current preview = {latestPreview}");
        if (CommitSearchConfig.IsPreviewMode && (latestPreview == default || latestPreview < latestStable))
        {
            var releaseLabels = new[] { CommitSearchConfig.PreviewMark, "1" };
            nextVersion = new(nextVersion.Major, nextVersion.Minor, nextVersion.Patch, releaseLabels!, null);

        }
        else if (CommitSearchConfig.IsPreviewMode && latestPreview != default && latestPreview >= latestStable)
        {
            nextVersion = new(nextVersion.Major, nextVersion.Minor, nextVersion.Patch, latestPreview.ReleaseLabels, null);
            nextVersion = nextVersion.IncrementPrelease();
        }

        return nextVersion;
    }
    private bool HasAnyBreakingChanges(List<TrackedCommitMessageStructure> commits)
    {
        foreach (var item in commits)
            if (item.TokenMessages[TrackerReleaseType.FeatureBreaking].Count() > 0)
                return true;
            else if (item.TokenMessages[TrackerReleaseType.NoteBreaking].Count() > 0)
                return true;
            else if (item.TokenMessages[TrackerReleaseType.FixBreaking].Count() > 0)
                return true;
            else if (item.TokenMessages[TrackerReleaseType.SecurityBreaking].Count() > 0)
                return true;
        return false;
    }

    private int CountMinorChanges(List<TrackedCommitMessageStructure> commits)
    {
        int count = 0;
        foreach (var item in commits)
        {
            count += item.TokenMessages[TrackerReleaseType.Feature].Count();
            count += item.TokenMessages[TrackerReleaseType.FeatureImportant].Count();
        }
        return count;
    }

    private int CountPatchChanges(List<TrackedCommitMessageStructure> commits)
    {
        int count = 0;
        foreach (var item in commits)
        {
            count += item.TokenMessages[TrackerReleaseType.Fix].Count();
            count += item.TokenMessages[TrackerReleaseType.FixImportant].Count();
            count += item.TokenMessages[TrackerReleaseType.Note].Count();
            count += item.TokenMessages[TrackerReleaseType.NoteImportant].Count();
            count += item.TokenMessages[TrackerReleaseType.Security].Count();
            count += item.TokenMessages[TrackerReleaseType.SecurityImportant].Count();
        }
        return count;
    }
    public static IRunner Create() => new CalculateNextReleaseVersionRunner();
    public static string GetCommandName() => nameof(CalculateNextReleaseVersionRunner);
}
