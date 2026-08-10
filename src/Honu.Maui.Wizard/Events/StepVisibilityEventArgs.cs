using Microsoft.Maui.Controls;
using System;

namespace Honu.Maui.Wizard;

/// <summary>
/// Allows a consumer to decide whether a given step should be part of the wizard flow.
/// </summary>
public class StepVisibilityEventArgs : EventArgs
{
    public StepVisibilityEventArgs(View step, int stepIndex, bool isVisible)
    {
        Step = step;
        StepIndex = stepIndex;
        IsVisible = isVisible;
    }

    #region Step (View)

    /// <summary>
    /// The step view being evaluated.
    /// </summary>
    public View Step { get; }

    #endregion

    #region StepIndex (int)

    /// <summary>
    /// Index of the step within <see cref="WizardControl.Steps"/> (0-based). -1 when not found.
    /// </summary>
    public int StepIndex { get; }

    #endregion

    #region IsVisible (bool)

    /// <summary>
    /// Whether the step should be included in the wizard flow.
    /// Pre-populated from <see cref="WizardStep.IsStepVisible"/> (or true for non-<see cref="WizardStep"/> views);
    /// a handler may override the value.
    /// </summary>
    public bool IsVisible { get; set; }

    #endregion
}
