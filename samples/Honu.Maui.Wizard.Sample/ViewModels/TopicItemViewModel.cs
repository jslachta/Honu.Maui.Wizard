namespace Honu.Maui.Wizard.Sample.ViewModels;

/// <summary>
/// One checkable row of the multi-select step.
/// </summary>
public sealed class TopicItemViewModel : ObservableObject
{
    public TopicItemViewModel(string name, string description)
    {
        Name = name;
        Description = description;
    }

    #region Name (string)

    /// <summary>
    /// Label shown next to the checkbox.
    /// </summary>
    public string Name { get; }

    #endregion

    #region Description (string)

    /// <summary>
    /// One-line explanation under the label.
    /// </summary>
    public string Description { get; }

    #endregion

    #region IsSelected (bool)

    private bool _isSelected;

    /// <summary>
    /// Whether the user ticked this topic.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    #endregion
}
