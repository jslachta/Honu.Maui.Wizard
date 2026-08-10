using System;
using System.Threading.Tasks;

namespace Honu.Maui.Wizard;

/// <summary>
/// Keeps the wizard waiting while an asynchronous <see cref="WizardControl.Navigating"/>
/// handler makes up its mind. Obtained from
/// <see cref="WizardNavigatingEventArgs.GetDeferral"/>; the wizard reads
/// <see cref="System.ComponentModel.CancelEventArgs.Cancel"/> only once every deferral taken
/// during the event has been completed.
/// </summary>
/// <remarks>
/// Completing twice is harmless. Never completing one stalls the navigation for good, so
/// complete it in a <c>finally</c> - or take it with <c>using</c>, which does that for you.
/// </remarks>
public sealed class WizardDeferral : IDisposable
{
    #region Completion (Task)

    private readonly TaskCompletionSource _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Completes when the handler releases the deferral.
    /// </summary>
    internal Task Completion => _completion.Task;

    #endregion

    /// <summary>
    /// Releases the wizard to act on the handler's decision.
    /// </summary>
    public void Complete() => _completion.TrySetResult();

    /// <summary>
    /// Same as <see cref="Complete"/>, so the deferral can be scoped with <c>using</c>.
    /// </summary>
    public void Dispose() => Complete();
}
