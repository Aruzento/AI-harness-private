using MyAgent.Messages;

namespace MyAgent.Guardrails;

public class ConsoleToolApproval
    : IToolApproval
{
    public Task<bool> ApproveAsync(
        ToolCall toolCall)
    {
        Console.WriteLine();

        Console.WriteLine(
            "[Approval required]");

        Console.WriteLine(
            $"Tool: {toolCall.Name}");

        Console.WriteLine(
            $"Arguments: {toolCall.Arguments.GetRawText()}");

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
}