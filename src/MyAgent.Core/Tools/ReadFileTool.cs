using System.Text;
using System.Text.Json;
using MyAgent.Workspace;

namespace MyAgent.Tools;

public class ReadFileTool : ITool
{
    private readonly AgentWorkspace _workspace;

    public string Name =>
        "read_file";

    public string Description =>
        "Читает текстовый файл внутри workspace "
        + "целиком или по диапазону строк.";

    public JsonElement ParametersSchema =>
        JsonSerializer.SerializeToElement(
            new
            {
                type = "object",

                properties = new
                {
                    path = new
                    {
                        type = "string",

                        description =
                            "Путь к файлу относительно workspace."
                    },

                    start_line = new
                    {
                        type = "integer",

                        minimum = 1,

                        description =
                            "Первая строка для чтения. "
                            + "Нумерация начинается с 1. "
                            + "Если не указана, чтение начинается "
                            + "с первой строки."
                    },

                    end_line = new
                    {
                        type = "integer",

                        minimum = 1,

                        description =
                            "Последняя строка для чтения включительно. "
                            + "Нумерация начинается с 1. "
                            + "Если не указана, чтение продолжается "
                            + "до конца файла."
                    }
                },

                required = new[]
                {
                    "path"
                }
            });

    public ReadFileTool(
        AgentWorkspace workspace)
    {
        _workspace = workspace;
    }

    public async Task<ToolResult> ExecuteAsync(
        JsonElement arguments,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!arguments.TryGetProperty(
                    "path",
                    out JsonElement pathElement)
                ||
                pathElement.ValueKind !=
                    JsonValueKind.String)
            {
                return ToolResult.Fail(
                    "Missing parameter: path.");
            }

            string path =
                pathElement.GetString()
                ?? string.Empty;

            if (!TryReadOptionalPositiveInt(
                    arguments,
                    "start_line",
                    out int? startLine,
                    out string? startLineError))
            {
                return ToolResult.Fail(
                    startLineError
                    ?? "Invalid parameter: start_line.");
            }

            if (!TryReadOptionalPositiveInt(
                    arguments,
                    "end_line",
                    out int? endLine,
                    out string? endLineError))
            {
                return ToolResult.Fail(
                    endLineError
                    ?? "Invalid parameter: end_line.");
            }

            if (startLine.HasValue
                &&
                endLine.HasValue
                &&
                endLine.Value < startLine.Value)
            {
                return ToolResult.Fail(
                    "end_line cannot be less than start_line.");
            }

            string fullPath =
                _workspace.ResolvePath(path);

            if (!File.Exists(fullPath))
            {
                return ToolResult.Fail(
                    $"File not found: {path}");
            }

            bool hasRange =
                startLine.HasValue
                ||
                endLine.HasValue;

            if (!hasRange)
            {
                string content =
                    await File.ReadAllTextAsync(
                        fullPath,
                        cancellationToken);

                return ToolResult.Ok(content);
            }

            int firstLine =
                startLine
                ?? 1;

            var output =
                new StringBuilder();

            int currentLine =
                0;

            using var stream =
                new FileStream(
                    fullPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 4096,
                    useAsync: true);

            using var reader =
                new StreamReader(stream);

            while (!endLine.HasValue
                   ||
                   currentLine < endLine.Value)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                string? line =
                    await reader.ReadLineAsync(
                        cancellationToken);

                if (line is null)
                {
                    break;
                }

                currentLine++;

                if (currentLine < firstLine)
                {
                    continue;
                }

                if (output.Length > 0)
                {
                    output.AppendLine();
                }

                output
                    .Append(currentLine)
                    .Append(" | ")
                    .Append(line);
            }

            if (currentLine == 0)
            {
                return ToolResult.Ok(
                    "(file is empty)");
            }

            if (output.Length == 0)
            {
                return ToolResult.Ok(
                    "(no lines in requested range)");
            }

            return ToolResult.Ok(
                output.ToString());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return ToolResult.Fail(
                exception.Message);
        }
    }

    private static bool TryReadOptionalPositiveInt(
        JsonElement arguments,
        string name,
        out int? value,
        out string? error)
    {
        value = null;
        error = null;

        if (!arguments.TryGetProperty(
                name,
                out JsonElement element))
        {
            return true;
        }

        if (element.ValueKind !=
                JsonValueKind.Number
            ||
            !element.TryGetInt32(
                out int parsedValue))
        {
            error =
                $"{name} must be an integer.";

            return false;
        }

        if (parsedValue <= 0)
        {
            error =
                $"{name} must be a positive integer.";

            return false;
        }

        value =
            parsedValue;

        return true;
    }
}