using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Honu.Maui.Wizard.Sample.ViewModels;

/// <summary>
/// Minimal <see cref="INotifyPropertyChanged"/> base. The sample hand-rolls this instead of
/// pulling in an MVVM package so that the only dependency on show is the wizard itself.
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
