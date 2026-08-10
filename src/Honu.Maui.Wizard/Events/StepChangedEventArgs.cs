using System;

namespace Honu.Maui.Wizard;

/// <summary>
/// Raised after the visible wizard step has changed.
/// </summary>
public class StepChangedEventArgs : EventArgs
{
    public StepChangedEventArgs(WizardStepInfo? previousStep, WizardStepInfo currentStep)
    {
        PreviousStep = previousStep;
        CurrentStep = currentStep;
    }

    #region PreviousStep (WizardStepInfo?)

    /// <summary>
    /// The step that was left, including its index and <see cref="WizardStepInfo.StepId"/>.
    /// Null when no step was visible before.
    /// </summary>
    public WizardStepInfo? PreviousStep { get; }

    #endregion

    #region CurrentStep (WizardStepInfo)

    /// <summary>
    /// The step that is now visible, including its index and <see cref="WizardStepInfo.StepId"/>.
    /// </summary>
    public WizardStepInfo CurrentStep { get; }

    #endregion
}
