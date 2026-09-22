namespace MyAgent.Messages;

public class ConversationHistory
{
    private readonly List<Message>
        _messages =
            new();

    public IReadOnlyList<Message> Messages =>
        _messages;

    public void AddSystem(
        string content)
    {
        _messages.Add(
            new Message(
                MessageRole.System,
                content));
    }

    public void AddUser(
        string content)
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

    public void AddPersistentMessages(
        IReadOnlyList<Message> messages)
    {
        ArgumentNullException.ThrowIfNull(
            messages);

        foreach (Message message
                 in messages)
        {
            if (message.Role ==
                MessageRole.System)
            {
                throw new ArgumentException(
                    "Persistent history cannot "
                    + "contain system messages.",
                    nameof(messages));
            }

            _messages.Add(
                message);
        }
    }

    public Message[]
        CreatePersistentSnapshot()
    {
        return _messages
            .Where(
                message =>
                    message.Role !=
                    MessageRole.System)
            .ToArray();
    }
}