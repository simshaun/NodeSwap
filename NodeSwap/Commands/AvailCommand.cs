using System;
using System.Collections.Generic;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using NodeSwap;
using NodeSwap.Utils;

namespace NodeSwap.Commands
{
    public class AvailCommand : ICommandHandler
    {
        private readonly NodeJsWebApi _nodeWeb;

        public AvailCommand(NodeJsWebApi nodeWeb)
        {
            _nodeWeb = nodeWeb;
        }

        public Task<int> InvokeAsync(InvocationContext context)
        {
            var minVersion = context.ParseResult.ValueForArgument("min");
            
            var versions = _nodeWeb.GetInstallableNodeVersions(minVersion?.ToString());
            if (versions.Count == 0)
            {
                Console.WriteLine("Unable to download index from nodejs.org");
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
    }
}
