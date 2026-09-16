using System.Text.Json;
using MyAgent.Guardrails;

namespace MyAgent.Tools;

public interface IApprovedToolExecutor
{
    Task<ToolResult> ExecuteApprovedAsync(
        JsonElement arguments,
        ToolApprovalPreview preview,
        CancellationToken cancellationToken = default);
}