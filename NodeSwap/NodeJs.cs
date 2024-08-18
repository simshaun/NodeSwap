#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NodeSwap;

public partial class NodeJs(GlobalContext globalContext)
{
    public NodeJsVersion? GetLatestInstalledVersion()
    {
        return GetInstalledVersions().FirstOrDefault();
    }

    public List<NodeJsVersion> GetInstalledVersions()
    {
        var activeVersion = GetActiveVersion();

        return
            Directory
                .GetDirectories(globalContext.StoragePath, "node-v*", SearchOption.TopDirectoryOnly)
                .Select(dir =>
                {
                    var version = VersionParser.Parse(NodeVersionRegex().Match(dir).Groups[1].Value);
                    return new NodeJsVersion
                    {
                        Path = dir,
                        Version = version,
                        IsActive = version.Equals(activeVersion),
                    };
                })
                .OrderByDescending(v => v.Version)
                .ToList();
    }

    private Version GetActiveVersion()
    {
        var str = File.ReadAllText(globalContext.ActiveVersionTrackerFilePath);
        return VersionParser.Parse(str);
    }

    [GeneratedRegex(@"node-v(\d+\.\d+\.\d+)")]
    private static partial Regex NodeVersionRegex();
}

public class NodeJsVersion
{
    public required string Path;
    public required Version Version;
    public bool IsActive;
}