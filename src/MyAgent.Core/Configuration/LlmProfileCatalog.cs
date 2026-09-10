namespace MyAgent.Configuration;

public sealed class LlmProfileCatalog
{
    public string? ActiveProfileId
    {
        get;
        init;
    }

    public LlmProfile[] Profiles
    {
        get;
        init;
    } =
        Array.Empty<LlmProfile>();
}