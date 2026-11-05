using DeployItSmart.Engine.Configuration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Reflection;
using static DeployItSmart.Engine.Configuration.ConfigurationRunner;
namespace DeployItSmart.Engine.Utils;

internal static class CustomScriptLoader
{
    private static ToolConf ToolConfig => GetToolConfiguration();
    private static List<string> _globalStaticUsings = new()
    {
        "using static DeployItSmart.Engine.Configuration.ConfigurationRunner;",
        "using static DeployItSmart.BaseConfigurator;",
    };

    private static List<string> _sharedUsings = new()
    {
        "System",
        "System.Text.Json",
        "System.Text.Json.Serialization",
        "System.Text.RegularExpressions",
        "System.Threading.Tasks",
        "DeployItSmart",
        "DeployItSmart.Engine.Utils",
        "DeployItSmart.Engine.Configuration",
        "DeployItSmart.Engine.Runner",
        "DeployItSmart.Model.Configuration",
    };
    public static List<Type> LoadCustomRunners()
    {
        var runnerTypes = new List<Type>();
        // using static DeployItSmart.Engine.Configuration.ConfigurationRunner;
        string scriptFolder = ToolConfig.CustomScriptLocation;
        if (!Directory.Exists(scriptFolder))
            return runnerTypes;

        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithUsings(_sharedUsings);
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location));
        var parseOptions = CSharpParseOptions.Default.WithKind(SourceCodeKind.Script);
        foreach (var file in Directory.GetFiles(scriptFolder, "*.csx"))
        {
            var fileContent = File.ReadAllText(file);
            var revisedContent = string.Join("\n", _globalStaticUsings) + "\n" + fileContent;
            var syntaxTree = CSharpSyntaxTree.ParseText(revisedContent, parseOptions);
            var compilation = CSharpCompilation.Create(Path.GetFileNameWithoutExtension(file))
                .WithOptions(options)
                .WithReferences(references)
                .AddSyntaxTrees(syntaxTree);

            using var ms = new MemoryStream();
            var emitResult = compilation.Emit(ms);
            if (emitResult.Success)
            {
                ms.Seek(0, SeekOrigin.Begin);

                // Load straight into the main application context
                var assembly = Assembly.Load(ms.ToArray());

                // Extract and filter down to just the valid IRunner implementation types
                var types = assembly.GetTypes().Where(t => typeof(IRunner).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                runnerTypes.AddRange(types);
            }
            else
            {
                foreach (var failed in emitResult.Diagnostics)
                    System.Console.Error.WriteLine($"{failed.Location} -> {failed.GetMessage()} ({failed.Id})");

            }
        }

        return runnerTypes;
    }

}
