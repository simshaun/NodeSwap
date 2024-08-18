using System;
using DotMake.CommandLine;

namespace NodeSwap.Commands;

[CliCommand(
    Description = "List installed versions of Node.js.",
    Parent = typeof(RootCommand)
)]
public class ListCommand(NodeJs nodeJs)
{
    public void Run()
    {
        var versions = nodeJs.GetInstalledVersions();
        if (versions.Count == 0)
        {
            Console.WriteLine("None installed");
            return;
        }

        Console.WriteLine();
        versions.ForEach(v =>
        {
            var prefix = v.IsActive ? "  * " : "    ";
            Console.WriteLine($"{prefix}{v.Version}");
        });
        Console.WriteLine();
    }
}