namespace MyAgent.Secrets;

public interface ISecretStore
{
    Task<string?> GetAsync(
        string secretId,
        CancellationToken cancellationToken = default);

    Task SetAsync(
        string secretId,
        string value,
        CancellationToken cancellationToken = default);

    Task<bool> ContainsAsync(
        string secretId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string secretId,
        CancellationToken cancellationToken = default);
}