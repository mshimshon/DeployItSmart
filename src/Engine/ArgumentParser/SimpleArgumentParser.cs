using System;
using System.Runtime.CompilerServices;
using DeployItSmart.Engine.ArgumentParser.Exceptions;

namespace DeployItSmart.Engine.ArgumentParser;

public class SimpleArgumentParser
{
    private readonly string[] args;

    public SimpleArgumentParser(string[] args)
    {
        this.args = args;
    }

    public bool CheckOptionalFlag(string key) => GetArgument<bool>(key, null!);

    public bool CheckRequiredFlag(string key)
    {
        bool value = GetArgument<bool>(key, null!);
        if (!value)
            throw new ParserFailedException($"Argument '{key}' was not found.");
        return value;
    }

    public string? GetOptionalOption(string key)
    {
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
            for (var i = 0; i < args.Length; i++)
            {
                if (!string.Equals(args[i], key, StringComparison.Ordinal))
                    continue;

                if (typeof(T) == typeof(bool))
                    return (T)(object)true;

                if (i + 1 >= args.Length)
                {
                    throw new ParserFailedException($"No value was provided for '{key}'.");
                }

                return converter(args[i + 1]);
            }
            if (typeof(T) == typeof(bool))
                return (T)(object)false;
        }
        catch (System.Exception)
        {

            throw new ParserFailedException($"Argument '{key}' value not set or conversion failed.");
        }


        throw new ParserFailedException($"Argument '{key}' was not found.");
    }
}
