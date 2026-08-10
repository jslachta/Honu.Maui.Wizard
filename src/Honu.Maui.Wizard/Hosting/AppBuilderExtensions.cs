using Microsoft.Maui.Hosting;

namespace Microsoft.Maui.Controls.Hosting;

public static class AppBuilderExtensions
{
    /// <summary>
    /// Registers Honu.Maui.Wizard with the application. Currently a no-op reserved as a stable
    /// integration point for future handler or service registrations.
    /// </summary>
    public static MauiAppBuilder UseHonuWizard(this MauiAppBuilder builder) => builder;
}
