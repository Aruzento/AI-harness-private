using System.Text.Json;
using MyAgent.Guardrails;

namespace MyAgent.Tools;

public class ToolRegistry
{
    private readonly Dictionary<string, ITool>
        _tools =
            new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<ITool> Tools =>
        _tools.Values;

    public void Register(
        ITool tool)
    {
        if (!_tools.TryAdd(
                tool.Name,
                tool))
        {
            throw new InvalidOperationException(
                $"Tool '{tool.Name}' is already registered.");
        }
    }

    public async Task<ToolApprovalPreview>
        CreateApprovalPreviewAsync(
            string toolName,
            JsonElement arguments,
            CancellationToken cancellationToken = default)
    {
        if (!_tools.TryGetValue(
                toolName,
                out ITool? tool)
            ||
            tool is not IToolApprovalPreviewProvider
                previewProvider)
        {
            return new ToolApprovalPreview(
                arguments.GetRawText());
        }

        return await previewProvider
            .CreateApprovalPreviewAsync(
                arguments,
                cancellationToken);
    }

    public async Task<ToolResult>
        ExecuteApprovedAsync(
            string toolName,
            JsonElement arguments,
            ToolApprovalPreview preview,
            CancellationToken cancellationToken = default)
    {
        if (!_tools.TryGetValue(
                toolName,
                out ITool? tool))
        {
            return ToolResult.Fail(
                $"Unknown tool: {toolName}");
        }

        if (tool is IApprovedToolExecutor
            approvedExecutor)
        {
            return await approvedExecutor
                .ExecuteApprovedAsync(
                    arguments,
                    preview,
                    cancellationToken);
        }

        return await tool.ExecuteAsync(
            arguments,
            cancellationToken);
    }

    public async Task<ToolResult> ExecuteAsync(
        string toolName,
        JsonElement arguments,
        CancellationToken cancellationToken = default)
    {
        if (!_tools.TryGetValue(
                toolName,
                out var tool))
        {
            return ToolResult.Fail(
                $"Unknown tool: {toolName}");
        }

        return await tool.ExecuteAsync(
            arguments,
            cancellationToken);
    }
}
