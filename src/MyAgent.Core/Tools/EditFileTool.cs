using System.Text.Json;
using MyAgent.Workspace;

namespace MyAgent.Tools;

public class EditFileTool : ITool
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