using Honu.Maui.Wizard.Sample.Models;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;

namespace Honu.Maui.Wizard.Sample;

public partial class DonePage : ContentPage, IQueryAttributable
{
    public DonePage()
    {
        InitializeComponent();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(NavigationKeys.Result, out var value) && value is WizardResult result)
        {
            BindingContext = result;
        }
    }

    /// <summary>
    /// Blocks the Android hardware back button - the wizard is finished.
    /// </summary>
    protected override bool OnBackButtonPressed() => true;

    private void OnQuitClicked(object? sender, EventArgs e)
    {
        // No-op on iOS, where apps are not allowed to terminate themselves.
        Application.Current?.Quit();
    }
}
