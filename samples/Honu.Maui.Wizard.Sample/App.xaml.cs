using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace Honu.Maui.Wizard.Sample;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
        => new Window(new AppShell());
}
