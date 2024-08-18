using System;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace NodeSwap.Commands;

public class ListCommand(NodeJs nodeJs) : ICommandHandler
{
    public Task<int> InvokeAsync(InvocationContext context)
    {
        var versions = nodeJs.GetInstalledVersions();
        if (versions.Count == 0)
        {
            Console.WriteLine("None installed");
            return Task.Factory.StartNew(() => 0);
        }

        Console.WriteLine();
        versions.ForEach(v =>
        {
            var prefix = v.IsActive ? "  * " : "    ";
            Console.WriteLine($"{prefix}{v.Version}");
        });
        Console.WriteLine();

        return Task.Factory.StartNew(() => 0);
    }
}