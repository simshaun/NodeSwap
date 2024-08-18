using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace NodeSwap
{
    public class NodeJsWebApi(GlobalContext globalContext)
    {
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
        public List<Version> GetInstallableNodeVersions(string prefix = "")
        {
            var minVersion = VersionParser.Parse(prefix != "" ? prefix : "0.0.0");

            var versions = new List<Version>();
            var reader = NodeVersionsStreamReader();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var lineVersion = VersionParser.Parse(line.Split("\t")[0]);
                if (lineVersion >= minVersion && (prefix == "" || lineVersion.ToString().StartsWith(prefix)))
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
            var arch = globalContext.Is64Bit ? "x64" : "x86";
            return $"https://nodejs.org/dist/v{version}/node-v{version}-win-{arch}.zip";
        }

        protected virtual Stream NodeVersionsStream()
        {
            Stream stream;
            try
            {
                stream = new WebClient().OpenRead("https://nodejs.org/dist/index.tab");
            }
            catch (WebException)
            {
                throw new Exception("Unable to connect to nodejs.org. Try opening " +
                                    "https://nodejs.org/dist/index.tab in your browser.");
            }

            if (stream == null)
            {
                throw new Exception("Unable to connect to nodejs.org !");
            }

            return stream;
        }

        private StreamReader NodeVersionsStreamReader()
        {
            if (_nodeVersionsStreamReader != null)
            {
                // Reset reader to beginning on each access
                _nodeVersionsStreamReader.DiscardBufferedData();
                _nodeVersionsStreamReader.BaseStream.Seek(0, SeekOrigin.Begin);
                _nodeVersionsStreamReader.ReadLine(); // skip first line
                
                return _nodeVersionsStreamReader;
            }

            var reader = new StreamReader(NodeVersionsStream());
            reader.ReadLine(); // skip first line;
            _nodeVersionsStreamReader = reader;

            return _nodeVersionsStreamReader;
        }

        private StreamReader _nodeVersionsStreamReader;
    }
}
