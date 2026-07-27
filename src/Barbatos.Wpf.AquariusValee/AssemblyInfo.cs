// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows.Markup;

// Maps every AquariusValee namespace onto a single XAML xmlns, the same way Aquarius/
// AquariusRouter each map their own namespaces onto one xmlns - so consumers only need one
// `xmlns:aqv="http://schemas.barbatos.co/aquariusvalee/2026/xaml"` to reach Field/Form (from
// code, not XAML, but kept consistent) and the Valee attached properties. A distinct prefix
// from Aquarius's `aq:` and AquariusRouter's `aqr:` is deliberate - all three are commonly used
// together in the same XAML file.
[assembly: XmlnsDefinition("http://schemas.barbatos.co/aquariusvalee/2026/xaml", "Barbatos.Wpf.AquariusValee.Validation")]
[assembly: XmlnsDefinition("http://schemas.barbatos.co/aquariusvalee/2026/xaml", "Barbatos.Wpf.AquariusValee.Rules")]
[assembly: XmlnsDefinition("http://schemas.barbatos.co/aquariusvalee/2026/xaml", "Barbatos.Wpf.AquariusValee.Xaml")]
[assembly: XmlnsPrefix("http://schemas.barbatos.co/aquariusvalee/2026/xaml", "aqv")]
