using MyAgent.Agent;
using MyAgent.Messages;
using MyAgent.Tools;

namespace MyAgent.Desktop;

public class DesktopAgentObserver
    : IAgentObserver
{
    public void OnStepStarted(
        int step)
    {
    }

    public void OnEmptyResponseRetry()
    {
    }

    public void OnToolCall(
        ToolCall toolCall)
    {
    }

    public void OnToolResult(
        ToolCall toolCall,
        ToolResult toolResult)
    {
    }
}