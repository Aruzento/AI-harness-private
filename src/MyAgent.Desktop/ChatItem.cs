namespace MyAgent.Desktop;

public abstract class ChatItem
{
    public string Text { get; }

    protected ChatItem(
        string text)
    {
        Text = text;
    }
}

public sealed class UserMessageItem
    : ChatItem
{
    public UserMessageItem(
        string text)
        : base(text)
    {
    }
}

public sealed class AssistantMessageItem
    : ChatItem
{
    public AssistantMessageItem(
        string text)
        : base(text)
    {
    }
}

public sealed class ActivityItem
    : ChatItem
{
    public ActivityItem(
        string text)
        : base(text)
    {
    }
}