using MyAgent.Guardrails;

namespace MyAgent.Cli;

public class ConsoleToolApproval
    : IToolApproval
{
    public Task<bool> ApproveAsync(
        ToolApprovalRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        Console.WriteLine();

        Console.WriteLine(
            "[Approval required]");

        Console.WriteLine(
            $"Tool: {request.ToolCall.Name}");

        Console.WriteLine();

        Console.WriteLine(
            request.Preview.Text);

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
}