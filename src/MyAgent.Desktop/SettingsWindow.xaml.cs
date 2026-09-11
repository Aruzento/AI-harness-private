using System.Windows;
using MyAgent.Configuration;

namespace MyAgent.Desktop;

public partial class SettingsWindow
    : Window
{
    private readonly HarnessOptions _originalOptions;
    private readonly LlmProfileManager _profileManager;

    private LlmProfileCatalog _profileCatalog;

    public HarnessOptions?
        SelectedOptions
    {
        get;
        private set;
    }

    public LlmProfileCatalog ProfileCatalog =>
        _profileCatalog;

    public SettingsWindow(
        HarnessOptions options,
        LlmProfileManager profileManager,
        LlmProfileCatalog profileCatalog)
    {
        InitializeComponent();

        _originalOptions =
            options
            ?? throw new ArgumentNullException(
                nameof(options));

        _profileManager =
            profileManager
            ?? throw new ArgumentNullException(
                nameof(profileManager));

        _profileCatalog =
            profileCatalog
            ?? throw new ArgumentNullException(
                nameof(profileCatalog));

        WorkspaceTextBox.Text =
            options.WorkspacePath;

        MaxStepsTextBox.Text =
            options.MaxSteps.ToString();

        MaxToolCallsTextBox.Text =
            options.MaxToolCalls.ToString();

        TerminalTimeoutTextBox.Text =
            options.TerminalTimeoutSeconds
                .ToString();

        RefreshProfiles(
            _profileCatalog.ActiveProfileId);
    }

    private async void AddProfileButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var window =
            new LlmProfileWindow
            {
                Owner =
                    this
            };

        bool? result =
            window.ShowDialog();

        if (result != true)
        {
            return;
        }

        try
        {
            _profileCatalog =
                await _profileManager
                    .AddEncryptedAsync(
                        name:
                            window.ProfileName,

                        endpoint:
                            window.Endpoint,

                        model:
                            window.Model,

                        apiKey:
                            window.ApiKey,

                        makeActive:
                            window.MakeActive,

                        apiFormat:
                            window.ApiFormat);

            LlmProfile? addedProfile =
                _profileCatalog.Profiles
                    .LastOrDefault();

            RefreshProfiles(
                addedProfile?.Id);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                exception.Message,
                "Не удалось добавить модель",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void SetActiveProfileButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (ProfilesListBox.SelectedItem
            is not LlmProfile profile)
        {
            MessageBox.Show(
                this,
                "Выберите модель.",
                "Модель не выбрана",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        try
        {
            _profileCatalog =
                await _profileManager
                    .SetActiveAsync(
                        profile.Id);

            RefreshProfiles(
                profile.Id);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                exception.Message,
                "Не удалось выбрать модель",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void DeleteProfileButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (ProfilesListBox.SelectedItem
            is not LlmProfile profile)
        {
            MessageBox.Show(
                this,
                "Выберите модель.",
                "Модель не выбрана",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        if (string.Equals(
                profile.Id,
                _profileCatalog.ActiveProfileId,
                StringComparison.Ordinal))
        {
            MessageBox.Show(
                this,
                "Активную модель удалить нельзя. "
                + "Сначала выберите другую модель "
                + "и сделайте её активной.",
                "Нельзя удалить модель",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        MessageBoxResult confirmation =
            MessageBox.Show(
                this,
                "Удалить модель «"
                + profile.Name
                + "»?"
                + Environment.NewLine
                + Environment.NewLine
                + "Связанный зашифрованный API key "
                + "также будет удалён, если он больше "
                + "не используется другими профилями.",
                "Удаление модели",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

        if (confirmation !=
            MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            _profileCatalog =
                await _profileManager.DeleteAsync(
                    profile.Id);

            RefreshProfiles(
                _profileCatalog.ActiveProfileId);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                exception.Message,
                "Не удалось удалить модель",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void RefreshProfiles(
        string? selectedProfileId)
    {
        ProfilesListBox.ItemsSource =
            null;

        ProfilesListBox.ItemsSource =
            _profileCatalog.Profiles;

        LlmProfile? activeProfile =
            _profileCatalog.Profiles
                .FirstOrDefault(
                    profile =>
                        string.Equals(
                            profile.Id,
                            _profileCatalog.ActiveProfileId,
                            StringComparison.Ordinal));

        ActiveProfileTextBlock.Text =
            activeProfile is null
                ? "Активная модель не выбрана."
                : "Активная: "
                  + activeProfile.Name
                  + " · "
                  + activeProfile.Model;

        LlmProfile? selectedProfile =
            _profileCatalog.Profiles
                .FirstOrDefault(
                    profile =>
                        string.Equals(
                            profile.Id,
                            selectedProfileId,
                            StringComparison.Ordinal));

        ProfilesListBox.SelectedItem =
            selectedProfile
            ?? activeProfile;
    }

    private void SaveButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!TryReadPositiveInt(
                MaxStepsTextBox.Text,
                "Max steps",
                out int maxSteps))
        {
            return;
        }

        if (!TryReadPositiveInt(
                MaxToolCallsTextBox.Text,
                "Max tool calls",
                out int maxToolCalls))
        {
            return;
        }

        if (!TryReadPositiveInt(
                TerminalTimeoutTextBox.Text,
                "Terminal timeout",
                out int terminalTimeout))
        {
            return;
        }

        try
        {
            SelectedOptions =
                new HarnessOptions(
                    workspacePath:
                        WorkspaceTextBox.Text.Trim(),

                    maxSteps:
                        maxSteps,

                    maxToolCalls:
                        maxToolCalls,

                    terminalTimeoutSeconds:
                        terminalTimeout);

            DialogResult =
                true;
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                exception.Message,
                "Некорректные настройки",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void CancelButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult =
            false;
    }

    private bool TryReadPositiveInt(
        string text,
        string name,
        out int value)
    {
        if (int.TryParse(
                text,
                out value)
            &&
            value > 0)
        {
            return true;
        }

        MessageBox.Show(
            this,
            $"{name} должен быть положительным целым числом.",
            "Некорректное значение",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);

        return false;
    }

    private async void EditProfileButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (ProfilesListBox.SelectedItem
            is not LlmProfile profile)
        {
            MessageBox.Show(
                this,
                "Выберите модель.",
                "Модель не выбрана",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        var window =
            new LlmProfileWindow(
                profile)
            {
                Owner =
                    this
            };

        bool? result =
            window.ShowDialog();

        if (result != true)
        {
            return;
        }

        string? newApiKey =
            string.IsNullOrEmpty(
                window.ApiKey)
                ? null
                : window.ApiKey;

        try
        {
            _profileCatalog =
                await _profileManager.UpdateAsync(
                    profileId:
                        profile.Id,

                    name:
                        window.ProfileName,

                    endpoint:
                        window.Endpoint,

                    model:
                        window.Model,

                    newApiKey:
                        newApiKey,

                    apiFormat:
                        window.ApiFormat);

            RefreshProfiles(
                profile.Id);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                exception.Message,
                "Не удалось изменить модель",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}