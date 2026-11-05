using DeployItSmart.Engine.Utils;
using DeployItSmart.Engine.Configuration;
using DeployItSmart.Engine.Runner;
using DeployItSmart.Model.Configuration;
using static DeployItSmart.Engine.Configuration.ConfigurationRunner;
using DeployItSmart.Model.DataStructure;
using DeployItSmart.Model.DataStructure.Enums;

namespace DeployItSmart.Runners;

public class CalculateNextReleaseVersionRunner : IRunner
{
    private ToolConf ToolConfig => GetToolConfiguration();
    private CommitSearchParameters CommitSearchConfig => GetConfigurationFor<CommitSearchParameters>();

    private readonly GetCurrentReleaseVersionRunner _currentReleaseVersion;
    private readonly GetTrackCommitMessagesRunner _getTrackCommitMessages;
    private readonly MessageSearchTool _messageTool;
    public CalculateNextReleaseVersionRunner()
    {
        _currentReleaseVersion = new GetCurrentReleaseVersionRunner();
        _getTrackCommitMessages = new GetTrackCommitMessagesRunner();
        _messageTool = new MessageSearchTool();
    }

    public async Task RunAsync()
    {
        var version = await GetNextVersion();
        Console.WriteLine(version);
    }

    public async Task<System.Version> GetNextVersion()
    {
        var currentVersion = await _currentReleaseVersion.GetCurrentTag();
        var IsFirstRelease = CommitSearchConfig.IsFirstRelease;
        var IsAllowingUnsafe = CommitSearchConfig.AllowUnsafeVersionCalculation;
        var tagSince = CommitSearchConfig.SinceTag;
        if (tagSince != default && IsAllowingUnsafe)
            currentVersion = new Version(tagSince);

        if (currentVersion == default && !IsFirstRelease)
            throw new Exception("--IsFirstRelease not active for protection we block continuing.");
        if (currentVersion == default)
            return new System.Version(ToolConfig.InitialVersion);



        var nextVersion = currentVersion;
        var commits = await _getTrackCommitMessages.GetHistory(currentVersion.ToString(3));
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
        if (major > 0)
            nextVersion = new(currentVersion.Major + 1, 0, 0);
        else if (minor > 0)
            nextVersion = new(currentVersion.Major, currentVersion.Minor + 1, 0);
        else
            nextVersion = new(currentVersion.Major, currentVersion.Minor, currentVersion.Build + 1);

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
