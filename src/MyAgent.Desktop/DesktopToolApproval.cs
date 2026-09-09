using MyAgent.Guardrails;
using MyAgent.Messages;

namespace MyAgent.Desktop;

public class DesktopToolApproval
    : IToolApproval
{
    private readonly Func<
        ToolCall,
        CancellationToken,
        Task<bool>>
        _requestApproval;

    public DesktopToolApproval(
        Func<
            ToolCall,
            CancellationToken,
            Task<bool>>
            requestApproval)
    {
        _requestApproval =
            requestApproval;
    }

    public Task<bool> ApproveAsync(
        ToolCall toolCall,
        CancellationToken cancellationToken = default)
    {
        return _requestApproval(
            toolCall,
            cancellationToken);
    }
}