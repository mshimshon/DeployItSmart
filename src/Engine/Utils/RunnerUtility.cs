using DeployItSmart.Engine.ArgumentParser;
using DeployItSmart.Engine.Configuration;
using DeployItSmart.Engine.Utils;
using System;
using System.Reflection;
using static DeployItSmart.Engine.Configuration.ConfigurationRunner;
namespace DeployItSmart.Engine;

public static class RunnerUtility
{
    private static ToolConf ToolConfig => GetToolConfiguration();
    private static Dictionary<string, Type> _runners = default!;

    private static List<Type> _customTypes = new();

    public static void Configure()
    {
        var types = Assembly.GetExecutingAssembly().GetTypes().ToList();
        types.AddRange(_customTypes);
        _runners = types.Where(t => typeof(IRunner).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .Select(GetRunnerInfo)
            .Where(p => p.Key != null)
            .ToDictionary(p => p.Key!.ToLower(), p => p.Value);

    }
    public static void ImportCustom()
    {
        if (!ToolConfig.UseCustomScriptLoader)
        {
            System.Console.WriteLine("Custom Script Loader Disabled.... Skipping.");
            return;
        }
        _customTypes = CustomScriptLoader.LoadCustomRunners();

    }

    public static async Task ExecuteCommand(string command)
    {
        command = command.ToLower();
        Terminal.WriteLine("---------");
        System.Console.Error.WriteLine($"Requested Command: {command}");
        System.Console.Error.WriteLine($"Using Parameters: {string.Join(" ", Environment.GetCommandLineArgs())}");
        Terminal.WriteLine("---------");
        if (!_runners.ContainsKey(command))
        {
            foreach (var item in _runners)
            {
                System.Console.Error.WriteLine($"Available: {item.Key}");
            }
            throw new Exception($"The command '{command}' was not found");
        }

        var method = _runners[command].GetMethod(nameof(IRunner.Create), BindingFlags.Public | BindingFlags.Static);
        if (SimpleArgumentParser.AvailableParameters.Count > 0)
        {
            Terminal.WriteLine("Available Optional Arguments");
            foreach (var item in SimpleArgumentParser.AvailableParameters)
            {
                Terminal.WriteLine(item);
            }
        }

        IRunner runner = (method!.Invoke(null, null) as IRunner)!;
        await runner.RunAsync();
    }

    private static KeyValuePair<string?, Type> GetRunnerInfo(Type runnerType)
    {
        var method = runnerType.GetMethod(nameof(IRunner.GetCommandName), BindingFlags.Public | BindingFlags.Static);
        string? command = method?.Invoke(null, null) as string ?? default;
        return new(command, runnerType);
    }



}
