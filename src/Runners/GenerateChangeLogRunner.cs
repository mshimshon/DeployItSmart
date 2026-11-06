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

public class GenerateChangeLogRunner : IRunner
{

    private ToolConf ToolConfig => GetToolConfiguration();
    private ChangeLogParameters ChangeLogConfig => GetConfigurationFor<ChangeLogParameters>();
    private VersionRunnerParameters VersionRunnerConfig => GetConfigurationFor<VersionRunnerParameters>();
    private CommitSearchParameters CommitSearchConfig => GetConfigurationFor<CommitSearchParameters>();
    private readonly CalculateNextReleaseVersionRunner _calculateNextReleaseVersion;
    private readonly GetTrackCommitMessagesRunner _getTrackCommitMessages;
    private readonly GetCurrentReleaseVersionRunner _currentReleaseVersion;
    private readonly GetCurrentPreReleaseVersionRunner _currentPreReleaseVersion;
    private readonly MessageSearchTool _messageTool;
    Dictionary<string, string> _replacePattern = new();
    public GenerateChangeLogRunner()
    {
        _calculateNextReleaseVersion = new CalculateNextReleaseVersionRunner();
        _currentPreReleaseVersion = new GetCurrentPreReleaseVersionRunner();
        _getTrackCommitMessages = new GetTrackCommitMessagesRunner();
        _messageTool = new MessageSearchTool();
        _currentReleaseVersion = new GetCurrentReleaseVersionRunner();
        _replacePattern = ToolConfig.TagMarkdown.ToDictionary(p => p.Key, p => p.Value);
    }
    public static IRunner Create() => new GenerateChangeLogRunner();
    public static string GetCommandName() => nameof(GenerateChangeLogRunner);
    public async Task RunAsync()
    {
        await GenerateChangelog();
    }
    private void ExplodeTokenIntoIndividualCommits(CommitInfoStructure baseInfo, TrackerReleaseType releaseType, Dictionary<TrackerReleaseType, List<string>> tokenMessages, ref Dictionary<TrackerReleaseType, List<CommitInfoStructure>> newCommits)
    {
        foreach (var tokenMessage in tokenMessages[releaseType])
            newCommits[releaseType].Add(baseInfo with { CommitMessage = tokenMessage });
    }

    public string GetCommitLink(CommitInfoStructure data)
    {
        var repo = new Repository(Environment.CurrentDirectory);
        var remoteUrl = repo.Network.Remotes["origin"].Url;
        var str = remoteUrl.Replace(".git", string.Empty);
        if (str.EndsWith("/"))
            str += $"commit/{data.CommitId}";
        else
            str += $"/commit/{data.CommitId}";
        return str;
    }

    private bool AffectingFilesForProject(List<string> files, string project, Dictionary<string, string> associatedFiles)
    {
        if (project == "_" && files.Count <= 0) return true;
        foreach (var file in files)
        {
            string fileClean = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, file));
            if (!associatedFiles.ContainsKey(fileClean)) continue;
            if (!associatedFiles[fileClean].Equals(project, StringComparison.OrdinalIgnoreCase)) continue;
            return true;
        }

        return false;
    }
    private List<KeyValuePair<string, MdListItem>> GetBlockSection(TrackerReleaseType releaseType, Dictionary<TrackerReleaseType, List<CommitInfoStructure>> newCommits, string project, Dictionary<string, string> associatedFiles)
    {
        List<KeyValuePair<string, MdListItem>> section = newCommits[releaseType].Where(p =>
            {
                return AffectingFilesForProject(p.AffectedFiles, project, associatedFiles);
            })
            .Select(p =>
            {
                if (ChangeLogConfig.IncludeCommitLinks)
                    return new KeyValuePair<string, MdListItem>(p.CommitId, new MdListItem(new MdRawMarkdownSpan(ProcessSpecialTags(p.CommitMessage)), " | (Commit: ", new MdLinkSpan(p.CommitId.Substring(0, 6), GetCommitLink(p)), ")"));
                else
                    return new KeyValuePair<string, MdListItem>(p.CommitId, new MdListItem(new MdRawMarkdownSpan(ProcessSpecialTags(p.CommitMessage))));
            }).ToList();
        return section;
    }

    public string ProcessSpecialTags(string data)
    {
        string unclean = data;
        foreach (var pattern in _replacePattern)
        {
            try
            {
                Terminal.WriteLine($"Testing Pattern: {pattern.Key} -> {pattern.Value} | {data}");
                if (!Regex.IsMatch(unclean, pattern.Key))
                    continue;
                unclean = Regex.Replace(unclean, pattern.Key, match =>
                    {
                        var replaceWith = pattern.Value;
                        for (int i = 1; i < match.Groups.Count; i++)
                        {
                            var value = match.Groups[i].Value;
                            replaceWith = replaceWith.Replace($"{{gp:{i}}}", value);
                        }
                        Terminal.WriteLine($"Cleaning Metadata: {pattern.Key} -> {pattern.Value} | {replaceWith}");

                        return replaceWith;
                    });
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"Error Complex Parsing {ex.Message}");
            }

        }
        return unclean;
    }
    private Dictionary<string, string> AssociateFilesToProjects()
    {
        Dictionary<string, string> fileToProjectAssociated = new();
        if (ToolConfig.Projects.Count(p => p.Key != "_") > 0)
            foreach (var project in ToolConfig.Projects.Where(p => p.Key != "_"))
            {
                string filePathDiretory = Path.GetDirectoryName(project.Key) == default ? Environment.CurrentDirectory : Path.GetDirectoryName(project.Key)!;
                var allFiles = Directory.EnumerateFiles(filePathDiretory, "*", SearchOption.AllDirectories);
                foreach (var file in allFiles)
                    if (!fileToProjectAssociated.ContainsKey(file))
                        fileToProjectAssociated[file] = project.Key;
            }

        var rootFilesDown = Directory.EnumerateFiles(Environment.CurrentDirectory, "*", SearchOption.AllDirectories);
        foreach (var file in rootFilesDown)
            if (!fileToProjectAssociated.ContainsKey(file))
                fileToProjectAssociated[file] = "_";

        return fileToProjectAssociated;
    }
    private string GetProjectHeader(string title, string path)
    {
        if (path == "_") return title;
        string projectName = Path.GetFileNameWithoutExtension(path);
        if (ToolConfig.Projects.ContainsKey(path))
            projectName = ToolConfig.Projects[path];
        return $"{title} ({projectName})";
    }



    private Dictionary<string, List<KeyValuePair<string, MdListItem>>> RemoveCrossProjectDuplicateForMaster(Dictionary<string, List<KeyValuePair<string, MdListItem>>> projectSections)
    {
        var sectionCleanedOut = new Dictionary<string, List<KeyValuePair<string, MdListItem>>>();
        List<string> processedHash = new();

        /*
            1. Find

        */
        foreach (var projectSection in projectSections)
        {
            List<KeyValuePair<string, MdListItem>> newSectionBlocks = new();
            var toBeProcessed = projectSection.Value.Where(p => !processedHash.Contains(p.Key));
            var processingHash = new List<string>();
            foreach (var item in toBeProcessed)
            {
                // if was globally processed skip
                if (processedHash.Contains(item.Key)) continue;
                // if locally process just add
                if (!processingHash.Contains(item.Key))
                    processingHash.Add(item.Key);
                newSectionBlocks.Add(item);
            }
            processedHash.AddRange(processingHash);
            sectionCleanedOut[projectSection.Key] = newSectionBlocks;
        }
        return sectionCleanedOut;
    }

    private void ProcessSectionMd(string project, Dictionary<string, List<KeyValuePair<string, MdListItem>>> sections, string header, ref Dictionary<string, List<MdBlock>> finalBlock)
    {
        if (!finalBlock.ContainsKey(project)) finalBlock[project] = new();
        if (sections.ContainsKey(project) && sections[project].Count > 0)
            finalBlock[project].AddRange([new MdHeading(header, 2), new MdBulletList(sections[project].Select(p => p.Value))]);
        //root detected
        if (project == "_")
        {
            Dictionary<string, List<KeyValuePair<string, MdListItem>>> rootSections = sections;
            if (ChangeLogConfig.FixNonLinearMaster)
                rootSections = RemoveCrossProjectDuplicateForMaster(sections);
            foreach (var section in rootSections)
                if (section.Key != "_")
                    if (section.Value.Count > 0)
                    {
                        var blocks = rootSections[section.Key].Select(p => p.Value);
                        finalBlock[project].AddRange([new MdHeading(GetProjectHeader(header, section.Key), 3), new MdBulletList(blocks)]);
                    }
        }

    }

    private Dictionary<string, List<MdBlock>> MapToProjects(
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> featureChanges,
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> featureBreakingChanges,
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> featureImportantChanges,
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> fixChanges,
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> fixBreakingChanges,
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> fixImportantChanges,
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> notesChanges,
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> noteBreakingChanges,
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> noteImportantChanges,
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> sponsorChanges,
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> sponsorImportantChanges,
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> thanksChanges,
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> thanksImportantChanges,
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> securityChanges,
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> securityBreakingChanges,
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> securityImportantChanges
    )
    {
        List<string> projects = new() { };
        if (ToolConfig.Projects.Count(p => p.Key != "_") > 0)
            projects.AddRange(ToolConfig.Projects
                .Where(p => p.Key != "_")
                .Select(p => Path.IsPathFullyQualified(p.Key) ? p.Key : Path.Combine(Environment.CurrentDirectory, p.Key)));
        projects.Add("_"); // Make sure master is last
        Dictionary<string, List<MdBlock>> finalBlock = new();
        foreach (var project in projects)
        {
            ProcessSectionMd(project, securityBreakingChanges, ToolConfig.SecurityBreakingHeader, ref finalBlock);
            ProcessSectionMd(project, featureBreakingChanges, ToolConfig.FeatureBreakingHeader, ref finalBlock);
            ProcessSectionMd(project, fixBreakingChanges, ToolConfig.FixBreakingHeader, ref finalBlock);
            ProcessSectionMd(project, noteBreakingChanges, ToolConfig.NoteBreakingHeader, ref finalBlock);

            ProcessSectionMd(project, securityImportantChanges, ToolConfig.SecurityImportantHeader, ref finalBlock);
            ProcessSectionMd(project, featureImportantChanges, ToolConfig.FeatureImportantHeader, ref finalBlock);
            ProcessSectionMd(project, fixImportantChanges, ToolConfig.FixImportantHeader, ref finalBlock);
            ProcessSectionMd(project, noteImportantChanges, ToolConfig.NoteImportantHeader, ref finalBlock);

            ProcessSectionMd(project, featureChanges, ToolConfig.FeatureHeader, ref finalBlock);
            ProcessSectionMd(project, securityChanges, ToolConfig.SecurityHeader, ref finalBlock);
            ProcessSectionMd(project, fixChanges, ToolConfig.FixHeader, ref finalBlock);
            ProcessSectionMd(project, notesChanges, ToolConfig.NoteHeader, ref finalBlock);
            ProcessSectionMd(project, sponsorImportantChanges, ToolConfig.SponsorImportantHeader, ref finalBlock);
            ProcessSectionMd(project, sponsorChanges, ToolConfig.SponsorHeader, ref finalBlock);
            ProcessSectionMd(project, thanksImportantChanges, ToolConfig.ThanksImportantHeader, ref finalBlock);
            ProcessSectionMd(project, thanksChanges, ToolConfig.ThanksHeader, ref finalBlock);
        }

        return finalBlock;
    }
    private Dictionary<string, List<MdBlock>> GetBlocks(Dictionary<TrackerReleaseType, List<CommitInfoStructure>> newCommits)
    {
        bool splitProject = ToolConfig.Projects.Count(p => p.Key != "_") > 0;
        var associated = AssociateFilesToProjects();
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> projectFeatureSection = new();
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> projectFeatureBreakingSection = new();
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> projectFeatureImportantSection = new();
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> projectFixSection = new();
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> projectFixBreakingSection = new();
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> projectFixImportantSection = new();
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> projectNoteSection = new();
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> projectNoteBreakingSection = new();
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> projectNoteImportantSection = new();
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> projectSponsorSection = new();
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> projectSponsorImportantSection = new();
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> projectThanksSection = new();
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> projectThanksImportantSection = new();
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> projectSecuritySection = new();
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> projectSecurityBreakingSection = new();
        Dictionary<string, List<KeyValuePair<string, MdListItem>>> projectSecurityImportantSection = new();


        List<string> projects = splitProject ? ToolConfig.Projects.Keys.ToList() : new() { "_" };
        if (!projects.Contains("_")) projects.Add("_");
        foreach (var project in projects)
        {
            var itemLocal = project;
            projectSecurityBreakingSection[project] = GetBlockSection(TrackerReleaseType.SecurityBreaking, newCommits, itemLocal, associated);
            projectFeatureBreakingSection[project] = GetBlockSection(TrackerReleaseType.FeatureBreaking, newCommits, itemLocal, associated);
            projectFixBreakingSection[project] = GetBlockSection(TrackerReleaseType.FixBreaking, newCommits, itemLocal, associated);
            projectNoteBreakingSection[project] = GetBlockSection(TrackerReleaseType.NoteBreaking, newCommits, itemLocal, associated);
            projectSecurityImportantSection[project] = GetBlockSection(TrackerReleaseType.SecurityImportant, newCommits, itemLocal, associated);
            projectFeatureImportantSection[project] = GetBlockSection(TrackerReleaseType.FeatureImportant, newCommits, itemLocal, associated);
            projectFixImportantSection[project] = GetBlockSection(TrackerReleaseType.FixImportant, newCommits, itemLocal, associated);
            projectNoteImportantSection[project] = GetBlockSection(TrackerReleaseType.NoteImportant, newCommits, itemLocal, associated);
            projectFeatureSection[project] = GetBlockSection(TrackerReleaseType.Feature, newCommits, itemLocal, associated);
            projectFeatureSection[project] = GetBlockSection(TrackerReleaseType.Security, newCommits, itemLocal, associated);
            projectFixSection[project] = GetBlockSection(TrackerReleaseType.Fix, newCommits, itemLocal, associated);
            projectNoteSection[project] = GetBlockSection(TrackerReleaseType.Note, newCommits, itemLocal, associated);
            projectSponsorImportantSection[project] = GetBlockSection(TrackerReleaseType.SponsorImportant, newCommits, itemLocal, associated);
            projectSponsorSection[project] = GetBlockSection(TrackerReleaseType.Sponsor, newCommits, itemLocal, associated);
            projectThanksImportantSection[project] = GetBlockSection(TrackerReleaseType.ThanksImportant, newCommits, itemLocal, associated);
            projectThanksSection[project] = GetBlockSection(TrackerReleaseType.Thanks, newCommits, itemLocal, associated);
        }
        return MapToProjects(
            projectFeatureSection,
            projectFeatureBreakingSection,
            projectFeatureImportantSection,
            projectFixSection,
            projectFixBreakingSection,
            projectFixImportantSection,
            projectNoteSection,
            projectNoteBreakingSection,
            projectNoteImportantSection,
            projectSponsorSection,
            projectSponsorImportantSection,
            projectThanksSection,
            projectThanksImportantSection,
            projectSecuritySection,
            projectSecurityBreakingSection,
            projectSecurityImportantSection
        );
    }

    public async Task GenerateChangelog()
    {

        var currentVersion = await _currentReleaseVersion.GetCurrentTag();
        var currentPrerelease = await _currentPreReleaseVersion.GetCurrentPreviewTag();
        var nextVersion = await _calculateNextReleaseVersion.GetNextVersion();

        var versions = await _currentReleaseVersion.GetVersions();
        var tagSince = CommitSearchConfig.SinceTag;
        var IsFirstRelease = CommitSearchConfig.IsFirstRelease;
        if (tagSince != default)
            currentVersion = versions.FirstOrDefault(p => p.ToString() == tagSince);


        if (currentVersion == default && tagSince != default)
            throw new Exception($"cannot find specific tag since {tagSince}");
        else if (currentVersion == default && !IsFirstRelease)
            throw new Exception("--IsFirstRelease not active for protection we block continuing.");
        else if (currentVersion != default)
            if (VersionRunnerConfig.IsPreviewMode && currentPrerelease != default && currentPrerelease > currentVersion)
                currentVersion = currentPrerelease;

        var commits = await _getTrackCommitMessages.GetHistory(currentVersion!.ToString());
        // Get Structure commit and transform to reguler commit structure one message per commit per type then process
        Dictionary<TrackerReleaseType, List<CommitInfoStructure>> newCommits = new()
        {
            [TrackerReleaseType.SecurityBreaking] = new List<CommitInfoStructure>(),
            [TrackerReleaseType.FeatureBreaking] = new List<CommitInfoStructure>(),
            [TrackerReleaseType.FixBreaking] = new List<CommitInfoStructure>(),
            [TrackerReleaseType.NoteBreaking] = new List<CommitInfoStructure>(),
            [TrackerReleaseType.SecurityImportant] = new List<CommitInfoStructure>(),
            [TrackerReleaseType.FeatureImportant] = new List<CommitInfoStructure>(),
            [TrackerReleaseType.FixImportant] = new List<CommitInfoStructure>(),
            [TrackerReleaseType.NoteImportant] = new List<CommitInfoStructure>(),
            [TrackerReleaseType.Feature] = new List<CommitInfoStructure>(),
            [TrackerReleaseType.Security] = new List<CommitInfoStructure>(),
            [TrackerReleaseType.Fix] = new List<CommitInfoStructure>(),
            [TrackerReleaseType.Note] = new List<CommitInfoStructure>(),
            [TrackerReleaseType.SponsorImportant] = new List<CommitInfoStructure>(),
            [TrackerReleaseType.Sponsor] = new List<CommitInfoStructure>(),
            [TrackerReleaseType.ThanksImportant] = new List<CommitInfoStructure>(),
            [TrackerReleaseType.Thanks] = new List<CommitInfoStructure>(),
        };
        foreach (var item in commits)
        {
            var baseCommit = new CommitInfoStructure()
            {
                CommitId = item.CommitId,
                Author = item.CommitId,
                AffectedFiles = item.AffectedFiles
            };
            // 1. MAJOR / BREAKING CHANGES (Highest Priority)
            ExplodeTokenIntoIndividualCommits(baseCommit, TrackerReleaseType.SecurityBreaking, item.TokenMessages, ref newCommits);
            ExplodeTokenIntoIndividualCommits(baseCommit, TrackerReleaseType.FeatureBreaking, item.TokenMessages, ref newCommits);
            ExplodeTokenIntoIndividualCommits(baseCommit, TrackerReleaseType.FixBreaking, item.TokenMessages, ref newCommits);
            ExplodeTokenIntoIndividualCommits(baseCommit, TrackerReleaseType.NoteBreaking, item.TokenMessages, ref newCommits);

            // 2. MINOR / IMPORTANT CHANGES (Medium Priority)
            ExplodeTokenIntoIndividualCommits(baseCommit, TrackerReleaseType.SecurityImportant, item.TokenMessages, ref newCommits);
            ExplodeTokenIntoIndividualCommits(baseCommit, TrackerReleaseType.FeatureImportant, item.TokenMessages, ref newCommits);
            ExplodeTokenIntoIndividualCommits(baseCommit, TrackerReleaseType.FixImportant, item.TokenMessages, ref newCommits);
            ExplodeTokenIntoIndividualCommits(baseCommit, TrackerReleaseType.NoteImportant, item.TokenMessages, ref newCommits);
            ExplodeTokenIntoIndividualCommits(baseCommit, TrackerReleaseType.Feature, item.TokenMessages, ref newCommits); // Standard Features usually bump Minor too

            // 3. PATCH / STANDARD CHANGES (Standard Priority)
            ExplodeTokenIntoIndividualCommits(baseCommit, TrackerReleaseType.Security, item.TokenMessages, ref newCommits);
            ExplodeTokenIntoIndividualCommits(baseCommit, TrackerReleaseType.Fix, item.TokenMessages, ref newCommits);
            ExplodeTokenIntoIndividualCommits(baseCommit, TrackerReleaseType.Note, item.TokenMessages, ref newCommits);

            // 4. METADATA & COMMUNITY (Lowest Priority)
            ExplodeTokenIntoIndividualCommits(baseCommit, TrackerReleaseType.SponsorImportant, item.TokenMessages, ref newCommits);
            ExplodeTokenIntoIndividualCommits(baseCommit, TrackerReleaseType.Sponsor, item.TokenMessages, ref newCommits);
            ExplodeTokenIntoIndividualCommits(baseCommit, TrackerReleaseType.ThanksImportant, item.TokenMessages, ref newCommits);
            ExplodeTokenIntoIndividualCommits(baseCommit, TrackerReleaseType.Thanks, item.TokenMessages, ref newCommits);
        }

        Dictionary<string, List<MdBlock>>? projectSplitBlocks = GetBlocks(newCommits);
        if (ToolConfig.Projects.Count(p => p.Key != "_") > 0)
        {
            Dictionary<string, List<MdBlock>> documentStart = [];
            foreach (var item in projectSplitBlocks)
            {
                if (!documentStart.ContainsKey(item.Key))
                    documentStart[item.Key] = new();
                documentStart[item.Key].Add(new MdHeading($"Changelog for v{nextVersion}", 1));
                bool emptyLog = item.Value.Count <= 0;
                if (emptyLog && ChangeLogConfig.FailOnEmpty)
                    throw new Exception("Nothing to Write in Changelogs");
                else if (emptyLog)
                    documentStart[item.Key].Add(new MdParagraph(ToolConfig.EmptyLogMessage));
                else
                    documentStart[item.Key].AddRange(item.Value);
                //ROOT Handle
                if (item.Key == "_" && ChangeLogConfig.FixNonLinearMaster)
                {

                }
                MdDocument doc = new MdDocument(documentStart[item.Key]);
                string filename = item.Key;
                if (filename != "_") filename = Path.GetFileNameWithoutExtension(item.Key);
                string outputPath = Path.Combine(ChangeLogConfig.OutputLogs, $"{filename}.{nextVersion}.md");
                Terminal.WriteLine(doc.ToString());
                doc.Save(outputPath);
            }
        }
        else
        {
            List<MdBlock> blocks = [new MdHeading($"Changelog for v{nextVersion}", 1)];
            bool emptyLog = projectSplitBlocks["_"].Count <= 0;
            if (emptyLog && ChangeLogConfig.FailOnEmpty)
                throw new Exception("Nothing to Write in Changelogs");
            else if (emptyLog)
                blocks.Add(new MdParagraph(ToolConfig.EmptyLogMessage));
            else
                blocks.AddRange(projectSplitBlocks["_"]);
            MdDocument doc = new MdDocument(blocks);
            string outputPath = Path.Combine(ChangeLogConfig.OutputLogs, $"_.{nextVersion}.md");
            Terminal.WriteLine(doc.ToString());
            doc.Save(outputPath);
        }
    }


}
