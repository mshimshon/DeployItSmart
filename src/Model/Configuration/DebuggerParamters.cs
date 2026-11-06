using System;

namespace DeployItSmart.Model.Configuration;

public sealed record class DebuggerParamters : BaseConfigurator
{
    public bool HasVerbose { get; init; } = default;
    public DebuggerParamters() : base()
    {
        HasVerbose = StartupParams.CheckOptionalFlag("--verbose");
    }
}
