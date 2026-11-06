using System.Reflection;
using DeployItSmart;
using DeployItSmart.Engine;
using DeployItSmart.Engine.ArgumentParser;
using DeployItSmart.Runners;
Version appVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);
string formattedVersion = $"v{appVersion.ToString(3)}";

Console.ForegroundColor = ConsoleColor.Cyan;
Console.Error.WriteLine("==================================================");
Console.ForegroundColor = ConsoleColor.White;
Console.Error.Write("DeployItSmart CLI  ");
Console.ForegroundColor = ConsoleColor.DarkGray;
Console.Error.WriteLine($"[{formattedVersion}]");
Console.ForegroundColor = ConsoleColor.Cyan;
Console.Error.WriteLine("==================================================");
Console.ResetColor();
bool isValidMinimum = args.Length > 0;
try
{
    if (!isValidMinimum)
        throw new Exception("Minimum one argument required CLI + CMD, example: depitsmart getCommitInfoSince");
    string command = args[0].ToLower();
    RunnerUtility.ImportCustom();
    RunnerUtility.Configure();
    await RunnerUtility.ExecuteCommand(command);
}
catch (ExitException ex)
{
    Console.Error.WriteLine(ex.Message);
    Environment.Exit(ex.ExitCode);
}
catch (TargetInvocationException ex)
{
    Console.Error.WriteLine(ex.InnerException?.Message);
    Console.Error.WriteLine(ex.InnerException?.StackTrace);
    Environment.Exit(1);
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);

    Environment.Exit(1);
}

