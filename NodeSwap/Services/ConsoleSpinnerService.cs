using NodeSwap.Interfaces;
using NodeSwap.Utils;

namespace NodeSwap.Services;

public class ConsoleSpinnerService : IConsoleSpinner
{
    public void Update()
    {
        ConsoleSpinner.Instance.Update();
    }

    public void Reset()
    {
        ConsoleSpinner.Reset();
    }
}