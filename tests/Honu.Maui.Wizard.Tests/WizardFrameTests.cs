using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace Honu.Maui.Wizard.Tests;

/// <summary>
/// The frame is exercised without a layout pass, so <see cref="VisualElement.Width"/> stays -1
/// and <see cref="WizardFrame.GoToAsync"/> takes its non-animated path. That is deliberate:
/// these tests are about the bookkeeping, not the animation.
/// </summary>
public class WizardFrameTests
{
    private static WizardFrame CreateFrame(params View[] steps)
    {
        var frame = new WizardFrame();

        foreach (var step in steps)
        {
            frame.Children.Add(step);
        }

        return frame;
    }

    [Fact]
    public void EmptyFrame_HasNoCurrentStep()
    {
        var frame = CreateFrame();

        Assert.Equal(-1, frame.GetCurrentIndex());
    }

    [Fact]
    public void FirstAddedChild_BecomesVisible()
    {
        var frame = CreateFrame(new WizardStep(), new WizardStep());

        Assert.Equal(0, frame.GetCurrentIndex());
        Assert.True(((VisualElement)frame.Children[0]).IsVisible);
        Assert.False(((VisualElement)frame.Children[1]).IsVisible);
    }

    [Fact]
    public void GetStepInfo_OutOfRange_ReturnsNull()
    {
        var frame = CreateFrame(new WizardStep());

        Assert.Null(frame.GetStepInfo(-1));
        Assert.Null(frame.GetStepInfo(1));
    }

    [Fact]
    public void GetStepInfo_CarriesIndexViewAndId()
    {
        var second = new WizardStep { StepId = "second" };
        var frame = CreateFrame(new WizardStep { StepId = "first" }, second);

        var info = frame.GetStepInfo(1);

        Assert.NotNull(info);
        Assert.Equal(1, info.Index);
        Assert.Same(second, info.Step);
        Assert.Equal("second", info.StepId);
    }

    [Fact]
    public void GetStepInfo_NonWizardStepView_HasNullId()
    {
        var frame = CreateFrame(new ContentView());

        Assert.Null(frame.GetStepInfo(0)!.StepId);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public async Task GoToAsync_OutOfRange_ReturnsFalse(int targetIndex)
    {
        var frame = CreateFrame(new WizardStep(), new WizardStep());

        Assert.False(await frame.GoToAsync(targetIndex));
    }

    [Fact]
    public async Task GoToAsync_CurrentIndex_ReturnsFalse()
    {
        var frame = CreateFrame(new WizardStep(), new WizardStep());

        Assert.False(await frame.GoToAsync(0));
    }

    [Fact]
    public async Task ForwardAsync_MovesVisibilityToNextChild()
    {
        var frame = CreateFrame(new WizardStep(), new WizardStep());

        Assert.True(await frame.ForwardAsync());

        Assert.Equal(1, frame.GetCurrentIndex());
        Assert.False(((VisualElement)frame.Children[0]).IsVisible);
        Assert.True(((VisualElement)frame.Children[1]).IsVisible);
    }

    [Fact]
    public async Task BackwardAsync_MovesVisibilityToPreviousChild()
    {
        var frame = CreateFrame(new WizardStep(), new WizardStep());
        await frame.ForwardAsync();

        Assert.True(await frame.BackwardAsync());

        Assert.Equal(0, frame.GetCurrentIndex());
    }

    [Fact]
    public async Task StepChanged_CarriesBothEndpoints()
    {
        var frame = CreateFrame(
            new WizardStep { StepId = "a" },
            new WizardStep { StepId = "b" });

        StepChangedEventArgs? captured = null;
        frame.StepChanged += (_, e) => captured = e;

        await frame.ForwardAsync();

        Assert.NotNull(captured);
        Assert.Equal("a", captured.PreviousStep?.StepId);
        Assert.Equal(0, captured.PreviousStep?.Index);
        Assert.Equal("b", captured.CurrentStep.StepId);
        Assert.Equal(1, captured.CurrentStep.Index);
    }

    [Fact]
    public void SetCurrent_UnknownView_FallsBackToFirstChild()
    {
        var frame = CreateFrame(new WizardStep(), new WizardStep());

        frame.SetCurrent(new WizardStep());

        Assert.Equal(0, frame.GetCurrentIndex());
    }

    [Fact]
    public async Task SetCurrent_KnownView_MakesItCurrent()
    {
        var second = new WizardStep();
        var frame = CreateFrame(new WizardStep(), second);
        await frame.ForwardAsync();

        frame.SetCurrent(second);

        Assert.Equal(1, frame.GetCurrentIndex());
    }
}
