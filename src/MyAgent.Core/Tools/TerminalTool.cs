using System.Diagnostics;
using System.Text.Json;
using MyAgent.Workspace;
using System.Text;

namespace MyAgent.Tools;

public class TerminalTool : ITool
{
    private readonly int _timeoutSeconds;

    private readonly AgentWorkspace _workspace;

    public string Name =>
        "run_terminal";

    public string Description =>
        "Выполняет PowerShell-команду. "
        + "Команда запускается в корне workspace. "
        + "Процесс неинтерактивный и имеет ограничение по времени.";

    public JsonElement ParametersSchema =>
        JsonSerializer.SerializeToElement(
            new
            {
                type = "object",

                properties = new
                {
                    command = new
                    {
                        type = "string",

                        description =
                            "PowerShell-команда для выполнения."
                    }
                },

                required = new[]
                {
                    "command"
                }
            });

    public TerminalTool(
    AgentWorkspace workspace,
    int timeoutSeconds)
    {
        if (timeoutSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeoutSeconds));
        }

        _workspace = workspace;
        _timeoutSeconds = timeoutSeconds;
    }

    public async Task<ToolResult> ExecuteAsync(
        JsonElement arguments)
    {
        if (!arguments.TryGetProperty(
                "command",
                out JsonElement commandElement)
            ||
            commandElement.ValueKind !=
                JsonValueKind.String)
        {
            return ToolResult.Fail(
                "Missing parameter: command.");
        }

        string command =
            commandElement.GetString()
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(command))
        {
            return ToolResult.Fail(
                "Command cannot be empty.");
        }

        var startInfo =
            new ProcessStartInfo
            {
                FileName = "powershell.exe",

                WorkingDirectory =
                    _workspace.RootPath,

                RedirectStandardOutput =
                    true,

                RedirectStandardError =
                    true,

                RedirectStandardInput =
                    true,

                UseShellExecute =
                    false,

                CreateNoWindow =
                    true,
                
                StandardOutputEncoding =
                    Encoding.UTF8,

                StandardErrorEncoding =
                    Encoding.UTF8
            };

        string powershellCommand =
            "[Console]::OutputEncoding = "
            + "[System.Text.Encoding]::UTF8; "
            + "$OutputEncoding = "
            + "[System.Text.Encoding]::UTF8; "
            + command;

        startInfo.ArgumentList.Add(
            "-NoProfile");

        startInfo.ArgumentList.Add(
            "-NonInteractive");

        startInfo.ArgumentList.Add(
            "-Command");

        startInfo.ArgumentList.Add(
            powershellCommand);

        using var process =
            new Process
            {
                StartInfo = startInfo
            };

        try
        {
            process.Start();

            Task<string> stdoutTask =
                process.StandardOutput
                    .ReadToEndAsync();

            Task<string> stderrTask =
                process.StandardError
                    .ReadToEndAsync();

            using var timeout =
                new CancellationTokenSource(
                    TimeSpan.FromSeconds(
                        _timeoutSeconds));

            try
            {
                await process.WaitForExitAsync(
                    timeout.Token);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    process.Kill(
                        entireProcessTree: true);
                }
                catch
                {
                }

                return ToolResult.Fail(
                    $"Command timed out after {_timeoutSeconds} seconds.");
            }

            string stdout =
                await stdoutTask;

            string stderr =
                await stderrTask;

            string result =
                $"Exit code: {process.ExitCode}"
                + Environment.NewLine
                + Environment.NewLine
                + "STDOUT:"
                + Environment.NewLine
                + (string.IsNullOrWhiteSpace(stdout)
                    ? "(empty)"
                    : stdout.TrimEnd())
                + Environment.NewLine
                + Environment.NewLine
                + "STDERR:"
                + Environment.NewLine
                + (string.IsNullOrWhiteSpace(stderr)
                    ? "(empty)"
                    : stderr.TrimEnd());

            if (process.ExitCode != 0)
            {
                return ToolResult.Fail(
                    result);
            }

            return ToolResult.Ok(
                result);
        }
        catch (Exception exception)
        {
            return ToolResult.Fail(
                exception.Message);
        }
    }
}