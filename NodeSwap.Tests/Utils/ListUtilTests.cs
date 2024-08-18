using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System.Collections.Generic;
using NodeSwap.Utils;

namespace NodeSwap.Tests.Utils;

[TestClass]
public class ListUtilTests
{
    [DataTestMethod]
    public void Chunk_ShouldChunkProperly()
    {
        var input = new List<int> {1, 2, 3, 4, 5};
        var expected = new List<List<int>>
        {
            new() {1, 2},
            new() {3, 4},
            new() {5},
        };
        ListUtil.Chunk(input, 2).ShouldBe(expected);
    }
}