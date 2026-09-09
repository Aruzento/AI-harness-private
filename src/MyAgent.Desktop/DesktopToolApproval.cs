using MyAgent.Guardrails;
using MyAgent.Messages;

namespace MyAgent.Desktop;

public class DesktopToolApproval
    : IToolApproval
{
    private readonly Func<ToolCall, Task<bool>>
        _requestApproval;

    public DesktopToolApproval(
        Func<ToolCall, Task<bool>>
            requestApproval)
    {
        _requestApproval =
            requestApproval;
    }

    public Task<bool> ApproveAsync(
        ToolCall toolCall)
    {
        return _requestApproval(
            toolCall);
    }
}