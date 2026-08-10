using System.ComponentModel;

using Honu.Maui.Wizard.Sample.ViewModels;
using Microsoft.Maui.Controls;

namespace Honu.Maui.Wizard.Sample;

public partial class WizardPage : ContentPage
{
    private readonly WizardViewModel _viewModel = new();

    public WizardPage()
    {
        InitializeComponent();

        BindingContext = _viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    /// <summary>
    /// Decides whether the conditional step belongs to the flow.
    /// </summary>
    /// <remarks>
    /// This cannot be a binding on <see cref="WizardStep.IsStepVisible"/>: a step outside the
    /// flow is not in the visual tree and therefore has no binding context to resolve against.
    /// </remarks>
    private void OnStepVisibilityEvaluating(object? sender, StepVisibilityEventArgs e)
    {
        if ((e.Step as WizardStep)?.StepId == StepIds.Advanced)
        {
            e.IsVisible = _viewModel.ShowAdvanced;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WizardViewModel.ShowAdvanced))
        {
            Wizard.RefreshStepVisibility();
        }
    }
}
