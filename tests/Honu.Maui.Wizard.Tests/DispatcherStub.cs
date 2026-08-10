using System;
using System.Runtime.CompilerServices;

using Microsoft.Maui.Dispatching;

namespace Honu.Maui.Wizard.Tests;

/// <summary>
/// MAUI resolves a dispatcher whenever a bindable property change has to be propagated to a
/// binding. Outside an app host there is none, so the controls throw on the first
/// <c>SetValue</c>. This stub runs everything inline on the calling thread.
/// </summary>
internal sealed class DispatcherStub : IDispatcher
{
    public bool IsDispatchRequired => false;

    public bool Dispatch(Action action)
    {
        action();
        return true;
    }

    public bool DispatchDelayed(TimeSpan delay, Action action)
    {
        action();
        return true;
    }

    public IDispatcherTimer CreateTimer() => new DispatcherTimerStub();
}

internal sealed class DispatcherTimerStub : IDispatcherTimer
{
    public TimeSpan Interval { get; set; }

    public bool IsRepeating { get; set; }

    public bool IsRunning { get; private set; }

    public event EventHandler? Tick;

    public void Start() => IsRunning = true;

    public void Stop() => IsRunning = false;

    /// <summary>
    /// Lets a test drive the timer by hand; nothing ticks on its own here.
    /// </summary>
    public void FireTick() => Tick?.Invoke(this, EventArgs.Empty);
}

internal sealed class DispatcherProviderStub : IDispatcherProvider
{
    private readonly DispatcherStub _dispatcher = new();

    public IDispatcher? GetForCurrentThread() => _dispatcher;
}

internal static class TestSetup
{
    [ModuleInitializer]
    internal static void Initialize()
        => DispatcherProvider.SetCurrent(new DispatcherProviderStub());
}
