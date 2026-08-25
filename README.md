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
bookkeeping - skipping, indexes, navigation, event payloads - and not rendering.

## Sample

`samples/Honu.Maui.Wizard.Sample` walks through one family of MAUI input controls per step,
each with a different flavour of gate:

| Step | `StepId` | Controls | Gate |
| --- | --- | --- | --- |
| Welcome | `intro` | - | none, explanatory step |
| Environment | `server` | `CollectionView` + conditional `Entry`, `Switch` | selection required; picking *Custom address* reveals a URL entry that must parse as an absolute http(s) address - then an **awaited** reachability check runs before the step may be left |
| Appearance | `appearance` | `RadioButtonGroup`, `Picker` | both must be chosen |
| Notifications | `notifications` | `Switch` ×3, `CheckBox` | consent required **only when** notifications are on |
| Advanced | `advanced` | `Slider` (with live preview), `Stepper` | **conditional step** - `IsSkipped` is bound to the switch above; cross-field rule between font size and page size |
| Topics | `topics` | `BindableLayout` + `CheckBox` | at least two selected |
| Profile | `profile` | `Entry`, `Editor`, `DatePicker`, `TimePicker` | name length, minimum text length, date must be in the past |
| Summary | `summary` | `CheckBox` | `IsFinishEnabled` keeps Finish disabled until confirmed |

Validation lives in `WizardViewModel.TryValidateStep(stepId, out error)` and is applied from
the view model's `NavigatingCommand` - forward navigation is cancelled and the reason is shown,
while Back is always allowed. Each step also renders the same message inline.

The server step goes one further and awaits a simulated reachability probe through a deferral,
with a spinner bound to `IsNavigating`. A *Simulate an unreachable server* switch is there so
the rejection path can be triggered on purpose rather than only being described.

Navigation, Finish and the conditional step are all bindings on the view model, so
`WizardPage.xaml.cs` holds nothing but `InitializeComponent()` and the binding context. The view
model keeps its own positive vocabulary (`ShowAdvanced`, driven by the switch) and exposes
`SkipAdvanced => !ShowAdvanced` for the step to bind to - the inversion belongs at the boundary,
not in the view.

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
        <honu:WizardStep StepId="details" Title="Details" IsSkipped="{Binding SkipDetails}">
            <!-- conditional step: Next and Back pass over it while the binding says so -->
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
| `CurrentStep` (`WizardStepInfo`), `CurrentStepIndex` | Read-only position within `Steps`, skipped steps included. |
| `TransitionDuration` | Slide animation length in ms; `0` disables animation. |
| `GoNextAsync()`, `GoBackAsync()` | Programmatic navigation, passing over skipped steps. |
| `GoToStepAsync(int)`, `GoToStepAsync(string)` | Deliberate jump by index or `StepId`; reaches skipped steps too. |
| `Navigating` (cancelable, deferrable) | Per-step validation hook, sync or async. |
| `NavigatingCommand` | Same hook as an `ICommand`, for view models without code-behind. |
| `IsNavigating` | True while a navigation is in flight, deferrals included. |
| `StepChanged`, `Finished` | Lifecycle events. |

### `WizardStep`

`ContentView` with `StepId`, `Title` and `IsSkipped`. `IsSkipped` says whether Next and Back pass
over the step; it is deliberately separate from `VisualElement.IsVisible`, which the wizard owns
and mutates while switching steps.

`StepId` is a stable identifier that survives reordering and conditional steps - prefer it over
indexes when reacting to navigation.

`IsSkipped` is bindable and needs nothing else - no event, no refresh call, no code-behind:

```xml
<honu:WizardStep StepId="advanced" Title="Advanced" IsSkipped="{Binding SkipAdvanced}" />
```

It is bindable precisely because **nothing moves**. Changing it recomputes which buttons show
and stops there; the step keeps its place in the visual tree, so it never loses the binding
context the flag itself is coming from. That also preserves state living in the tree rather
than on the view, such as `RadioButtonGroup.SelectedValue`, focus and scroll offsets.

A skipped step is not a hidden step:

- it keeps its index, and the wizard keeps its length - which is what makes a progress
  indicator possible, since the numbers do not move under you
- `GoToStepAsync` still goes there when asked; only `GoNextAsync` and `GoBackAsync` pass over it
- skipping the step the user is standing on leaves them there, so nothing jumps under their
  feet; they leave it on the next navigation like any other step

The wizard opens on the first step whatever its flag says - skipping governs transitions, and
opening the wizard is not one. Finish appears on the last step that can be reached, which is not
necessarily the last in the list.

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

## Versioning

The major version tracks .NET MAUI, not this library's API - `10.x` targets MAUI 10. That leaves
no major version of our own to spend, so **breaking changes land on minor releases** and are
spelled out here and in the release notes. Pin the minor if that matters to you.

### 10.0.x → 10.1.0

Conditional steps stopped being a visibility question and became a navigation one. A step is
never removed from the wizard; it is stepped over. That makes the flag bindable (nothing moves in
the visual tree, so no binding context is ever lost) and it keeps every index and the wizard's
length fixed, which is what a progress indicator needs.

| 10.0.x | 10.1.0 |
| --- | --- |
| `WizardStep.IsStepVisible` | `WizardStep.IsSkipped` — **note the inverted meaning** |
| `WizardControl.RefreshStepVisibility()` | removed - changing `IsSkipped` is enough |
| `WizardControl.StepVisibilityEvaluating` | removed - bind `IsSkipped` instead |
| `StepVisibilityEventArgs` | removed |

⚠️ **`IsStepVisible` → `IsSkipped` is a negation, not a rename.** `IsStepVisible="False"` must
become `IsSkipped="True"`. A blind find-and-replace inverts the behaviour of every conditional
step. The compiler catches the name, not the polarity - that part is on you.

⚠️ **`CurrentStepIndex` and `WizardStepInfo.Index` changed meaning silently.** They used to index
the active flow, with excluded steps closing the gap; they now index `Steps` in full, skipped
steps included. Nothing fails to compile - the numbers are simply different. This is the only
change in this release that does not announce itself, so audit anything that does arithmetic on
step indexes.

Behaviour worth knowing, all of it new rather than changed: the wizard opens on the first step
whatever its flag says; skipping the step the user is on leaves them there until they navigate;
`GoToStepAsync` reaches skipped steps while `GoNextAsync`/`GoBackAsync` pass over them; and
Finish appears on the last *reachable* step, which may not be the last in the list.

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
- Conditional steps via the bindable `WizardStep.IsSkipped`, which changes navigation only -
  indexes and wizard length stay put, so a progress indicator has stable numbers.
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
