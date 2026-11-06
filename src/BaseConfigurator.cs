using DeployItSmart.Engine.ArgumentParser;

namespace DeployItSmart;

public abstract record class BaseConfigurator
{
    public static SimpleArgumentParser StartupParams { get; } = new SimpleArgumentParser(Environment.GetCommandLineArgs());

    public BaseConfigurator()
    {

    }
}
