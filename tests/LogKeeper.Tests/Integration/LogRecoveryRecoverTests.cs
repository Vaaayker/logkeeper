using LogKeeper.Core;
using LogKeeper.Integration;

namespace LogKeeper.Tests.Integration;

public class LogRecoveryRecoverTests
{
    private const string Password = "replica-pass";

    private static readonly LogRecord A = new(1000, 1, 10, 5, 1, 1, false, 0);
    private static readonly LogRecord B = new(2000, 2, 20, -5, 2, 2, true, 1);
    private static readonly LogRecord C = new(3000, 3, 30, 0, 3, 4, false, 0xF);
    private static readonly LogRecord D = new(4000, 4, 40, 7, 4, 0, true, 2);

    private static byte[] Pack(params LogRecord[] records)
        => PacketFormat.Build(RecordCodec.SerializeMany(records), Password);

    [Fact]
    public void Recover_ThreePacketsOneBroken_ReturnsRecordsWithoutDuplicates()
    {
        var broken = Pack(D);
        broken[^1] ^= 0xFF;

        var packets = new[] { Pack(C, A, B), broken, Pack(B, C, A) };

        Assert.Equal([A, B, C], LogRecovery.Recover(packets, Password));
    }

    [Fact]
    public void Recover_CombinesDifferentReplicas()
    {
        var packets = new[] { Pack(A, B), Pack(B, C) };

        Assert.Equal([A, B, C], LogRecovery.Recover(packets, Password));
    }

    [Fact]
    public void Recover_AllBrokenOrNull_ReturnsEmpty()
    {
        var packets = new byte[][] { [1, 2, 3], null!, Pack(A)[..^5] };

        Assert.Empty(LogRecovery.Recover(packets, Password));
    }

    [Fact]
    public void Recover_WrongPassword_ReturnsEmpty()
    {
        Assert.Empty(LogRecovery.Recover([Pack(A, B)], "other"));
    }

    [Fact]
    public void Recover_PacketWithInvalidRecord_IsSkipped()
    {
        var badPayload = RecordCodec.SerializeMany([A]);
        badPayload[15] = 0xF0; // зарезервовані біти прапорців
        var packets = new[] { PacketFormat.Build(badPayload, Password), Pack(B) };

        Assert.Equal([B], LogRecovery.Recover(packets, Password));
    }
}
