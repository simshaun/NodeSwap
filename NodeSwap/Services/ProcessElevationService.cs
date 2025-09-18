using NodeSwap.Interfaces;
using NodeSwap.Utils;

namespace NodeSwap.Services;

public class ProcessElevationService : IProcessElevation
{
    public bool IsAdministrator()
    {
        return ProcessElevation.IsAdministrator();
    }

    public int ElevateApplication()
    {
        return ProcessElevation.ElevateApplication();
    }
}