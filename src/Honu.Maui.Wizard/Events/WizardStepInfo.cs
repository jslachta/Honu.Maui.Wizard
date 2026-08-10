using Microsoft.Maui.Controls;

namespace Honu.Maui.Wizard;

/// <summary>
/// Describes one endpoint of a wizard navigation: where the step sits in the active flow
/// (<see cref="Index"/>), which view it is (<see cref="Step"/>) and how the consumer identifies
/// it (<see cref="StepId"/>).
/// </summary>
public sealed class WizardStepInfo
{
    public WizardStepInfo(int index, View step)
    {
        Index = index;
        Step = step;
    }

    #region Index (int)

    /// <summary>
    /// Index of the step within the active flow (0-based).
    /// </summary>
    public int Index { get; }

    #endregion

    #region Step (View)

    /// <summary>
    /// The step view.
    /// </summary>
    public View Step { get; }

    #endregion

    #region StepId (string?)

    /// <summary>
    /// Identifier of the step, taken from <see cref="WizardStep.StepId"/>.
    /// Null for steps that are not a <see cref="WizardStep"/> or have no identifier assigned.
    /// </summary>
    public string? StepId => (Step as WizardStep)?.StepId;

    #endregion
}
