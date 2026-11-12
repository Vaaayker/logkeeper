
using System;
using System.Text;
using LogKeeper.Security;

namespace LogKeeper.Tests.Security;

public class MacTests
{
    private static readonly byte[] Key =
        Encoding.ASCII.GetBytes("Jefe");

    private static readonly byte[] Header =
        Encoding.ASCII.GetBytes("what do ya ");

    private static readonly byte[] Ciphertext =
        Encoding.ASCII.GetBytes("want for nothing?");

    private static readonly byte[] ExpectedTag =
        Convert.FromHexString(
            "5BDCC146BF60754E6A042426089575C7" +
            "5A003F089D2739839DEC58B964EC3843");

    [Fact]
    public void Compute_Rfc4231TestCase2_ReturnsExpectedTag()
    {
        byte[] tag = Mac.Compute(Key, Header, Ciphertext);

        Assert.Equal(ExpectedTag, tag);
        Assert.Equal(32, tag.Length);
    }

    [Fact]
    public void Verify_ValidTag_ReturnsTrue()
    {
        Assert.True(Mac.Verify(Key, Header, Ciphertext, ExpectedTag));
    }

    [Fact]
    public void Verify_ModifiedKey_ReturnsFalse()
    {
        byte[] modifiedKey = (byte[])Key.Clone();
        modifiedKey[0] ^= 1;

        Assert.False(
            Mac.Verify(modifiedKey, Header, Ciphertext, ExpectedTag));
    }

    [Fact]
    public void Verify_ModifiedHeader_ReturnsFalse()
    {
        byte[] modifiedHeader = (byte[])Header.Clone();
        modifiedHeader[0] ^= 1;

        Assert.False(
            Mac.Verify(Key, modifiedHeader, Ciphertext, ExpectedTag));
    }

    [Fact]
    public void Verify_ModifiedCiphertext_ReturnsFalse()
    {
        byte[] modifiedCiphertext = (byte[])Ciphertext.Clone();
        modifiedCiphertext[0] ^= 1;

        Assert.False(
            Mac.Verify(Key, Header, modifiedCiphertext, ExpectedTag));
    }

    [Fact]
    public void Verify_WrongTagLength_ReturnsFalse()
    {
        byte[] shortTag = new byte[31];

        Assert.False(Mac.Verify(Key, Header, Ciphertext, shortTag));
    }

    [Fact]
    public void Compute_NullKey_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => Mac.Compute(null!, Header, Ciphertext));
    }

    [Fact]
    public void Compute_NullHeader_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => Mac.Compute(Key, null!, Ciphertext));
    }

    [Fact]
    public void Compute_NullCiphertext_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => Mac.Compute(Key, Header, null!));
    }

    [Fact]
    public void Verify_NullTag_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => Mac.Verify(Key, Header, Ciphertext, null!));
    }
}
