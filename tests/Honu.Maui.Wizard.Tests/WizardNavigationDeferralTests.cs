using Microsoft.Maui.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Honu.Maui.Wizard.Tests;

public class WizardNavigationDeferralTests
{
    private static WizardControl CreateControl(params string[] stepIds)
    {
        var control = new WizardControl();

        foreach (var stepId in stepIds)
        {
            control.Steps.Add(new WizardStep { StepId = stepId });
        }

        return control;
    }

    [Fact]
    public async Task WithoutDeferral_NavigationStillResolvesSynchronously()
    {
        var control = CreateControl("a", "b");
        control.Navigating += (_, _) => { };

        Assert.True(await control.GoNextAsync());
        Assert.Equal("b", control.CurrentStep?.StepId);
    }

    [Fact]
    public async Task DeferredHandler_CanCancelAfterAwaiting()
    {
        var control = CreateControl("a", "b");
        var gate = new TaskCompletionSource();

        control.Navigating += async (_, e) =>
        {
            using var deferral = e.GetDeferral();
            await gate.Task;
            e.Cancel = true;
        };

        var navigation = control.GoNextAsync();

        // Still on the first step - the wizard is waiting for the deferral.
        Assert.Equal("a", control.CurrentStep?.StepId);

        gate.SetResult();

        Assert.False(await navigation);
        Assert.Equal("a", control.CurrentStep?.StepId);
    }

    [Fact]
    public async Task DeferredHandler_CanAllowAfterAwaiting()
    {
        var control = CreateControl("a", "b");
        var gate = new TaskCompletionSource();

        control.Navigating += async (_, e) =>
        {
            using var deferral = e.GetDeferral();
            await gate.Task;
        };

        var navigation = control.GoNextAsync();
        Assert.Equal("a", control.CurrentStep?.StepId);

        gate.SetResult();

        Assert.True(await navigation);
        Assert.Equal("b", control.CurrentStep?.StepId);
    }

    [Fact]
    public async Task Navigation_WaitsForEveryDeferral()
    {
        var control = CreateControl("a", "b");
        var first = new TaskCompletionSource();
        var second = new TaskCompletionSource();

        control.Navigating += async (_, e) =>
        {
            using var deferral = e.GetDeferral();
            await first.Task;
        };

        control.Navigating += async (_, e) =>
        {
            using var deferral = e.GetDeferral();
            await second.Task;
        };

        var navigation = control.GoNextAsync();

        first.SetResult();
        Assert.Equal("a", control.CurrentStep?.StepId);

        second.SetResult();

        Assert.True(await navigation);
    }

    [Fact]
    public async Task AnyDeferredHandlerCancelling_BlocksNavigation()
    {
        var control = CreateControl("a", "b");
        var gate = new TaskCompletionSource();

        control.Navigating += (_, _) => { };

        control.Navigating += async (_, e) =>
        {
            using var deferral = e.GetDeferral();
            await gate.Task;
            e.Cancel = true;
        };

        var navigation = control.GoNextAsync();
        gate.SetResult();

        Assert.False(await navigation);
    }

    [Fact]
    public async Task CompletingADeferralTwice_IsHarmless()
    {
        var control = CreateControl("a", "b");

        control.Navigating += (_, e) =>
        {
            var deferral = e.GetDeferral();
            deferral.Complete();
            deferral.Complete();
            deferral.Dispose();
        };

        Assert.True(await control.GoNextAsync());
    }

    [Fact]
    public async Task IsNavigating_TracksTheDeferralWindow()
    {
        var control = CreateControl("a", "b");
        var gate = new TaskCompletionSource();

        control.Navigating += async (_, e) =>
        {
            using var deferral = e.GetDeferral();
            await gate.Task;
        };

        Assert.False(control.IsNavigating);

        var navigation = control.GoNextAsync();
        Assert.True(control.IsNavigating);

        gate.SetResult();
        await navigation;

        Assert.False(control.IsNavigating);
    }

    [Fact]
    public async Task RequestsArrivingDuringADeferral_AreIgnored()
    {
        var control = CreateControl("a", "b", "c");
        var gate = new TaskCompletionSource();
        var raised = 0;

        control.Navigating += async (_, e) =>
        {
            Interlocked.Increment(ref raised);
            using var deferral = e.GetDeferral();
            await gate.Task;
        };

        var pending = control.GoNextAsync();

        // Second tap while the first is still waiting.
        Assert.False(await control.GoNextAsync());

        gate.SetResult();

        Assert.True(await pending);
        Assert.Equal(1, raised);
        Assert.Equal("b", control.CurrentStep?.StepId);
    }

    [Fact]
    public async Task NavigatingCommand_CanCancel()
    {
        var control = CreateControl("a", "b");
        control.NavigatingCommand = new Command<WizardNavigatingEventArgs>(args => args.Cancel = true);

        Assert.False(await control.GoNextAsync());
        Assert.Equal("a", control.CurrentStep?.StepId);
    }

    [Fact]
    public async Task NavigatingCommand_ReceivesTheSameArgsAsTheEvent()
    {
        var control = CreateControl("a", "b");

        WizardNavigatingEventArgs? fromEvent = null;
        WizardNavigatingEventArgs? fromCommand = null;

        control.Navigating += (_, e) => fromEvent = e;
        control.NavigatingCommand = new Command<WizardNavigatingEventArgs>(args => fromCommand = args);

        await control.GoNextAsync();

        Assert.NotNull(fromCommand);
        Assert.Same(fromEvent, fromCommand);
        Assert.Equal("a", fromCommand.NavigatingFrom?.StepId);
        Assert.Equal("b", fromCommand.NavigatingTo.StepId);
        Assert.Equal(WizardNavigationDirection.Next, fromCommand.Direction);
    }

    [Fact]
    public async Task NavigatingCommand_CanDeferAndCancelAfterAwaiting()
    {
        var control = CreateControl("a", "b");
        var gate = new TaskCompletionSource();

        control.NavigatingCommand = new Command<WizardNavigatingEventArgs>(async args =>
        {
            using var deferral = args.GetDeferral();
            await gate.Task;
            args.Cancel = true;
        });

        var navigation = control.GoNextAsync();
        Assert.Equal("a", control.CurrentStep?.StepId);

        gate.SetResult();

        Assert.False(await navigation);
    }

    [Fact]
    public async Task NavigatingCommand_CancellingAlongsideAPermissiveEventStillBlocks()
    {
        var control = CreateControl("a", "b");

        control.Navigating += (_, _) => { };
        control.NavigatingCommand = new Command<WizardNavigatingEventArgs>(args => args.Cancel = true);

        Assert.False(await control.GoNextAsync());
    }

    /// <summary>
    /// The control does not swallow exceptions from a handler or command - that would hide the
    /// consumer's bug. What it does guarantee is that it stays usable afterwards: the
    /// <see cref="WizardControl.IsNavigating"/> latch is released even when the request blows up.
    /// </summary>
    [Fact]
    public async Task NavigatingCommand_ThrowingSynchronously_PropagatesAndLeavesTheWizardUsable()
    {
        var control = CreateControl("a", "b");

        control.NavigatingCommand = new Command<WizardNavigatingEventArgs>(args =>
        {
            using var deferral = args.GetDeferral();
            throw new InvalidOperationException("boom");
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => control.GoNextAsync());

        Assert.False(control.IsNavigating);
        Assert.Equal("a", control.CurrentStep?.StepId);

        // Not latched shut - a later request goes through.
        control.NavigatingCommand = null;
        Assert.True(await control.GoNextAsync());
    }

    [Fact]
    public async Task NavigationCanBeRepeatedOnceTheDeferralSettles()
    {
        var control = CreateControl("a", "b", "c");

        control.Navigating += async (_, e) =>
        {
            using var deferral = e.GetDeferral();
            await Task.Yield();
        };

        Assert.True(await control.GoNextAsync());
        Assert.True(await control.GoNextAsync());
        Assert.Equal("c", control.CurrentStep?.StepId);
    }
}
