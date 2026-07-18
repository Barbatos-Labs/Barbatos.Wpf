// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Hosting Contributors.
// All Rights Reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Barbatos.Wpf.Hosting.Sample;

public interface IGreetingService
{
    string GetGreeting();

    string GetEnvironmentDescription();
}

/// <summary>
/// A sample service that demonstrates constructor injection of the host's
/// <see cref="IConfiguration"/> and <see cref="IHostEnvironment"/>.
/// </summary>
public class GreetingService : IGreetingService
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public GreetingService(IConfiguration configuration, IHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public string GetGreeting() =>
        _configuration["Sample:Greeting"] ?? "Hello!";

    public string GetEnvironmentDescription() =>
        $"Application: {_environment.ApplicationName} | Environment: {_environment.EnvironmentName} | Content root: {_environment.ContentRootPath}";
}
