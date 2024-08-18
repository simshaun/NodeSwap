using System.Collections.Generic;
using System.Linq;

namespace NodeSwap.Utils;

public static class ListUtil
{
    public static List<List<T>> Chunk<T>(List<T> collection, int size)
    {
        var chunks = new List<List<T>>();
        var chunkCount = collection.Count / size;

        if (collection.Count % size > 0)
            chunkCount++;

        for (var i = 0; i < chunkCount; i++)
            chunks.Add(collection.Skip(i * size).Take(size).ToList());

        return chunks;
    }
}