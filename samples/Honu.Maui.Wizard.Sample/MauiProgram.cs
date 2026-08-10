using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;

namespace Honu.Maui.Wizard.Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseHonuWizard();

        return builder.Build();
    }
}
