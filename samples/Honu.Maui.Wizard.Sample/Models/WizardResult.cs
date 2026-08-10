namespace Honu.Maui.Wizard.Sample.Models;

/// <summary>
/// Snapshot of what the wizard collected, handed to the closing page. Deliberately a set of
/// finished strings: nothing here is editable any more.
/// </summary>
public sealed class WizardResult
{
    public WizardResult(
        string server,
        string appearance,
        string notifications,
        string advanced,
        string topics,
        string profile)
    {
        Server = server;
        Appearance = appearance;
        Notifications = notifications;
        Advanced = advanced;
        Topics = topics;
        Profile = profile;
    }

    #region Server (string)

    /// <summary>
    /// Chosen environment and the address behind it.
    /// </summary>
    public string Server { get; }

    #endregion

    #region Appearance (string)

    /// <summary>
    /// Chosen theme and language.
    /// </summary>
    public string Appearance { get; }

    #endregion

    #region Notifications (string)

    /// <summary>
    /// Notification and sound settings.
    /// </summary>
    public string Notifications { get; }

    #endregion

    #region Advanced (string)

    /// <summary>
    /// Advanced values, or a note that the defaults were kept.
    /// </summary>
    public string Advanced { get; }

    #endregion

    #region Topics (string)

    /// <summary>
    /// Topics the user subscribed to.
    /// </summary>
    public string Topics { get; }

    #endregion

    #region Profile (string)

    /// <summary>
    /// Name, birth date and digest time.
    /// </summary>
    public string Profile { get; }

    #endregion
}
