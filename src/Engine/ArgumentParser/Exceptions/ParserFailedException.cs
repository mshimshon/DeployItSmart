using System;

namespace DeployItSmart.Engine.ArgumentParser.Exceptions;

public class ParserFailedException : Exception
{
    public ParserFailedException(string message) : base(message)
    {
    }

}
