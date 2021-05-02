using System;
using System.Collections.Generic;

namespace NodeSwap.Utils
{
    public class ConsoleColumns
    {
        public static void WriteColumns<T>(List<T> data, int numColumns, Func<T, string> formatValue)
        {
            var chunks = ListUtil.Chunk(data, numColumns);
            chunks.ForEach(chunk =>
            {
                chunk.ForEach(v => Console.Write(formatValue(v)));
                Console.WriteLine();
            });
        }
    }
}
