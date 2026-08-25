using Microsoft.Maui.Controls;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Honu.Maui.Wizard.Tests;

/// <summary>
/// <see cref="WizardStep.IsSkipped"/> is bindable only because nothing ever moves in the visual
/// tree when it changes. Two earlier designs excluded a step by detaching or re-parenting it;
/// both broke, because losing a binding context resets a bound property to its default and the
/// value driving the exclusion was destroyed by the exclusion itself. These tests pin down that
/// the current design does not have that failure mode.
/// </summary>
public class WizardStepSkippingBindingTests
{
    private sealed class SkipSource : INotifyPropertyChanged
    {
        private bool _skipConditional;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool SkipConditional
        {
            get => _skipConditional;
            set
            {
                if (_skipConditional == value)
                {
                    return;
                }

                _skipConditional = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SkipConditional)));
            }
        }
    }

    private static (WizardControl Control, WizardStep Conditional, SkipSource Source) CreateBoundControl()
    {
        var source = new SkipSource();
        var conditional = new WizardStep { StepId = "conditional" };
        conditional.SetBinding(WizardStep.IsSkippedProperty, nameof(SkipSource.SkipConditional));

        var control = new WizardControl { BindingContext = source };
        control.Steps.Add(new WizardStep { StepId = "a" });
        control.Steps.Add(conditional);
        control.Steps.Add(new WizardStep { StepId = "z" });

        return (control, conditional, source);
    }

    [Fact]
    public async Task BoundIsSkipped_TrueAtStartup_MakesNavigationStepOver()
    {
        var (control, _, source) = CreateBoundControl();
        source.SkipConditional = true;

        await control.GoNextAsync();

        Assert.Equal("z", control.CurrentStep?.StepId);
    }

    /// <summary>
    /// The scenario both earlier designs died on: a step is skipped, then unskipped, then
    /// skipped again. Each toggle has to arrive, which it only does while the step keeps its
    /// binding context throughout.
    /// </summary>
    [Fact]
    public async Task BoundIsSkipped_SurvivesRepeatedToggling()
    {
        var (control, conditional, source) = CreateBoundControl();

        source.SkipConditional = true;
        Assert.True(conditional.IsSkipped);

        source.SkipConditional = false;
        Assert.False(conditional.IsSkipped);

        source.SkipConditional = true;
        Assert.True(conditional.IsSkipped);

        await control.GoNextAsync();
        Assert.Equal("z", control.CurrentStep?.StepId);
    }

    [Fact]
    public void BoundIsSkipped_TogglingKeepsTheStepInTheTree()
    {
        var (control, conditional, source) = CreateBoundControl();

        var parentChanges = 0;
        conditional.ParentChanged += (_, _) => parentChanges++;

        source.SkipConditional = true;
        source.SkipConditional = false;

        Assert.Equal(0, parentChanges);
        Assert.NotNull(conditional.Parent);
    }

    /// <summary>
    /// A bound property can change at any moment, including while a deferred
    /// <see cref="WizardControl.Navigating"/> handler is still deciding. Nothing is restructured
    /// on a skip change, so there is nothing for it to collide with.
    /// </summary>
    [Fact]
    public async Task IsSkippedChangedDuringNavigation_IsHarmless()
    {
        var (control, _, source) = CreateBoundControl();
        var gate = new TaskCompletionSource();

        control.Navigating += async (_, e) =>
        {
            using var deferral = e.GetDeferral();
            await gate.Task;
        };

        var navigation = control.GoNextAsync();

        source.SkipConditional = true;

        gate.SetResult();
        Assert.True(await navigation);

        // The navigation had already chosen its target before the flag changed, and keeps it.
        Assert.Equal("conditional", control.CurrentStep?.StepId);
    }
}
