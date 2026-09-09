using MyAgent.Messages;
using MyAgent.Tools;

namespace MyAgent.Agent;

public interface IAgentObserver
{
    void OnStepStarted(
        int step);

    void OnEmptyResponseRetry();

    void OnToolCall(
        ToolCall toolCall);

    void OnToolResult(
        ToolCall toolCall,
        ToolResult toolResult);
}