namespace LogKeeper.Integration;

/// <summary>Формат репліки LOG3: відкритий заголовок + шифроване навантаження + HMAC.
/// Сигнатури — чернетка, уточнити за FORMAT_SPEC_P3.md.</summary>
public static class PacketFormat
{
    public static byte[] Build(byte[] payload, string password) => throw new NotImplementedException();

    /// <summary>Перевіряє magic, довжину, HMAC; при помилці — FormatException (без аварії програми).</summary>
    public static byte[] Read(byte[] data, string password) => throw new NotImplementedException();
}
