using System;
using System.Collections.Generic;
using NodeSwap.Interfaces;

namespace NodeSwap.Services;

public class NodeJsService : INodeJs
{
    private readonly NodeJs _nodeJs;

    public NodeJsService(NodeJs nodeJs)
    {
        _nodeJs = nodeJs;
    }

    public NodeJsVersion? GetLatestInstalledVersion()
    {
        return _nodeJs.GetLatestInstalledVersion();
    }

    public List<NodeJsVersion> GetInstalledVersions()
    {
        return _nodeJs.GetInstalledVersions();
    }

    public Version? GetActiveVersion()
    {
        return _nodeJs.GetActiveVersion();
    }

    public Version? GetPreviousVersion()
    {
        return _nodeJs.GetPreviousVersion();
    }
}