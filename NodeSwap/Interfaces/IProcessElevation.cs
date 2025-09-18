namespace NodeSwap.Interfaces;

public interface IProcessElevation
{
    bool IsAdministrator();
    int ElevateApplication();
}