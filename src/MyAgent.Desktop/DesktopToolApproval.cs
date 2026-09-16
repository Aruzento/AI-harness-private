using MyAgent.Guardrails;

namespace MyAgent.Desktop;

public class DesktopToolApproval
    : IToolApproval
{
    private readonly Func<
        ToolApprovalRequest,
        CancellationToken,
        Task<bool>>
        _requestApproval;

    public DesktopToolApproval(
        Func<
            ToolApprovalRequest,
            CancellationToken,
            Task<bool>>
            requestApproval)
    {
        _requestApproval =
            requestApproval;
    }

    public Task<bool> ApproveAsync(
        ToolApprovalRequest request,
        CancellationToken cancellationToken = default)
    {
        return _requestApproval(
            request,
            cancellationToken);
    }
}