using System;

namespace DeployItSmart;

public class ExitException : Exception
{
    public ExitException(string message) : this(message, 1)
    {
    }

    public ExitException(string message, int exitCode) : base(message)
    {
        ExitCode = exitCode;
    }

    public int ExitCode { get; }
}
