using LogKeeper.Security;
using Xunit;

namespace LogKeeper.Tests.Security;

public class KeyDerivationTests
{
    [Fact]
    public void DeriveKeys_ReturnsTwo32ByteKeys()
    {
        byte[] salt = new byte[16];

        var keys = KeyDerivation.DeriveKeys("password", salt);

        Assert.Equal(32, keys.KEnc.Length);
        Assert.Equal(32, keys.KMac.Length);
    }

    [Fact]
    public void SamePasswordAndSalt_ReturnSameKeys()
    {
        byte[] salt = new byte[16];

        var first = KeyDerivation.DeriveKeys("password", salt);
        var second = KeyDerivation.DeriveKeys("password", salt);

        Assert.Equal(first.KEnc, second.KEnc);
        Assert.Equal(first.KMac, second.KMac);
    }

    [Fact]
    public void DifferentSalt_ReturnsDifferentKeys()
    {
        byte[] salt1 = new byte[16];
        byte[] salt2 = new byte[16];
        salt2[0] = 1;

        var first = KeyDerivation.DeriveKeys("password", salt1);
        var second = KeyDerivation.DeriveKeys("password", salt2);

        Assert.NotEqual(first.KEnc, second.KEnc);
        Assert.NotEqual(first.KMac, second.KMac);
    }

    [Fact]
    public void DifferentPassword_ReturnsDifferentKeys()
    {
        byte[] salt = new byte[16];

        var first = KeyDerivation.DeriveKeys("password1", salt);
        var second = KeyDerivation.DeriveKeys("password2", salt);

        Assert.NotEqual(first.KEnc, second.KEnc);
        Assert.NotEqual(first.KMac, second.KMac);
    }

    [Fact]
    public void InvalidSaltLength_ThrowsArgumentException()
    {
        byte[] salt = new byte[15];

        Assert.Throws<ArgumentException>(
            () => KeyDerivation.DeriveKeys("password", salt));
    }
}
