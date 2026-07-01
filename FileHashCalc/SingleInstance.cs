using System.IO;
using System.IO.Pipes;

namespace FileHashCalc;

public sealed class SingleInstance : IDisposable
{
    private const string MutexName = "FileHashCalc_SingleInstance";
    private const string PipeName = "FileHashCalc_Pipe";

    private readonly Mutex _mutex;
    private readonly bool _isFirst;
    private CancellationTokenSource? _cts;

    public event Action<string[]>? FilesReceived;

    public SingleInstance()
    {
        _mutex = new Mutex(true, MutexName, out _isFirst);
    }

    public bool IsFirstInstance => _isFirst;

    public void SendToRunningInstance(string[] args)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(3000);
            using var writer = new StreamWriter(client) { AutoFlush = true };
            writer.WriteLine(string.Join("|", args));
        }
        catch
        {
            // Running instance not reachable — ignore
        }
    }

    public void StartListening()
    {
        _cts = new CancellationTokenSource();
        Task.Run(() => ListenLoop(_cts.Token));
    }

    private async Task ListenLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(
                    PipeName, PipeDirection.In, 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(ct);
                using var reader = new StreamReader(server);

                var line = await reader.ReadLineAsync(ct);
                if (!string.IsNullOrEmpty(line))
                {
                    var files = line.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    if (files.Length > 0)
                        FilesReceived?.Invoke(files);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Pipe error — retry
            }
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _mutex.ReleaseMutex();
        _mutex.Dispose();
    }
}
