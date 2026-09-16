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

        WritePreview(
            request);

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

    private static void WritePreview(
        ToolApprovalRequest request)
    {
        if (request.Preview.FileChange
            is null)
        {
            Console.WriteLine(
                request.Preview.Text);

            return;
        }

        string normalized =
            request.Preview.Text
                .Replace(
                    "\r\n",
                    "\n")
                .Replace(
                    '\r',
                    '\n');

        string[] lines =
            normalized.Split(
                '\n');

        foreach (string line in lines)
        {
            WriteDiffLine(
                line);
        }
    }

    private static void WriteDiffLine(
        string line)
    {
        if (line.StartsWith(
                "+ ",
                StringComparison.Ordinal))
        {
            WriteColoredLine(
                line,
                ConsoleColor.Green);

            return;
        }

        if (line.StartsWith(
                "- ",
                StringComparison.Ordinal))
        {
            WriteColoredLine(
                line,
                ConsoleColor.Red);

            return;
        }

        if (line.StartsWith(
                "@@",
                StringComparison.Ordinal))
        {
            WriteColoredLine(
                line,
                ConsoleColor.Cyan);

            return;
        }

        if (string.Equals(
                line,
                "...",
                StringComparison.Ordinal))
        {
            WriteColoredLine(
                line,
                ConsoleColor.DarkGray);

            return;
        }

        Console.WriteLine(
            line);
    }

    private static void WriteColoredLine(
        string text,
        ConsoleColor foregroundColor)
    {
        ConsoleColor previousColor =
            Console.ForegroundColor;

        try
        {
            Console.ForegroundColor =
                foregroundColor;

            Console.WriteLine(
                text);
        }
        finally
        {
            Console.ForegroundColor =
                previousColor;
        }
    }
}