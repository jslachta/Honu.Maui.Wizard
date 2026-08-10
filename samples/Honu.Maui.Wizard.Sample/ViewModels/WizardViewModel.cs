using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using Honu.Maui.Wizard.Sample.Models;
using Microsoft.Maui.Controls;

namespace Honu.Maui.Wizard.Sample.ViewModels;

/// <summary>
/// State behind <c>WizardPage</c>. Each step showcases a different family of MAUI input
/// controls and carries the rule that decides whether the user may leave it - see
/// <see cref="TryValidateStep"/>, which the page calls from the cancelable
/// <c>Navigating</c> event.
/// </summary>
public sealed class WizardViewModel : ObservableObject
{
    public const string ThemeLight = "light";
    public const string ThemeDark = "dark";
    public const string ThemeSystem = "system";

    public WizardViewModel()
    {
        ConnectionOptions =
        [
            new ConnectionOption("Production", "Live traffic. Everything written here is kept for good.", "https://api.example.com"),
            new ConnectionOption("Staging", "Shared test environment; its data is reset every night.", "https://test.example.com"),
            new ConnectionOption("Custom address", "A server on your own network or machine. You type the address below."),
        ];

        Languages = ["English", "Deutsch", "Français", "Čeština"];

        Topics =
        [
            new TopicItemViewModel("Release notes", "A summary of what changed in every version."),
            new TopicItemViewModel("Tips and tricks", "Short guides on getting more out of the app."),
            new TopicItemViewModel("Security", "Alerts about updates and vulnerabilities."),
            new TopicItemViewModel("Performance", "Advice on speed and battery usage."),
            new TopicItemViewModel("Design", "Changes to the look and new themes."),
        ];

        foreach (var topic in Topics)
        {
            topic.PropertyChanged += OnTopicChanged;
        }

        NavigatingCommand = new Command<WizardNavigatingEventArgs>(async args => await OnNavigatingAsync(args));
        FinishCommand = new Command(async () => await FinishAsync());
    }

    #region NavigatingCommand (ICommand)

    /// <summary>
    /// Bound to <c>WizardControl.NavigatingCommand</c>: the only gate the wizard cannot express
    /// declaratively, because validation has to be able to cancel.
    /// </summary>
    public ICommand NavigatingCommand { get; }

    private async Task OnNavigatingAsync(WizardNavigatingEventArgs e)
    {
        // Going back is always allowed - only forward navigation is gated.
        if (e.Direction != WizardNavigationDirection.Next)
        {
            return;
        }

        // Taken before the first await, so the wizard waits for the whole method. Disposing it
        // releases the navigation - including when something below throws, which is why the
        // deferral is scoped with using rather than completed at the end.
        using var deferral = e.GetDeferral();

        try
        {
            var stepId = e.NavigatingFrom?.StepId;

            if (!TryValidateStep(stepId, out var error))
            {
                e.Cancel = true;
                await Shell.Current.DisplayAlertAsync("Not so fast", error, "Got it");
                return;
            }

            // The server step additionally waits for a reachability check - the point of the
            // deferral: the answer only arrives after an await.
            if (stepId == StepIds.Server && !await IsServerReachableAsync())
            {
                e.Cancel = true;
                await Shell.Current.DisplayAlertAsync(
                    "Server did not answer",
                    $"Could not reach {EffectiveServerUrl}. Try again, or pick a different environment.",
                    "Got it");
            }
        }
        catch (Exception ex)
        {
            // Command wraps this in an async void, so an escaping exception would vanish and
            // the step would silently let the user through. Refuse instead.
            e.Cancel = true;
            await Shell.Current.DisplayAlertAsync("Check failed", ex.Message, "OK");
        }
    }

    #endregion

    #region FinishCommand (ICommand)

    /// <summary>
    /// Executed by the wizard's Finish button. Hands the collected answers to the closing page.
    /// </summary>
    public ICommand FinishCommand { get; }

    private async Task FinishAsync()
    {
        // Command wraps this in an async void, which swallows exceptions - a failed navigation
        // would look like the Finish button doing nothing at all. Surface it instead.
        try
        {
            await Shell.Current.GoToAsync("done", new Dictionary<string, object>
            {
                [NavigationKeys.Result] = CreateResult(),
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Navigation failed", ex.Message, "OK");
        }
    }

    #endregion

    #region Step: server (CollectionView + conditional Entry)

    #region ConnectionOptions (IReadOnlyList<ConnectionOption>)

    /// <summary>
    /// Environments offered on the server step.
    /// </summary>
    public IReadOnlyList<ConnectionOption> ConnectionOptions { get; }

    #endregion

    #region SelectedConnection (ConnectionOption?)

    private ConnectionOption? _selectedConnection;

    /// <summary>
    /// Environment picked in the list; null until the user chooses.
    /// </summary>
    public ConnectionOption? SelectedConnection
    {
        get => _selectedConnection;
        set
        {
            if (SetProperty(ref _selectedConnection, value))
            {
                OnPropertyChanged(nameof(IsCustomConnection));
                RaiseValidation(nameof(ServerError), nameof(HasServerError));
                RaiseSummaryChanged();
            }
        }
    }

    #endregion

    #region IsCustomConnection (bool)

    /// <summary>
    /// Reveals the address entry on the server step.
    /// </summary>
    public bool IsCustomConnection => SelectedConnection?.IsCustom == true;

    #endregion

    #region CustomServerUrl (string)

    private string _customServerUrl = string.Empty;

    /// <summary>
    /// Address typed by the user; only relevant for the custom environment.
    /// </summary>
    public string CustomServerUrl
    {
        get => _customServerUrl;
        set
        {
            if (SetProperty(ref _customServerUrl, value))
            {
                RaiseValidation(nameof(ServerError), nameof(HasServerError));
                RaiseSummaryChanged();
            }
        }
    }

    #endregion

    #region EffectiveServerUrl (string)

    /// <summary>
    /// Address the app would actually use.
    /// </summary>
    public string EffectiveServerUrl => IsCustomConnection
        ? CustomServerUrl.Trim()
        : SelectedConnection?.Url ?? string.Empty;

    #endregion

    #region SimulateUnreachableServer (bool)

    private bool _simulateUnreachableServer;

    /// <summary>
    /// Lets the sample demonstrate both outcomes of the asynchronous check on demand, instead
    /// of hiding the failure path behind something the user cannot trigger.
    /// </summary>
    public bool SimulateUnreachableServer
    {
        get => _simulateUnreachableServer;
        set => SetProperty(ref _simulateUnreachableServer, value);
    }

    #endregion

    #region ServerError (string?)

    /// <summary>
    /// Why the server step cannot be left yet, or null when it is fine.
    /// </summary>
    public string? ServerError
    {
        get
        {
            if (SelectedConnection is null)
            {
                return "Pick the environment the app should connect to.";
            }

            if (!IsCustomConnection)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(CustomServerUrl))
            {
                return "Enter the address of your server.";
            }

            return IsHttpUrl(CustomServerUrl)
                ? null
                : "The address has to be a full URL, for example https://192.168.1.10:5000.";
        }
    }

    #endregion

    #region HasServerError (bool)

    /// <summary>
    /// Drives the inline message on the server step.
    /// </summary>
    public bool HasServerError => ServerError is not null;

    #endregion

    /// <summary>
    /// Stands in for a real reachability probe. The wizard awaits this through a deferral on
    /// the <c>Navigating</c> event, so the step cannot be left until the answer is in.
    /// </summary>
    public async Task<bool> IsServerReachableAsync()
    {
        await Task.Delay(1200);
        return !SimulateUnreachableServer;
    }

    private static bool IsHttpUrl(string value)
        => Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && !string.IsNullOrEmpty(uri.Host);

    #endregion

    #region Step: appearance (RadioButton + Picker)

    #region Theme (string?)

    private string? _theme;

    /// <summary>
    /// Bound to <c>RadioButtonGroup.SelectedValue</c>, so it stays null until picked.
    /// </summary>
    public string? Theme
    {
        get => _theme;
        set
        {
            if (SetProperty(ref _theme, value))
            {
                OnPropertyChanged(nameof(ThemeDisplay));
                RaiseValidation(nameof(AppearanceError), nameof(HasAppearanceError));
                RaiseSummaryChanged();
            }
        }
    }

    #endregion

    #region ThemeDisplay (string)

    /// <summary>
    /// Human-readable form of <see cref="Theme"/> for the summary.
    /// </summary>
    public string ThemeDisplay => Theme switch
    {
        ThemeLight => "Light",
        ThemeDark => "Dark",
        ThemeSystem => "Match the system",
        _ => "—",
    };

    #endregion

    #region Languages (IReadOnlyList<string>)

    /// <summary>
    /// Languages offered in the picker.
    /// </summary>
    public IReadOnlyList<string> Languages { get; }

    #endregion

    #region SelectedLanguage (string?)

    private string? _selectedLanguage;

    /// <summary>
    /// Language picked by the user; null until chosen.
    /// </summary>
    public string? SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (SetProperty(ref _selectedLanguage, value))
            {
                RaiseValidation(nameof(AppearanceError), nameof(HasAppearanceError));
                RaiseSummaryChanged();
            }
        }
    }

    #endregion

    #region AppearanceError (string?)

    /// <summary>
    /// Why the appearance step cannot be left yet, or null when it is fine.
    /// </summary>
    public string? AppearanceError
    {
        get
        {
            if (string.IsNullOrEmpty(Theme))
            {
                return "Pick a theme for the app.";
            }

            return SelectedLanguage is null ? "Pick a language." : null;
        }
    }

    #endregion

    #region HasAppearanceError (bool)

    /// <summary>
    /// Drives the inline message on the appearance step.
    /// </summary>
    public bool HasAppearanceError => AppearanceError is not null;

    #endregion

    #endregion

    #region Step: notifications (Switch + CheckBox)

    #region AreNotificationsEnabled (bool)

    private bool _areNotificationsEnabled = true;

    /// <summary>
    /// Master switch for notifications; also decides whether the consent is required.
    /// </summary>
    public bool AreNotificationsEnabled
    {
        get => _areNotificationsEnabled;
        set
        {
            if (SetProperty(ref _areNotificationsEnabled, value))
            {
                RaiseValidation(nameof(NotificationsError), nameof(HasNotificationsError));
                RaiseSummaryChanged();
            }
        }
    }

    #endregion

    #region IsSoundEnabled (bool)

    private bool _isSoundEnabled = true;

    /// <summary>
    /// Only meaningful with notifications on; the switch is disabled otherwise.
    /// </summary>
    public bool IsSoundEnabled
    {
        get => _isSoundEnabled;
        set
        {
            if (SetProperty(ref _isSoundEnabled, value))
            {
                RaiseSummaryChanged();
            }
        }
    }

    #endregion

    #region HasAcceptedTerms (bool)

    private bool _hasAcceptedTerms;

    /// <summary>
    /// Consent required before notifications may be turned on.
    /// </summary>
    public bool HasAcceptedTerms
    {
        get => _hasAcceptedTerms;
        set
        {
            if (SetProperty(ref _hasAcceptedTerms, value))
            {
                RaiseValidation(nameof(NotificationsError), nameof(HasNotificationsError));
                RaiseSummaryChanged();
            }
        }
    }

    #endregion

    #region ShowAdvanced (bool)

    private bool _showAdvanced;

    /// <summary>
    /// Bound straight to <c>WizardStep.IsStepVisible</c> of the advanced step, which is all it
    /// takes to add or remove that step from the flow.
    /// </summary>
    public bool ShowAdvanced
    {
        get => _showAdvanced;
        set
        {
            if (SetProperty(ref _showAdvanced, value))
            {
                RaiseValidation(nameof(AdvancedError), nameof(HasAdvancedError));
                RaiseSummaryChanged();
            }
        }
    }

    #endregion

    #region NotificationsError (string?)

    /// <summary>
    /// The consent is only required when notifications are actually turned on.
    /// </summary>
    public string? NotificationsError => AreNotificationsEnabled && !HasAcceptedTerms
        ? "Confirm you agree to receive notifications before turning them on."
        : null;

    #endregion

    #region HasNotificationsError (bool)

    /// <summary>
    /// Drives the inline message on the notifications step.
    /// </summary>
    public bool HasNotificationsError => NotificationsError is not null;

    #endregion

    #endregion

    #region Step: advanced (Slider + Stepper, conditional)

    #region ListFontSize (double)

    private double _listFontSize = 16;

    /// <summary>
    /// Drives the live preview label on the step as well as the value readout.
    /// </summary>
    public double ListFontSize
    {
        get => _listFontSize;
        set
        {
            if (SetProperty(ref _listFontSize, value))
            {
                OnPropertyChanged(nameof(ListFontSizeDisplay));
                RaiseValidation(nameof(AdvancedError), nameof(HasAdvancedError));
                RaiseSummaryChanged();
            }
        }
    }

    #endregion

    #region ListFontSizeDisplay (string)

    /// <summary>
    /// <see cref="ListFontSize"/> rounded for display.
    /// </summary>
    public string ListFontSizeDisplay
        => $"{((int)ListFontSize).ToString(CultureInfo.InvariantCulture)} px";

    #endregion

    #region ItemsPerPage (double)

    private double _itemsPerPage = 20;

    /// <summary>
    /// Page size chosen with the stepper. Double because that is what <c>Stepper</c> binds to.
    /// </summary>
    public double ItemsPerPage
    {
        get => _itemsPerPage;
        set
        {
            if (SetProperty(ref _itemsPerPage, value))
            {
                OnPropertyChanged(nameof(ItemsPerPageDisplay));
                RaiseValidation(nameof(AdvancedError), nameof(HasAdvancedError));
                RaiseSummaryChanged();
            }
        }
    }

    #endregion

    #region ItemsPerPageDisplay (string)

    /// <summary>
    /// <see cref="ItemsPerPage"/> rounded for display.
    /// </summary>
    public string ItemsPerPageDisplay
        => ((int)ItemsPerPage).ToString(CultureInfo.InvariantCulture);

    #endregion

    #region AdvancedError (string?)

    /// <summary>
    /// A cross-field rule rather than a "something is missing" one: both values are always
    /// set, but some combinations of them do not go together.
    /// </summary>
    public string? AdvancedError => ShowAdvanced && ListFontSize >= 20 && ItemsPerPage > 20
        ? "With text at 20 px or larger, keep to 20 items per page at most."
        : null;

    #endregion

    #region HasAdvancedError (bool)

    /// <summary>
    /// Drives the inline message on the advanced step.
    /// </summary>
    public bool HasAdvancedError => AdvancedError is not null;

    #endregion

    #endregion

    #region Step: topics (multi-select)

    #region Topics (IReadOnlyList<TopicItemViewModel>)

    /// <summary>
    /// Checkable rows of the multi-select step.
    /// </summary>
    public IReadOnlyList<TopicItemViewModel> Topics { get; }

    #endregion

    #region SelectedTopicCount (int)

    /// <summary>
    /// How many topics are ticked right now.
    /// </summary>
    public int SelectedTopicCount => Topics.Count(topic => topic.IsSelected);

    #endregion

    #region TopicsError (string?)

    /// <summary>
    /// Why the topics step cannot be left yet, or null when it is fine.
    /// </summary>
    public string? TopicsError => SelectedTopicCount < 2
        ? $"Pick at least two topics; you have {SelectedTopicCount} so far."
        : null;

    #endregion

    #region HasTopicsError (bool)

    /// <summary>
    /// Drives the inline message on the topics step.
    /// </summary>
    public bool HasTopicsError => TopicsError is not null;

    #endregion

    private void OnTopicChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(SelectedTopicCount));
        RaiseValidation(nameof(TopicsError), nameof(HasTopicsError));
        RaiseSummaryChanged();
    }

    #endregion

    #region Step: profile (Entry, Editor, DatePicker, TimePicker)

    #region DisplayName (string)

    private string _displayName = string.Empty;

    /// <summary>
    /// Name the user wants to be shown as.
    /// </summary>
    public string DisplayName
    {
        get => _displayName;
        set
        {
            if (SetProperty(ref _displayName, value))
            {
                RaiseValidation(nameof(ProfileError), nameof(HasProfileError));
                RaiseSummaryChanged();
            }
        }
    }

    #endregion

    #region Bio (string)

    private string _bio = string.Empty;

    /// <summary>
    /// Free-form text with a minimum length.
    /// </summary>
    public string Bio
    {
        get => _bio;
        set
        {
            if (SetProperty(ref _bio, value))
            {
                OnPropertyChanged(nameof(BioLengthDisplay));
                RaiseValidation(nameof(ProfileError), nameof(HasProfileError));
                RaiseSummaryChanged();
            }
        }
    }

    #endregion

    #region BioLengthDisplay (string)

    /// <summary>
    /// Live character counter under the editor.
    /// </summary>
    public string BioLengthDisplay => $"{Bio.Length} / 10 characters minimum";

    #endregion

    #region BirthDate (DateTime)

    private DateTime _birthDate = DateTime.Today;

    /// <summary>
    /// Must end up in the past; starts at today so the step gates until it is changed.
    /// </summary>
    public DateTime BirthDate
    {
        get => _birthDate;
        set
        {
            if (SetProperty(ref _birthDate, value))
            {
                RaiseValidation(nameof(ProfileError), nameof(HasProfileError));
                RaiseSummaryChanged();
            }
        }
    }

    #endregion

    #region DigestTime (TimeSpan)

    private TimeSpan _digestTime = new(8, 0, 0);

    /// <summary>
    /// When the daily digest should arrive. Always valid.
    /// </summary>
    public TimeSpan DigestTime
    {
        get => _digestTime;
        set
        {
            if (SetProperty(ref _digestTime, value))
            {
                RaiseSummaryChanged();
            }
        }
    }

    #endregion

    #region ProfileError (string?)

    /// <summary>
    /// Why the profile step cannot be left yet, or null when it is fine.
    /// </summary>
    public string? ProfileError
    {
        get
        {
            if (DisplayName.Trim().Length < 2)
            {
                return "The display name needs at least two characters.";
            }

            if (Bio.Trim().Length < 10)
            {
                return "Write at least ten characters about yourself.";
            }

            return BirthDate >= DateTime.Today
                ? "The date of birth has to be in the past."
                : null;
        }
    }

    #endregion

    #region HasProfileError (bool)

    /// <summary>
    /// Drives the inline message on the profile step.
    /// </summary>
    public bool HasProfileError => ProfileError is not null;

    #endregion

    #endregion

    #region Step: summary

    #region IsSummaryConfirmed (bool)

    private bool _isSummaryConfirmed;

    /// <summary>
    /// The confirmation tick that releases the Finish button.
    /// </summary>
    public bool IsSummaryConfirmed
    {
        get => _isSummaryConfirmed;
        set
        {
            if (SetProperty(ref _isSummaryConfirmed, value))
            {
                OnPropertyChanged(nameof(IsFinishEnabled));
            }
        }
    }

    #endregion

    #region SummaryServer (string)

    /// <summary>
    /// Server line of the summary.
    /// </summary>
    public string SummaryServer => SelectedConnection is null
        ? "—"
        : $"{SelectedConnection.Name} ({EffectiveServerUrl})";

    #endregion

    #region SummaryAppearance (string)

    /// <summary>
    /// Appearance line of the summary.
    /// </summary>
    public string SummaryAppearance => $"{ThemeDisplay}, {SelectedLanguage ?? "—"}";

    #endregion

    #region SummaryNotifications (string)

    /// <summary>
    /// Notifications line of the summary.
    /// </summary>
    public string SummaryNotifications
    {
        get
        {
            if (!AreNotificationsEnabled)
            {
                return "Off";
            }

            return IsSoundEnabled ? "On, with sound" : "On, silent";
        }
    }

    #endregion

    #region SummaryAdvanced (string)

    /// <summary>
    /// Advanced line of the summary.
    /// </summary>
    public string SummaryAdvanced => ShowAdvanced
        ? $"Text {ListFontSizeDisplay}, {ItemsPerPageDisplay} items per page"
        : "Default values";

    #endregion

    #region SummaryTopics (string)

    /// <summary>
    /// Topics line of the summary.
    /// </summary>
    public string SummaryTopics
    {
        get
        {
            var selected = Topics.Where(topic => topic.IsSelected).Select(topic => topic.Name).ToArray();
            return selected.Length == 0 ? "—" : string.Join(", ", selected);
        }
    }

    #endregion

    #region SummaryProfile (string)

    /// <summary>
    /// Profile line of the summary.
    /// </summary>
    public string SummaryProfile => string.IsNullOrWhiteSpace(DisplayName)
        ? "—"
        : $"{DisplayName}, born {BirthDate:d}, digest at {DigestTime:hh\\:mm}";

    #endregion

    #region IsFinishEnabled (bool)

    /// <summary>
    /// Bound to <c>WizardControl.IsFinishEnabled</c>: every step must validate and the user
    /// has to tick the confirmation.
    /// </summary>
    public bool IsFinishEnabled =>
        IsSummaryConfirmed
        && ServerError is null
        && AppearanceError is null
        && NotificationsError is null
        && AdvancedError is null
        && TopicsError is null
        && ProfileError is null;

    #endregion

    #endregion

    /// <summary>
    /// Freezes the collected answers for the closing page. Nothing is persisted anywhere -
    /// restarting the app runs the wizard again.
    /// </summary>
    public WizardResult CreateResult() => new(
        SummaryServer,
        SummaryAppearance,
        SummaryNotifications,
        SummaryAdvanced,
        SummaryTopics,
        SummaryProfile);

    /// <summary>
    /// Returns true when the step may be left in the forward direction; otherwise
    /// <paramref name="error"/> explains what is missing.
    /// </summary>
    public bool TryValidateStep(string? stepId, [NotNullWhen(false)] out string? error)
    {
        error = stepId switch
        {
            StepIds.Server => ServerError,
            StepIds.Appearance => AppearanceError,
            StepIds.Notifications => NotificationsError,
            StepIds.Advanced => AdvancedError,
            StepIds.Topics => TopicsError,
            StepIds.Profile => ProfileError,
            _ => null,
        };

        return error is null;
    }

    private void RaiseValidation(string errorProperty, string hasErrorProperty)
    {
        OnPropertyChanged(errorProperty);
        OnPropertyChanged(hasErrorProperty);
    }

    private void RaiseSummaryChanged()
    {
        OnPropertyChanged(nameof(SummaryServer));
        OnPropertyChanged(nameof(SummaryAppearance));
        OnPropertyChanged(nameof(SummaryNotifications));
        OnPropertyChanged(nameof(SummaryAdvanced));
        OnPropertyChanged(nameof(SummaryTopics));
        OnPropertyChanged(nameof(SummaryProfile));
        OnPropertyChanged(nameof(IsFinishEnabled));
    }
}
