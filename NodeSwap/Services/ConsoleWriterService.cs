using System;
using NodeSwap.Interfaces;

namespace NodeSwap.Services;

public class ConsoleWriterService : IConsoleWriter
{
    public void WriteLine(string message)
    {
        Console.WriteLine(message);
    }

    public void WriteErrorLine(string message)
    {
        Console.Error.WriteLine(message);
    }
}