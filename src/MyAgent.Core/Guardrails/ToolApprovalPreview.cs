namespace MyAgent.Guardrails;

public sealed class ToolApprovalPreview
{
    public string Text { get; }

    public FileChangePreview? FileChange { get; }

    public ToolApprovalPreview(
        string text,
        FileChangePreview? fileChange = null)
    {
        Text =
            text
            ?? string.Empty;

        FileChange =
            fileChange;
    }
}