// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows.Markup;

// Maps every AquariusRouter namespace onto a single XAML xmlns, the same way Aquarius itself
// maps Reactivity/Composition/Xaml/Animation onto one xmlns - so consumers only need one
// `xmlns:aqr="http://schemas.barbatos.co/aquariusrouter/2026/xaml"` to reach RouteRecord/Router,
// the guard hook interfaces, and RouterView/RouterLink together. A distinct prefix from
// Aquarius's own `aq:` is deliberate - both are commonly used together in the same XAML file.
[assembly: XmlnsDefinition("http://schemas.barbatos.co/aquariusrouter/2026/xaml", "Barbatos.Wpf.AquariusRouter.Matching")]
[assembly: XmlnsDefinition("http://schemas.barbatos.co/aquariusrouter/2026/xaml", "Barbatos.Wpf.AquariusRouter.Routing")]
[assembly: XmlnsDefinition("http://schemas.barbatos.co/aquariusrouter/2026/xaml", "Barbatos.Wpf.AquariusRouter.Xaml")]
[assembly: XmlnsPrefix("http://schemas.barbatos.co/aquariusrouter/2026/xaml", "aqr")]
