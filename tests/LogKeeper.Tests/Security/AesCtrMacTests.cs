using System.Text;
using LogKeeper.Security;

namespace LogKeeper.Tests.Security;

public class AesCtrMacTests
{
    // Еталон отримано через openssl:
    // openssl enc -aes-256-ctr -K <Key> -iv <Nonce>00000001, відкритий текст 00..1F
    private static readonly byte[] Key = Convert.FromHexString(
        "F6D66D6BD52D59BB0796365879EFF886C66DD51A5B6A99744B50590C87A23884");
    private static readonly byte[] Nonce = Convert.FromHexString("00FAAC24C0543B59DA48D90B");
    private static readonly byte[] Plain = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
    private static readonly byte[] Expected = Convert.FromHexString(
        "A676BD7EF49228E9ED8E499391CC72A2AD710FF8346F92F5939180F0F35FCB43");

    [Fact]
    public void AesCtr_MatchesOpenSsl()
    {
        Assert.Equal(Expected, AesCtr.Crypt(Key, Nonce, Plain));
    }

    [Fact]
    public void AesCtr_PartialBlock_IsPrefixOfFullOutput()
    {
        Assert.Equal(Expected[..20], AesCtr.Crypt(Key, Nonce, Plain[..20]));
    }

    [Fact]
    public void AesCtr_TwiceReturnsOriginal()
    {
        Assert.Equal(Plain, AesCtr.Crypt(Key, Nonce, AesCtr.Crypt(Key, Nonce, Plain)));
    }

    [Fact]
    public void AesCtr_Empty_ReturnsEmpty()
    {
        Assert.Empty(AesCtr.Crypt(Key, Nonce, []));
    }

    [Theory]
    [InlineData(16, 12)]
    [InlineData(32, 16)]
    public void AesCtr_WrongKeyOrNonceSize_Throws(int keySize, int nonceSize)
    {
        Assert.Throws<ArgumentException>(() => AesCtr.Crypt(new byte[keySize], new byte[nonceSize], Plain));
    }

    [Fact]
    public void Mac_MatchesRfc4231Case2_SplitAnywhere()
    {
        // RFC 4231, test case 2; дані розбиті на "заголовок" і "шифротекст"
        var key = Encoding.ASCII.GetBytes("Jefe");
        var header = Encoding.ASCII.GetBytes("what do ya ");
        var body = Encoding.ASCII.GetBytes("want for nothing?");

        Assert.Equal(
            Convert.FromHexString("5BDCC146BF60754E6A042426089575C75A003F089D2739839DEC58B964EC3843"),
            Mac.Compute(key, header, body));
    }

    [Fact]
    public void Mac_Verify_AcceptsCorrectTag_RejectsChanged()
    {
        var tag = Mac.Compute(Key, [1, 2, 3], Plain);
        Assert.True(Mac.Verify(Key, [1, 2, 3], Plain, tag));

        tag[0] ^= 1;
        Assert.False(Mac.Verify(Key, [1, 2, 3], Plain, tag));
        Assert.False(Mac.Verify(Key, [1, 2, 3], Plain, tag[..16]));
    }
    [Fact]
    public void AesCtr_DifferentNonce_ProducesDifferentCiphertext()
    {
        byte[] nonce2 = (byte[])Nonce.Clone();
        nonce2[0] ^= 1;

        byte[] first = AesCtr.Crypt(Key, Nonce, Plain);
        byte[] second = AesCtr.Crypt(Key, nonce2, Plain);

        Assert.NotEqual(first, second);
    }
    [Fact]
    public void Mac_Verify_RejectsChangedHeaderOrCiphertext()
    {
        byte[] header = [1, 2, 3];
        byte[] ciphertext = [4, 5, 6, 7];

        byte[] tag = Mac.Compute(Key, header, ciphertext);

        byte[] changedHeader = (byte[])header.Clone();
        changedHeader[0] ^= 1;

        byte[] changedCiphertext = (byte[])ciphertext.Clone();
        changedCiphertext[0] ^= 1;

        Assert.False(Mac.Verify(Key, changedHeader, ciphertext, tag));
        Assert.False(Mac.Verify(Key, header, changedCiphertext, tag));
    }
}

