using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace NodeSwap.Tests
{
    [TestClass]
    public class VersionParserTests
    {
        private static IEnumerable<object[]> ParseTestData
        {
            get
            {
                return new[]
                {
                    new object[] {"v1", new Version(1, 0, 0)},
                    new object[] {"2", new Version(2, 0, 0)},
                    new object[] {"1.2.3", new Version(1, 2, 3)},
                    new object[] {"1.5", new Version(1, 5, 0)},
                };
            }
        }

        [TestMethod]
        [DynamicData(nameof(ParseTestData))]
        public void Parse_ShouldParseProperly(string rawInput, Version version)
        {
            VersionParser.Parse(rawInput).ShouldBe(version);
        }

        [TestMethod]
        public void Parse_ShouldThrowExceptionOnInvalidInput()
        {
            Assert.ThrowsException<ArgumentException>(() => VersionParser.Parse("a"));
            Assert.ThrowsException<ArgumentException>(() => VersionParser.Parse("1.a"));
            Assert.ThrowsException<ArgumentException>(() => VersionParser.Parse("1.2.a"));
        }

        [TestMethod]
        public void StrictParse_ShouldParse()
        {
            VersionParser.StrictParse("v14.5.8").ShouldBe(new Version(14, 5, 8));
            VersionParser.StrictParse("14.5.8").ShouldBe(new Version(14, 5, 8));
        }

        [TestMethod]
        public void StrictParse_ShouldThrowExceptionOnInvalidInput()
        {
            Assert.ThrowsException<ArgumentException>(() => VersionParser.StrictParse("14.5"));
            Assert.ThrowsException<ArgumentException>(() => VersionParser.StrictParse("14"));
        }
    }
}
