using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Honu.Maui.Wizard;

/// <summary>
/// Raised before the wizard navigates between steps. Set <see cref="CancelEventArgs.Cancel"/>
/// to true to prevent the navigation (e.g. when step validation fails).
/// </summary>
/// <remarks>
/// A handler that has to await something first - a server check, a confirmation dialog - takes
/// a deferral with <see cref="GetDeferral"/> and completes it once <see cref="CancelEventArgs.Cancel"/>
/// has been set. Handlers that decide synchronously need no deferral and keep working unchanged.
/// </remarks>
public class WizardNavigatingEventArgs : CancelEventArgs
{
    private List<WizardDeferral>? _deferrals;

    public WizardNavigatingEventArgs(
        WizardStepInfo? navigatingFrom,
        WizardStepInfo navigatingTo,
        WizardNavigationDirection direction)
    {
        NavigatingFrom = navigatingFrom;
        NavigatingTo = navigatingTo;
        Direction = direction;
    }

    #region NavigatingFrom (WizardStepInfo?)

    /// <summary>
    /// The step being left, including its index and <see cref="WizardStepInfo.StepId"/>.
    /// Null when no step is visible yet.
    /// </summary>
    public WizardStepInfo? NavigatingFrom { get; }

    #endregion

    #region NavigatingTo (WizardStepInfo)

    /// <summary>
    /// The step being navigated to, including its index and <see cref="WizardStepInfo.StepId"/>.
    /// </summary>
    public WizardStepInfo NavigatingTo { get; }

    #endregion

    #region Direction (WizardNavigationDirection)

    /// <summary>
    /// Direction of the navigation request.
    /// </summary>
    public WizardNavigationDirection Direction { get; }

    #endregion

    /// <summary>
    /// Takes a deferral, so the wizard waits for this handler before acting on
    /// <see cref="CancelEventArgs.Cancel"/>.
    /// </summary>
    /// <remarks>
    /// Must be requested synchronously - before the handler's first <c>await</c> - otherwise the
    /// wizard has already moved on. Multiple handlers may each take their own; navigation
    /// resumes once all of them are complete.
    /// </remarks>
    public WizardDeferral GetDeferral()
    {
        var deferral = new WizardDeferral();
        (_deferrals ??= []).Add(deferral);
        return deferral;
    }

    /// <summary>
    /// Completes once every deferral taken during the event has been completed.
    /// </summary>
    internal Task WaitForDeferralsAsync()
        => _deferrals is null || _deferrals.Count == 0
            ? Task.CompletedTask
            : Task.WhenAll(_deferrals.ConvertAll(deferral => deferral.Completion));
}
