using System.Text.Json;
using MyAgent.Guardrails;
using MyAgent.Messages;

namespace MyAgent.Cli;

public class ConsoleToolApproval
    : IToolApproval
{
    public Task<bool> ApproveAsync(
        ToolCall toolCall,
        CancellationToken cancellationToken = default)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        Console.WriteLine();

        Console.WriteLine(
            "[Approval required]");

        Console.WriteLine(
            $"Tool: {toolCall.Name}");

        Console.WriteLine();

        Console.WriteLine(
            DescribeApproval(
                toolCall));

        Console.WriteLine();

        Console.Write(
            "Разрешить выполнение? [y/N]: ");

        string? answer =
            Console.ReadLine();

        bool approved =
            string.Equals(
                answer,
                "y",
                StringComparison.OrdinalIgnoreCase)
            ||
            string.Equals(
                answer,
                "yes",
                StringComparison.OrdinalIgnoreCase);

        Console.WriteLine();

        return Task.FromResult(
            approved);
    }

    private static string DescribeApproval(
        ToolCall toolCall)
    {
        if (toolCall.Name.Equals(
                "run_terminal",
                StringComparison.OrdinalIgnoreCase))
        {
            return ReadStringArgument(
                toolCall,
                "command");
        }

        if (toolCall.Name.Equals(
                "edit_file",
                StringComparison.OrdinalIgnoreCase))
        {
            string path =
                ReadStringArgument(
                    toolCall,
                    "path");

            string oldText =
                ReadStringArgument(
                    toolCall,
                    "old_text");

            string newText =
                ReadStringArgument(
                    toolCall,
                    "new_text");

            string newPreview =
                newText.Length == 0
                    ? "(пусто — фрагмент будет удалён)"
                    : newText;

            return
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
        }

        if (toolCall.Name.Equals(
                "write_file",
                StringComparison.OrdinalIgnoreCase))
        {
            string path =
                ReadStringArgument(
                    toolCall,
                    "path");

            string content =
                ReadStringArgument(
                    toolCall,
                    "content");

            string contentPreview =
                content.Length == 0
                    ? "(пустой файл)"
                    : content;

            return
                "Файл: "
                + path
                + Environment.NewLine
                + Environment.NewLine
                + "Файл будет создан или полностью перезаписан."
                + Environment.NewLine
                + Environment.NewLine
                + "+++ Полное содержимое после записи"
                + Environment.NewLine
                + contentPreview;
        }

        return toolCall.Arguments
            .GetRawText();
    }

    private static string ReadStringArgument(
        ToolCall toolCall,
        string name)
    {
        if (toolCall.Arguments.ValueKind ==
                JsonValueKind.Object
            &&
            toolCall.Arguments.TryGetProperty(
                name,
                out JsonElement element)
            &&
            element.ValueKind ==
                JsonValueKind.String)
        {
            return element.GetString()
                ?? string.Empty;
        }

        return "(не указано)";
    }
}