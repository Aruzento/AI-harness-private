using MyAgent.Messages;

namespace MyAgent.Chats;

public sealed class AgentChat
{
    public string Id { get; init; } =
        string.Empty;

    public string ProjectId { get; init; } =
        string.Empty;

    public string Title { get; init; } =
        string.Empty;

    public DateTimeOffset CreatedAtUtc
    {
        get;
        init;
    }

    public DateTimeOffset UpdatedAtUtc
    {
        get;
        init;
    }

    public Message[] Messages
    {
        get;
        init;
    } =
        Array.Empty<Message>();
}