using Honu.Maui.Wizard.Sample.ViewModels;
using Microsoft.Maui.Controls;

namespace Honu.Maui.Wizard.Sample;

public partial class WizardPage : ContentPage
{
    public WizardPage()
    {
        InitializeComponent();

        BindingContext = new WizardViewModel();
    }
}
