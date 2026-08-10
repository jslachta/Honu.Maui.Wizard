using Microsoft.Maui.Controls;

namespace Honu.Maui.Wizard;

/// <summary>
/// A single step of a <see cref="WizardControl"/>. Any <see cref="View"/> can act as a step;
/// this class adds wizard-specific metadata (<see cref="StepId"/>, <see cref="Title"/>,
/// <see cref="IsStepVisible"/>).
/// </summary>
public class WizardStep : ContentView
{
    #region StepId (string?)

    public static readonly BindableProperty StepIdProperty =
        BindableProperty.Create(
            nameof(StepId),
            typeof(string),
            typeof(WizardStep),
            default(string));

    /// <summary>
    /// Stable identifier of the step, independent of its position in the flow. Lets consumers
    /// recognise a step in <see cref="WizardNavigatingEventArgs"/> without relying on indexes,
    /// which shift as steps are added, removed or hidden via <see cref="IsStepVisible"/>.
    /// </summary>
    public string? StepId
    {
        get => (string?)GetValue(StepIdProperty);
        set => SetValue(StepIdProperty, value);
    }

    #endregion

    #region Title (string?)

    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(WizardStep),
            default(string));

    /// <summary>
    /// Optional title of the step, surfaced via <see cref="WizardControl.CurrentStepTitle"/>.
    /// </summary>
    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    #endregion

    #region IsStepVisible (bool)

    public static readonly BindableProperty IsStepVisibleProperty =
        BindableProperty.Create(
            nameof(IsStepVisible),
            typeof(bool),
            typeof(WizardStep),
            true);

    /// <summary>
    /// Whether this step is part of the wizard flow. Deliberately separate from
    /// <see cref="VisualElement.IsVisible"/>, which the wizard mutates while switching steps.
    /// </summary>
    /// <remarks>
    /// Set this in XAML for a step that starts out excluded, and call
    /// <see cref="WizardControl.RefreshStepVisibility"/> after changing it at runtime.
    /// <para>
    /// Do not bind it. A step outside the flow is not in the visual tree, so it has no binding
    /// context and the binding would fall back to this property's default of true - putting the
    /// step back into the flow, where the binding resolves to false again. Drive a conditional
    /// step from <see cref="WizardControl.StepVisibilityEvaluating"/> instead.
    /// </para>
    /// </remarks>
    public bool IsStepVisible
    {
        get => (bool)GetValue(IsStepVisibleProperty);
        set => SetValue(IsStepVisibleProperty, value);
    }

    #endregion
}
