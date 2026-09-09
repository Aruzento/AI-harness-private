using System.Text.Json;
using MyAgent.Workspace;

namespace MyAgent.Tools;

public class WriteFileTool : ITool
{
    private readonly AgentWorkspace _workspace;

    public string Name =>
        "write_file";

    public string Description =>
        "Создаёт или полностью перезаписывает текстовый файл внутри workspace.";

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

                    content = new
                    {
                        type = "string",

                        description =
                            "Полное содержимое файла."
                    }
                },

                required = new[]
                {
                    "path",
                    "content"
                }
            });

    public WriteFileTool(
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

            if (!arguments.TryGetProperty(
                    "content",
                    out JsonElement contentElement)
                ||
                contentElement.ValueKind !=
                    JsonValueKind.String)
            {
                return ToolResult.Fail(
                    "Missing parameter: content.");
            }

            string path =
                pathElement.GetString()
                ?? string.Empty;

            string content =
                contentElement.GetString()
                ?? string.Empty;

            string fullPath =
                _workspace.ResolvePath(path);

            string? directory =
                Path.GetDirectoryName(
                    fullPath);

            if (!string.IsNullOrWhiteSpace(
                    directory))
            {
                Directory.CreateDirectory(
                    directory);
            }

            await File.WriteAllTextAsync(
                fullPath,
                content,
                cancellationToken);

            return ToolResult.Ok(
                $"File written: {path}");
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
}