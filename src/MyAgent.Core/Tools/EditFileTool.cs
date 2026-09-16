using System.Text.Json;
using MyAgent.Guardrails;
using MyAgent.Workspace;

namespace MyAgent.Tools;

public class EditFileTool
    : ITool,
      IToolApprovalPreviewProvider
{
    private readonly AgentWorkspace _workspace;

    public string Name =>
        "edit_file";

    public string Description =>
        "Точечно редактирует существующий текстовый файл "
        + "внутри workspace. Заменяет old_text на new_text. "
        + "old_text должен встречаться в файле ровно один раз.";

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

                    old_text = new
                    {
                        type = "string",

                        description =
                            "Точный существующий фрагмент текста, "
                            + "который нужно заменить. "
                            + "Он должен встречаться ровно один раз."
                    },

                    new_text = new
                    {
                        type = "string",

                        description =
                            "Новый текст вместо old_text. "
                            + "Может быть пустым для удаления фрагмента."
                    }
                },

                required = new[]
                {
                    "path",
                    "old_text",
                    "new_text"
                }
            });

    public EditFileTool(
        AgentWorkspace workspace)
    {
        _workspace =
            workspace;
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

            if (!TryReadString(
                    arguments,
                    "path",
                    out string path)
                ||
                !TryReadString(
                    arguments,
                    "old_text",
                    out string oldText)
                ||
                !TryReadString(
                    arguments,
                    "new_text",
                    out string newText))
            {
                return new ToolApprovalPreview(
                    arguments.GetRawText());
            }

            if (string.IsNullOrEmpty(
                    oldText))
            {
                return new ToolApprovalPreview(
                    "Файл: "
                    + path
                    + Environment.NewLine
                    + Environment.NewLine
                    + "Не удалось подготовить изменение: "
                    + "old_text cannot be empty.");
            }

            string fullPath =
                _workspace.ResolvePath(
                    path);

            if (!File.Exists(
                    fullPath))
            {
                return new ToolApprovalPreview(
                    "Файл: "
                    + path
                    + Environment.NewLine
                    + Environment.NewLine
                    + "Не удалось подготовить изменение: "
                    + "файл не существует.");
            }

            string oldContent =
                await File.ReadAllTextAsync(
                    fullPath,
                    cancellationToken);

            int firstIndex =
                oldContent.IndexOf(
                    oldText,
                    StringComparison.Ordinal);

            if (firstIndex < 0)
            {
                return new ToolApprovalPreview(
                    "Файл: "
                    + path
                    + Environment.NewLine
                    + Environment.NewLine
                    + "Не удалось подготовить изменение: "
                    + "old_text не найден.");
            }

            int secondIndex =
                oldContent.IndexOf(
                    oldText,
                    firstIndex + oldText.Length,
                    StringComparison.Ordinal);

            if (secondIndex >= 0)
            {
                return new ToolApprovalPreview(
                    "Файл: "
                    + path
                    + Environment.NewLine
                    + Environment.NewLine
                    + "Не удалось подготовить изменение: "
                    + "old_text встречается больше одного раза.");
            }

            if (string.Equals(
                    oldText,
                    newText,
                    StringComparison.Ordinal))
            {
                return new ToolApprovalPreview(
                    "Файл: "
                    + path
                    + Environment.NewLine
                    + Environment.NewLine
                    + "Не удалось подготовить изменение: "
                    + "старый и новый текст идентичны.");
            }

            string newContent =
                oldContent
                    .Remove(
                        firstIndex,
                        oldText.Length)
                    .Insert(
                        firstIndex,
                        newText);

            string newPreview =
                newText.Length == 0
                    ? "(пусто — фрагмент будет удалён)"
                    : newText;

            string previewText =
                "Файл: "
                + path
                + Environment.NewLine
                + Environment.NewLine
                + "--- Текущий фрагмент"
                + Environment.NewLine
                + oldText
                + Environment.NewLine
                + Environment.NewLine
                + "+++ Новый фрагмент"
                + Environment.NewLine
                + newPreview;

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
            if (!TryReadString(
                    arguments,
                    "path",
                    out string path))
            {
                return ToolResult.Fail(
                    "Missing parameter: path.");
            }

            if (!TryReadString(
                    arguments,
                    "old_text",
                    out string oldText))
            {
                return ToolResult.Fail(
                    "Missing parameter: old_text.");
            }

            if (!TryReadString(
                    arguments,
                    "new_text",
                    out string newText))
            {
                return ToolResult.Fail(
                    "Missing parameter: new_text.");
            }

            if (string.IsNullOrEmpty(oldText))
            {
                return ToolResult.Fail(
                    "old_text cannot be empty.");
            }

            cancellationToken
                .ThrowIfCancellationRequested();

            string fullPath =
                _workspace.ResolvePath(
                    path);

            if (!File.Exists(fullPath))
            {
                return ToolResult.Fail(
                    $"File not found: {path}");
            }

            string content =
                await File.ReadAllTextAsync(
                    fullPath,
                    cancellationToken);

            int firstIndex =
                content.IndexOf(
                    oldText,
                    StringComparison.Ordinal);

            if (firstIndex < 0)
            {
                return ToolResult.Fail(
                    "old_text was not found in "
                    + $"file: {path}");
            }

            int secondIndex =
                content.IndexOf(
                    oldText,
                    firstIndex + oldText.Length,
                    StringComparison.Ordinal);

            if (secondIndex >= 0)
            {
                return ToolResult.Fail(
                    "old_text occurs more than once. "
                    + "Use a larger unique fragment.");
            }

            if (string.Equals(
                    oldText,
                    newText,
                    StringComparison.Ordinal))
            {
                return ToolResult.Fail(
                    "old_text and new_text are identical.");
            }

            string newContent =
                content
                    .Remove(
                        firstIndex,
                        oldText.Length)
                    .Insert(
                        firstIndex,
                        newText);

            await File.WriteAllTextAsync(
                fullPath,
                newContent,
                cancellationToken);

            string diff =
                BuildDiff(
                    oldText,
                    newText);

            return ToolResult.Ok(
                $"File edited: {path}"
                + Environment.NewLine
                + Environment.NewLine
                + diff);
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

    private static bool TryReadString(
        JsonElement arguments,
        string name,
        out string value)
    {
        value =
            string.Empty;

        if (!arguments.TryGetProperty(
                name,
                out JsonElement element)
            ||
            element.ValueKind !=
                JsonValueKind.String)
        {
            return false;
        }

        value =
            element.GetString()
            ?? string.Empty;

        return true;
    }

    private static string BuildDiff(
        string oldText,
        string newText)
    {
        return
            "--- old"
            + Environment.NewLine
            + PrefixLines(
                oldText,
                "- ")
            + Environment.NewLine
            + "+++ new"
            + Environment.NewLine
            + PrefixLines(
                newText,
                "+ ");
    }

    private static string PrefixLines(
        string text,
        string prefix)
    {
        string normalized =
            text
                .Replace(
                    "\r\n",
                    "\n")
                .Replace(
                    '\r',
                    '\n');

        string[] lines =
            normalized.Split(
                '\n');

        return string.Join(
            Environment.NewLine,
            lines.Select(
                line =>
                    prefix + line));
    }
}