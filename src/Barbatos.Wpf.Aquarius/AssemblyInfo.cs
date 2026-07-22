// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows.Markup;

// Maps every Aquarius namespace onto a single XAML xmlns, the same way WPF itself maps
// System.Windows/System.Windows.Controls/... onto one presentation xmlns - so consumers
// only need one `xmlns:aq="http://schemas.barbatos.co/aquarius/2026/xaml"` to reach Ref<T>/Computed<T>,
// Lifecycle.Enable, the Directives.* attached properties, and Teleport/TeleportHost together.
[assembly: XmlnsDefinition("http://schemas.barbatos.co/aquarius/2026/xaml", "Barbatos.Wpf.Aquarius.Reactivity")]
[assembly: XmlnsDefinition("http://schemas.barbatos.co/aquarius/2026/xaml", "Barbatos.Wpf.Aquarius.Composition")]
[assembly: XmlnsDefinition("http://schemas.barbatos.co/aquarius/2026/xaml", "Barbatos.Wpf.Aquarius.Xaml")]
[assembly: XmlnsDefinition("http://schemas.barbatos.co/aquarius/2026/xaml", "Barbatos.Wpf.Aquarius.Animation")]
[assembly: XmlnsPrefix("http://schemas.barbatos.co/aquarius/2026/xaml", "aq")]
