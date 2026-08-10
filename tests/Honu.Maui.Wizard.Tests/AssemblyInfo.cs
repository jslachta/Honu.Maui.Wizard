// MAUI's XamlLoader keeps a static, non-thread-safe cache, so two test classes constructing a
// WizardControl at the same time corrupt it. Everything here is fast; run it serially.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
