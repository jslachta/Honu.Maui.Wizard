using Microsoft.Maui.Controls;

namespace Honu.Maui.Wizard;

/// <summary>
/// A single step of a <see cref="WizardControl"/>. Any <see cref="View"/> can act as a step;
/// this class adds wizard-specific metadata (<see cref="StepId"/>, <see cref="Title"/>,
/// <see cref="IsSkipped"/>).
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
    /// Stable identifier of the step, independent of its position. Lets consumers recognise a
    /// step in <see cref="WizardNavigatingEventArgs"/> without relying on indexes, which shift
    /// as steps are added to or removed from <see cref="WizardControl.Steps"/>.
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

    #region IsSkipped (bool)

    public static readonly BindableProperty IsSkippedProperty =
        BindableProperty.Create(
            nameof(IsSkipped),
            typeof(bool),
            typeof(WizardStep),
            false,
            propertyChanged: OnIsSkippedChanged);

    /// <summary>
    /// Whether <see cref="WizardControl.GoNextAsync"/> and <see cref="WizardControl.GoBackAsync"/>
    /// pass over this step.
    /// </summary>
    /// <remarks>
    /// Affects transitions and nothing else. The step keeps its place in
    /// <see cref="WizardControl.Steps"/> and its index, so the wizard's length and every step's
    /// position stay the same however the flags move - which is what makes a progress indicator
    /// possible. Nothing is added to or removed from the visual tree either, so this is safe to
    /// bind: the step never loses its binding context.
    /// <para>
    /// A skipped step is not hidden. <see cref="WizardControl.GoToStepAsync(int)"/> still goes
    /// there when asked, and a step skipped while the user is standing on it stays on screen
    /// until they navigate away.
    /// </para>
    /// </remarks>
    public bool IsSkipped
    {
        get => (bool)GetValue(IsSkippedProperty);
        set => SetValue(IsSkippedProperty, value);
    }

    private static void OnIsSkippedChanged(BindableObject bindable, object oldValue, object newValue)
    {
        // Only the Back/Next/Finish buttons depend on this; nothing is restructured.
        (bindable as WizardStep)?.FindOwner()?.OnStepSkippedChanged();
    }

    /// <summary>
    /// Walks up to the <see cref="WizardControl"/> hosting this step, or null when the step is
    /// not part of one.
    /// </summary>
    private WizardControl? FindOwner()
    {
        for (Element? element = Parent; element is not null; element = element.Parent)
        {
            if (element is WizardControl control)
            {
                return control;
            }
        }

        return null;
    }

    #endregion
}
