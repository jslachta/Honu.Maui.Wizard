using Microsoft.Maui;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace Honu.Maui.Wizard;

/// <summary>
/// Hosts wizard step views stacked on top of each other and animates transitions between them.
/// Exactly one child is visible at a time. Usually managed by <see cref="WizardControl"/>,
/// but can be used standalone.
/// </summary>
/// <remarks>
/// This class is where the library's origin sits: Redth's post "Building a step-by-step wizard
/// control in .NET MAUI" (https://redth.codes/building-a-step-by-step-wizard-control-in-net-maui).
/// <see cref="OnChildAdded"/> and <see cref="GetCurrentIndex"/> are essentially verbatim from it,
/// and the slide transition in <see cref="GoToAsync"/> is derived from it - generalised to both
/// directions, with a configurable duration, a guard against overlapping transitions and a
/// fallback for the pre-layout case.
/// </remarks>
public class WizardFrame : Grid
{
    private bool _isAnimating;

    /// <summary>
    /// Raised after the visible step has changed.
    /// </summary>
    public event EventHandler<StepChangedEventArgs>? StepChanged;

    #region TransitionDuration (uint)

    public static readonly BindableProperty TransitionDurationProperty =
        BindableProperty.Create(
            nameof(TransitionDuration),
            typeof(uint),
            typeof(WizardFrame),
            350u);

    /// <summary>
    /// Duration of the slide transition in milliseconds. 0 disables the animation.
    /// </summary>
    public uint TransitionDuration
    {
        get => (uint)GetValue(TransitionDurationProperty);
        set => SetValue(TransitionDurationProperty, value);
    }

    #endregion

    protected override void OnChildAdded(Element child)
    {
        if (child is VisualElement ve)
        {
            // Newly added children are hidden unless nothing is visible yet;
            // then the child becomes the current step.
            ve.IsVisible = false;

            if (GetCurrentIndex() < 0)
            {
                ve.IsVisible = true;
            }
        }

        base.OnChildAdded(child);
    }

    /// <summary>
    /// Describes the step at <paramref name="index"/>, or null when the index does not point at
    /// a step (e.g. -1 when nothing is visible).
    /// </summary>
    public WizardStepInfo? GetStepInfo(int index)
    {
        if (index < 0 || index >= Children.Count)
        {
            return null;
        }

        return Children[index] is View view
            ? new WizardStepInfo(index, view)
            : null;
    }

    /// <summary>
    /// Returns the index of the currently visible child, or -1 when none is visible.
    /// </summary>
    public int GetCurrentIndex()
    {
        for (var i = 0; i < Children.Count; i++)
        {
            if (Children[i] is VisualElement { IsVisible: true })
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Makes the given child the currently visible step without animation.
    /// Falls back to the first child when <paramref name="view"/> is null or not present.
    /// </summary>
    public void SetCurrent(View? view)
    {
        View? target = null;

        if (view is not null && Children.Contains(view))
        {
            target = view;
        }
        else if (Children.Count > 0)
        {
            target = Children[0] as View;
        }

        foreach (var child in Children)
        {
            if (child is VisualElement ve)
            {
                ve.TranslationX = 0;
                ve.IsVisible = ReferenceEquals(child, target);
            }
        }
    }

    /// <summary>
    /// Navigates to the next step, if any.
    /// </summary>
    public Task<bool> ForwardAsync() => GoToAsync(GetCurrentIndex() + 1);

    /// <summary>
    /// Navigates to the previous step, if any.
    /// </summary>
    public Task<bool> BackwardAsync() => GoToAsync(GetCurrentIndex() - 1);

    /// <summary>
    /// Navigates to the step at <paramref name="targetIndex"/> with a slide animation.
    /// Returns false when the index is out of range, equals the current step,
    /// or another transition is still running.
    /// </summary>
    public async Task<bool> GoToAsync(int targetIndex)
    {
        if (_isAnimating)
        {
            return false;
        }

        if (targetIndex < 0 || targetIndex >= Children.Count)
        {
            return false;
        }

        var currentIndex = GetCurrentIndex();

        if (targetIndex == currentIndex)
        {
            return false;
        }

        var targetView = (VisualElement)Children[targetIndex];

        // Nothing visible yet - just show the target, nothing to animate against.
        if (currentIndex < 0)
        {
            targetView.TranslationX = 0;
            targetView.IsVisible = true;
            RaiseStepChanged(currentIndex, targetIndex);
            return true;
        }

        var currentView = (VisualElement)Children[currentIndex];
        var forward = targetIndex > currentIndex;
        var width = Width;

        if (width <= 0 || TransitionDuration == 0)
        {
            // Not laid out yet (Width is -1 before the first layout pass) or animation disabled:
            // switch without animating.
            targetView.TranslationX = 0;
            targetView.IsVisible = true;
            currentView.IsVisible = false;
        }
        else
        {
            targetView.TranslationX = forward ? width : -width;
            targetView.IsVisible = true;

            _isAnimating = true;
            try
            {
                await Task.WhenAll(
                    targetView.TranslateToAsync(0, 0, TransitionDuration, Easing.CubicInOut),
                    currentView.TranslateToAsync(forward ? -width : width, 0, TransitionDuration, Easing.CubicInOut));
            }
            finally
            {
                _isAnimating = false;
            }

            currentView.IsVisible = false;
            currentView.TranslationX = 0;
        }

        RaiseStepChanged(currentIndex, targetIndex);
        return true;
    }

    private void RaiseStepChanged(int previousIndex, int currentIndex)
    {
        var handler = StepChanged;

        if (handler is null)
        {
            return;
        }

        var current = GetStepInfo(currentIndex);

        if (current is null)
        {
            return;
        }

        handler(this, new StepChangedEventArgs(GetStepInfo(previousIndex), current));
    }
}
