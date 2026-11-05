using DeployItSmart.Engine.ArgumentParser;

namespace DeployItSmart;

public abstract record class BaseConfigurator
{
    public static SimpleArgumentParser StartupParams { get; } = new SimpleArgumentParser(Environment.GetCommandLineArgs());
    public string? PreviewMark { get; init; } = "preview";
    public bool IsPreviewMode { get; init; }
    public BaseConfigurator()
    {
        IsPreviewMode = StartupParams.CheckOptionalFlag("--previewMode");
        PreviewMark = StartupParams.GetOptionalOption("--previewMark") ?? "preview";
    }
}
