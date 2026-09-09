using System.Text.Json;

namespace MyAgent.Tools;

public class ToolRegistry
{
    private readonly Dictionary<string, ITool>
        _tools =
            new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<ITool> Tools =>
        _tools.Values;

    public void Register(ITool tool)
    {
        if (!_tools.TryAdd(
                tool.Name,
                tool))
        {
            throw new InvalidOperationException(
                $"Tool '{tool.Name}' is already registered.");
        }
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