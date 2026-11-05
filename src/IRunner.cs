using DeployItSmart.Engine;
using System;

namespace DeployItSmart;

public interface IRunner
{
    public static abstract IRunner Create();
    abstract Task RunAsync();
    public static abstract string GetCommandName();
}
