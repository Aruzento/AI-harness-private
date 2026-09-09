using System.Text.Json;

namespace MyAgent.Tools;

public interface ITool
{
    string Name { get; }

    string Description { get; }

    JsonElement ParametersSchema { get; }

    Task<ToolResult> ExecuteAsync(
        JsonElement arguments);
}