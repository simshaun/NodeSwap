namespace NodeSwap.Interfaces;

public interface IConsoleWriter
{
    void WriteLine(string message);
    void WriteErrorLine(string message);
}