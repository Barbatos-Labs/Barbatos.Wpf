// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Barbatos.Wpf.Hosting;

public static partial class AppHostBuilderExtensions
{
    /// <summary>
    /// Adds the dialog service: registers <see cref="IDialogService"/>, which centralizes
    /// owner assignment, duplicate-open prevention, and graceful bulk-close for child windows
    /// shown through it. The feature can be configured from code via
    /// <paramref name="configure"/> and/or from the <c>Barbatos:Dialogs</c> configuration
    /// section (configuration values override code values).
    /// </summary>
    public static WpfAppBuilder ConfigureDialogs(this WpfAppBuilder builder, Action<DialogOptions>? configure = null)
    {
        var options = builder.Services.AddOptions<DialogOptions>();
        if (configure != null)
            options.Configure(configure);
        options.Bind(builder.Configuration.GetSection(DialogOptions.SectionName));

        builder.Services.TryAddSingleton<IDialogService, DialogService>();

        return builder;
    }
}
