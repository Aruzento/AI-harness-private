using MyAgent.Agent;
using MyAgent.Messages;
using MyAgent.Tools;

namespace MyAgent.Cli;

public class ConsoleAgentObserver
    : IAgentObserver
{
    public void OnStepStarted(
        int step)
    {
        Console.WriteLine(
            $"[Agent step: {step}]");
    }

    public void OnEmptyResponseRetry()
    {
        Console.WriteLine(
            "[Empty response: retrying]");
    }

    public void OnToolCall(
        ToolCall toolCall)
    {
        Console.WriteLine(
            $"[Tool call: {toolCall.Name}]");
    }

    public void OnToolResult(
        ToolCall toolCall,
        ToolResult toolResult)
    {
        string content =
            toolResult.Success
                ? toolResult.Content
                : $"ERROR: {toolResult.Error}";

        Console.WriteLine(
            $"[Tool result: {content}]");
    }
}