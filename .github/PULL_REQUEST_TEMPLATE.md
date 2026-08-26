## What this changes

<!-- One or two sentences. What behaviour is different after this PR? -->

Closes #

## Why

<!-- The problem being solved, not a restatement of the diff. If a design was
     considered and dropped, say which and why - that is the part review cannot
     reconstruct from the code. -->

## Public API

- [ ] No public API change
- [ ] Additive only - new members, existing ones untouched
- [ ] **Breaking** - existing members removed, renamed or changed in meaning

If breaking, fill this in - it goes into `PackageReleaseNotes` and the README migration
table verbatim:

| Before | After | Note |
| --- | --- | --- |
|  |  |  |

## Checks

- [ ] `dotnet build Honu.Maui.Wizard.sln` passes
- [ ] `dotnet test` passes - new behaviour has a test, or the reason it cannot have one is
      stated below
- [ ] `samples/Honu.Maui.Wizard.Sample` still runs, and demonstrates the change where the
      change is something a user can see
- [ ] README and XML docs updated if the public API moved

Tests here cover flow bookkeeping - skipping, indexes, navigation, event payloads - on the
plain `net10.0` target with no emulator. Rendering, animation and layout are out of reach of
the test host; if that is what this PR touches, say how it was verified by hand and on which
platform.

## Verified on

<!-- e.g. Android 14 emulator, iOS 18 simulator, unit tests only -->
