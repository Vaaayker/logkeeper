using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LogKeeper.Compression;

/// <summary>канонічний гаффман</summary>
public static class Huffman
{
    private const byte MODE_NO_COMPRESSION = 0;
    private const byte MODE_COMPRESSED = 1;

    // вузол для дерева
    private sealed class Node
    {
        public byte? Symbol { get; set; }
        public Node? Left { get; set; }
        public Node? Right { get; set; }
        public int Frequency { get; set; }

        public bool IsLeaf => Left == null && Right == null;
    }

    // рахуємо довжини кодів: байт -> довжина
    public static Dictionary<byte, int> CodeLengths(byte[] data)
    {
        var result = new Dictionary<byte, int>();
        if (data == null || data.Length == 0)
        {
            return result;
        }

        // рахуємо частоту кожного байта
        var frequencies = new Dictionary<byte, int>();
        for (int i = 0; i < data.Length; i++)
        {
            byte b = data[i];
            if (frequencies.ContainsKey(b))
            {
                frequencies[b] = frequencies[b] + 1;
            }
            else
            {
                frequencies[b] = 1;
            }
        }

        // якщо тільки один символ -> довжина 1
        if (frequencies.Count == 1)
        {
            foreach (var key in frequencies.Keys)
            {
                result[key] = 1;
                break;
            }
            return result;
        }

        // черга з пріоритетом для побудови дерева
        var pq = new PriorityQueue<Node, int>();
        foreach (var pair in frequencies)
        {
            var node = new Node();
            node.Symbol = pair.Key;
            node.Frequency = pair.Value;
            pq.Enqueue(node, pair.Value);
        }

        // попарно з'єднуємо два найменші вузли
        while (pq.Count > 1)
        {
            var left = pq.Dequeue();
            var right = pq.Dequeue();

            var parent = new Node();
            parent.Frequency = left.Frequency + right.Frequency;
            parent.Left = left;
            parent.Right = right;

            pq.Enqueue(parent, parent.Frequency);
        }

        var root = pq.Dequeue();

        // спускаємось по дереву і шукаємо довжину (глибину)
        void FindDepth(Node node, int depth)
        {
            if (node.IsLeaf)
            {
                if (node.Symbol.HasValue)
                {
                    result[node.Symbol.Value] = depth;
                }
                return;
            }

            if (node.Left != null)
            {
                FindDepth(node.Left, depth + 1);
            }
            if (node.Right != null)
            {
                FindDepth(node.Right, depth + 1);
            }
        }

        FindDepth(root, 0);
        return result;
    }

    // робимо канонічні коди з довжин
    public static Dictionary<byte, string> CanonicalCodes(IReadOnlyDictionary<byte, int> lengths)
    {
        var result = new Dictionary<byte, string>();
        if (lengths == null || lengths.Count == 0)
        {
            return result;
        }

        // сортуємо: спочатку за довжиною -> потім за байтом
        var list = lengths.ToList();
        list.Sort((a, b) =>
        {
            int cmp = a.Value.CompareTo(b.Value);
            if (cmp != 0) return cmp;
            return a.Key.CompareTo(b.Key);
        });

        int code = 0;
        int prevLen = list[0].Value;

        for (int i = 0; i < list.Count; i++)
        {
            byte symbol = list[i].Key;
            int len = list[i].Value;

            if (i > 0)
            {
                code = (code + 1) << (len - prevLen);
            }
            prevLen = len;

            // число в двійковий рядок, наприклад: 2 -> "10"
            string bits = Convert.ToString(code, 2).PadLeft(len, '0');
            result[symbol] = bits;
        }

        return result;
    }

    // стиснення масиву байтів
    public static byte[] Encode(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            var empty = new byte[5];
            empty[0] = MODE_NO_COMPRESSION;
            return empty;
        }

        var lengths = CodeLengths(data);
        var canonicalCodes = CanonicalCodes(lengths);

        // перетворюємо байти в біти
        var bits = new List<bool>();
        for (int i = 0; i < data.Length; i++)
        {
            string codeStr = canonicalCodes[data[i]];
            for (int j = 0; j < codeStr.Length; j++)
            {
                bits.Add(codeStr[j] == '1');
            }
        }

        int compressedDataBytes = (bits.Count + 7) / 8;
        int headerSize = 1 + 4 + 2 + (lengths.Count * 2);
        int totalCompressedSize = headerSize + compressedDataBytes;

        // якщо стиснутий розмір більше ніж оригінал -> записуємо без стиснення
        if (totalCompressedSize >= data.Length + 5)
        {
            var noComp = new byte[5 + data.Length];
            noComp[0] = MODE_NO_COMPRESSION;
            byte[] lenBytes = BitConverter.GetBytes(data.Length);
            Array.Copy(lenBytes, 0, noComp, 1, 4);
            Array.Copy(data, 0, noComp, 5, data.Length);
            return noComp;
        }

        using var ms = new MemoryStream(totalCompressedSize);
        using var writer = new BinaryWriter(ms);

        writer.Write(MODE_COMPRESSED);
        writer.Write(data.Length);
        writer.Write((ushort)lengths.Count);

        foreach (var pair in lengths)
        {
            writer.Write(pair.Key);
            writer.Write((byte)pair.Value);
        }

        // збиваємо біти по 8 штук у байти
        byte currentByte = 0;
        int bitCount = 0;
        for (int i = 0; i < bits.Count; i++)
        {
            if (bits[i])
            {
                currentByte |= (byte)(1 << (7 - bitCount));
            }

            bitCount++;
            if (bitCount == 8)
            {
                writer.Write(currentByte);
                currentByte = 0;
                bitCount = 0;
            }
        }

        if (bitCount > 0)
        {
            writer.Write(currentByte);
        }

        return ms.ToArray();
    }

    // розпакування байтів
    public static byte[] Decode(byte[] blob)
    {
        if (blob == null)
        {
            throw new FormatException("blob is null");
        }

        if (blob.Length < 5)
        {
            throw new FormatException($"blob занадто короткий: {blob.Length} байт");
        }

        byte mode = blob[0];
        if (mode != MODE_NO_COMPRESSION && mode != MODE_COMPRESSED)
        {
            throw new FormatException($"невідомий mode: {mode}");
        }

        int originalLength = BitConverter.ToInt32(blob, 1);
        if (originalLength < 0)
        {
            throw new FormatException($"від'ємна довжина: {originalLength}");
        }

        if (originalLength == 0)
        {
            return Array.Empty<byte>();
        }

        // режим 0 -> без стиснення
        if (mode == MODE_NO_COMPRESSION)
        {
            if (blob.Length < 5 + originalLength)
            {
                throw new FormatException("дані обрізані");
            }

            var raw = new byte[originalLength];
            Array.Copy(blob, 5, raw, 0, originalLength);
            return raw;
        }

        // режим 1 -> стиснуто
        int offset = 5;
        if (blob.Length < offset + 2)
        {
            throw new FormatException("немає розміру таблиці");
        }

        ushort tableCount = BitConverter.ToUInt16(blob, offset);
        offset += 2;

        if (blob.Length < offset + tableCount * 2)
        {
            throw new FormatException("таблиця виходить за межі blob");
        }

        var lengths = new Dictionary<byte, int>();
        for (int i = 0; i < tableCount; i++)
        {
            byte symbol = blob[offset];
            offset++;
            byte len = blob[offset];
            offset++;

            if (len <= 0)
            {
                throw new FormatException($"некоректна довжина {len}");
            }
            lengths[symbol] = len;
        }

        var canonicalCodes = CanonicalCodes(lengths);
        var codeToSymbol = new Dictionary<string, byte>();
        foreach (var pair in canonicalCodes)
        {
            codeToSymbol[pair.Value] = pair.Key;
        }

        var output = new byte[originalLength];
        int outIndex = 0;
        var currentBits = new StringBuilder();

        for (int i = offset; i < blob.Length && outIndex < originalLength; i++)
        {
            byte b = blob[i];
            for (int bitPos = 7; bitPos >= 0 && outIndex < originalLength; bitPos--)
            {
                bool bit = (b & (1 << bitPos)) != 0;
                currentBits.Append(bit ? '1' : '0');

                string candidate = currentBits.ToString();
                if (codeToSymbol.ContainsKey(candidate))
                {
                    output[outIndex] = codeToSymbol[candidate];
                    outIndex++;
                    currentBits.Clear();
                }
            }
        }

        if (outIndex < originalLength)
        {
            throw new FormatException("біти закінчилися раніше ніж очікувалось");
        }

        return output;
    }
}
