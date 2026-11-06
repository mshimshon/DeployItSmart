namespace DeployItSmart.Engine.Configuration;

public record class ToolConf
{
    public string CustomScriptLocationComment { get; set; } = "Relative path to the custom script folder Cross platform supported \\ and / using .NET Path.GetFullPath";
    public string CustomScriptLocation { get; set; } = Path.Combine(Environment.CurrentDirectory, ".github", "scripts", "runners");
    public string UseCustomScriptLoaderComment { get; set; } = "Default is false for security reasons, only enable if repository is secured as any other scripts.";
    public bool UseCustomScriptLoader { get; set; } = false;
    public string InitialVersionComment { get; set; } = "Initial Release version when using --isFirstRelease.";
    public string InitialVersion { get; set; } = "0.0.1";
    public string MasterReleaseBranchComment { get; set; } = "Remote Master Branch";
    public string MasterReleaseBranch { get; set; } = "main";
    public string EmptyLogMessageComment { get; set; } = "When you are not using --autoFailEmptyResult the generator will put that message in empty logs for master and projects.";
    public string EmptyLogMessage { get; set; } = "This release has no information.";

    public string HeaderComment { get; set; } = "Headers are the actual printed head section of the final changelog markdown.";
    public string FeatureHeader { get; set; } = "New Features";
    public string FeatureImportantHeader { get; set; } = "New Features (Important)";
    public string FeatureBreakingHeader { get; set; } = "New Features (Breaking)";


    public string SecurityHeader { get; set; } = "Security Patches";
    public string SecurityImportantHeader { get; set; } = "Security (Critical)";
    public string SecurityBreakingHeader { get; set; } = "Security (Breaking)";

    public string FixHeader { get; set; } = "Fixes";
    public string FixImportantHeader { get; set; } = "Fixes (Critical)";
    public string FixBreakingHeader { get; set; } = "Fixes (Breaking)";
    public string NoteHeader { get; set; } = "Notes";
    public string NoteImportantHeader { get; set; } = "Notes (Important)";
    public string NoteBreakingHeader { get; set; } = "Notes (Breaking)";
    public string SponsorHeader { get; set; } = "Release Sponsors 🤝";
    public string SponsorImportantHeader { get; set; } = "The Super Duper Release Sponsors 🏆";
    public string ThanksHeader { get; set; } = "Special Thanks 💖";
    public string ThanksImportantHeader { get; set; } = "Super Special Thanks 💖";
    public string ProjectsComment { get; set; } = "Relative path to a file any file which will be known as project and its friendly name for changelog printing. (_ = root master log/project, do not remove)";

    public Dictionary<string, string> Projects { get; set; } = new()
    {
        {"_","My Project Name"}
    };

    public string TagMarkdownExamplesComment { get; set; } = "Following are examples in commit message you use then as such {SOCIAL MEDIA;USERNAME OR ID;Display Name}";

    public Dictionary<string, string> TagMarkdownExamples { get; set; } = new()
    {
        {@"\{gh;([^;]+);([^}]+)\}", "[{gp:2}](https://github.com/{gp:1})"},
        {@"\{lkdin;([^;]+);([^}]+)\}", "[{gp:2}](https://www.linkedin.com/in/{gp:1})"},
        {@"\{fbk;([^;]+);([^}]+)\}", "[{gp:2}](https://www.facebook.com/profile.php?id={gp:1})"},
        {@"\{steam;([^;]+);([^}]+)\}", "[{gp:2}](https://steamcommunity.com/id/{gp:1})"},
        {@"\{discord_user;([^;]+);([^}]+)\}", "[{gp:2}](https://discord.com/users/{gp:1})"},
        {@"\{discord;([^;]+);([^}]+)\}", "[{gp:2}](https://discord.gg/{gp:1})"},
    };
    public string TagMarkdownComment { get; set; } = "Those are the post processing filtering, using regex to group and map to Markdown with ability to extract groups like so 'XXXXX' -> '[Visit {gp:1} At MyProfile](https://myprofile.com/{gp:0})'";
    public Dictionary<string, string> TagMarkdown { get; set; } = new();
    public string TokensComment { get; set; } = "You can add or remove tokens for each sections for you aren't using standard... each token will be used as such in commit 'token:', 'token!:' or 'token~:' you do not need to include '!' nor '~' ";
    public string TokensComment2 { get; set; } = "'!', '~' are included as code standard of this tool ! = Breaking Change Bump Major, ~ = Important Change but not breaking code bump normally and no symbol = regular change no special attention. ";

    public IReadOnlyList<string> ReleaseFeatureTokens { get; set; } = new[] { "feat" };
    public IReadOnlyList<string> ReleaseSecurityTokens { get; set; } = new[] { "sec" };

    public IReadOnlyList<string> ReleaseFixTokens { get; set; } = new[] { "fix" };
    public IReadOnlyList<string> ReleaseNoteTokens { get; set; } = new[] { "note" };

    public string ReleaseSponsorComment { get; set; } = "'!' are not doing anything for sponsor or thank but '~' does set as more important.";

    public IReadOnlyList<string> ReleaseSponsorTokens { get; set; } = new[] { "sponsor" };
    public IReadOnlyList<string> ReleaseThanksTokens { get; set; } = new[] { "thank" };


    public string ReleaseDirtyTokensComment { get; set; } = "Those dirty token aren't meant to be push remotely, if pushed dev will have to do manually reset to clear it before pushing finished work... those are meant to be local the tool clear them up instantly post-commit.";

    public IReadOnlyList<string> ReleaseDirtyTokens { get; set; } = new[] { "wip" };

    public string ReleaseNoTriggerTokensComment { get; set; } = "Another special token is the skipci~: which if configured correctly makes the action skip, this is design for early stage or lax rules direct main push.";
    public IReadOnlyList<string> ReleaseNoTriggerTokens { get; set; } = new[] { "skipci" };

    public string PatchRolloverMinorComment { get; set; } = "Design to define how many patches is allowed until the patch count is so high that is just count towards minor bump (Max 10000)";
    public int PatchRolloverMinor { get; set; } = 10000;

    public string MinorRolloverMajorMinorComment { get; set; } = "Design to define how many minors is allowed until the minors count is so high that is just count towards major bump (Max 10000)";
    public int MinorRolloverMajor { get; set; } = 10000;
    public string RolloverNoteComment { get; set; } = "the rollover is exponentional thus why the limit of 10K... meaning if minor to major threshold is 10 and patch to minor is 2 then patch count at 20 will rollover to major.";


    public string ActWorkflowBuilderComment { get; set; } = "The Act Workflow builder is a simple merging tool to allow you to break your CICD into multiple files and merge them be making a creator builder.";
    public Dictionary<string, Dictionary<string, List<string>>> ActWorkflowBuilder { get; set; } = new()
    {
    };

}
