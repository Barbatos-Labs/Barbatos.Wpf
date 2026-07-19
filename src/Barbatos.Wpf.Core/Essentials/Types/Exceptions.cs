// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.ApplicationModel;

/// <summary>
/// Exception that occurs when calling an API that requires a specific permission.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>PermissionException</c>.</remarks>
public class PermissionException : UnauthorizedAccessException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionException"/> class with the specified message.
    /// </summary>
    /// <param name="message">A message that describes this exception in more detail.</param>
    public PermissionException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// Exception that occurs when an attempt is made to use a feature that is not supported.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>FeatureNotSupportedException</c>.</remarks>
public class FeatureNotSupportedException : NotSupportedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureNotSupportedException"/> class.
    /// </summary>
    public FeatureNotSupportedException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureNotSupportedException"/> class with the specified message.
    /// </summary>
    /// <param name="message">A message that describes this exception in more detail.</param>
    public FeatureNotSupportedException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureNotSupportedException"/> class with the specified message and inner exception.
    /// </summary>
    /// <param name="message">A message that describes this exception in more detail.</param>
    /// <param name="innerException">An inner exception that has relation to this exception.</param>
    public FeatureNotSupportedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception that occurs when an attempt is made to use a feature that is not currently enabled.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>FeatureNotEnabledException</c>.</remarks>
public class FeatureNotEnabledException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureNotEnabledException"/> class.
    /// </summary>
    public FeatureNotEnabledException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureNotEnabledException"/> class with the specified message.
    /// </summary>
    /// <param name="message">A message that describes this exception in more detail.</param>
    public FeatureNotEnabledException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureNotEnabledException"/> class with the specified message and inner exception.
    /// </summary>
    /// <param name="message">A message that describes this exception in more detail.</param>
    /// <param name="innerException">An inner exception that has relation to this exception.</param>
    public FeatureNotEnabledException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
