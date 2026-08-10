namespace Honu.Maui.Wizard.Sample;

/// <summary>
/// Identifiers assigned to the steps in <c>WizardPage.xaml</c> via <c>WizardStep.StepId</c>.
/// Validation and conditional visibility key off these instead of step indexes, which shift
/// as the conditional "advanced" step comes and goes.
/// </summary>
public static class StepIds
{
    public const string Intro = "intro";
    public const string Server = "server";
    public const string Appearance = "appearance";
    public const string Notifications = "notifications";
    public const string Advanced = "advanced";
    public const string Topics = "topics";
    public const string Profile = "profile";
    public const string Summary = "summary";
}
