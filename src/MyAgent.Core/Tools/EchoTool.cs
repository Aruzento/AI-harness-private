using System.Text.Json;

namespace MyAgent.Tools;

public class EchoTool : ITool
{
    public string Name => "echo";

    public string Description =>
        "Возвращает переданный текст без изменений.";

    public JsonElement ParametersSchema =>
        JsonSerializer.SerializeToElement(
            new
            {
                type = "object",

                properties = new
                {
                    text = new
                    {
                        type = "string",
                        description =
                            "Текст, который нужно вернуть."
                    }
                },

                required = new[]
                {
                    "text"
                }
            });

    public Task<ToolResult> ExecuteAsync(
        JsonElement arguments)
    {
        if (arguments.ValueKind !=
            JsonValueKind.Object)
        {
            return Task.FromResult(
                ToolResult.Fail(
                    "Arguments must be a JSON object."));
        }

        if (!arguments.TryGetProperty(
                "text",
                out JsonElement textElement))
        {
            return Task.FromResult(
                ToolResult.Fail(
                    "Missing parameter: text."));
        }

        if (textElement.ValueKind !=
            JsonValueKind.String)
        {
            return Task.FromResult(
                ToolResult.Fail(
                    "Parameter 'text' must be a string."));
        }

        string text =
            textElement.GetString()
            ?? string.Empty;

        return Task.FromResult(
            ToolResult.Ok(text));
    }
}