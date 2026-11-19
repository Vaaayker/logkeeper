using LogKeeper.Integration.Replication;

namespace LogKeeper.Tests.Integration;

/// <summary>Якщо Python недоступний, тести нічого не перевіряють і проходять (xUnit 2 не має динамічного Skip).</summary>
public class ReplicationBridgeTests
{
    private static string WriteScript(string body)
    {
        string path = Path.Combine(Path.GetTempPath(), $"lk-{Guid.NewGuid():N}.py");
        File.WriteAllText(path, body);
        return path;
    }

    [Fact]
    public void RunPython_ReturnsStdout()
    {
        if (ReplicationBridge.FindPython() is null) return;
        string script = WriteScript("import sys\nprint('args:' + ','.join(sys.argv[1:]))\n");
        try
        {
            string output = ReplicationBridge.RunPython(script, "--scenario", "none", "a b");
            Assert.Equal("args:--scenario,none,a b", output.Trim());
        }
        finally { File.Delete(script); }
    }

    [Fact]
    public void RunPython_NonZeroExit_ThrowsWithStderr()
    {
        if (ReplicationBridge.FindPython() is null) return;
        string script = WriteScript("import sys\nsys.stderr.write('boom')\nsys.exit(3)\n");
        try
        {
            var ex = Assert.Throws<ReplicationException>(() => ReplicationBridge.RunPython(script));
            Assert.Equal(3, ex.ExitCode);
            Assert.Contains("boom", ex.StandardError);
        }
        finally { File.Delete(script); }
    }

    [Fact]
    public void RunPython_MissingScript_Throws()
    {
        Assert.Throws<FileNotFoundException>(() => ReplicationBridge.RunPython("/no/such/replication.py"));
    }

    [Fact]
    public void PythonReplicaStore_WritesFiles_RunsScript_ReadsSurvivors()
    {
        if (ReplicationBridge.FindPython() is null) return;
        // Тестовий «replication.py»: сценарій node-loss видаляє папку вузла A.
        string script = WriteScript(
            "import sys, shutil, os\n" +
            "d, sc = sys.argv[1], sys.argv[2]\n" +
            "if sc == 'node-loss': shutil.rmtree(os.path.join(d, 'A'))\n" +
            "print('ok')\n");
        try
        {
            using var store = new PythonReplicaStore(script, "{dir} {scenario}");
            store.Write("A", new[] { new byte[] { 1 }, new byte[] { 2 } });
            store.Write("B", new[] { new byte[] { 3 } });
            store.Apply(FailureScenario.Parse("node-loss", new[] { "A", "B" }));

            var left = store.ReadAll();
            Assert.Single(left);
            Assert.Equal(3, left[0][0]);
            Assert.Equal("ok", store.LastOutput.Trim());
        }
        finally { File.Delete(script); }
    }
}
