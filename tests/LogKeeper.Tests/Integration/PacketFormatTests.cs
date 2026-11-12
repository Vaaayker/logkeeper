using System.Text;
using LogKeeper.Integration;

namespace LogKeeper.Tests.Integration;

public class PacketFormatTests
{
    private const string Password = "correct horse";
    private static readonly byte[] Payload =
        Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("log line; ", 40)));

    // PBKDF2 на 100000 ітерацій повільний, тож один пакет на всі тести
    private static readonly byte[] Packet = PacketFormat.Build(Payload, Password);

    [Fact]
    public void ReadBuild_ReturnsOriginal()
    {
        Assert.Equal(Payload, PacketFormat.Read(Packet, Password));
    }

    [Fact]
    public void ReadBuild_EmptyPayload()
    {
        Assert.Empty(PacketFormat.Read(PacketFormat.Build([], Password), Password));
    }

    [Fact]
    public void Build_HeaderLayout()
    {
        Assert.Equal("LOG3"u8.ToArray(), Packet[..4]);
        Assert.Equal(PacketFormat.Version, Packet[4]);

        uint len = BitConverter.ToUInt32(Packet, 33);
        Assert.Equal(PacketFormat.HeaderSize + len + PacketFormat.TagSize, (uint)Packet.Length);
    }

    [Fact]
    public void Build_SamePayloadTwice_DifferentPackets()
    {
        Assert.NotEqual(Packet, PacketFormat.Build(Payload, Password));
    }

    [Fact]
    public void Read_WrongPassword_Throws()
    {
        Assert.Throws<FormatException>(() => PacketFormat.Read(Packet, "wrong"));
    }

    [Theory]
    [InlineData(0)]     // magic
    [InlineData(4)]     // version
    [InlineData(10)]    // salt
    [InlineData(25)]    // nonce
    [InlineData(33)]    // len
    [InlineData(40)]    // ciphertext
    [InlineData(-1)]    // останній байт тегу
    public void Read_ChangedByte_Throws(int index)
    {
        var broken = (byte[])Packet.Clone();
        broken[index < 0 ? broken.Length + index : index] ^= 0x01;

        Assert.Throws<FormatException>(() => PacketFormat.Read(broken, Password));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(32)]
    [InlineData(68)]
    public void Read_Truncated_Throws(int cut)
    {
        var truncated = Packet[..^cut];

        Assert.Throws<FormatException>(() => PacketFormat.Read(truncated, Password));
    }

    [Fact]
    public void Read_NullOrEmpty_Throws()
    {
        Assert.Throws<FormatException>(() => PacketFormat.Read(null!, Password));
        Assert.Throws<FormatException>(() => PacketFormat.Read([], Password));
    }
}
