using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NodeSwap.Interfaces;

namespace NodeSwap.Services;

public class NodeJsWebApiService : INodeJsWebApi
{
    private readonly NodeJsWebApi _webApi;

    public NodeJsWebApiService(NodeJsWebApi webApi)
    {
        _webApi = webApi;
    }

    public Task<Version> GetLatestNodeVersion(string prefix = null)
    {
        return string.IsNullOrEmpty(prefix) ? 
            _webApi.GetLatestNodeVersion() : 
            _webApi.GetLatestNodeVersion(prefix);
    }

    public Task<List<Version>> GetInstallableNodeVersions(string prefix = "")
    {
        return _webApi.GetInstallableNodeVersions(prefix);
    }

    public string GetDownloadUrl(Version version)
    {
        return _webApi.GetDownloadUrl(version);
    }
}