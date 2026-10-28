using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Schema;
using DeployItSmart.Engine.ArgumentParser;
using DeployItSmart.Engine.Runner;
using DeployItSmart.Model.Configuration.Runner;
using DeployItSmart.Model.DataStructure;
using DeployItSmart.Model.DataStructure.Enums;
using Grynwald.MarkdownGenerator;
using LibGit2Sharp;
namespace DeployItSmart.Engine.Runners;

public class GenerateChangeLogRunner : BaseRunner
{
    private JsonSerializerOptions jsonSerializerOption = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles

    };
    public CalculateNextReleaseVersionRunner CalculateNextReleaseVersion { get; init; }
    public GetTrackCommitMessagesRunner GetTrackCommitMessages { get; init; }
    public GetCurrentReleaseVersionRunner CurrentReleaseVersion { get; init; }

    public MessageSearchTool MessageTool { get; init; }
    public GenerateChangeLogConf Configuration { get; init; }
    public GenerateChangeLogRunner(SimpleArgumentParser simpleArgument)
    {
        CalculateNextReleaseVersion = new CalculateNextReleaseVersionRunner(simpleArgument);
        GetTrackCommitMessages = new GetTrackCommitMessagesRunner(simpleArgument);
        MessageTool = new MessageSearchTool();
        Configuration = new GenerateChangeLogConf(simpleArgument);
        CurrentReleaseVersion = new GetCurrentReleaseVersionRunner(simpleArgument);
    }

    public override async Task RunAsync()
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
    private List<MdListItem> GetBlockSection(TrackerReleaseType releaseType, Dictionary<TrackerReleaseType, List<CommitInfoStructure>> newCommits, string project, Dictionary<string, string> associatedFiles)
    {
        List<MdListItem> section = newCommits[releaseType].Where(p =>
            {
                return AffectingFilesForProject(p.AffectedFiles, project, associatedFiles);
            })
            .Select(p =>
            {
                if (Configuration.IncludeCommitLinks)
                    return new MdListItem(new MdRawMarkdownSpan(p.CommitMessage), " ", new MdLinkSpan(p.CommitId.Substring(0, 6), GetCommitLink(p)));
                else
                    return new MdListItem(new MdRawMarkdownSpan(p.CommitMessage));
            }).ToList();
        return section;
    }


    private Dictionary<string, string> AssociateFilesToProjects()
    {
        Dictionary<string, string> fileToProjectAssociated = new();
        if (MessageTool.ToolConfig.Configuration.Projects.Count(p => p.Key != "_") > 0)
            foreach (var project in MessageTool.ToolConfig.Configuration.Projects.Where(p => p.Key != "_"))
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
        if (MessageTool.ToolConfig.Configuration.Projects.ContainsKey(path))
            projectName = MessageTool.ToolConfig.Configuration.Projects[path];
        return $"{title} ({projectName})";
    }



    private void ProcessSectionMd(string project, Dictionary<string, List<MdListItem>> sections, string header, ref Dictionary<string, List<MdBlock>> finalBlock)
    {
        if (!finalBlock.ContainsKey(project)) finalBlock[project] = new();
        if (sections.ContainsKey(project) && sections[project].Count > 0)
            finalBlock[project].AddRange([new MdHeading(header, 2), new MdBulletList(sections[project])]);
        //root detected
        if (project == "_")
            foreach (var section in sections)
                if (section.Key != "_")
                    if (section.Value.Count > 0)
                        finalBlock[project].AddRange([new MdHeading(GetProjectHeader(header, section.Key), 3), new MdBulletList(section.Value)]);
    }

    private Dictionary<string, List<MdBlock>> MapToProjects(
        Dictionary<string, List<MdListItem>> breakingChanges,
        Dictionary<string, List<MdListItem>> featureChanges,
        Dictionary<string, List<MdListItem>> fixChanges,
        Dictionary<string, List<MdListItem>> notesChanges,
        Dictionary<string, List<MdListItem>> sponsorChanges,
        Dictionary<string, List<MdListItem>> thanksChanges
    )
    {
        List<string> projects = new() { };
        if (MessageTool.ToolConfig.Configuration.Projects.Count(p => p.Key != "_") > 0)
            projects.AddRange(MessageTool.ToolConfig.Configuration.Projects
                .Where(p => p.Key != "_")
                .Select(p => Path.IsPathFullyQualified(p.Key) ? p.Key : Path.Combine(Environment.CurrentDirectory, p.Key)));

        Dictionary<string, List<MdBlock>> finalBlock = new();
        foreach (var project in projects)
        {
            ProcessSectionMd(project, breakingChanges, MessageTool.ToolConfig.Configuration.BreakingHeader, ref finalBlock);
            ProcessSectionMd(project, featureChanges, MessageTool.ToolConfig.Configuration.FeatureHeader, ref finalBlock);
            ProcessSectionMd(project, fixChanges, MessageTool.ToolConfig.Configuration.FixHeader, ref finalBlock);
            ProcessSectionMd(project, notesChanges, MessageTool.ToolConfig.Configuration.NoteHeader, ref finalBlock);
            ProcessSectionMd(project, sponsorChanges, MessageTool.ToolConfig.Configuration.SponsorHeader, ref finalBlock);
            ProcessSectionMd(project, thanksChanges, MessageTool.ToolConfig.Configuration.ThanksHeader, ref finalBlock);
        }
        ProcessSectionMd("_", breakingChanges, MessageTool.ToolConfig.Configuration.BreakingHeader, ref finalBlock);
        ProcessSectionMd("_", featureChanges, MessageTool.ToolConfig.Configuration.FeatureHeader, ref finalBlock);
        ProcessSectionMd("_", fixChanges, MessageTool.ToolConfig.Configuration.FixHeader, ref finalBlock);
        ProcessSectionMd("_", notesChanges, MessageTool.ToolConfig.Configuration.NoteHeader, ref finalBlock);
        ProcessSectionMd("_", sponsorChanges, MessageTool.ToolConfig.Configuration.SponsorHeader, ref finalBlock);
        ProcessSectionMd("_", thanksChanges, MessageTool.ToolConfig.Configuration.ThanksHeader, ref finalBlock);
        return finalBlock;
    }
    private Dictionary<string, List<MdBlock>> GetBlocks(Dictionary<TrackerReleaseType, List<CommitInfoStructure>> newCommits)
    {
        bool splitProject = MessageTool.ToolConfig.Configuration.Projects.Count(p => p.Key != "_") > 0;
        var associated = AssociateFilesToProjects();
        Dictionary<string, List<MdListItem>> projectBreakingSection = new();
        Dictionary<string, List<MdListItem>> projectFeatureSection = new();
        Dictionary<string, List<MdListItem>> projectFixSection = new();
        Dictionary<string, List<MdListItem>> projectNoteSection = new();
        Dictionary<string, List<MdListItem>> projectSponsorSection = new();
        Dictionary<string, List<MdListItem>> projectThanksSection = new();

        List<string> projects = splitProject ? MessageTool.ToolConfig.Configuration.Projects.Keys.ToList() : new() { "_" };
        if (!projects.Contains("_")) projects.Add("_");
        foreach (var project in projects)
        {
            var itemLocal = project;
            projectBreakingSection[project] = GetBlockSection(TrackerReleaseType.Breaking, newCommits, itemLocal, associated);
            projectFeatureSection[project] = GetBlockSection(TrackerReleaseType.Feature, newCommits, itemLocal, associated);
            projectFixSection[project] = GetBlockSection(TrackerReleaseType.Fix, newCommits, itemLocal, associated);
            projectNoteSection[project] = GetBlockSection(TrackerReleaseType.Note, newCommits, itemLocal, associated);
            projectSponsorSection[project] = GetBlockSection(TrackerReleaseType.Sponsor, newCommits, itemLocal, associated);
            projectThanksSection[project] = GetBlockSection(TrackerReleaseType.Thanks, newCommits, itemLocal, associated);
        }
        return MapToProjects(projectBreakingSection, projectFeatureSection, projectFixSection, projectNoteSection, projectSponsorSection, projectThanksSection);
    }

    public async Task GenerateChangelog()
    {
        var currentVersion = await CurrentReleaseVersion.GetCurrentTag();
        var nextVersion = await CalculateNextReleaseVersion.GetNextVersion();
        var commits = await GetTrackCommitMessages.GetHistory(currentVersion?.ToString(3) ?? default);
        // Get Structure commit and transform to reguler commit structure one message per commit per type then process
        Dictionary<TrackerReleaseType, List<CommitInfoStructure>> newCommits = new()
        {
            [TrackerReleaseType.Breaking] = new List<CommitInfoStructure>(),
            [TrackerReleaseType.Feature] = new List<CommitInfoStructure>(),
            [TrackerReleaseType.Fix] = new List<CommitInfoStructure>(),
            [TrackerReleaseType.Note] = new List<CommitInfoStructure>(),
            [TrackerReleaseType.Sponsor] = new List<CommitInfoStructure>(),
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
            ExplodeTokenIntoIndividualCommits(baseCommit, TrackerReleaseType.Breaking, item.TokenMessages, ref newCommits);
            ExplodeTokenIntoIndividualCommits(baseCommit, TrackerReleaseType.Feature, item.TokenMessages, ref newCommits);
            ExplodeTokenIntoIndividualCommits(baseCommit, TrackerReleaseType.Fix, item.TokenMessages, ref newCommits);
            ExplodeTokenIntoIndividualCommits(baseCommit, TrackerReleaseType.Note, item.TokenMessages, ref newCommits);
            ExplodeTokenIntoIndividualCommits(baseCommit, TrackerReleaseType.Sponsor, item.TokenMessages, ref newCommits);
            ExplodeTokenIntoIndividualCommits(baseCommit, TrackerReleaseType.Thanks, item.TokenMessages, ref newCommits);
        }

        Dictionary<string, List<MdBlock>>? projectSplitBlocks = GetBlocks(newCommits);
        if (MessageTool.ToolConfig.Configuration.Projects.Count(p => p.Key != "_") > 0)
        {
            Dictionary<string, List<MdBlock>> documentStart = [];
            foreach (var item in projectSplitBlocks)
            {
                if (!documentStart.ContainsKey(item.Key))
                    documentStart[item.Key] = new();
                documentStart[item.Key].Add(new MdHeading($"Changelog for v{nextVersion}", 1));
                bool emptyLog = item.Value.Count <= 0;
                if (emptyLog && Configuration.FailOnEmpty)
                    throw new Exception("Nothing to Write in Changelogs");
                else if (emptyLog)
                    documentStart[item.Key].Add(new MdParagraph(MessageTool.ToolConfig.Configuration.EmptyLogMessage));
                else
                    documentStart[item.Key].AddRange(item.Value);
                MdDocument doc = new MdDocument(documentStart[item.Key]);
                string filename = item.Key;
                if (filename != "_") filename = Path.GetFileNameWithoutExtension(item.Key);
                string outputPath = Path.Combine(Configuration.OutputLogs, $"{filename}.md");
                System.Console.WriteLine(doc.ToString());
                doc.Save(outputPath);
            }
        }
        else
        {
            List<MdBlock> blocks = [new MdHeading($"Changelog for v{nextVersion}", 1)];
            bool emptyLog = projectSplitBlocks["_"].Count <= 0;
            if (emptyLog && Configuration.FailOnEmpty)
                throw new Exception("Nothing to Write in Changelogs");
            else if (emptyLog)
                blocks.Add(new MdParagraph(MessageTool.ToolConfig.Configuration.EmptyLogMessage));
            else
                blocks.AddRange(projectSplitBlocks["_"]);
            MdDocument doc = new MdDocument(blocks);
            string outputPath = Path.Combine(Configuration.OutputLogs, $"_.md");
            System.Console.WriteLine(doc.ToString());
            doc.Save(outputPath);
        }
    }
}
