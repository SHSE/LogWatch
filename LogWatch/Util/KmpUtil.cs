using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LogWatch.Util {
    internal static class KmpUtil {
        private const int BufferSize = 16*1024;

        /// <summary>
        ///     Searches pattern in stream using Knuth-Moris-Pratt algorithm
        /// </summary>
        /// <remarks>
        ///     The original code was borrowed from the article http://www.codeproject.com/Articles/34971/A-NET-implementation-for-the-Knuth-Moris-Pratt-KMP by Nairooz Nilafdeen
        /// </remarks>
        /// <param name="pattern"></param>
        /// <param name="stream"></param>
        /// <param name="limit"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<IReadOnlyList<long>> GetOccurencesAsync(
            byte[] pattern, 
            Stream stream, 
            int limit, 
            CancellationToken cancellationToken) {
            var transitions = CreatePrefixArray(pattern);
            var occurences = new List<long>(Math.Min(4096, limit));

            var m = 0;
            var buffer = new byte[BufferSize];

            while (true) {
                var count = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                if (count == 0)
                    break;

                for (var i = 0; i < count; i++) {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (buffer[i] == pattern[m])
                        m++;
                    else {
                        var prefix = transitions[m];

                        if (prefix + 1 > pattern.Length &&
                            buffer[i] != pattern[prefix + 1])
                            m = 0;
                        else
                            m = prefix;
                    }

                    if (m == pattern.Length) {
                        occurences.Add(stream.Position - count + (i - (pattern.Length - 1)));

                        if (occurences.Count == limit)
                            return occurences;

                        m = transitions[m - 1];
                    }
                }
            }

            return occurences;
        }

        public static IReadOnlyList<long> GetOccurences(
            byte[] pattern,
            ArraySegment<byte> bufferSegment,
            CancellationToken cancellationToken) {
            var transitions = CreatePrefixArray(pattern);
            var occurences = new List<long>();
            var buffer = bufferSegment.Array;
            var m = 0;

            for (var i = bufferSegment.Offset; i < bufferSegment.Count; i++) {
                cancellationToken.ThrowIfCancellationRequested();

                if (buffer[i] == pattern[m])
                    m++;
                else {
                    var prefix = transitions[m];

                    if (prefix + 1 > pattern.Length &&
                        buffer[i] != pattern[prefix + 1])
                        m = 0;
                    else
                        m = prefix;
                }

                if (m == pattern.Length) {
                    occurences.Add(i - (pattern.Length - 1));
                    m = transitions[m - 1];
                }
            }

            return occurences;
        }

        private static int[] CreatePrefixArray(byte[] pattern) {
            var firstByte = pattern[0];

            var result = new int[pattern.Length];

            for (var i = 1; i < pattern.Length; i++) {
                var aux = new byte[i + 1];

                Buffer.BlockCopy(pattern, 0, aux, 0, aux.Length);

                result[i] = GetPrefixLegth(aux, firstByte);
            }

            return result;
        }

        private static int GetPrefixLegth(byte[] array, byte byteToMatch) {
            for (var i = 2; i < array.Length; i++)
                if (array[i] == byteToMatch)
                    if (IsSuffixExist(i, array))
                        return array.Length - i;

            return 0;
        }

        private static bool IsSuffixExist(int index, byte[] array) {
            var k = 0;
            for (var i = index; i < array.Length; i++) {
                if (array[i] != array[k])
                    return false;
                k++;
            }
            return true;
        }
    }
}