// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

// Mirrors Barbatos.Wpf.Aquarius.UnitTests's own AssemblyInfo.cs exactly, including the reasoning -
// this attribute does not carry across assemblies, and this suite has the same shape (many
// StaThread.Run-created Windows/dispatchers) that caused a real, confirmed test-host crash there
// under xUnit's default cross-class parallelism.
[assembly: Parallelization(Mode = ParallelMode.None)]

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,
    ResourceDictionaryLocation.SourceAssembly
)]
