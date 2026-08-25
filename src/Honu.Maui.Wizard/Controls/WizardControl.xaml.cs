using Microsoft.Maui.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Honu.Maui.Wizard;

/// <summary>
/// A multi-step wizard control. Steps are provided via <see cref="Steps"/>; navigation
/// buttons, step transitions and skipping are handled by the control.
/// Being a plain <see cref="View"/>, it works both in Shell and non-Shell applications.
/// </summary>
/// <remarks>
/// <see cref="Steps"/> is the wizard, in order and in full: every step keeps its place and its
/// index whatever happens. Conditional steps are expressed by <see cref="WizardStep.IsSkipped"/>,
/// which only makes <see cref="GoNextAsync"/> and <see cref="GoBackAsync"/> pass over them.
/// </remarks>
public partial class WizardControl : Grid
{
    public WizardControl()
    {
        InitializeComponent();
        StepsFrame.StepChanged += OnFrameStepChanged;
        HookStepsNotifier(Steps as INotifyCollectionChanged);
        SyncSteps();
    }

    #region Events

    /// <summary>
    /// Raised before navigating between steps; cancelable, and deferrable for handlers that
    /// need to await something first.
    /// </summary>
    public event EventHandler<WizardNavigatingEventArgs>? Navigating;

    /// <summary>
    /// Raised after the visible step has changed.
    /// </summary>
    public event EventHandler<StepChangedEventArgs>? StepChanged;

    /// <summary>
    /// Raised when the Finish button is activated, before <see cref="FinishCommand"/> executes.
    /// </summary>
    public event EventHandler? Finished;

    #endregion

    #region Properties

    #region BackText (string)

    public static readonly BindableProperty BackTextProperty =
        BindableProperty.Create(nameof(BackText), typeof(string), typeof(WizardControl), "Back");

    /// <summary>
    /// Caption of the Back button.
    /// </summary>
    public string BackText
    {
        get => (string)GetValue(BackTextProperty);
        set => SetValue(BackTextProperty, value);
    }

    #endregion

    #region NextText (string)

    public static readonly BindableProperty NextTextProperty =
        BindableProperty.Create(nameof(NextText), typeof(string), typeof(WizardControl), "Next");

    /// <summary>
    /// Caption of the Next button.
    /// </summary>
    public string NextText
    {
        get => (string)GetValue(NextTextProperty);
        set => SetValue(NextTextProperty, value);
    }

    #endregion

    #region FinishText (string)

    public static readonly BindableProperty FinishTextProperty =
        BindableProperty.Create(nameof(FinishText), typeof(string), typeof(WizardControl), "Finish");

    /// <summary>
    /// Caption of the Finish button.
    /// </summary>
    public string FinishText
    {
        get => (string)GetValue(FinishTextProperty);
        set => SetValue(FinishTextProperty, value);
    }

    #endregion

    #region NavigatingCommand (ICommand?)

    public static readonly BindableProperty NavigatingCommandProperty =
        BindableProperty.Create(nameof(NavigatingCommand), typeof(ICommand), typeof(WizardControl), default(ICommand));

    /// <summary>
    /// MVVM counterpart of the <see cref="Navigating"/> event: executed with the same
    /// <see cref="WizardNavigatingEventArgs"/> instance as its parameter, so a view model can
    /// cancel the navigation - and take a deferral to decide asynchronously - without any
    /// code-behind. Runs after the event handlers; either side setting
    /// <see cref="System.ComponentModel.CancelEventArgs.Cancel"/> blocks the navigation.
    /// </summary>
    public ICommand? NavigatingCommand
    {
        get => (ICommand?)GetValue(NavigatingCommandProperty);
        set => SetValue(NavigatingCommandProperty, value);
    }

    #endregion

    #region FinishCommand (ICommand?)

    public static readonly BindableProperty FinishCommandProperty =
        BindableProperty.Create(nameof(FinishCommand), typeof(ICommand), typeof(WizardControl), default(ICommand));

    /// <summary>
    /// Command executed when the Finish button is activated.
    /// </summary>
    public ICommand? FinishCommand
    {
        get => (ICommand?)GetValue(FinishCommandProperty);
        set => SetValue(FinishCommandProperty, value);
    }

    #endregion

    #region FinishCommandParameter (object?)

    public static readonly BindableProperty FinishCommandParameterProperty =
        BindableProperty.Create(nameof(FinishCommandParameter), typeof(object), typeof(WizardControl), null);

    /// <summary>
    /// Parameter for <see cref="FinishCommand"/>. Defaults to the control itself when null.
    /// </summary>
    public object? FinishCommandParameter
    {
        get => GetValue(FinishCommandParameterProperty);
        set => SetValue(FinishCommandParameterProperty, value);
    }

    #endregion

    #region IsFinishEnabled (bool)

    public static readonly BindableProperty IsFinishEnabledProperty =
        BindableProperty.Create(nameof(IsFinishEnabled), typeof(bool), typeof(WizardControl), true);

    /// <summary>
    /// Enables or disables the Finish button (e.g. bound to overall form validity).
    /// </summary>
    public bool IsFinishEnabled
    {
        get => (bool)GetValue(IsFinishEnabledProperty);
        set => SetValue(IsFinishEnabledProperty, value);
    }

    #endregion

    #region TransitionDuration (uint)

    public static readonly BindableProperty TransitionDurationProperty =
        BindableProperty.Create(nameof(TransitionDuration), typeof(uint), typeof(WizardControl), 350u);

    /// <summary>
    /// Duration of the slide transition in milliseconds. 0 disables the animation.
    /// </summary>
    public uint TransitionDuration
    {
        get => (uint)GetValue(TransitionDurationProperty);
        set => SetValue(TransitionDurationProperty, value);
    }

    #endregion

    #region ShowStepTitle (bool)

    public static readonly BindableProperty ShowStepTitleProperty =
        BindableProperty.Create(nameof(ShowStepTitle), typeof(bool), typeof(WizardControl), false);

    /// <summary>
    /// Shows a header with <see cref="CurrentStepTitle"/> above the steps.
    /// </summary>
    public bool ShowStepTitle
    {
        get => (bool)GetValue(ShowStepTitleProperty);
        set => SetValue(ShowStepTitleProperty, value);
    }

    #endregion

    #region IsBackVisible (bool)

    private static readonly BindablePropertyKey IsBackVisiblePropertyKey =
        BindableProperty.CreateReadOnly(nameof(IsBackVisible), typeof(bool), typeof(WizardControl), false);

    public static readonly BindableProperty IsBackVisibleProperty = IsBackVisiblePropertyKey.BindableProperty;

    /// <summary>
    /// True when some earlier step can be navigated back to.
    /// </summary>
    public bool IsBackVisible
    {
        get => (bool)GetValue(IsBackVisibleProperty);
        private set => SetValue(IsBackVisiblePropertyKey, value);
    }

    #endregion

    #region IsNextVisible (bool)

    private static readonly BindablePropertyKey IsNextVisiblePropertyKey =
        BindableProperty.CreateReadOnly(nameof(IsNextVisible), typeof(bool), typeof(WizardControl), false);

    public static readonly BindableProperty IsNextVisibleProperty = IsNextVisiblePropertyKey.BindableProperty;

    /// <summary>
    /// True when some later step can be navigated forward to.
    /// </summary>
    public bool IsNextVisible
    {
        get => (bool)GetValue(IsNextVisibleProperty);
        private set => SetValue(IsNextVisiblePropertyKey, value);
    }

    #endregion

    #region IsFinishVisible (bool)

    private static readonly BindablePropertyKey IsFinishVisiblePropertyKey =
        BindableProperty.CreateReadOnly(nameof(IsFinishVisible), typeof(bool), typeof(WizardControl), false);

    public static readonly BindableProperty IsFinishVisibleProperty = IsFinishVisiblePropertyKey.BindableProperty;

    /// <summary>
    /// True on the last step the user can reach - everything after it, if anything, is skipped.
    /// </summary>
    public bool IsFinishVisible
    {
        get => (bool)GetValue(IsFinishVisibleProperty);
        private set => SetValue(IsFinishVisiblePropertyKey, value);
    }

    #endregion

    #region IsNavigating (bool)

    private static readonly BindablePropertyKey IsNavigatingPropertyKey =
        BindableProperty.CreateReadOnly(nameof(IsNavigating), typeof(bool), typeof(WizardControl), false);

    public static readonly BindableProperty IsNavigatingProperty = IsNavigatingPropertyKey.BindableProperty;

    /// <summary>
    /// True while a navigation request is in flight, including the time spent waiting for a
    /// deferred <see cref="Navigating"/> handler. Bind a busy indicator to it; further
    /// navigation requests are ignored while it is true.
    /// </summary>
    public bool IsNavigating
    {
        get => (bool)GetValue(IsNavigatingProperty);
        private set => SetValue(IsNavigatingPropertyKey, value);
    }

    #endregion

    #region CurrentStepIndex (int)

    private static readonly BindablePropertyKey CurrentStepIndexPropertyKey =
        BindableProperty.CreateReadOnly(nameof(CurrentStepIndex), typeof(int), typeof(WizardControl), -1);

    public static readonly BindableProperty CurrentStepIndexProperty = CurrentStepIndexPropertyKey.BindableProperty;

    /// <summary>
    /// Index of the currently visible step within <see cref="Steps"/>, skipped steps included.
    /// -1 when none.
    /// </summary>
    public int CurrentStepIndex
    {
        get => (int)GetValue(CurrentStepIndexProperty);
        private set => SetValue(CurrentStepIndexPropertyKey, value);
    }

    #endregion

    #region CurrentStep (WizardStepInfo?)

    private static readonly BindablePropertyKey CurrentStepPropertyKey =
        BindableProperty.CreateReadOnly(nameof(CurrentStep), typeof(WizardStepInfo), typeof(WizardControl), null);

    public static readonly BindableProperty CurrentStepProperty = CurrentStepPropertyKey.BindableProperty;

    /// <summary>
    /// Descriptor of the currently visible step - its <see cref="WizardStepInfo.Index"/> in the
    /// step list, the <see cref="WizardStepInfo.Step"/> view and its
    /// <see cref="WizardStepInfo.StepId"/>. Null when no step is visible.
    /// </summary>
    public WizardStepInfo? CurrentStep
    {
        get => (WizardStepInfo?)GetValue(CurrentStepProperty);
        private set => SetValue(CurrentStepPropertyKey, value);
    }

    #endregion

    #region CurrentStepTitle (string?)

    private static readonly BindablePropertyKey CurrentStepTitlePropertyKey =
        BindableProperty.CreateReadOnly(nameof(CurrentStepTitle), typeof(string), typeof(WizardControl), null);

    public static readonly BindableProperty CurrentStepTitleProperty = CurrentStepTitlePropertyKey.BindableProperty;

    /// <summary>
    /// Title of the current step when it is a <see cref="WizardStep"/>, otherwise null.
    /// </summary>
    public string? CurrentStepTitle
    {
        get => (string?)GetValue(CurrentStepTitleProperty);
        private set => SetValue(CurrentStepTitlePropertyKey, value);
    }

    #endregion

    #region Steps (IList)

    private INotifyCollectionChanged? _stepsNotifier;

    public static readonly BindableProperty StepsProperty =
        BindableProperty.Create(
            nameof(Steps),
            typeof(IList),
            typeof(WizardControl),
            defaultValueCreator: _ => new ObservableCollection<View>(),
            propertyChanged: OnStepsChanged);

    /// <summary>
    /// Source collection of steps, in order. Every step counts towards the wizard's length and
    /// keeps its index; <see cref="WizardStep.IsSkipped"/> only decides which ones navigation
    /// passes over.
    /// </summary>
    public IList Steps
    {
        get => (IList)GetValue(StepsProperty);
        set => SetValue(StepsProperty, value);
    }

    private static void OnStepsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (WizardControl)bindable;
        control.HookStepsNotifier(newValue as INotifyCollectionChanged);
        control.SyncSteps();
    }

    private void HookStepsNotifier(INotifyCollectionChanged? newNotifier)
    {
        if (_stepsNotifier is not null)
        {
            _stepsNotifier.CollectionChanged -= OnStepsCollectionChanged;
        }

        _stepsNotifier = newNotifier;

        if (_stepsNotifier is not null)
        {
            _stepsNotifier.CollectionChanged += OnStepsCollectionChanged;
        }
    }

    private void OnStepsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Step collections are small; a full resync is cheaper to reason about than
        // incremental handling of inserts/moves/resets and it preserves the current step.
        SyncSteps();
    }

    #endregion

    #endregion Properties

    #region Steps

    /// <summary>
    /// Called by a step whose <see cref="WizardStep.IsSkipped"/> changed. Only the buttons
    /// depend on it - nothing moves in the visual tree, which is precisely why the property is
    /// safe to bind.
    /// </summary>
    internal void OnStepSkippedChanged()
    {
        if (StepsFrame is null)
        {
            return;
        }

        UpdateState();
    }

    private bool _isSyncingSteps;

    private void SyncSteps()
    {
        // Guard against property-changed callbacks firing before InitializeComponent.
        if (StepsFrame is null)
        {
            return;
        }

        // Mirroring mutates the frame's children, which is not something to start again from
        // inside. A sync asked for mid-sync is dropped rather than nested.
        if (_isSyncingSteps)
        {
            return;
        }

        _isSyncingSteps = true;

        try
        {
            SyncStepsCore();
        }
        finally
        {
            _isSyncingSteps = false;
        }
    }

    private void SyncStepsCore()
    {
        var currentIndex = StepsFrame.GetCurrentIndex();
        var currentView = currentIndex >= 0 ? StepsFrame.Children[currentIndex] as View : null;

        var steps = new List<View>();

        if (Steps is not null)
        {
            foreach (var item in Steps)
            {
                if (item is View view)
                {
                    steps.Add(view);
                }
            }
        }

        MirrorSteps(steps);

        // Null on the first pass, so the wizard opens on the first step whether it is skipped or
        // not: skipping governs transitions, and opening the wizard is not one.
        StepsFrame.SetCurrent(currentView);
        UpdateState();
    }

    /// <summary>
    /// Brings the frame's children in line with <paramref name="steps"/> by touching only what
    /// actually differs. Skipping plays no part here - every step is hosted, always.
    /// </summary>
    /// <remarks>
    /// Deliberately not a clear-and-refill: re-parenting a step view resets state that lives in
    /// the visual tree rather than in the view itself - <c>RadioButtonGroup.SelectedValue</c>
    /// loses its selection, focus and scroll offsets are dropped. Only a step actually added to
    /// or removed from <see cref="Steps"/> moves.
    /// </remarks>
    private void MirrorSteps(List<View> steps)
    {
        var children = StepsFrame.Children;

        for (var i = children.Count - 1; i >= 0; i--)
        {
            if (children[i] is not View view || !steps.Contains(view))
            {
                children.RemoveAt(i);
            }
        }

        for (var i = 0; i < steps.Count; i++)
        {
            var view = steps[i];
            var currentPosition = children.IndexOf(view);

            if (currentPosition < 0)
            {
                children.Insert(i, view);
            }
            else if (currentPosition != i)
            {
                // Only reached when the source collection itself was reordered.
                children.RemoveAt(currentPosition);
                children.Insert(i, view);
            }
        }
    }

    /// <summary>
    /// Index of the nearest step navigation may land on, searching from <paramref name="fromIndex"/>
    /// in <paramref name="direction"/> (+1 forward, -1 back). -1 when there is none.
    /// </summary>
    private int FindNavigableStep(int fromIndex, int direction)
    {
        var children = StepsFrame.Children;

        for (var i = fromIndex + direction; i >= 0 && i < children.Count; i += direction)
        {
            if (children[i] is View view && view is not WizardStep { IsSkipped: true })
            {
                return i;
            }
        }

        return -1;
    }

    private void UpdateState()
    {
        var index = StepsFrame.GetCurrentIndex();
        var current = StepsFrame.GetStepInfo(index);

        CurrentStepIndex = index;
        CurrentStep = current;
        CurrentStepTitle = (current?.Step as WizardStep)?.Title;

        if (index < 0)
        {
            IsBackVisible = false;
            IsNextVisible = false;
            IsFinishVisible = false;
            return;
        }

        IsBackVisible = FindNavigableStep(index, -1) >= 0;
        IsNextVisible = FindNavigableStep(index, 1) >= 0;

        // Finish belongs on the last step the user can actually get to, which is not necessarily
        // the last step in the list - everything after it may be skipped.
        IsFinishVisible = !IsNextVisible;
    }

    #endregion

    #region Navigation

    /// <summary>
    /// Navigates to the next step the user may land on, passing over any
    /// <see cref="WizardStep.IsSkipped"/> in between. Returns false when cancelled or when
    /// nothing lies ahead.
    /// </summary>
    public Task<bool> GoNextAsync()
        => GoToNavigableStepAsync(1);

    /// <summary>
    /// Navigates to the previous step the user may land on, passing over any
    /// <see cref="WizardStep.IsSkipped"/> in between. Returns false when cancelled or when
    /// nothing lies behind.
    /// </summary>
    public Task<bool> GoBackAsync()
        => GoToNavigableStepAsync(-1);

    private Task<bool> GoToNavigableStepAsync(int direction)
    {
        var targetIndex = FindNavigableStep(StepsFrame.GetCurrentIndex(), direction);

        return targetIndex < 0
            ? Task.FromResult(false)
            : GoToStepAsync(targetIndex);
    }

    /// <summary>
    /// Navigates to the step at <paramref name="targetIndex"/> in <see cref="Steps"/>.
    /// Raises the cancelable <see cref="Navigating"/> event first and, when a handler took a
    /// deferral, waits for it before acting on the outcome.
    /// </summary>
    /// <remarks>
    /// A jump is taken at its word: unlike <see cref="GoNextAsync"/> and <see cref="GoBackAsync"/>,
    /// this goes to a <see cref="WizardStep.IsSkipped"/> step too. Asking for a specific step is
    /// deliberate, so the wizard does not second-guess it.
    /// </remarks>
    public async Task<bool> GoToStepAsync(int targetIndex)
    {
        // A deferred handler can keep the wizard waiting for a while; further requests in the
        // meantime are dropped rather than queued.
        if (IsNavigating)
        {
            return false;
        }

        var fromIndex = StepsFrame.GetCurrentIndex();

        if (targetIndex == fromIndex || targetIndex < 0 || targetIndex >= StepsFrame.Children.Count)
        {
            return false;
        }

        var direction = targetIndex > fromIndex
            ? WizardNavigationDirection.Next
            : WizardNavigationDirection.Back;

        IsNavigating = true;

        try
        {
            if (await IsNavigationCancelledAsync(fromIndex, targetIndex, direction))
            {
                return false;
            }

            return await StepsFrame.GoToAsync(targetIndex);
        }
        finally
        {
            IsNavigating = false;
        }
    }

    /// <summary>
    /// Navigates to the step whose <see cref="WizardStep.StepId"/> matches <paramref name="stepId"/>
    /// (ordinal comparison). Returns false when no step carries that identifier. Skipped steps are
    /// reachable this way, for the reason given on <see cref="GoToStepAsync(int)"/>.
    /// </summary>
    public Task<bool> GoToStepAsync(string stepId)
    {
        var targetIndex = IndexOfStepId(stepId);

        return targetIndex < 0
            ? Task.FromResult(false)
            : GoToStepAsync(targetIndex);
    }

    /// <summary>
    /// Returns the index of the step with the given identifier, or -1.
    /// </summary>
    private int IndexOfStepId(string stepId)
    {
        if (string.IsNullOrEmpty(stepId))
        {
            return -1;
        }

        for (var i = 0; i < StepsFrame.Children.Count; i++)
        {
            if (StepsFrame.Children[i] is WizardStep step
                && string.Equals(step.StepId, stepId, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private async Task<bool> IsNavigationCancelledAsync(
        int fromIndex,
        int toIndex,
        WizardNavigationDirection direction)
    {
        var handler = Navigating;
        var command = NavigatingCommand;

        if (handler is null && command is null)
        {
            return false;
        }

        var target = StepsFrame.GetStepInfo(toIndex);

        if (target is null)
        {
            return false;
        }

        var args = new WizardNavigatingEventArgs(StepsFrame.GetStepInfo(fromIndex), target, direction);

        handler?.Invoke(this, args);

        if (command is not null && command.CanExecute(args))
        {
            command.Execute(args);
        }

        // No-op for handlers that decided synchronously.
        await args.WaitForDeferralsAsync();

        return args.Cancel;
    }

    private void OnFrameStepChanged(object? sender, StepChangedEventArgs e)
    {
        UpdateState();
        StepChanged?.Invoke(this, e);
    }

    #endregion

    #region Button handlers

    private async void OnBackClicked(object? sender, EventArgs e) => await GoBackAsync();

    private async void OnNextClicked(object? sender, EventArgs e) => await GoNextAsync();

    private void OnFinishClicked(object? sender, EventArgs e)
    {
        Finished?.Invoke(this, EventArgs.Empty);

        var command = FinishCommand;
        var parameter = FinishCommandParameter ?? this;

        if (command is not null && command.CanExecute(parameter))
        {
            command.Execute(parameter);
        }
    }

    #endregion
}
