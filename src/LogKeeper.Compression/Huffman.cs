using System.Text;

namespace LogKeeper.Compression;


/// <summary>
/// Канонічний Гаффман
/// </summary>
public static class Huffman
{
    private const byte MODE_NO_COMPRESSION = 0;
    private const byte MODE_COMPRESSED = 1;

    // Клас вузла дерева для Гаффмана
    private sealed class Node
    {
        public byte? Symbol { get; set; }
        public Node Left { get; set; }
        public Node Right { get; set; }
        public int Frequency { get; set; }
        
        public bool IsLeaf => Left == null && Right == null;
    }

    // Довжини кодів Гаффмана
    public static Dictionary<byte, int> CodeLenghts(byte[] data)
    {
        var result = new Dictionary<byte, int>();
        if (data == null || data.Length == 0) return result;
        
        var frequen = new Dictionary<byte, int>();
        foreach (byte b in data)
        {
            frequen[b] = frequen.GetValueOrDefault(b, 0) + 1;
        }
        
        // Якщо один символ
        if (frequen.Count == 1)
        {
            result[frequen.Keys.First()] = 1;
            return result;
        }
        
        // Чега з пріоритетом
        var pq = new PriorityQueue<Node, int>();
        foreach (var (symbol, freq) in frequen)
        {
            pq.Enqueue(new Node { Symbol = symbol, Frequency = freq },  freq);
        }
        
        // Вузли попарно
        while (pq.Count > 1)
        {
            var left = pq.Dequeue();
            var right = pq.Dequeue();
            var parent = new Node
            {
                Frequency = left.Frequency + right.Frequency,
                Left = left,
                Right = right,
            };
            pq.Enqueue(parent, parent.Frequency);
        }
        
        var root = pq.Dequeue();

        // Обхід в гулбину
        void Traverse(Node node, int depth)
        {
            if (node.IsLeaf)
            {
                result[node.Symbol!.Value] = depth;
                return;
            }
            if (node.Left != null) Traverse(node.Left, depth + 1);
            if (node.Right != null) Traverse(node.Right, depth + 1);
        }
        Traverse(root, 0);
        return result;
    }

    public static Dictionary<byte, string> CanonicalCodes(IReadOnlyDictionary<byte, int> lenghts)
    {
        var result = new Dictionary<byte, string>();
        if (lenghts == null || lenghts.Count == 0) return  result;
        
        var sorted = lenghts.OrderBy(kv => kv.Value).ThenBy(kv => kv.Key).ToList();

        int code = 0;
        int prevLen = sorted[0].Value;

        for (int i = 0; i < sorted.Count; i++)
        {
            var (symbol, len) = (sorted[i].Key, sorted[i].Value);
            if (i > 0) code = (code + 1) << (len - prevLen);
            prevLen = len;
            
            string bitString = Convert.ToString(code, 2).PadLeft(len, '0');
            result[symbol] = bitString;
        }
        return result;
    }

    public static byte[] Encode(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            var empty = new byte[5];
            empty[0] = MODE_NO_COMPRESSION;
            return empty;
        }

        var lenghts = CodeLenghts(data);
        var canonicalCodes = CanonicalCodes(lenghts);

        var bitList = new List<bool>();
        foreach (byte b in data)
        {
            string codeStr = canonicalCodes[b];
            foreach (char bitChar in codeStr)
            {
                bitList.Add(bitChar == '1');
            }
        }
        //Байти для стиснених даних
        int compressedDataBytes = (bitList.Count + 7) / 8;
        
        // 1 байт режиму, 4 байти довжини, 2 байти розміру таблиці та N*2 байтів таблиці
        int headerSize = 1 + 4 + 2 + (lenghts.Count * 2);
        int totalCompressedSize = headerSize + compressedDataBytes;

        if (totalCompressedSize >= data.Length + 5)
        {
            var noComp = new byte[5 + data.Length];
            noComp[0] = MODE_NO_COMPRESSION;
            Array.Copy(BitConverter.GetBytes(data.Length), 0, noComp, 1, 4);
            Array.Copy(data, 0, noComp, 5, data.Length);
            return noComp;  
        }

        using var ms = new MemoryStream(totalCompressedSize);
        using var writer = new BinaryWriter(ms);
        
        writer.Write(MODE_COMPRESSED);
        writer.Write(data.Length);
        writer.Write((ushort)lenghts.Count);

        foreach (var (symbol, len) in lenghts )
        {
            writer.Write(symbol);
            writer.Write((byte)len);
        }

        byte currentByte = 0;
        int bitCount = 0;
        foreach (bool bit in bitList)
        {
            if (bit) currentByte |= (byte)(1 << (7 - bitCount));

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


    public static byte[] Decode(byte[] blob)
    {
        if (blob == null || blob.Length < 5) return Array.Empty<byte>();
        
        byte mode = blob[0];
        int originalLength = BitConverter.ToInt32(blob, 1);
        
        if (originalLength == 0) return Array.Empty<byte>();

        if (mode == MODE_NO_COMPRESSION)
        {
            var raw = new byte[originalLength];
            Array.Copy(blob, 5, raw, 0, originalLength);
            return raw;
        }

        int offset = 5;
        ushort tableCount = BitConverter.ToUInt16(blob, offset);
        offset += 2;

        var lenghts = new Dictionary<byte, int>();
        for (int i = 0; i < tableCount; i++)
        {
            byte symbol = blob[offset++];
            byte len = blob[offset++];
            lenghts[symbol] = len;
        }
        
        var canonicalCodes = CanonicalCodes(lenghts);
        var codeToSymbol = canonicalCodes.ToDictionary(kv => kv.Value, kv => kv.Key);

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
                if (codeToSymbol.TryGetValue(candidate, out byte symbol))
                {
                    output[outIndex++] = symbol;
                    currentBits.Clear();
                }
            }
        }

        return output;
    }
}


