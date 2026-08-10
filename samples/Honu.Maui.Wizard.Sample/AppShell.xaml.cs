using Microsoft.Maui.Controls;

namespace Honu.Maui.Wizard.Sample;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Where the wizard lands once it is finished.
        Routing.RegisterRoute("done", typeof(DonePage));
    }
}
