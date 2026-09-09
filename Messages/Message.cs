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
    public string Content { get; }

    public Message(MessageRole role, string content)
    {
        Role = role;
        Content = content;
    }
}