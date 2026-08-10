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

    #region Conditional visibility

    [Fact]
    public void StepWithIsStepVisibleFalse_IsExcludedFromFlow()
    {
        var control = CreateControl(
            new WizardStep { StepId = "a" },
            new WizardStep { StepId = "b", IsStepVisible = false });

        // Only one step is left, so the first one is also the last.
        Assert.True(control.IsFinishVisible);
        Assert.False(control.IsNextVisible);
    }

    [Fact]
    public void StepVisibilityEvaluating_CanExcludeAStep()
    {
        var control = new WizardControl();
        control.StepVisibilityEvaluating += (_, e) =>
        {
            if ((e.Step as WizardStep)?.StepId == "b")
            {
                e.IsVisible = false;
            }
        };

        control.Steps.Add(new WizardStep { StepId = "a" });
        control.Steps.Add(new WizardStep { StepId = "b" });

        Assert.True(control.IsFinishVisible);
    }

    [Fact]
    public void StepVisibilityEvaluating_CanIncludeAnOtherwiseHiddenStep()
    {
        var control = new WizardControl();
        control.StepVisibilityEvaluating += (_, e) => e.IsVisible = true;

        control.Steps.Add(new WizardStep { StepId = "a" });
        control.Steps.Add(new WizardStep { StepId = "b", IsStepVisible = false });

        Assert.True(control.IsNextVisible);
        Assert.False(control.IsFinishVisible);
    }

    /// <summary>
    /// Changing <see cref="WizardStep.IsStepVisible"/> is not picked up on its own - the
    /// consumer has to ask for a refresh. Watching the property would mean rebuilding the flow
    /// from inside a property-changed callback, which is exactly what deadlocked the control
    /// when the change arrived during MAUI's binding-context propagation.
    /// </summary>
    [Fact]
    public void ChangingIsStepVisible_WithoutARefresh_LeavesTheFlowAlone()
    {
        var toggled = new WizardStep { StepId = "b", IsStepVisible = false };
        var control = CreateControl(new WizardStep { StepId = "a" }, toggled);

        Assert.True(control.IsFinishVisible);

        toggled.IsStepVisible = true;

        Assert.True(control.IsFinishVisible);
        Assert.False(control.IsNextVisible);
    }

    [Fact]
    public async Task RefreshStepVisibility_PicksUpAStepAddedLater()
    {
        var control = CreateControl(new WizardStep { StepId = "a" });
        var added = new WizardStep { StepId = "b", IsStepVisible = false };

        control.Steps.Add(added);
        Assert.True(control.IsFinishVisible);

        added.IsStepVisible = true;
        control.RefreshStepVisibility();

        Assert.True(await control.GoToStepAsync("b"));
    }

    [Fact]
    public void RemovedStep_NoLongerAffectsTheFlow()
    {
        var removed = new WizardStep { StepId = "b" };
        var control = CreateControl(new WizardStep { StepId = "a" }, removed);

        control.Steps.Remove(removed);
        Assert.True(control.IsFinishVisible);

        removed.IsStepVisible = false;
        control.RefreshStepVisibility();

        Assert.True(control.IsFinishVisible);
        Assert.False(control.IsNextVisible);
    }

    [Fact]
    public void RefreshStepVisibility_PicksUpChangedIsStepVisible()
    {
        var toggled = new WizardStep { StepId = "b", IsStepVisible = false };
        var control = CreateControl(new WizardStep { StepId = "a" }, toggled);

        Assert.True(control.IsFinishVisible);

        toggled.IsStepVisible = true;
        control.RefreshStepVisibility();

        Assert.True(control.IsNextVisible);
        Assert.False(control.IsFinishVisible);
    }

    [Fact]
    public async Task RefreshStepVisibility_KeepsTheCurrentStep()
    {
        var second = new WizardStep { StepId = "b" };
        var control = CreateControl(
            new WizardStep { StepId = "a" },
            second,
            new WizardStep { StepId = "c", IsStepVisible = false });

        await control.GoNextAsync();
        Assert.Equal("b", control.CurrentStep?.StepId);

        control.RefreshStepVisibility();

        Assert.Equal("b", control.CurrentStep?.StepId);
        Assert.Same(second, control.CurrentStep?.Step);
    }

    /// <summary>
    /// Regression: <c>SyncSteps</c> used to clear and refill the frame, re-parenting every step.
    /// That silently reset state living in the visual tree rather than on the view - most
    /// visibly <c>RadioButtonGroup.SelectedValue</c>, which dropped the user's choice.
    /// </summary>
    [Fact]
    public void RefreshStepVisibility_DoesNotReparentStepsThatStayInTheFlow()
    {
        var kept = new WizardStep { StepId = "kept" };
        var toggled = new WizardStep { StepId = "toggled", IsStepVisible = false };
        var control = CreateControl(kept, toggled);

        var reparentCount = 0;
        kept.ParentChanged += (_, _) => reparentCount++;

        toggled.IsStepVisible = true;
        control.RefreshStepVisibility();

        toggled.IsStepVisible = false;
        control.RefreshStepVisibility();

        Assert.Equal(0, reparentCount);
    }

    [Fact]
    public void RefreshStepVisibility_ReparentsOnlyTheStepThatEntersTheFlow()
    {
        var toggled = new WizardStep { StepId = "toggled", IsStepVisible = false };
        var control = CreateControl(new WizardStep { StepId = "kept" }, toggled);

        var reparentCount = 0;
        toggled.ParentChanged += (_, _) => reparentCount++;

        toggled.IsStepVisible = true;
        control.RefreshStepVisibility();

        Assert.Equal(1, reparentCount);
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
    public async Task GoToStepAsync_ById_IgnoresStepsOutsideTheFlow()
    {
        var control = CreateControl(
            new WizardStep { StepId = "a" },
            new WizardStep { StepId = "hidden", IsStepVisible = false });

        Assert.False(await control.GoToStepAsync("hidden"));
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
