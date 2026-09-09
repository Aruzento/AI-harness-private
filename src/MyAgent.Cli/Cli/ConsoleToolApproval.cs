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
        cancellationToken.ThrowIfCancellationRequested();
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