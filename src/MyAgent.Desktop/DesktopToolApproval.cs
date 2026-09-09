using MyAgent.Guardrails;
using MyAgent.Messages;

namespace MyAgent.Desktop;

public class DesktopToolApproval
    : IToolApproval
{
    public Task<bool> ApproveAsync(
        ToolCall toolCall)
    {
        return Task.FromResult(false);
    }
}