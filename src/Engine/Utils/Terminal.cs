using DeployItSmart.Engine.Configuration;
using DeployItSmart.Model.Configuration;
using System;

namespace DeployItSmart.Engine.Utils;

public static class Terminal
{

    public static void WriteLine(string message)
    {
        bool verbose = ConfigurationRunner.GetConfigurationFor<DebuggerParamters>().HasVerbose;
        if (verbose)
            System.Console.Error.WriteLine(message);

    }
}
