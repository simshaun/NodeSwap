using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace NodeSwap.Tests
{
    [TestClass]
    public class NodeJsWebApiTests
    {
        [TestMethod]
        public void GetLatestNodeVersion_ShouldWork()
        {
            var api = GetNodeJsWebApi();
            api.GetLatestNodeVersion().ShouldBe(new Version(16, 0, 0));
        }
        
        [TestMethod]
        public void GetLatestNodeVersion_ShouldWorkWithPrefix()
        {
            var api = GetNodeJsWebApi();
            api.GetLatestNodeVersion("14").ShouldBe(new Version(14, 16, 1));
        }
        
        [TestMethod]
        public void GetInstallableNodeVersions_ShouldWork()
        {
            var api = GetNodeJsWebApi();
            api.GetInstallableNodeVersions().Count.ShouldBe(11);
        }
        
        [TestMethod]
        public void GetInstallableNodeVersions_ShouldWorkWithPrefix()
        {
            var api = GetNodeJsWebApi();
            api.GetInstallableNodeVersions("14.16.1").Count.ShouldBe(1);
            api.GetInstallableNodeVersions("14.16").Count.ShouldBe(2);
            api.GetInstallableNodeVersions("14").Count.ShouldBe(6);
        }

        private static NodeJsWebApi GetNodeJsWebApi()
        {
            var globalContext = new GlobalContext();
            return new NodeJsWebApi(globalContext);
        }
    }

    internal class NodeJsWebApi : NodeSwap.NodeJsWebApi
    {
        public NodeJsWebApi(GlobalContext globalContext) : base(globalContext)
        {
        }

        protected override Stream NodeVersionsStream()
        {
            return GenerateStreamFromString("version\tdate\nv16.0.0\t2021-04-20\nv15.14.0\t2021-04-06" +
                                            "\nv15.10.0\t2021-02-23\nv15.9.0\t2021-02-18\nv15.0.0\t2020-10-20" +
                                            "\nv14.16.1\t2021-04-06\nv14.16.0\t2021-02-23\nv14.14.0\t2020-10-15" +
                                            "\nv14.10.0\t2020-09-08\nv14.9.0\t2020-08-27\nv14.8.0\t2020-08-11\n");
        }

        private static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}

namespace NodeSwap
{
}
