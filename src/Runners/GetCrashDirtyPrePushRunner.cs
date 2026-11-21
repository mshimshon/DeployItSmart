using DeployItSmart.Engine.Utils;
using DeployItSmart.Engine.Runner;
using DeployItSmart.Model.DataStructure;

namespace DeployItSmart.Runners;

public class GetCrashDirtyPrePushRunner : IRunner
{
    private readonly GetCurrentReleaseVersionRunner _currentReleaseVersion;
    private readonly GetCurrentPreReleaseVersionRunner _previewReleaseVersion;
    private readonly GetTrackCommitMessagesRunner _getTrackCommitMessages;
    public GetCrashDirtyPrePushRunner()
    {
        _currentReleaseVersion = new GetCurrentReleaseVersionRunner();
        _previewReleaseVersion = new GetCurrentPreReleaseVersionRunner();
        _getTrackCommitMessages = new GetTrackCommitMessagesRunner();
    }
    public static IRunner Create() => new GetCrashDirtyPrePushRunner();
    public static string GetCommandName() => nameof(GetCrashDirtyPrePushRunner);
    public async Task RunAsync()
    {
        await CleanCommits();
    }
    public async Task CleanCommits()
    {

        var currentVersion = await _currentReleaseVersion.GetCurrentTag();
        bool currentExist = currentVersion != default && ServiceAccessor.Use<IGitSmart>().TagInBranch(currentVersion.ToString());
        var previewVersion = await _previewReleaseVersion.GetCurrentPreviewTag();
        bool previewExist = previewVersion != default && ServiceAccessor.Use<IGitSmart>().TagInBranch(previewVersion.ToString());
        string? sinceTag = default;
        if (previewVersion != default && currentVersion != default && currentExist && previewExist)
            if (previewVersion > currentVersion)
                sinceTag = previewVersion.ToString();
            else
                sinceTag = currentVersion.ToString();
        else if (previewExist && previewVersion != default)
            sinceTag = previewVersion.ToString();
        else if (currentExist && currentVersion != default)
            sinceTag = currentVersion.ToString();

        List<TrackedCommitMessageStructure> history = await _getTrackCommitMessages.GetHistory(sinceTag);
        if (history.Count <= 1) return;

        var commits = history.ToList();
        foreach (var item in commits) // ss
        {

            if (item.Pushed)
                break;
            var countDirtyToken = item.CountWipToken();
            if (countDirtyToken > 0)
                throw new Exception("Dirty Token Found");
            else
                break;
        }
    }


}
