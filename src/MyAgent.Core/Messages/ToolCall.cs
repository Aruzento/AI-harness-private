using System.Text.Json;

namespace MyAgent.Messages;

public class ToolCall
{
    public string Id { get; }

    public string Name { get; }

    public JsonElement Arguments { get; }

    public ToolCall(
        string id,
        string name,
        JsonElement arguments)
    {
        Id = id;
        Name = name;
        Arguments = arguments;
    }
}