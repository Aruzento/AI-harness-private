using MyAgent.Messages;

namespace MyAgent.Guardrails;

public sealed class ToolApprovalRequest
{
    public ToolCall ToolCall { get; }

    public ToolApprovalPreview Preview { get; }

    public ToolApprovalRequest(
        ToolCall toolCall,
        ToolApprovalPreview preview)
    {
        ArgumentNullException.ThrowIfNull(
            toolCall);

        ArgumentNullException.ThrowIfNull(
            preview);

        ToolCall =
            toolCall;

        Preview =
            preview;
    }
}