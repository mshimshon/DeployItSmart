using System;

namespace DeployItSmart.Engine.Utils;

public static class GithubWriter
{
    public static void GithubOutput(string key, string value)
        => GithubOutput(new Dictionary<string, string>() { { key, value } });

    public static void GithubOutput(Dictionary<string, string> addOutput)
    {
        string? githubOutput = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");
        if (!string.IsNullOrEmpty(githubOutput) && File.Exists(githubOutput))
        {
            System.Console.WriteLine(githubOutput);
            List<string> artifacts = new();
            Dictionary<string, string> outputs = new();
            foreach (var item in File.ReadAllLines(githubOutput))
            {
                string[] line = item.Split('=');
                if (line.Length != 1)
                    artifacts.Add(item);
                else
                    outputs[line[0]] = line[1];
            }
            foreach (var item in addOutput)
            {
                if (string.IsNullOrWhiteSpace(item.Value))
                    throw new Exception($"{item.Key} is required when using --ghOutput the output must be define or crash workflow purposefully. ");
                outputs[item.Key] = item.Value;
                System.Console.WriteLine($"{item.Key} is set in output.");

            }
            artifacts.AddRange(outputs.Select(p => $"{p.Key}={p.Value}"));
            File.WriteAllLines(githubOutput, artifacts);
        }
        else
        {
            foreach (var item in addOutput)
                System.Console.WriteLine($"{item.Key} = XXXXX is NOT set as it runs outside github action.");
        }
    }
}
