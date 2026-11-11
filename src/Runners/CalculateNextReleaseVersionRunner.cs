using DeployItSmart.Engine.Utils;
using DeployItSmart.Engine.Configuration;
using DeployItSmart.Engine.Runner;
using DeployItSmart.Model.Configuration;
using static DeployItSmart.Engine.Configuration.ConfigurationRunner;
using DeployItSmart.Model.DataStructure;
using DeployItSmart.Model.DataStructure.Enums;
using NuGet.Versioning;
using static DeployItSmart.Engine.Utils.GithubWriter;

namespace DeployItSmart.Runners;

public class CalculateNextReleaseVersionRunner : IRunner
{
    private ToolConf ToolConfig => GetToolConfiguration();
    private CommitSearchParameters CommitSearchConfig => GetConfigurationFor<CommitSearchParameters>();
    private VersionRunnerParameters VersionRunnerConfig => GetConfigurationFor<VersionRunnerParameters>();
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
        _isDevBranch = ServiceAccessor.Use<IGitSmart>().IsDevBranch;
        _isMasterBranch = ServiceAccessor.Use<IGitSmart>().IsMainBranch;
    }
    private readonly bool _isDevBranch;
    private readonly bool _isMasterBranch;
    public async Task RunAsync()
    {
        var version = await GetNextVersion();
        if (VersionRunnerConfig.GithubOutput)
            if (VersionRunnerConfig.IsPreviewMode)
            {
                GithubOutput("version_next_preview", version.ToString(), VersionRunnerConfig.GithubOutputNullable);
                GithubOutput("version_next_preview_v", version.Version.ToString(3), VersionRunnerConfig.GithubOutputNullable);
                GithubOutput("version_next_preview_s", version.Release, VersionRunnerConfig.GithubOutputNullable);
            }
            else
                GithubOutput("version_next", version.ToString(), VersionRunnerConfig.GithubOutputNullable);
        Console.WriteLine(version);
    }

    public async Task<NuGetVersion> GetNextVersion()
    {
        var latestStable = await _currentReleaseVersion.GetCurrentTag();
        var latestPreview = await _getCurrentPreReleaseVersion.GetCurrentPreviewTag();
        var IsFirstRelease = VersionRunnerConfig.IsFirstRelease;
        var IsAllowingUnsafe = CommitSearchConfig.AllowUnsafeVersionCalculation;
        var isPreview = VersionRunnerConfig.IsPreviewMode || _isDevBranch;
        var isPatchOnly = VersionRunnerConfig.PatchStableOnly;
        var tagSince = CommitSearchConfig.SinceTag;
        if (tagSince != default && IsAllowingUnsafe)
            latestStable = NuGetVersion.Parse(tagSince);

        if (latestStable == default && !IsFirstRelease)
            throw new Exception("--IsFirstRelease not active for protection we block continuing.");
        if (latestStable == default)
            return NuGetVersion.Parse(ToolConfig.InitialVersion);

        if (isPatchOnly && !IsFirstRelease)
            return new(latestStable.Major, latestStable.Minor, latestStable.Patch + 1);

        List<TrackedCommitMessageStructure> commits = new();
        try
        {
            commits = await _getTrackCommitMessages.GetHistory(latestStable.ToString());
        }
        catch (System.Exception)
        {
        }

        NuGetVersion? previewCommitToRelease = default;
        if (!IsFirstRelease && !isPreview)
        {
            var previewVersions = await _getCurrentPreReleaseVersion.GetPreviewVersions();
            previewCommitToRelease = previewVersions.LastOrDefault(p => ServiceAccessor.Use<IGitSmart>().GetTagCommitId(p.ToString()) != default);
        }

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

        bool hasSignificantChanges = minor > 0 || major > 0;
        NuGetVersion? nextVersion;

        if (previewCommitToRelease == default || latestStable > previewCommitToRelease)
        {
            if (major > 0)
                nextVersion = new(latestStable.Major + 1, 0, 0);
            else if (minor > 0)
                nextVersion = new(latestStable.Major, latestStable.Minor + 1, 0);
            else
                nextVersion = new(latestStable.Major, isPreview ? latestStable.Minor + 1 : latestStable.Minor, isPreview ? 0 : latestStable.Patch + 1);
        }
        else
        {
            nextVersion = new(previewCommitToRelease.Major, previewCommitToRelease.Minor, previewCommitToRelease.Patch);
        }

        bool isPreviewBehindStable = latestPreview != default && latestPreview < latestStable;
        bool isPreviewBehindNext = latestPreview != default && latestPreview < latestStable;
        bool requiresBrandNewPreview = isPreviewBehindStable || isPreviewBehindNext;
        if (isPreview && requiresBrandNewPreview)
        {
            var releaseLabels = new[] { ToolConfig.PreviewMark, "1" };
            nextVersion = new(nextVersion.Major, nextVersion.Minor, 0, releaseLabels!, null);

        }
        else if (isPreview && latestPreview != default && latestPreview >= latestStable)
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
