using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Schema;
using DeployItSmart.Engine.ArgumentParser;
using DeployItSmart.Engine.Configuration;
using DeployItSmart.Engine.Runner;
using DeployItSmart.Model.Configuration;
using DeployItSmart.Model.DataStructure;
using DeployItSmart.Engine.Utils;
using DeployItSmart.Model.DataStructure.Enums;
using Grynwald.MarkdownGenerator;
using LibGit2Sharp;
using static DeployItSmart.Engine.Configuration.ConfigurationRunner;

namespace DeployItSmart.Runners;

public class GenerateDevMessagesRunner : IRunner
{

    private ToolConf ToolConfig => GetToolConfiguration();
    private ChangeLogParameters ChangeLogConfig => GetConfigurationFor<ChangeLogParameters>();
    private VersionRunnerParameters VersionRunnerConfig => GetConfigurationFor<VersionRunnerParameters>();
    private CommitSearchParameters CommitSearchConfig => GetConfigurationFor<CommitSearchParameters>();
    private readonly CalculateNextReleaseVersionRunner _calculateNextReleaseVersion;
    private readonly GetTrackCommitMessagesRunner _getTrackCommitMessages;
    private readonly GetCurrentReleaseVersionRunner _currentReleaseVersion;
    private readonly GetCurrentPreReleaseVersionRunner _currentPreReleaseVersion;
    public GenerateDevMessagesRunner()
    {
        _calculateNextReleaseVersion = new CalculateNextReleaseVersionRunner();
        _currentPreReleaseVersion = new GetCurrentPreReleaseVersionRunner();
        _getTrackCommitMessages = new GetTrackCommitMessagesRunner();
        _currentReleaseVersion = new GetCurrentReleaseVersionRunner();
        _isDevBranch = ServiceAccessor.Use<IGitSmart>().IsDevBranch;
        _isMasterBranch = ServiceAccessor.Use<IGitSmart>().IsMainBranch;
    }
    private readonly bool _isDevBranch;
    private readonly bool _isMasterBranch;
    public static IRunner Create() => new GenerateDevMessagesRunner();
    public static string GetCommandName() => nameof(GenerateDevMessagesRunner);
    public async Task RunAsync()
    {
        await GenerateChangelog();
    }
    public async Task GenerateChangelog()
    {

        var currentVersion = await _currentReleaseVersion.GetCurrentTag();
        var currentPrerelease = await _currentPreReleaseVersion.GetCurrentPreviewTag();
        var nextVersion = await _calculateNextReleaseVersion.GetNextVersion();

        var versions = await _currentReleaseVersion.GetVersions();
        var tagSince = CommitSearchConfig.SinceTag;
        var IsFirstRelease = VersionRunnerConfig.IsFirstRelease;
        if (tagSince != default)
            currentVersion = versions.FirstOrDefault(p => p.ToString() == tagSince);

        bool isPreview = VersionRunnerConfig.IsPreviewMode || _isDevBranch;
        if (currentVersion == default && tagSince != default)
            throw new Exception($"cannot find specific tag since {tagSince}");
        else if (currentVersion == default && !IsFirstRelease)
            throw new Exception("--IsFirstRelease not active for protection we block continuing.");
        else if (currentVersion != default)
            if (isPreview && currentPrerelease != default && currentPrerelease > currentVersion)
                currentVersion = currentPrerelease;

        var commits = await _getTrackCommitMessages.GetHistory(currentVersion!.ToString());
        // Get Structure commit and transform to reguler commit structure one message per commit per type then process 0732775004
        List<string> devCleanMessages = new();
        foreach (var item in commits)
        {
            var baseCommit = new CommitInfoStructure()
            {
                CommitId = item.CommitId,
                Author = item.CommitId,
                AffectedFiles = item.AffectedFiles
            };
            foreach (var message in item.TokenDirtyMessages[TrackerDirtyType.DevMessage])
                if (!devCleanMessages.Contains(message, StringComparer.OrdinalIgnoreCase))
                    devCleanMessages.Add(message);
        }

        List<MdBlock> blocks = [new MdHeading($"## Development Changes", 1)];
        bool emptyLog = devCleanMessages.Count <= 0;
        if (emptyLog && ChangeLogConfig.FailOnEmpty)
            throw new Exception("Nothing to Write in Dev changes");
        else if (emptyLog)
            blocks.Add(new MdParagraph(ToolConfig.EmptyLogMessage));
        else
            blocks.Add(new MdBulletList(devCleanMessages.Select(p => new MdListItem(p))));

        MdDocument doc = new MdDocument(blocks);
        string outputPath = Path.Combine(ChangeLogConfig.OutputLogs, $"dev.{nextVersion}.md");
        System.Console.WriteLine($"Writing '{outputPath}'");
        Terminal.WriteLine(doc.ToString());
        doc.Save(outputPath);
    }


}
