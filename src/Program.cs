using System.Reflection;
using DeployItSmart;
using DeployItSmart.Engine.ArgumentParser;
using DeployItSmart.Engine.Runner;
using DeployItSmart.Engine.Runners;
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
    var argumentParse = new SimpleArgumentParser(args);
    BaseRunner? runner = default;
    if (command.Equals(nameof(GetCommitInfoSinceRunner), StringComparison.OrdinalIgnoreCase))
        runner = new GetCommitInfoSinceRunner(argumentParse);
    else if (command.Equals(nameof(GetTrackCommitMessagesRunner), StringComparison.OrdinalIgnoreCase))
        runner = new GetTrackCommitMessagesRunner(argumentParse);
    else if (command.Equals(nameof(GetCurrentReleaseVersionRunner), StringComparison.OrdinalIgnoreCase))
        runner = new GetCurrentReleaseVersionRunner(argumentParse);
    else if (command.Equals(nameof(GenerateChangeLogRunner), StringComparison.OrdinalIgnoreCase))
        runner = new GenerateChangeLogRunner(argumentParse);
    else if (command.Equals(nameof(CalculateNextReleaseVersionRunner), StringComparison.OrdinalIgnoreCase))
        runner = new CalculateNextReleaseVersionRunner(argumentParse);
    else if (command.Equals(nameof(GenerateCustomConfigRunner), StringComparison.OrdinalIgnoreCase))
        runner = new GenerateCustomConfigRunner(argumentParse);
    else if (command.Equals(nameof(LocalTrashCommitCleanerRunner), StringComparison.OrdinalIgnoreCase))
        runner = new LocalTrashCommitCleanerRunner(argumentParse);
    else throw new Exception($"'{command}' not found.");
    await runner.RunAsync();
}
catch (ExitException ex)
{
    Console.Error.WriteLine(ex.Message);
    Environment.Exit(ex.ExitCode);
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    Environment.Exit(1);
}
