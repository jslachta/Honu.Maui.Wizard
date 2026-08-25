using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace Honu.Maui.Wizard.Tests;

public class WizardControlTests
{
    private static WizardControl CreateControl(params View[] steps)
    {
        var control = new WizardControl();

        foreach (var step in steps)
        {
            control.Steps.Add(step);
        }

        return control;
    }

    #region Flow state

    [Fact]
    public void NoSteps_HasNoCurrentStepAndNoButtons()
    {
        var control = CreateControl();

        Assert.Equal(-1, control.CurrentStepIndex);
        Assert.Null(control.CurrentStep);
        Assert.False(control.IsBackVisible);
        Assert.False(control.IsNextVisible);
        Assert.False(control.IsFinishVisible);
    }

    [Fact]
    public void FirstStep_ShowsNextButNotBackOrFinish()
    {
        var control = CreateControl(new WizardStep(), new WizardStep());

        Assert.Equal(0, control.CurrentStepIndex);
        Assert.False(control.IsBackVisible);
        Assert.True(control.IsNextVisible);
        Assert.False(control.IsFinishVisible);
    }

    [Fact]
    public void SingleStep_ShowsFinishImmediately()
    {
        var control = CreateControl(new WizardStep());

        Assert.False(control.IsBackVisible);
        Assert.False(control.IsNextVisible);
        Assert.True(control.IsFinishVisible);
    }

    [Fact]
    public async Task LastStep_ShowsBackAndFinish()
    {
        var control = CreateControl(new WizardStep(), new WizardStep());

        await control.GoNextAsync();

        Assert.True(control.IsBackVisible);
        Assert.False(control.IsNextVisible);
        Assert.True(control.IsFinishVisible);
    }

    [Fact]
    public void CurrentStep_CarriesIndexViewAndId()
    {
        var first = new WizardStep { StepId = "first", Title = "První" };
        var control = CreateControl(first, new WizardStep());

        Assert.NotNull(control.CurrentStep);
        Assert.Equal(0, control.CurrentStep.Index);
        Assert.Same(first, control.CurrentStep.Step);
        Assert.Equal("first", control.CurrentStep.StepId);
        Assert.Equal("První", control.CurrentStepTitle);
    }

    [Fact]
    public void CurrentStepTitle_IsNullForPlainViews()
    {
        var control = CreateControl(new ContentView());

        Assert.Null(control.CurrentStepTitle);
    }

    #endregion

    #region Skipping

    /// <summary>
    /// The point of skipping rather than removing: indexes do not shift, so a progress
    /// indicator can be built on them.
    /// </summary>
    [Fact]
    public async Task SkippedStep_DoesNotShiftTheIndexesAroundIt()
    {
        var skipped = new WizardStep { StepId = "b" };
        var control = CreateControl(
            new WizardStep { StepId = "a" },
            skipped,
            new WizardStep { StepId = "c" });

        await control.GoToStepAsync("c");
        Assert.Equal(2, control.CurrentStepIndex);

        skipped.IsSkipped = true;

        // "c" is still the third step, not the second.
        Assert.Equal(2, control.CurrentStepIndex);
    }

    [Fact]
    public async Task GoNextAsync_StepsOverASkippedStep()
    {
        var control = CreateControl(
            new WizardStep { StepId = "a" },
            new WizardStep { StepId = "b", IsSkipped = true },
            new WizardStep { StepId = "c" });

        Assert.True(await control.GoNextAsync());

        Assert.Equal("c", control.CurrentStep?.StepId);
        Assert.Equal(2, control.CurrentStepIndex);
    }

    [Fact]
    public async Task GoNextAsync_StepsOverSeveralSkippedStepsInARow()
    {
        var control = CreateControl(
            new WizardStep { StepId = "a" },
            new WizardStep { StepId = "b", IsSkipped = true },
            new WizardStep { StepId = "c", IsSkipped = true },
            new WizardStep { StepId = "d" });

        Assert.True(await control.GoNextAsync());

        Assert.Equal("d", control.CurrentStep?.StepId);
    }

    [Fact]
    public async Task GoBackAsync_StepsOverASkippedStep()
    {
        var control = CreateControl(
            new WizardStep { StepId = "a" },
            new WizardStep { StepId = "b", IsSkipped = true },
            new WizardStep { StepId = "c" });

        await control.GoNextAsync();
        Assert.True(await control.GoBackAsync());

        Assert.Equal("a", control.CurrentStep?.StepId);
    }

    [Fact]
    public void TrailingSkippedSteps_PutFinishOnTheLastReachableStep()
    {
        var control = CreateControl(
            new WizardStep { StepId = "a" },
            new WizardStep { StepId = "b", IsSkipped = true });

        // Nothing lies ahead that can be reached, so this is where the wizard ends.
        Assert.True(control.IsFinishVisible);
        Assert.False(control.IsNextVisible);
    }

    [Fact]
    public void LeadingSkippedSteps_HideBackOnTheFirstReachableStep()
    {
        var control = CreateControl(
            new WizardStep { StepId = "a", IsSkipped = true },
            new WizardStep { StepId = "b" },
            new WizardStep { StepId = "c" });

        // The wizard opens on the first step whatever its flag says; skipping governs
        // transitions, and opening is not one.
        Assert.Equal(0, control.CurrentStepIndex);
        Assert.False(control.IsBackVisible);
    }

    [Fact]
    public async Task SkippingAStepAhead_MovesFinishWithoutTouchingTheCurrentStep()
    {
        var last = new WizardStep { StepId = "c" };
        var control = CreateControl(
            new WizardStep { StepId = "a" },
            new WizardStep { StepId = "b" },
            last);

        await control.GoNextAsync();
        Assert.True(control.IsNextVisible);
        Assert.False(control.IsFinishVisible);

        last.IsSkipped = true;

        Assert.False(control.IsNextVisible);
        Assert.True(control.IsFinishVisible);
        Assert.Equal("b", control.CurrentStep?.StepId);
    }

    /// <summary>
    /// Skipping the step the user is standing on does not move them: the wizard would otherwise
    /// jump under their feet. They leave it on the next navigation like any other step.
    /// </summary>
    [Fact]
    public async Task SkippingTheCurrentStep_LeavesTheUserOnIt()
    {
        var second = new WizardStep { StepId = "b" };
        var control = CreateControl(
            new WizardStep { StepId = "a" },
            second,
            new WizardStep { StepId = "c" });

        await control.GoNextAsync();

        second.IsSkipped = true;

        Assert.Equal("b", control.CurrentStep?.StepId);
        Assert.True(control.IsBackVisible);
        Assert.True(control.IsNextVisible);
    }

    [Fact]
    public void EveryStepSkipped_LeavesTheFirstStepShowingWithFinishOnly()
    {
        var control = CreateControl(
            new WizardStep { StepId = "a", IsSkipped = true },
            new WizardStep { StepId = "b", IsSkipped = true });

        Assert.Equal("a", control.CurrentStep?.StepId);
        Assert.False(control.IsBackVisible);
        Assert.False(control.IsNextVisible);
        Assert.True(control.IsFinishVisible);
    }

    /// <summary>
    /// Nothing is added to or removed from the visual tree when a flag changes - which is what
    /// makes the property safe to bind, and preserves state living in the tree rather than on
    /// the view, such as <c>RadioButtonGroup.SelectedValue</c>.
    /// </summary>
    [Fact]
    public void ChangingIsSkipped_NeverReparentsAnything()
    {
        var kept = new WizardStep { StepId = "kept" };
        var toggled = new WizardStep { StepId = "toggled" };
        var control = CreateControl(kept, toggled);

        var reparentCount = 0;
        kept.ParentChanged += (_, _) => reparentCount++;
        toggled.ParentChanged += (_, _) => reparentCount++;

        toggled.IsSkipped = true;
        toggled.IsSkipped = false;

        Assert.Equal(0, reparentCount);
    }

    #endregion

    #region Deliberate jumps

    /// <summary>
    /// Next and Back pass over a skipped step, but asking for one by name is a deliberate act
    /// and the wizard takes it at its word.
    /// </summary>
    [Fact]
    public async Task GoToStepAsync_ById_ReachesASkippedStep()
    {
        var control = CreateControl(
            new WizardStep { StepId = "a" },
            new WizardStep { StepId = "skipped", IsSkipped = true },
            new WizardStep { StepId = "c" });

        Assert.True(await control.GoToStepAsync("skipped"));
        Assert.Equal("skipped", control.CurrentStep?.StepId);
    }

    [Fact]
    public async Task GoToStepAsync_ByIndex_ReachesASkippedStep()
    {
        var control = CreateControl(
            new WizardStep { StepId = "a" },
            new WizardStep { StepId = "skipped", IsSkipped = true });

        Assert.True(await control.GoToStepAsync(1));
        Assert.Equal("skipped", control.CurrentStep?.StepId);
    }

    [Fact]
    public async Task NavigationFromASkippedStep_ResumesTheNormalRules()
    {
        var control = CreateControl(
            new WizardStep { StepId = "a" },
            new WizardStep { StepId = "skipped", IsSkipped = true },
            new WizardStep { StepId = "c" });

        await control.GoToStepAsync("skipped");

        Assert.True(await control.GoNextAsync());
        Assert.Equal("c", control.CurrentStep?.StepId);
    }

    #endregion

    #region Navigation

    [Fact]
    public async Task GoNextAsync_AdvancesTheFlow()
    {
        var control = CreateControl(new WizardStep { StepId = "a" }, new WizardStep { StepId = "b" });

        Assert.True(await control.GoNextAsync());
        Assert.Equal("b", control.CurrentStep?.StepId);
    }

    [Fact]
    public async Task GoBackAsync_OnFirstStep_ReturnsFalse()
    {
        var control = CreateControl(new WizardStep(), new WizardStep());

        Assert.False(await control.GoBackAsync());
        Assert.Equal(0, control.CurrentStepIndex);
    }

    [Fact]
    public async Task GoNextAsync_OnLastStep_ReturnsFalse()
    {
        var control = CreateControl(new WizardStep());

        Assert.False(await control.GoNextAsync());
    }

    [Fact]
    public async Task Navigating_CanCancelForwardNavigation()
    {
        var control = CreateControl(new WizardStep { StepId = "a" }, new WizardStep { StepId = "b" });
        control.Navigating += (_, e) => e.Cancel = true;

        Assert.False(await control.GoNextAsync());
        Assert.Equal("a", control.CurrentStep?.StepId);
    }

    [Fact]
    public async Task Navigating_CarriesBothEndpointsAndDirection()
    {
        var control = CreateControl(new WizardStep { StepId = "a" }, new WizardStep { StepId = "b" });

        WizardNavigatingEventArgs? captured = null;
        control.Navigating += (_, e) => captured = e;

        await control.GoNextAsync();

        Assert.NotNull(captured);
        Assert.Equal("a", captured.NavigatingFrom?.StepId);
        Assert.Equal(0, captured.NavigatingFrom?.Index);
        Assert.Equal("b", captured.NavigatingTo.StepId);
        Assert.Equal(1, captured.NavigatingTo.Index);
        Assert.Equal(WizardNavigationDirection.Next, captured.Direction);
    }

    [Fact]
    public async Task Navigating_ReportsBackDirection()
    {
        var control = CreateControl(new WizardStep { StepId = "a" }, new WizardStep { StepId = "b" });
        await control.GoNextAsync();

        WizardNavigatingEventArgs? captured = null;
        control.Navigating += (_, e) => captured = e;

        await control.GoBackAsync();

        Assert.Equal(WizardNavigationDirection.Back, captured?.Direction);
        Assert.Equal("b", captured?.NavigatingFrom?.StepId);
        Assert.Equal("a", captured?.NavigatingTo.StepId);
    }

    [Fact]
    public async Task Navigating_IsNotRaisedForOutOfRangeTargets()
    {
        var control = CreateControl(new WizardStep());
        var raised = false;
        control.Navigating += (_, _) => raised = true;

        await control.GoNextAsync();

        Assert.False(raised);
    }

    [Fact]
    public async Task StepChanged_IsRaisedAfterNavigation()
    {
        var control = CreateControl(new WizardStep { StepId = "a" }, new WizardStep { StepId = "b" });

        StepChangedEventArgs? captured = null;
        control.StepChanged += (_, e) => captured = e;

        await control.GoNextAsync();

        Assert.Equal("a", captured?.PreviousStep?.StepId);
        Assert.Equal("b", captured?.CurrentStep.StepId);
    }

    #endregion

    #region Navigation by step id

    [Fact]
    public async Task GoToStepAsync_ById_NavigatesToThatStep()
    {
        var control = CreateControl(
            new WizardStep { StepId = "a" },
            new WizardStep { StepId = "b" },
            new WizardStep { StepId = "c" });

        Assert.True(await control.GoToStepAsync("c"));
        Assert.Equal("c", control.CurrentStep?.StepId);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("")]
    public async Task GoToStepAsync_ByUnusableId_ReturnsFalse(string stepId)
    {
        var control = CreateControl(new WizardStep { StepId = "a" }, new WizardStep { StepId = "b" });

        Assert.False(await control.GoToStepAsync(stepId));
        Assert.Equal("a", control.CurrentStep?.StepId);
    }

    [Fact]
    public async Task GoToStepAsync_ById_DoesNotMatchStepsWithoutAnId()
    {
        var control = CreateControl(new WizardStep(), new WizardStep());

        Assert.False(await control.GoToStepAsync("a"));
    }

    #endregion

    #region Steps collection

    [Fact]
    public void RemovingAStep_RebuildsTheFlow()
    {
        var second = new WizardStep { StepId = "b" };
        var control = CreateControl(new WizardStep { StepId = "a" }, second);

        Assert.True(control.IsNextVisible);

        control.Steps.Remove(second);

        Assert.False(control.IsNextVisible);
        Assert.True(control.IsFinishVisible);
    }

    [Fact]
    public void AddingAStep_RebuildsTheFlow()
    {
        var control = CreateControl(new WizardStep { StepId = "a" });

        Assert.True(control.IsFinishVisible);

        control.Steps.Add(new WizardStep { StepId = "b" });

        Assert.True(control.IsNextVisible);
        Assert.False(control.IsFinishVisible);
    }

    #endregion
}
