using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MyAgent.Llm;

public class OpenRouterLlmClient : ILlmClient
{
    private const string Endpoint =
        "https://openrouter.ai/api/v1/chat/completions";

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public OpenRouterLlmClient(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    public async Task<string> SendAsync(string prompt)
    {
        var requestBody = new
        {
            model = "openrouter/free",

            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            }
        };

        string json = JsonSerializer.Serialize(requestBody);

        using var request =
            new HttpRequestMessage(HttpMethod.Post, Endpoint);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _apiKey);

        request.Content =
            new StringContent(json, Encoding.UTF8, "application/json");

        using HttpResponseMessage response =
            await _httpClient.SendAsync(request);

        string responseJson =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"OpenRouter error {(int)response.StatusCode}: {responseJson}");
        }

        using JsonDocument document =
            JsonDocument.Parse(responseJson);

        string? answer = document
            .RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return answer ?? string.Empty;
    }
}