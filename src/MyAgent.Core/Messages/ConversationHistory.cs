namespace MyAgent.Messages;

public class ConversationHistory
{
    private readonly List<Message> _messages = new();

    public IReadOnlyList<Message> Messages =>
        _messages;

    public void AddSystem(string content)
    {
        _messages.Add(
            new Message(
                MessageRole.System,
                content));
    }

    public void AddUser(string content)
    {
        _messages.Add(
            new Message(
                MessageRole.User,
                content));
    }

    public void AddAssistant(
        string? content,
        IReadOnlyList<ToolCall>? toolCalls = null)
    {
        _messages.Add(
            new Message(
                MessageRole.Assistant,
                content,
                toolCalls));
    }

    public void AddTool(
        string toolCallId,
        string content)
    {
        _messages.Add(
            new Message(
                MessageRole.Tool,
                content,
                toolCallId: toolCallId));
    }
}