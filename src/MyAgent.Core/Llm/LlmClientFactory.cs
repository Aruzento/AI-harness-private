using MyAgent.Configuration;
using MyAgent.Secrets;

namespace MyAgent.Llm;

public sealed class LlmClientFactory
    : ILlmClientFactory
{
    private readonly HttpClient _httpClient;
    private readonly ILlmSecretResolver _secretResolver;

    public LlmClientFactory(
        HttpClient httpClient,
        ILlmSecretResolver secretResolver)
    {
        _httpClient =
            httpClient
            ?? throw new ArgumentNullException(
                nameof(httpClient));

        _secretResolver =
            secretResolver
            ?? throw new ArgumentNullException(
                nameof(secretResolver));
    }

    public async Task<ILlmClient> CreateAsync(
        LlmProfile profile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            profile);

        ValidateProfile(
            profile);

        string apiKey =
            await _secretResolver.ResolveAsync(
                profile,
                cancellationToken);

        if (string.Equals(
                profile.ApiFormat,
                LlmApiFormats.OpenAiChatCompletions,
                StringComparison.OrdinalIgnoreCase))
        {
            return new OpenAiCompatibleLlmClient(
                _httpClient,
                apiKey,
                profile.Endpoint,
                profile.Model);
        }

        throw new NotSupportedException(
            "Unsupported LLM API format: "
            + profile.ApiFormat);
    }

    private static void ValidateProfile(
        LlmProfile profile)
    {
        if (string.IsNullOrWhiteSpace(
                profile.Id))
        {
            throw new InvalidOperationException(
                "LLM profile has no ID.");
        }

        if (string.IsNullOrWhiteSpace(
                profile.Name))
        {
            throw new InvalidOperationException(
                "LLM profile has no name.");
        }

        if (string.IsNullOrWhiteSpace(
                profile.ApiFormat))
        {
            throw new InvalidOperationException(
                "LLM profile has no API format.");
        }

        if (string.IsNullOrWhiteSpace(
                profile.Endpoint))
        {
            throw new InvalidOperationException(
                "LLM profile has no endpoint.");
        }

        if (!Uri.TryCreate(
                profile.Endpoint,
                UriKind.Absolute,
                out Uri? endpointUri)
            ||
            (endpointUri.Scheme != Uri.UriSchemeHttps
             &&
             endpointUri.Scheme != Uri.UriSchemeHttp))
        {
            throw new InvalidOperationException(
                "LLM profile endpoint is not "
                + "a valid HTTP or HTTPS URL.");
        }

        if (string.IsNullOrWhiteSpace(
                profile.Model))
        {
            throw new InvalidOperationException(
                "LLM profile has no model.");
        }
    }
}