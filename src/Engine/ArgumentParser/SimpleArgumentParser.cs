using System;
using System.Runtime.CompilerServices;
using DeployItSmart.Engine.ArgumentParser.Exceptions;

namespace DeployItSmart.Engine.ArgumentParser;

public class SimpleArgumentParser
{
    private readonly string[] _args;

    public SimpleArgumentParser(string[] args)
    {
        _args = args;
    }
    public static List<string> AvailableParameters { get; } = new();
    public bool CheckOptionalFlag(string key)
    {
        string captureParam = $"{key} (Optional Flag)";
        if (!AvailableParameters.Contains(captureParam))
            AvailableParameters.Add(captureParam);
        return GetArgument<bool>(key, null!);
    }

    public bool CheckRequiredFlag(string key)
    {
        bool value = GetArgument<bool>(key, null!);
        if (!value)
            throw new ParserFailedException($"Argument '{key}' was not found.");
        return value;
    }

    public string? GetOptionalOption(string key)
    {
        string captureParam = $"{key} \"VALUE\" (Optional)";
        if (!AvailableParameters.Contains(captureParam))
            AvailableParameters.Add(captureParam);
        try
        {
            return GetArgument<string?>(key, (s) => s);
        }
        catch (System.Exception)
        {
            return default;
        }
    }

    public string GetRequiredOption(string key) => GetArgument(key, (s) => s);
    private T GetArgument<T>(string key, Func<string, T> converter)
    {
        try
        {
            for (var i = 0; i < _args.Length; i++)
            {
                if (!string.Equals(_args[i], key, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (typeof(T) == typeof(bool))
                    return (T)(object)true;

                if (i + 1 >= _args.Length)
                {
                    throw new ParserFailedException($"No value was provided for '{key}'.");
                }

                return converter(_args[i + 1]);
            }
            if (typeof(T) == typeof(bool))
                return (T)(object)false;
        }
        catch (System.Exception ex)
        {
            System.Console.Error.WriteLine($"{ex.Message}");
            throw new ParserFailedException($"Argument '{key}' value not set or conversion failed.");
        }

        System.Console.Error.WriteLine($"Argument '{key}' was not found.");

        throw new ParserFailedException($"Argument '{key}' was not found.");
    }
}
