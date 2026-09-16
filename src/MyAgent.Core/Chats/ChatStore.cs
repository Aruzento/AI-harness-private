using System.Text.Json;
using System.Text.Json.Serialization;
using MyAgent.Messages;

namespace MyAgent.Chats;

public sealed class ChatStore
{
    private const int CurrentSchemaVersion =
        1;

    private readonly string _filePath;

    private static readonly JsonSerializerOptions
        JsonOptions =
            CreateJsonOptions();

    public static string DefaultFilePath =>
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder
                    .LocalApplicationData),
            "MyAgent",
            "chats.json");

    public ChatStore(
        string? filePath = null)
    {
        _filePath =
            string.IsNullOrWhiteSpace(
                filePath)
                ? DefaultFilePath
                : Path.GetFullPath(
                    filePath);
    }

    public async Task<ChatCatalog>
        LoadAsync(
            CancellationToken cancellationToken = default)
    {
        if (!File.Exists(
                _filePath))
        {
            return new ChatCatalog();
        }

        try
        {
            await using FileStream stream =
                new(
                    _filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 4096,
                    useAsync: true);

            ChatDocument? document =
                await JsonSerializer
                    .DeserializeAsync<
                        ChatDocument>(
                        stream,
                        JsonOptions,
                        cancellationToken);

            if (document is null)
            {
                throw new InvalidOperationException(
                    $"Invalid chat file: {_filePath}");
            }

            if (document.SchemaVersion !=
                CurrentSchemaVersion)
            {
                throw new InvalidOperationException(
                    "Unsupported chat schema version "
                    + $"{document.SchemaVersion}. "
                    + $"Expected {CurrentSchemaVersion}.");
            }

            var catalog =
                new ChatCatalog
                {
                    ActiveChatId =
                        document.ActiveChatId,

                    Chats =
                        document.Chats
                        ?? Array.Empty<AgentChat>()
                };

            ValidateCatalog(
                catalog);

            return catalog;
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Invalid chat file: {_filePath}",
                exception);
        }
    }

    public async Task SaveAsync(
        ChatCatalog catalog,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            catalog);

        ValidateCatalog(
            catalog);

        string? directory =
            Path.GetDirectoryName(
                _filePath);

        if (!string.IsNullOrWhiteSpace(
                directory))
        {
            Directory.CreateDirectory(
                directory);
        }

        var document =
            new ChatDocument
            {
                SchemaVersion =
                    CurrentSchemaVersion,

                ActiveChatId =
                    catalog.ActiveChatId,

                Chats =
                    catalog.Chats
            };

        string temporaryPath =
            _filePath
            + "."
            + Guid.NewGuid().ToString("N")
            + ".tmp";

        try
        {
            await using (
                FileStream stream =
                    new(
                        temporaryPath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None,
                        bufferSize: 4096,
                        useAsync: true))
            {
                await JsonSerializer
                    .SerializeAsync(
                        stream,
                        document,
                        JsonOptions,
                        cancellationToken);

                await stream.FlushAsync(
                    cancellationToken);
            }

            cancellationToken
                .ThrowIfCancellationRequested();

            File.Move(
                temporaryPath,
                _filePath,
                overwrite: true);
        }
        catch
        {
            if (File.Exists(
                    temporaryPath))
            {
                File.Delete(
                    temporaryPath);
            }

            throw;
        }
    }

    private static void ValidateCatalog(
        ChatCatalog catalog)
    {
        var chatIds =
            new HashSet<string>(
                StringComparer.Ordinal);

        foreach (AgentChat chat
                 in catalog.Chats)
        {
            if (string.IsNullOrWhiteSpace(
                    chat.Id))
            {
                throw new InvalidOperationException(
                    "Chat has no ID.");
            }

            if (!chatIds.Add(
                    chat.Id))
            {
                throw new InvalidOperationException(
                    "Duplicate chat ID: "
                    + chat.Id);
            }

            if (string.IsNullOrWhiteSpace(
                    chat.ProjectId))
            {
                throw new InvalidOperationException(
                    "Chat has no project ID: "
                    + chat.Id);
            }

            if (string.IsNullOrWhiteSpace(
                    chat.Title))
            {
                throw new InvalidOperationException(
                    "Chat has no title: "
                    + chat.Id);
            }

            if (chat.CreatedAtUtc ==
                default)
            {
                throw new InvalidOperationException(
                    "Chat has no creation time: "
                    + chat.Id);
            }

            if (chat.UpdatedAtUtc ==
                default)
            {
                throw new InvalidOperationException(
                    "Chat has no update time: "
                    + chat.Id);
            }

            if (chat.UpdatedAtUtc <
                chat.CreatedAtUtc)
            {
                throw new InvalidOperationException(
                    "Chat update time is earlier "
                    + "than creation time: "
                    + chat.Id);
            }

            foreach (Message message
                     in chat.Messages)
            {
                if (message.Role ==
                    MessageRole.System)
                {
                    throw new InvalidOperationException(
                        "Persistent chat history "
                        + "cannot contain system messages: "
                        + chat.Id);
                }
            }
        }

        if (catalog.ActiveChatId
            is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(
                catalog.ActiveChatId))
        {
            throw new InvalidOperationException(
                "Active chat ID cannot be empty.");
        }

        if (!chatIds.Contains(
                catalog.ActiveChatId))
        {
            throw new InvalidOperationException(
                "Active chat does not exist: "
                + catalog.ActiveChatId);
        }
    }

    private static JsonSerializerOptions
        CreateJsonOptions()
    {
        var options =
            new JsonSerializerOptions
            {
                WriteIndented =
                    true,

                PropertyNamingPolicy =
                    JsonNamingPolicy.CamelCase,

                PropertyNameCaseInsensitive =
                    true
            };

        options.Converters.Add(
            new JsonStringEnumConverter(
                JsonNamingPolicy.CamelCase));

        return options;
    }

    private sealed class ChatDocument
    {
        public int SchemaVersion
        {
            get;
            set;
        }

        public string? ActiveChatId
        {
            get;
            set;
        }

        public AgentChat[]? Chats
        {
            get;
            set;
        }
    }
}