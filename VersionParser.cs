using System;
using NuGet.Versioning;

namespace NVM
{
    public static class VersionParser
    {
        public static Version Parse(string rawVersion)
        {
            if (rawVersion[0].ToString().ToLower() == "v")
            {
                rawVersion = rawVersion[1..];
            }

            var version = FloatRange.Parse(rawVersion);
            if (version is null)
            {
                throw new ArgumentException($"Unable to parse version: {rawVersion}");
            }

            return new Version(version.ToString());
        }

        public static Version StrictParse(string rawVersion)
        {
            if (rawVersion[0].ToString().ToLower() == "v")
            {
                rawVersion = rawVersion[1..];
            }

            try
            {
                return new Version(rawVersion);
            }
            catch (ArgumentException)
            {
                throw new ArgumentException($"Unable to parse version: {rawVersion}");
            }
        }
    }
}
