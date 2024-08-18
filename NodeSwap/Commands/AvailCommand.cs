using System;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using NodeSwap.Utils;

namespace NodeSwap.Commands
{
    public class AvailCommand(NodeJsWebApi nodeWeb) : ICommandHandler
    {
        public Task<int> InvokeAsync(InvocationContext context)
        {
            var versionPrefix = context.ParseResult.ValueForArgument("prefix");

            try
            {
                var versions = nodeWeb.GetInstallableNodeVersions(versionPrefix?.ToString());
                if (versions.Count == 0)
                {
                    Console.WriteLine("None found");
                    return Task.Factory.StartNew(() => 1);
                }

                var consoleWidth = Console.WindowWidth;
                var numColumns = (int) Math.Ceiling(consoleWidth / 14.0);
                Console.WriteLine();
                ConsoleColumns.WriteColumns(
                    versions,
                    numColumns,
                    (v) => v.ToString().PadLeft(consoleWidth / numColumns, ' ')
                );
                Console.WriteLine();

                return Task.Factory.StartNew(() => 0);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                return Task.Factory.StartNew(() => 1);
            }
        }
    }
}
