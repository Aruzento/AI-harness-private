namespace MyAgent.Tools;

public class ToolResult
{
    public bool Success { get; }

    public string Content { get; }

    public string? Error { get; }

    private ToolResult(
        bool success,
        string content,
        string? error)
    {
        Success = success;
        Content = content;
        Error = error;
    }

    public static ToolResult Ok(string content)
    {
        return new ToolResult(
            true,
            content,
            null);
    }

    public static ToolResult Fail(string error)
    {
        return new ToolResult(
            false,
            string.Empty,
            error);
    }
}