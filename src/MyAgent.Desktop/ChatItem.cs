using System.ComponentModel;

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

public sealed class ApprovalItem
    : ChatItem,
      INotifyPropertyChanged
{
    private readonly TaskCompletionSource<bool>
        _completion =
            new(
                TaskCreationOptions
                    .RunContinuationsAsynchronously);

    private bool _isPending =
        true;

    private string _decisionText =
        string.Empty;

    public string ToolName { get; }

    public bool IsPending =>
        _isPending;

    public string DecisionText =>
        _decisionText;

    public event PropertyChangedEventHandler?
        PropertyChanged;

    public ApprovalItem(
        string toolName,
        string text)
        : base(text)
    {
        ToolName =
            toolName;
    }

    public Task<bool> WaitAsync()
    {
        return _completion.Task;
    }

    public bool Resolve(
        bool approved)
    {
        if (!_isPending)
        {
            return false;
        }

        _isPending =
            false;

        _decisionText =
            approved
                ? "✓ Разрешено"
                : "✗ Отклонено";

        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(
                nameof(IsPending)));

        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(
                nameof(DecisionText)));

        return _completion.TrySetResult(
            approved);
    }
}