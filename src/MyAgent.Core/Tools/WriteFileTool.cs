using System.Text.Json;
using MyAgent.Diff;
using MyAgent.Guardrails;
using MyAgent.Workspace;

namespace MyAgent.Tools;

public class WriteFileTool
    : ITool,
      IToolApprovalPreviewProvider
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

    public async Task<ToolApprovalPreview>
        CreateApprovalPreviewAsync(
            JsonElement arguments,
            CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            if (!arguments.TryGetProperty(
                    "path",
                    out JsonElement pathElement)
                ||
                pathElement.ValueKind !=
                    JsonValueKind.String
                ||
                !arguments.TryGetProperty(
                    "content",
                    out JsonElement contentElement)
                ||
                contentElement.ValueKind !=
                    JsonValueKind.String)
            {
                return new ToolApprovalPreview(
                    arguments.GetRawText());
            }

            string path =
                pathElement.GetString()
                ?? string.Empty;

            string newContent =
                contentElement.GetString()
                ?? string.Empty;

            string fullPath =
                _workspace.ResolvePath(
                    path);

            string? oldContent =
                null;

            if (File.Exists(
                    fullPath))
            {
                oldContent =
                    await File.ReadAllTextAsync(
                        fullPath,
                        cancellationToken);
            }

            bool isNewFile =
                oldContent is null;

            string operationText;

            if (isNewFile)
            {
                operationText =
                    "Файл будет создан.";
            }
            else if (string.Equals(
                        oldContent,
                        newContent,
                        StringComparison.Ordinal))
            {
                operationText =
                    "Содержимое файла не изменится.";
            }
            else
            {
                operationText =
                    "Файл будет полностью перезаписан.";
            }

            string diffText;

            if (isNewFile
                &&
                newContent.Length == 0)
            {
                diffText =
                    "(будет создан пустой файл)";
            }
            else
            {
                IReadOnlyList<LineDiffLine> diff =
                    LineDiffEngine.Create(
                        oldContent
                        ?? string.Empty,
                        newContent);

                diffText =
                    LineDiffTextFormatter.Format(
                        diff,
                        contextLines: 2);
            }

            string previewText =
                "Файл: "
                + path
                + Environment.NewLine
                + Environment.NewLine
                + operationText
                + Environment.NewLine
                + Environment.NewLine
                + "Изменения:"
                + Environment.NewLine
                + Environment.NewLine
                + diffText;

            return new ToolApprovalPreview(
                previewText,
                new FileChangePreview(
                    path,
                    oldContent,
                    newContent));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new ToolApprovalPreview(
                "Не удалось подготовить preview."
                + Environment.NewLine
                + Environment.NewLine
                + exception.Message
                + Environment.NewLine
                + Environment.NewLine
                + arguments.GetRawText());
        }
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