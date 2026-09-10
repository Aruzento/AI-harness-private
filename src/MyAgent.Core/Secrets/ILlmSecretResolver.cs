using MyAgent.Configuration;

namespace MyAgent.Secrets;

public interface ILlmSecretResolver
{
    Task<string> ResolveAsync(
        LlmProfile profile,
        CancellationToken cancellationToken = default);
}