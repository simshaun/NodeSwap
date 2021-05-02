using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace NodeSwap
{
    public class NodeJsWebApi
    {
        private readonly GlobalContext _globalContext;

        public NodeJsWebApi(GlobalContext _globalContext)
        {
            this._globalContext = _globalContext;
        }

        /// <summary>
        /// Get the most recent Node version.
        /// </summary>
        public Version GetLatestNodeVersion()
        {
            return VersionParser.Parse(NodeVersionsStreamReader().ReadLine()?.Split("\t")[0]);
        }

        /// <summary>
        /// Get the latest Node version matching a given prefix.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public Version GetLatestNodeVersion(string prefix)
        {
            var reader = NodeVersionsStreamReader();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var lineVersion = VersionParser.Parse(line.Split("\t")[0]);
                if (lineVersion.ToString().StartsWith(prefix))
                {
                    return lineVersion;
                }
            }

            throw new ArgumentException($"NodeJS distribution not found for given prefix: {prefix}");
        }

        /// <summary>
        /// Get the versions of Node that may be installed.
        /// </summary>
        public List<Version> GetInstallableNodeVersions(string min = "")
        {
            var minVersion = VersionParser.Parse(min != "" ? min : "0.0.0");

            var versions = new List<Version>();
            var reader = NodeVersionsStreamReader();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var lineVersion = VersionParser.Parse(line.Split("\t")[0]);
                if (lineVersion >= minVersion)
                {
                    versions.Add(lineVersion);
                }
            }

            return versions;
        }

        /// <summary>
        /// Generates a download URL. URL is not guaranteed to exist.
        /// </summary>
        public string GetDownloadUrl(Version version)
        {
            var arch = _globalContext.Is64Bit ? "x64" : "x86";
            return $"https://nodejs.org/dist/v{version}/node-v{version}-win-{arch}.zip";
        }

        private StreamReader NodeVersionsStreamReader()
        {
            if (_nodeVersionsStreamReader != null)
            {
                return _nodeVersionsStreamReader;
            }

            var stream = new WebClient().OpenRead("https://nodejs.org/dist/index.tab");
            if (stream == null)
            {
                throw new InvalidOperationException("Unable to connect to nodejs.org");
            }

            var reader = new StreamReader(stream);
            reader.ReadLine(); // skip first line;
            _nodeVersionsStreamReader = reader;

            return _nodeVersionsStreamReader;
        }

        private StreamReader _nodeVersionsStreamReader;
    }
}
