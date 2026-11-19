namespace LogKeeper.Security;

/// <summary>
/// Навмисно слабкий шифр для тижня змагань: XOR з ключем, що повторюється (довжина 8 байтів).
/// Лише для демонстрації атаки — реплік не захищає (там AES-256-CTR + HMAC).
/// </summary>
public static class WeakCipher
{
    public static byte[] Crypt(byte[] data, byte[] key)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length == 0) throw new ArgumentException("Ключ порожній.", nameof(key));

        var result = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
            result[i] = (byte)(data[i] ^ key[i % key.Length]);
        return result;
    }
}

/// <summary>
/// Модуль атаки: ГА відновлює ключ <see cref="WeakCipher"/> за одним шифротекстом (AES не атакуємо).
/// Ген — один байт ключа (g · 256). Fitness — правдоподібність відкритого тексту як англомовного
/// ASCII-журналу (частоти літер, пробіли, цифри; недруковані символи штрафуються).
/// </summary>
public static class WeakCipherAttack
{
    public const int KeyLength = 8;
    public const int PopulationSize = 80;
    public const int Generations = 150;

    /// <summary>Повертає відновлений відкритий текст.</summary>
    public static byte[] Recover(byte[] ciphertext, int seed)
        => WeakCipher.Crypt(ciphertext, RecoverKey(ciphertext, seed));

    public static byte[] RecoverKey(byte[] ciphertext, int seed, int keyLength = KeyLength)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);
        if (ciphertext.Length < keyLength)
            throw new ArgumentException("Шифротекст коротший за ключ.", nameof(ciphertext));

        var result = GeneticAlgorithm.Run(
            genes => Score(WeakCipher.Crypt(ciphertext, ToKey(genes))) / ciphertext.Length,
            keyLength, PopulationSize, Generations, seed);
        return ToKey(result.BestGenes);
    }

    public static byte[] ToKey(double[] genes) =>
        genes.Select(g => (byte)Math.Clamp((int)(g * 256), 0, 255)).ToArray();

    // Відносні частоти англійських літер (‰), a..z.
    private static readonly int[] LetterFreq =
    {
        82, 15, 28, 43, 127, 22, 20, 61, 70, 2, 8, 40, 24, 67, 75, 19, 1, 60, 63, 91, 28, 10, 24, 2, 20, 1,
    };

    /// <summary>Сумарна «правдоподібність» тексту; більше — краще.</summary>
    public static double Score(byte[] text)
    {
        double s = 0;
        foreach (byte c in text)
        {
            if (c >= 'a' && c <= 'z') s += 1 + LetterFreq[c - 'a'] / 20.0;
            else if (c == ' ') s += 6;
            else if (c >= 'A' && c <= 'Z') s += 1;
            else if (c >= '0' && c <= '9') s += 1;
            else if (c == '\n' || c == '=' || c == ':' || c == '-' || c == '.' || c == ',' || c == '_') s += 0.5;
            else if (c >= 32 && c < 127) s += 0;
            else s -= 5;
        }
        return s;
    }
}
