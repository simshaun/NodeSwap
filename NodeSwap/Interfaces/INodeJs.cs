using System;
using System.Collections.Generic;

namespace NodeSwap.Interfaces;

public interface INodeJs
{
    NodeJsVersion? GetLatestInstalledVersion();
    List<NodeJsVersion> GetInstalledVersions();
    Version? GetActiveVersion();
    Version? GetPreviousVersion();
}