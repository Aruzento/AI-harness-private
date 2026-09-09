using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MyAgent.Messages;
using MyAgent.Tools;

namespace MyAgent.Llm;

public class OpenRouterLlmClient : ILlmClient
{
    private const string Endpoint =
        "https://openrouter.ai/api/v1/chat/completions";

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public OpenRouterLlmClient(
        HttpClient httpClient,
        string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    public async Task<LlmResponse> SendAsync(
        IReadOnlyList<Message> messages,
        IReadOnlyCollection<ITool> tools)
    {
        var apiMessages =
            messages
                .Select(ToApiMessage)
                .ToArray();

        var apiTools =
            tools
                .Select(tool => new
                {
                    type = "function",

                    function = new
                    {
                        name = tool.Name,
                        description = tool.Description,
                        parameters = tool.ParametersSchema
                    }
                })
                .ToArray();

        var requestBody = new
        {
            model = "openrouter/free",
            messages = apiMessages,
            tools = apiTools,
            tool_choice = "auto"
        };

        string json =
            JsonSerializer.Serialize(
                requestBody);

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                Endpoint);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                _apiKey);

        request.Content =
            new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

        using HttpResponseMessage response =
            await _httpClient.SendAsync(
                request);

        string responseJson =
            await response.Content
                .ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"OpenRouter error {(int)response.StatusCode}: {responseJson}");
        }

        using JsonDocument document =
            JsonDocument.Parse(
                responseJson);

        JsonElement root =
            document.RootElement;

        string model =
            root.TryGetProperty(
                "model",
                out JsonElement modelElement)
                ? modelElement.GetString()
                    ?? "unknown"
                : "unknown";

        JsonElement messageElement =
            root
                .GetProperty("choices")[0]
                .GetProperty("message");

        string? content = null;

        if (messageElement.TryGetProperty(
                "content",
                out JsonElement contentElement)
            &&
            contentElement.ValueKind ==
                JsonValueKind.String)
        {
            content =
                contentElement
                    .GetString()
                    ?.Trim();
        }

        var toolCalls =
            new List<ToolCall>();

        if (messageElement.TryGetProperty(
                "tool_calls",
                out JsonElement toolCallsElement)
            &&
            toolCallsElement.ValueKind ==
                JsonValueKind.Array)
        {
            foreach (
                JsonElement toolCallElement
                in toolCallsElement.EnumerateArray())
            {
                string id =
                    toolCallElement
                        .GetProperty("id")
                        .GetString()
                    ?? throw new InvalidOperationException(
                        "Tool call has no id.");

                JsonElement functionElement =
                    toolCallElement
                        .GetProperty("function");

                string name =
                    functionElement
                        .GetProperty("name")
                        .GetString()
                    ?? throw new InvalidOperationException(
                        "Tool call has no function name.");

                string argumentsJson =
                    functionElement
                        .GetProperty("arguments")
                        .GetString()
                    ?? "{}";

                using JsonDocument argumentsDocument =
                    JsonDocument.Parse(
                        argumentsJson);

                JsonElement arguments =
                    argumentsDocument
                        .RootElement
                        .Clone();

                toolCalls.Add(
                    new ToolCall(
                        id,
                        name,
                        arguments));
            }
        }

        return new LlmResponse(
            model,
            content,
            toolCalls);
    }

    private static string ToApiRole(
        MessageRole role)
    {
        return role switch
        {
            MessageRole.System => "system",
            MessageRole.User => "user",
            MessageRole.Assistant => "assistant",
            MessageRole.Tool => "tool",

            _ => throw new ArgumentOutOfRangeException(
                nameof(role))
        };
    }

    private static Dictionary<string, object?>
        ToApiMessage(Message message)
    {
        var apiMessage =
            new Dictionary<string, object?>
            {
                ["role"] =
                    ToApiRole(message.Role),

                ["content"] =
                    message.Content
            };

        if (message.ToolCalls.Count > 0)
        {
            apiMessage["tool_calls"] =
                message.ToolCalls
                    .Select(toolCall => new
                    {
                        id = toolCall.Id,
                        type = "function",

                        function = new
                        {
                            name = toolCall.Name,

                            arguments =
                                toolCall.Arguments
                                    .GetRawText()
                        }
                    })
                    .ToArray();
        }

        if (message.Role ==
            MessageRole.Tool)
        {
            apiMessage["tool_call_id"] =
                message.ToolCallId
                ?? throw new InvalidOperationException(
                    "Tool message has no ToolCallId.");
        }

        return apiMessage;
    }
}