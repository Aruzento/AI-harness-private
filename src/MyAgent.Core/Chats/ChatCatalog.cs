namespace MyAgent.Chats;

public sealed class ChatCatalog
{
    public string? ActiveChatId
    {
        get;
        init;
    }

    public AgentChat[] Chats
    {
        get;
        init;
    } =
        Array.Empty<AgentChat>();
}