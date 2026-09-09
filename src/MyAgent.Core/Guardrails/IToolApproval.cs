using MyAgent.Messages;

namespace MyAgent.Guardrails;

public interface IToolApproval
{
    Task<bool> ApproveAsync(
        ToolCall toolCall,
        CancellationToken cancellationToken = default);
}