using MyAgent.Messages;

namespace MyAgent.Llm;

public class LlmResponse
{
    public string Model { get; }

    public string? Content { get; }

    public IReadOnlyList<ToolCall> ToolCalls { get; }

    public bool HasToolCalls =>
        ToolCalls.Count > 0;

    public LlmResponse(
        string model,
        string? content,
        IReadOnlyList<ToolCall>? toolCalls = null)
    {
        Model = model;
        Content = content;

        ToolCalls =
            toolCalls
            ?? Array.Empty<ToolCall>();
    }
}