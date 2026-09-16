namespace MyAgent.Guardrails;

public interface IToolApproval
{
    Task<bool> ApproveAsync(
        ToolApprovalRequest request,
        CancellationToken cancellationToken = default);
}