using System.Diagnostics;

namespace LogKeeper.Integration.Replication;

/// <summary>
/// Міст до replication.py (готовий файл на Python, змінювати його заборонено).
/// Запускає `python replication.py ...` як окремий процес (System.Diagnostics.Process),
/// повертає stdout; ненульовий код виходу -> <see cref="ReplicationException"/> з текстом stderr.
/// Обмін даними — через файли в робочій папці (див. <see cref="PythonReplicaStore"/>, D-005).
/// </summary>
public static class ReplicationBridge
{
    /// <summary>Скільки чекати завершення процесу за замовчуванням.</summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(2);

    public static string RunPython(string scriptPath, params string[] args)
        => RunPython(scriptPath, workingDirectory: null, stdin: null, DefaultTimeout, args);

    /// <summary>Повна форма: робоча папка, необов'язковий stdin і таймаут.</summary>
    public static string RunPython(string scriptPath, string? workingDirectory, string? stdin,
        TimeSpan timeout, params string[] args)
    {
        ArgumentNullException.ThrowIfNull(scriptPath);
        ArgumentNullException.ThrowIfNull(args);
        if (!File.Exists(scriptPath))
            throw new FileNotFoundException("Скрипт Python не знайдено.", scriptPath);

        string python = FindPython()
            ?? throw new ReplicationException("Python не знайдено (шукали python3, python).", -1, "");

        var psi = new ProcessStartInfo(python)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = stdin is not null,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? Path.GetDirectoryName(Path.GetFullPath(scriptPath))!,
        };
        psi.ArgumentList.Add(Path.GetFullPath(scriptPath));
        foreach (var a in args) psi.ArgumentList.Add(a);
        psi.Environment["PYTHONIOENCODING"] = "utf-8";

        using var process = Process.Start(psi)
            ?? throw new ReplicationException("Не вдалося запустити процес Python.", -1, "");

        // stdout і stderr читаємо паралельно, інакше процес може заблокуватися на повному буфері.
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        if (stdin is not null)
        {
            process.StandardInput.Write(stdin);
            process.StandardInput.Close();
        }

        if (!process.WaitForExit(timeout))
        {
            try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
            throw new ReplicationException($"replication.py не завершився за {timeout.TotalSeconds:F0} с.", -1, "");
        }
        process.WaitForExit(); // дочитати буфери

        string stdout = stdoutTask.GetAwaiter().GetResult();
        string stderr = stderrTask.GetAwaiter().GetResult();
        if (process.ExitCode != 0)
            throw new ReplicationException(
                $"Python завершився з кодом {process.ExitCode}: {stderr.Trim()}", process.ExitCode, stderr);
        return stdout;
    }

    private static string? cachedPython;

    /// <summary>Шлях до інтерпретатора: змінна LOGKEEPER_PYTHON, далі python3 / python у PATH.</summary>
    public static string? FindPython()
    {
        if (cachedPython is not null) return cachedPython;

        string? env = Environment.GetEnvironmentVariable("LOGKEEPER_PYTHON");
        if (!string.IsNullOrWhiteSpace(env) && File.Exists(env))
            return cachedPython = env;

        string[] names = OperatingSystem.IsWindows()
            ? new[] { "python.exe", "python3.exe", "py.exe" }
            : new[] { "python3", "python" };
        string[] dirs = (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        foreach (var name in names)
            foreach (var dir in dirs)
            {
                string candidate = Path.Combine(dir, name);
                if (File.Exists(candidate))
                    return cachedPython = candidate;
            }
        return null;
    }
}

/// <summary>Помилка зовнішнього процесу replication.py.</summary>
public sealed class ReplicationException(string message, int exitCode, string stderr) : Exception(message)
{
    public int ExitCode { get; } = exitCode;
    public string StandardError { get; } = stderr;
}
