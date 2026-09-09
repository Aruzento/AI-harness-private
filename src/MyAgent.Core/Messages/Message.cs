namespace MyAgent.Messages;

public enum MessageRole
{
    System,
    User,
    Assistant,
    Tool
}

public class Message
{
    public MessageRole Role { get; }

    public string? Content { get; }

    public IReadOnlyList<ToolCall> ToolCalls { get; }

    public string? ToolCallId { get; }

    public Message(
        MessageRole role,
        string? content,
        IReadOnlyList<ToolCall>? toolCalls = null,
        string? toolCallId = null)
    {
        Role = role;
        Content = content;

        ToolCalls =
            toolCalls
            ?? Array.Empty<ToolCall>();

        ToolCallId = toolCallId;
    }
}