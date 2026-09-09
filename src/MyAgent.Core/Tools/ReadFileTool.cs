using System.Text.Json;
using MyAgent.Workspace;

namespace MyAgent.Tools;

public class ReadFileTool : ITool
{
    private readonly AgentWorkspace _workspace;

    public string Name =>
        "read_file";

    public string Description =>
        "Читает текстовый файл внутри workspace.";

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

            string fullPath =
                _workspace.ResolvePath(path);

            if (!File.Exists(fullPath))
            {
                return ToolResult.Fail(
                    $"File not found: {path}");
            }

            string content =
                await File.ReadAllTextAsync(
                    fullPath,
                    cancellationToken);

            return ToolResult.Ok(content);
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