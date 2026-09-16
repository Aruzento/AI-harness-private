using System.Text.Json;
using MyAgent.Guardrails;

namespace MyAgent.Tools;

public interface IToolApprovalPreviewProvider
{
    Task<ToolApprovalPreview>
        CreateApprovalPreviewAsync(
            JsonElement arguments,
            CancellationToken cancellationToken = default);
}