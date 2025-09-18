using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NodeSwap.Interfaces;

public interface INodeJsWebApi
{
    Task<Version> GetLatestNodeVersion(string prefix = null);
    Task<List<Version>> GetInstallableNodeVersions(string prefix = "");
    string GetDownloadUrl(Version version);
}