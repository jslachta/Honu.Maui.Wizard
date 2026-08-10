# Honu.Maui.Wizard

A multi-step wizard control for .NET MAUI (**.NET 10**, iOS + Android). The control is a plain
`View`, so it works identically with and without Shell; the sample in `samples/` runs it as the
app's Shell root.

Built on top of [Redth's step-by-step wizard walkthrough for .NET MAUI][redth] - see
[Credits](#credits).

[redth]: https://redth.codes/building-a-step-by-step-wizard-control-in-net-maui

## Repository layout

```
src/Honu.Maui.Wizard/            the library (net10.0 - see Target framework below)
    Controls/                    WizardControl, WizardFrame, WizardStep
    Events/                      event args, WizardStepInfo, navigation direction enum
    Hosting/                     MauiAppBuilder integration (UseHonuWizard)
    Properties/                  assembly-level XAML namespace metadata
samples/Honu.Maui.Wizard.Sample/ setup wizard as the Shell root, driven by a view model
    Models/                      ConnectionOption, WizardResult
    ViewModels/                  WizardViewModel, TopicItemViewModel 
tests/Honu.Maui.Wizard.Tests/    xunit tests for the control's flow bookkeeping
```

All public types stay in the single `Honu.Maui.Wizard` namespace regardless of folder, so the
library keeps one `xmlns` in XAML and one `using` in C#.

## Build

Requires the .NET 10 SDK with the MAUI workload (`dotnet workload install maui`).

```bash
dotnet build Honu.Maui.Wizard.sln
```

```bash
dotnet build samples/Honu.Maui.Wizard.Sample -f net10.0-android -t:Run
```

## Target framework

The package ships a single `net10.0` assembly, not one per platform. The control contains no
platform-specific code - no `#if`, no `Platforms/` folder - so `net10.0-android` and
`net10.0-ios` builds would be byte-for-byte equivalent copies of the same thing, and a
`net10.0` library is referenced happily from an app targeting any of them.

Add the platform target frameworks back if the library ever grows platform-specific code, such
as a custom handler. Doing so is additive for consumers.

## Tests

```bash
dotnet test
```

The tests run on the plain `net10.0` target - no emulator, no app host. Two things make that
work: a `DispatcherStub` installed from a module initializer (MAUI needs a dispatcher as soon
as a bindable property change reaches a binding), and the absence of a layout pass, which keeps
`Width` at -1 so `WizardFrame` takes its non-animated path. The tests therefore cover the flow
bookkeeping - step visibility, indexes, navigation, event payloads - and not rendering.

## Sample

`samples/Honu.Maui.Wizard.Sample` walks through one family of MAUI input controls per step,
each with a different flavour of gate:

| Step | `StepId` | Controls | Gate |
| --- | --- | --- | --- |
| Welcome | `intro` | - | none, explanatory step |
| Environment | `server` | `CollectionView` + conditional `Entry`, `Switch` | selection required; picking *Custom address* reveals a URL entry that must parse as an absolute http(s) address - then an **awaited** reachability check runs before the step may be left |
| Appearance | `appearance` | `RadioButtonGroup`, `Picker` | both must be chosen |
| Notifications | `notifications` | `Switch` ×3, `CheckBox` | consent required **only when** notifications are on |
| Advanced | `advanced` | `Slider` (with live preview), `Stepper` | **conditional step** - `StepVisibilityEvaluating` keys off the switch above; cross-field rule between font size and page size |
| Topics | `topics` | `BindableLayout` + `CheckBox` | at least two selected |
| Profile | `profile` | `Entry`, `Editor`, `DatePicker`, `TimePicker` | name length, minimum text length, date must be in the past |
| Summary | `summary` | `CheckBox` | `IsFinishEnabled` keeps Finish disabled until confirmed |

Validation lives in `WizardViewModel.TryValidateStep(stepId, out error)` and is applied from
the view model's `NavigatingCommand` - forward navigation is cancelled and the reason is shown,
while Back is always allowed. Each step also renders the same message inline.

The server step goes one further and awaits a simulated reachability probe through a deferral,
with a spinner bound to `IsNavigating`. A *Simulate an unreachable server* switch is there so
the rejection path can be triggered on purpose rather than only being described.

Navigation and Finish are bindings on the view model; the only thing left in
`WizardPage.xaml.cs` is the conditional step, which needs the `StepVisibilityEvaluating` event.

## Usage

```xml
xmlns:honu="http://schemas.slachta.eu/honu/maui"
<!-- or the plain CLR form: xmlns:honu="clr-namespace:Honu.Maui.Wizard;assembly=Honu.Maui.Wizard" -->

<honu:WizardControl ShowStepTitle="True"
                    NavigatingCommand="{Binding NavigatingCommand}"
                    FinishCommand="{Binding FinishCommand}">
    <honu:WizardControl.Steps>
        <honu:WizardStep StepId="welcome" Title="Welcome">
            <!-- any content -->
        </honu:WizardStep>
        <honu:WizardStep StepId="details" Title="Details" IsStepVisible="False">
            <!-- conditional step, enable via IsStepVisible + RefreshStepVisibility() -->
        </honu:WizardStep>
        <honu:WizardStep StepId="summary" Title="Summary">
            <!-- ... -->
        </honu:WizardStep>
    </honu:WizardControl.Steps>
</honu:WizardControl>
```

Note: steps must go into the `Steps` property element as shown above. `WizardControl`
derives from `Grid`, so direct children would land in the control's own layout instead.

Optionally register the library in `MauiProgram` (currently a no-op, reserved as a stable
integration point):

```csharp
builder.UseMauiApp<App>().UseHonuWizard();
```

## API overview

### `WizardControl`

| Member | Purpose |
| --- | --- |
| `Steps` (`IList`) | Source collection of steps; any `View` works, `WizardStep` adds metadata. |
| `BackText`, `NextText`, `FinishText` | Button captions (localization). |
| `FinishCommand`, `FinishCommandParameter`, `IsFinishEnabled` | Finish button wiring. |
| `ShowStepTitle`, `CurrentStepTitle` | Optional header with the current `WizardStep.Title`. |
| `CurrentStep` (`WizardStepInfo`), `CurrentStepIndex` | Read-only state of the active flow. |
| `TransitionDuration` | Slide animation length in ms; `0` disables animation. |
| `GoNextAsync()`, `GoBackAsync()` | Programmatic navigation. |
| `GoToStepAsync(int)`, `GoToStepAsync(string)` | Jump to a step by index or by `StepId`. |
| `RefreshStepVisibility()` | Rebuilds the flow after visibility conditions change. |
| `Navigating` (cancelable, deferrable) | Per-step validation hook, sync or async. |
| `NavigatingCommand` | Same hook as an `ICommand`, for view models without code-behind. |
| `IsNavigating` | True while a navigation is in flight, deferrals included. |
| `StepChanged`, `Finished`, `StepVisibilityEvaluating` | Lifecycle events. |

### `WizardStep`

`ContentView` with `StepId`, `Title` and `IsStepVisible`. `IsStepVisible` expresses "belongs to
the flow" and is deliberately separate from `VisualElement.IsVisible`, which the wizard mutates
while switching steps.

`StepId` is a stable identifier that survives reordering and conditional steps - prefer it over
indexes when reacting to navigation.

**`IsStepVisible` cannot be bound.** A step outside the flow is not in the visual tree, so it
has no binding context; the binding falls back to the property's default of `true`, which puts
the step back into the flow, where it resolves to `false` again. Drive conditional steps from
`StepVisibilityEvaluating` and call `RefreshStepVisibility()` when the condition changes:

```csharp
wizard.StepVisibilityEvaluating += (s, e) =>
{
    if ((e.Step as WizardStep)?.StepId == "advanced")
        e.IsVisible = viewModel.ShowAdvanced;
};
```

### `WizardStepInfo`

A step is described by `WizardStepInfo` wherever the control hands one out: its `Index` in the
active flow, the `Step` view itself and its `StepId`. Both navigation events use it for their
endpoints, and so does `WizardControl.CurrentStep`.

| Event | Endpoints | Null when |
| --- | --- | --- |
| `Navigating` (cancelable) | `NavigatingFrom`, `NavigatingTo` | `NavigatingFrom` - no step visible yet |
| `StepChanged` | `PreviousStep`, `CurrentStep` | `PreviousStep` - no step was visible before |

The target endpoint (`NavigatingTo` / `CurrentStep`) is never null.

```csharp
wizard.Navigating += (s, e) =>
{
    // e.NavigatingFrom?.StepId, e.NavigatingFrom?.Index
    // e.NavigatingTo.StepId,    e.NavigatingTo.Index
    if (e.Direction == WizardNavigationDirection.Next &&
        e.NavigatingFrom?.StepId == "details" && !DetailsValid)
    {
        e.Cancel = true;
    }
};

wizard.StepChanged += (s, e) => Track(e.PreviousStep?.StepId, e.CurrentStep.StepId);


// Where are we now?
var id = wizard.CurrentStep?.StepId;

// Jump straight to a step; false when no such step is in the active flow.
await wizard.GoToStepAsync("summary");
```

### Async validation

A handler that has to await something takes a **deferral**. The wizard reads `Cancel` only once
every deferral taken during the event has completed, so the step cannot be left in the meantime.

```csharp
wizard.Navigating += async (s, e) =>
{
    if (e.Direction != WizardNavigationDirection.Next)
        return;

    // Must be taken before the first await.
    using var deferral = e.GetDeferral();

    if (!await api.IsReachableAsync(serverUrl))
        e.Cancel = true;
};
```

The same hook is available as `NavigatingCommand`, which receives the very same
`WizardNavigatingEventArgs` as its parameter - so validation can live in a view model and the
page needs no code-behind:

```xml
<honu:WizardControl NavigatingCommand="{Binding NavigatingCommand}" ... />
```

```csharp
NavigatingCommand = new Command<WizardNavigatingEventArgs>(async e => await OnNavigatingAsync(e));
```

Rules worth knowing:

- `GetDeferral()` has to be called **synchronously**, before the handler's first `await` -
  afterwards the wizard has already moved on.
- Complete it in a `finally`, or take it with `using` as above. A deferral that is never
  completed stalls the navigation permanently.
- Several handlers may each take their own; navigation resumes when all are complete, and any
  one of them setting `Cancel` blocks it.
- Handlers that decide synchronously need no deferral and are unaffected.
- The event and the command both run, and either one setting `Cancel` blocks the navigation.
- Exceptions are not swallowed - they propagate to whoever asked for the navigation. The
  control only guarantees it stays usable: `IsNavigating` is released either way.
- While a navigation is pending, `IsNavigating` is true and further navigation requests are
  **ignored, not queued** - so a second tap on Next cannot stack up behind a slow check. Bind a
  busy indicator to `IsNavigating`.

### `WizardFrame`

The low-level step host (stacked children, one visible, slide transitions). Usable standalone
via `ForwardAsync()`, `BackwardAsync()`, `GoToAsync(int)`, `GetCurrentIndex()`,
`GetStepInfo(int)` and the `StepChanged` event.

## Credits

This control began as an extension of Redth's post
[Building a step-by-step wizard control in .NET MAUI][redth], which is where the idea and the
starting shape of the control come from. Thanks for writing it up.

Concretely, what came from that post lives in `WizardFrame`: the `OnChildAdded` override and
`GetCurrentIndex()` are essentially verbatim, and the slide transition in `GoToAsync` is derived
from it - generalised to both directions, with a configurable duration, a guard against
overlapping transitions and a fallback for the pre-layout case. Everything else is new.

What this library adds on top of that starting point:

- `WizardStep.StepId` - stable step identity, so validation and navigation never depend on
  indexes that shift when steps are added, removed or conditionally hidden.
- `WizardStepInfo` - every endpoint the control hands out (`Navigating`, `StepChanged`,
  `CurrentStep`) carries index, view and id together.
- Conditional steps via `WizardStep.IsStepVisible` and the `StepVisibilityEvaluating` event,
  plus `RefreshStepVisibility()` to rebuild the flow while preserving the current step.
- Cancelable `Navigating` for per-step validation, and `GoToStepAsync(string stepId)`.
- Read-only flow state (`CurrentStep`, `CurrentStepIndex`, `CurrentStepTitle`,
  `IsBackVisible` / `IsNextVisible` / `IsFinishVisible`) for binding.
- `FinishCommand` / `IsFinishEnabled`, localizable button captions, an optional step-title
  header, and an `XmlnsDefinition` so XAML gets a single `honu:` prefix. The namespace is
  deliberately family-wide rather than per-package, so further Honu MAUI packages join the same
  prefix instead of adding their own.
- NuGet packaging and a sample that exercises the above.

## License

MIT - see [LICENSE](LICENSE).
