using System.Text.Json;
using MyAgent.Workspace;

namespace MyAgent.Tools;

public class ListFilesTool : ITool
{
    private readonly AgentWorkspace _workspace;

    public string Name =>
        "list_files";

    public string Description =>
        "Показывает файлы и папки внутри workspace.";

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
                            "Путь к папке относительно workspace. "
                            + "Используй . для корня."
                    }
                }
            });

    public ListFilesTool(
        AgentWorkspace workspace)
    {
        _workspace = workspace;
    }

    public Task<ToolResult> ExecuteAsync(
        JsonElement arguments,
        CancellationToken cancellationToken = default)
    {
        try
        {
            string path = ".";

            if (arguments.TryGetProperty(
                    "path",
                    out JsonElement pathElement)
                &&
                pathElement.ValueKind ==
                    JsonValueKind.String)
            {
                path =
                    pathElement.GetString()
                    ?? ".";
            }

            string fullPath =
                _workspace.ResolvePath(path);

            if (!Directory.Exists(fullPath))
            {
                return Task.FromResult(
                    ToolResult.Fail(
                        $"Directory not found: {path}"));
            }

            string[] entries =
                Directory
                    .EnumerateFileSystemEntries(
                        fullPath)
                    .OrderBy(entry => entry)
                    .Select(entry =>
                    {
                        string relativePath =
                            Path.GetRelativePath(
                                _workspace.RootPath,
                                entry);

                        string prefix =
                            Directory.Exists(entry)
                                ? "[DIR]"
                                : "[FILE]";

                        return
                            $"{prefix} {relativePath}";
                    })
                    .ToArray();

            string content =
                entries.Length == 0
                    ? "(empty)"
                    : string.Join(
                        Environment.NewLine,
                        entries);

            return Task.FromResult(
                ToolResult.Ok(content));
        }
        catch (Exception exception)
        {
            return Task.FromResult(
                ToolResult.Fail(
                    exception.Message));
        }
    }
}