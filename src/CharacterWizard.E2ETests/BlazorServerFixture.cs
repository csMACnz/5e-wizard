using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace CharacterWizard.E2ETests;

/// <summary>
/// Starts the Blazor WebAssembly development server as a subprocess and exposes its
/// base URL.  Shared across all tests in the "Blazor E2E" collection so the server
/// is only started once per test run.
/// </summary>
public sealed class BlazorServerFixture : IAsyncLifetime
{
    private Process? _process;
    private readonly int _port = FindFreePort();

    public string BaseUrl => $"http://localhost:{_port}";

    public async Task InitializeAsync()
    {
        var projectPath = FindClientProjectPath();

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{projectPath}\" --urls http://localhost:{_port}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };

        var serverReady = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        _process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null &&
                (e.Data.Contains("Application started", StringComparison.OrdinalIgnoreCase) ||
                 e.Data.Contains("Now listening on", StringComparison.OrdinalIgnoreCase)))
            {
                serverReady.TrySetResult(true);
            }
        };

        _process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null &&
                (e.Data.Contains("Application started", StringComparison.OrdinalIgnoreCase) ||
                 e.Data.Contains("Now listening on", StringComparison.OrdinalIgnoreCase)))
            {
                serverReady.TrySetResult(true);
            }
        };

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        // Wait up to 90 seconds for the server to start, then fall back to polling
        var ready = await Task.WhenAny(serverReady.Task, Task.Delay(TimeSpan.FromSeconds(90)));
        if (ready != serverReady.Task)
        {
            // Server did not announce itself; poll the URL as a fallback
            await PollForReadyAsync(BaseUrl, TimeSpan.FromSeconds(90));
        }

        // Brief additional wait to ensure all Blazor WASM assets are initialised
        await Task.Delay(TimeSpan.FromSeconds(2));
    }

    public Task DisposeAsync()
    {
        try
        {
            _process?.Kill(entireProcessTree: true);
        }
        catch
        {
            // Ignore errors during teardown
        }

        _process?.Dispose();
        return Task.CompletedTask;
    }

    private static async Task PollForReadyAsync(string url, TimeSpan timeout)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch
            {
                // Not ready yet
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"Blazor dev server did not become ready at {url} within {timeout}.");
    }

    private static string FindClientProjectPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "CharacterWizard.Client");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the CharacterWizard.Client project directory. " +
            $"Started search from: {AppContext.BaseDirectory}");
    }

    private static int FindFreePort()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)socket.LocalEndPoint!).Port;
    }
}
