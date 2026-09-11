using System.Windows;
using System.Windows.Controls;
using MyAgent.Configuration;

namespace MyAgent.Desktop;

public partial class LlmProfileWindow
    : Window
{
    private readonly LlmProfile? _editingProfile;

    public string ProfileName
    {
        get;
        private set;
    } =
        string.Empty;

    public string ApiFormat
    {
        get;
        private set;
    } =
        LlmApiFormats.OpenAiChatCompletions;

    public string Endpoint
    {
        get;
        private set;
    } =
        string.Empty;

    public string Model
    {
        get;
        private set;
    } =
        string.Empty;

    public string ApiKey
    {
        get;
        private set;
    } =
        string.Empty;

    public bool MakeActive
    {
        get;
        private set;
    }

    public LlmProfileWindow()
    {
        InitializeComponent();

        ApiFormatComboBox.SelectedIndex =
            0;

        NameTextBox.Focus();
    }

    public LlmProfileWindow(
        LlmProfile profile)
    {
        _editingProfile =
            profile
            ?? throw new ArgumentNullException(
                nameof(profile));

        InitializeComponent();

        Title =
            "Редактировать модель";

        NameTextBox.Text =
            profile.Name;

        EndpointTextBox.Text =
            profile.Endpoint;

        ModelTextBox.Text =
            profile.Model;

        SelectApiFormat(
            profile.ApiFormat);

        ApiKeyHintTextBlock.Text =
            "Оставьте API key пустым, "
            + "чтобы сохранить текущий ключ. "
            + "Если ввести новый ключ, "
            + "он заменит текущий и будет защищён Windows DPAPI.";

        MakeActiveCheckBox.Visibility =
            Visibility.Collapsed;

        NameTextBox.Focus();
    }

    private void SelectApiFormat(
        string apiFormat)
    {
        foreach (object item
                in ApiFormatComboBox.Items)
        {
            if (item is ComboBoxItem comboBoxItem
                &&
                comboBoxItem.Tag is string tag
                &&
                string.Equals(
                    tag,
                    apiFormat,
                    StringComparison.OrdinalIgnoreCase))
            {
                ApiFormatComboBox.SelectedItem =
                    comboBoxItem;

                return;
            }
        }

        ApiFormatComboBox.SelectedIndex =
            0;
    }

    private void SaveButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        string name =
            NameTextBox.Text.Trim();

        string endpoint =
            EndpointTextBox.Text.Trim();

        string model =
            ModelTextBox.Text.Trim();

        string apiKey =
            ApiKeyPasswordBox.Password;

        if (string.IsNullOrWhiteSpace(
                name))
        {
            ShowValidationError(
                "Введите имя модели.");

            NameTextBox.Focus();

            return;
        }

        if (string.IsNullOrWhiteSpace(
                endpoint))
        {
            ShowValidationError(
                "Введите endpoint.");

            EndpointTextBox.Focus();

            return;
        }

        if (!Uri.TryCreate(
                endpoint,
                UriKind.Absolute,
                out Uri? endpointUri)
            ||
            (endpointUri.Scheme !=
                Uri.UriSchemeHttps
             &&
             endpointUri.Scheme !=
                Uri.UriSchemeHttp))
        {
            ShowValidationError(
                "Endpoint должен быть корректным HTTP или HTTPS URL.");

            EndpointTextBox.Focus();

            return;
        }

        if (string.IsNullOrWhiteSpace(
                model))
        {
            ShowValidationError(
                "Введите Model ID.");

            ModelTextBox.Focus();

            return;
        }

        if (_editingProfile is null
            &&
            string.IsNullOrWhiteSpace(
                apiKey))
        {
            ShowValidationError(
                "Введите API key.");

            ApiKeyPasswordBox.Focus();

            return;
        }

        if (ApiFormatComboBox.SelectedItem
            is not ComboBoxItem selectedItem
            ||
            selectedItem.Tag
            is not string apiFormat
            ||
            string.IsNullOrWhiteSpace(
                apiFormat))
        {
            ShowValidationError(
                "Выберите тип API.");

            return;
        }

        ProfileName =
            name;

        ApiFormat =
            apiFormat;

        Endpoint =
            endpoint;

        Model =
            model;

        ApiKey =
            apiKey;

        MakeActive =
            MakeActiveCheckBox.IsChecked
            == true;

        DialogResult =
            true;
    }

    private void CancelButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult =
            false;
    }

    private void ShowValidationError(
        string message)
    {
        MessageBox.Show(
            this,
            message,
            "Некорректные данные",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }
}