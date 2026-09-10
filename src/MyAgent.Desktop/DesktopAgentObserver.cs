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
                "Читает файл "
                + ReadArgument(
                    toolCall,
                    "path"),

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
