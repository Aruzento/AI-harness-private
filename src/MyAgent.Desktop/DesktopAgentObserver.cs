using MyAgent.Agent;
using MyAgent.Messages;
using MyAgent.Tools;

namespace MyAgent.Desktop;

public class DesktopAgentObserver
    : IAgentObserver
{
    private readonly Action<string>
        _addActivity;

    public DesktopAgentObserver(
        Action<string> addActivity)
    {
        _addActivity =
            addActivity;
    }

    public void OnStepStarted(
        int step)
    {
    }

    public void OnEmptyResponseRetry()
    {
        _addActivity(
            "↳ Пустой ответ модели — повтор запроса");
    }

    public void OnToolCall(
        ToolCall toolCall)
    {
        _addActivity(
            "↳ "
            + DescribeToolCall(
                toolCall));
    }

    public void OnToolResult(
        ToolCall toolCall,
        ToolResult toolResult)
    {
        if (!toolResult.Success)
        {
            if (string.Equals(
                toolResult.Error,
                "Tool execution denied by user.",
                StringComparison.Ordinal))
        {
            return;
        }
            _addActivity(
                "✗ "
                + toolCall.Name
                + ": "
                + toolResult.Error);

            return;
        }

        string text =
            toolCall.Name switch
            {
                "list_files" =>
                    "✓ Файлы получены",

                "read_file" =>
                    "✓ Файл прочитан",

                "write_file" =>
                    "✓ Файл записан",

                "run_terminal" =>
                    "✓ Команда выполнена",
                
                "search_files" =>
                    "✓ Поиск завершён",

                "edit_file" =>
                    "✓ Файл изменён",

                _ =>
                    $"✓ {toolCall.Name} завершён"
            };

        _addActivity(
            text);
    }

    private static string DescribeToolCall(
        ToolCall toolCall)
    {
        return toolCall.Name switch
        {
            "list_files" =>
                "Просматривает файлы",

            "read_file" =>
                DescribeReadFileCall(
                    toolCall),

            "write_file" =>
                "Записывает файл "
                + ReadArgument(
                    toolCall,
                    "path"),

            "run_terminal" =>
                "Запрашивает выполнение команды",

            "search_files" =>
                "Ищет по файлам: "
                + ReadArgument(
                    toolCall,
                    "query"),

            "edit_file" =>
                "Редактирует файл "
                + ReadArgument(
                    toolCall,
                    "path"),

            _ =>
                $"Вызывает {toolCall.Name}"
        };
    }

    private static string DescribeReadFileCall(
        ToolCall toolCall)
    {
        string description =
            "Читает файл "
            + ReadArgument(
                toolCall,
                "path");

        int? startLine =
            ReadIntArgument(
                toolCall,
                "start_line");

        int? endLine =
            ReadIntArgument(
                toolCall,
                "end_line");

        if (!startLine.HasValue
            &&
            !endLine.HasValue)
        {
            return description;
        }

        if (startLine.HasValue
            &&
            endLine.HasValue)
        {
            return description
                + " · строки "
                + startLine.Value
                + "–"
                + endLine.Value;
        }

        if (startLine.HasValue)
        {
            return description
                + " · с строки "
                + startLine.Value;
        }

        if (endLine.HasValue)
        {
            return description
                + " · строки 1–"
                + endLine.Value;
        }

        return description;
    }

    private static int? ReadIntArgument(
        ToolCall toolCall,
        string name)
    {
        if (toolCall.Arguments.TryGetProperty(
                name,
                out var element)
            &&
            element.ValueKind ==
                System.Text.Json.JsonValueKind.Number
            &&
            element.TryGetInt32(
                out int value))
        {
            return value;
        }

        return null;
    }

    private static string ReadArgument(
        ToolCall toolCall,
        string name)
    {
        if (toolCall.Arguments.TryGetProperty(
                name,
                out var element))
        {
            return element.GetString()
                ?? string.Empty;
        }

        return string.Empty;
    }
}
