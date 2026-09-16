using MyAgent.Messages;
using MyAgent.Projects;

namespace MyAgent.Chats;

public sealed class ChatManager
{
    private readonly ChatStore
        _chatStore;

    private readonly ProjectStore
        _projectStore;

    public ChatManager(
        ChatStore chatStore,
        ProjectStore projectStore)
    {
        _chatStore =
            chatStore
            ?? throw new ArgumentNullException(
                nameof(chatStore));

        _projectStore =
            projectStore
            ?? throw new ArgumentNullException(
                nameof(projectStore));
    }

    public Task<ChatCatalog> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        return _chatStore.LoadAsync(
            cancellationToken);
    }

    public async Task<IReadOnlyList<AgentChat>>
        GetProjectChatsAsync(
            string projectId,
            CancellationToken cancellationToken = default)
    {
        ValidateProjectId(
            projectId);

        await EnsureProjectExistsAsync(
            projectId,
            cancellationToken);

        ChatCatalog catalog =
            await _chatStore.LoadAsync(
                cancellationToken);

        return catalog.Chats
            .Where(
                chat =>
                    string.Equals(
                        chat.ProjectId,
                        projectId,
                        StringComparison.Ordinal))
            .OrderByDescending(
                chat =>
                    chat.UpdatedAtUtc)
            .ThenBy(
                chat =>
                    chat.Title,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<ChatCatalog>
        AddAsync(
            string projectId,
            string title = "Новый чат",
            bool makeActive = true,
            CancellationToken cancellationToken = default)
    {
        ValidateProjectId(
            projectId);

        string normalizedTitle =
            ValidateTitle(
                title);

        await EnsureProjectExistsAsync(
            projectId,
            cancellationToken);

        ChatCatalog current =
            await _chatStore.LoadAsync(
                cancellationToken);

        DateTimeOffset now =
            DateTimeOffset.UtcNow;

        var chat =
            new AgentChat
            {
                Id =
                    "chat-"
                    + Guid.NewGuid()
                        .ToString("N"),

                ProjectId =
                    projectId,

                Title =
                    normalizedTitle,

                CreatedAtUtc =
                    now,

                UpdatedAtUtc =
                    now,

                Messages =
                    Array.Empty<Message>()
            };

        AgentChat[] chats =
            current.Chats
                .Append(
                    chat)
                .ToArray();

        string? activeChatId =
            makeActive
            || current.ActiveChatId is null
                ? chat.Id
                : current.ActiveChatId;

        var updated =
            new ChatCatalog
            {
                ActiveChatId =
                    activeChatId,

                Chats =
                    chats
            };

        await _chatStore.SaveAsync(
            updated,
            cancellationToken);

        return updated;
    }

    public async Task<ChatCatalog>
        SetActiveAsync(
            string chatId,
            CancellationToken cancellationToken = default)
    {
        ValidateChatId(
            chatId);

        ChatCatalog current =
            await _chatStore.LoadAsync(
                cancellationToken);

        _ =
            GetChat(
                current,
                chatId);

        if (string.Equals(
                current.ActiveChatId,
                chatId,
                StringComparison.Ordinal))
        {
            return current;
        }

        var updated =
            new ChatCatalog
            {
                ActiveChatId =
                    chatId,

                Chats =
                    current.Chats
            };

        await _chatStore.SaveAsync(
            updated,
            cancellationToken);

        return updated;
    }

    public async Task<ChatCatalog>
        RenameAsync(
            string chatId,
            string newTitle,
            CancellationToken cancellationToken = default)
    {
        ValidateChatId(
            chatId);

        string normalizedTitle =
            ValidateTitle(
                newTitle);

        ChatCatalog current =
            await _chatStore.LoadAsync(
                cancellationToken);

        AgentChat existing =
            GetChat(
                current,
                chatId);

        if (string.Equals(
                existing.Title,
                normalizedTitle,
                StringComparison.Ordinal))
        {
            return current;
        }

        var updatedChat =
            CopyChat(
                existing,
                title: normalizedTitle,
                updatedAtUtc:
                    DateTimeOffset.UtcNow);

        ChatCatalog updated =
            ReplaceChat(
                current,
                updatedChat);

        await _chatStore.SaveAsync(
            updated,
            cancellationToken);

        return updated;
    }

    public async Task<ChatCatalog>
        ReplaceMessagesAsync(
            string chatId,
            IReadOnlyList<Message> messages,
            CancellationToken cancellationToken = default)
    {
        ValidateChatId(
            chatId);

        ArgumentNullException.ThrowIfNull(
            messages);

        ValidatePersistentMessages(
            messages);

        ChatCatalog current =
            await _chatStore.LoadAsync(
                cancellationToken);

        AgentChat existing =
            GetChat(
                current,
                chatId);

        Message[] messageSnapshot =
            messages.ToArray();

        var updatedChat =
            CopyChat(
                existing,
                messages:
                    messageSnapshot,
                updatedAtUtc:
                    DateTimeOffset.UtcNow);

        ChatCatalog updated =
            ReplaceChat(
                current,
                updatedChat);

        await _chatStore.SaveAsync(
            updated,
            cancellationToken);

        return updated;
    }

    public async Task<ChatCatalog>
        RemoveAsync(
            string chatId,
            CancellationToken cancellationToken = default)
    {
        ValidateChatId(
            chatId);

        ChatCatalog current =
            await _chatStore.LoadAsync(
                cancellationToken);

        AgentChat existing =
            GetChat(
                current,
                chatId);

        AgentChat[] remainingChats =
            current.Chats
                .Where(
                    chat =>
                        !string.Equals(
                            chat.Id,
                            chatId,
                            StringComparison.Ordinal))
                .ToArray();

        string? activeChatId =
            current.ActiveChatId;

        if (string.Equals(
                activeChatId,
                chatId,
                StringComparison.Ordinal))
        {
            activeChatId =
                remainingChats
                    .Where(
                        chat =>
                            string.Equals(
                                chat.ProjectId,
                                existing.ProjectId,
                                StringComparison.Ordinal))
                    .OrderByDescending(
                        chat =>
                            chat.UpdatedAtUtc)
                    .Select(
                        chat =>
                            chat.Id)
                    .FirstOrDefault();
        }

        var updated =
            new ChatCatalog
            {
                ActiveChatId =
                    activeChatId,

                Chats =
                    remainingChats
            };

        await _chatStore.SaveAsync(
            updated,
            cancellationToken);

        return updated;
    }

    private async Task EnsureProjectExistsAsync(
        string projectId,
        CancellationToken cancellationToken)
    {
        ProjectCatalog projects =
            await _projectStore.LoadAsync(
                cancellationToken);

        bool exists =
            projects.Projects.Any(
                project =>
                    string.Equals(
                        project.Id,
                        projectId,
                        StringComparison.Ordinal));

        if (!exists)
        {
            throw new InvalidOperationException(
                "Project does not exist: "
                + projectId);
        }
    }

    private static ChatCatalog ReplaceChat(
        ChatCatalog current,
        AgentChat updatedChat)
    {
        return new ChatCatalog
        {
            ActiveChatId =
                current.ActiveChatId,

            Chats =
                current.Chats
                    .Select(
                        chat =>
                            string.Equals(
                                chat.Id,
                                updatedChat.Id,
                                StringComparison.Ordinal)
                                ? updatedChat
                                : chat)
                    .ToArray()
        };
    }

    private static AgentChat CopyChat(
        AgentChat source,
        string? title = null,
        Message[]? messages = null,
        DateTimeOffset? updatedAtUtc = null)
    {
        return new AgentChat
        {
            Id =
                source.Id,

            ProjectId =
                source.ProjectId,

            Title =
                title
                ?? source.Title,

            CreatedAtUtc =
                source.CreatedAtUtc,

            UpdatedAtUtc =
                updatedAtUtc
                ?? source.UpdatedAtUtc,

            Messages =
                messages
                ?? source.Messages
        };
    }

    private static AgentChat GetChat(
        ChatCatalog catalog,
        string chatId)
    {
        AgentChat? chat =
            catalog.Chats
                .FirstOrDefault(
                    candidate =>
                        string.Equals(
                            candidate.Id,
                            chatId,
                            StringComparison.Ordinal));

        if (chat is null)
        {
            throw new InvalidOperationException(
                "Chat does not exist: "
                + chatId);
        }

        return chat;
    }

    private static void ValidateProjectId(
        string projectId)
    {
        if (string.IsNullOrWhiteSpace(
                projectId))
        {
            throw new ArgumentException(
                "Project ID cannot be empty.",
                nameof(projectId));
        }
    }

    private static void ValidateChatId(
        string chatId)
    {
        if (string.IsNullOrWhiteSpace(
                chatId))
        {
            throw new ArgumentException(
                "Chat ID cannot be empty.",
                nameof(chatId));
        }
    }

    private static string ValidateTitle(
        string title)
    {
        if (string.IsNullOrWhiteSpace(
                title))
        {
            throw new ArgumentException(
                "Chat title cannot be empty.",
                nameof(title));
        }

        return title.Trim();
    }

    private static void ValidatePersistentMessages(
        IReadOnlyList<Message> messages)
    {
        foreach (Message message
                 in messages)
        {
            if (message.Role ==
                MessageRole.System)
            {
                throw new ArgumentException(
                    "Persistent chat history "
                    + "cannot contain system messages.",
                    nameof(messages));
            }
        }
    }
}