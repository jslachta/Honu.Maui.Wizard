// One XAML namespace for the whole Honu MAUI family, not one per package: further packages
// add their own XmlnsDefinition for the same URI, so consumers keep a single "honu" prefix.
//
// The authority segment is a domain that is actually owned - a XAML namespace is only an
// identifier and nothing resolves it, but the DNS form is a claim of authority and should not
// be made over someone else's name. "honu" is the product family within it, leaving room for
// urn-free siblings such as .../honu/blazor.
//
// This string is public API: changing it silently breaks every consumer's XAML. A second URI
// can be added alongside later (non-breaking); removing this one could not be.
using Microsoft.Maui.Controls;

[assembly: XmlnsDefinition("http://schemas.slachta.eu/honu/maui", "Honu.Maui.Wizard")]
[assembly: Microsoft.Maui.Controls.XmlnsPrefix("http://schemas.slachta.eu/honu/maui", "honu")]
