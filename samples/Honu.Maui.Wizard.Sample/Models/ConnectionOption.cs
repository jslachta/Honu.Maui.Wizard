namespace Honu.Maui.Wizard.Sample.Models;

/// <summary>
/// One environment offered on the "server" step. The option with no <see cref="Url"/> stands
/// for a manually typed address and switches an extra entry into the step.
/// </summary>
public sealed class ConnectionOption
{
    public ConnectionOption(string name, string description, string? url = null)
    {
        Name = name;
        Description = description;
        Url = url;
    }

    #region Name (string)

    /// <summary>
    /// Short name of the environment.
    /// </summary>
    public string Name { get; }

    #endregion

    #region Description (string)

    /// <summary>
    /// What picking this environment means.
    /// </summary>
    public string Description { get; }

    #endregion

    #region Url (string?)

    /// <summary>
    /// Fixed address, or null when the user supplies one.
    /// </summary>
    public string? Url { get; }

    #endregion

    #region IsCustom (bool)

    /// <summary>
    /// True for the option whose address the user types in.
    /// </summary>
    public bool IsCustom => Url is null;

    #endregion

    #region UrlDisplay (string)

    /// <summary>
    /// Address line shown in the list, with a placeholder for the custom option.
    /// </summary>
    public string UrlDisplay => Url ?? "You enter the address";

    #endregion
}
