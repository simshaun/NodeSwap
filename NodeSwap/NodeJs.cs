#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NodeSwap;

public class NodeJs(GlobalContext globalContext)
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
                    var version = VersionParser.Parse(Regex.Match(dir, @"node-v(\d+\.\d+\.\d+)").Groups[1].Value);
                    return new NodeJsVersion
                    {
                        Path = dir,
                        Version = version,
                        IsActive = version.Equals(activeVersion)
                    };
                })
                .OrderByDescending(v => v.Version)
                .ToList();
    }

    private Version GetActiveVersion()
    {
        try
        {
            var str = File.ReadAllText(globalContext.ActiveVersionTrackerFilePath);
            return VersionParser.Parse(str);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }
}

public class NodeJsVersion
{
    public string Path;
    public Version Version;
    public bool IsActive;
}