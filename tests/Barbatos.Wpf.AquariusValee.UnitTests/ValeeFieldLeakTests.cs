// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace Barbatos.Wpf.AquariusValee.UnitTests;

/// <summary>
/// Proves that binding controls to a <see cref="Form"/> does not tie the View's lifetime to the
/// form's.
/// </summary>
/// <remarks>
/// <c>Valee</c> remembers the last control bound to each field so <c>Valee.Form</c> can focus it
/// when a submit fails. That lookup was a <c>ConditionalWeakTable&lt;IField, FrameworkElement&gt;</c>
/// - weak on the field, but a <c>ConditionalWeakTable</c> keeps each value alive for as long as its
/// key is, so a form owned by a singleton ViewModel held its last bound controls, and through their
/// parents the whole View, long after the View had gone. The form is rooted in the test's own frame
/// for exactly that reason: an unrooted form would be collected together with the View and the
/// test would pass with the leak still present.
/// </remarks>
public class ValeeFieldLeakTests
{
    [Fact]
    public void AViewIsCollectableWhileTheFormItWasBoundToLivesOn() =>
        StaThread.Run(() =>
        {
            var form = LongLivedForm();

            GcProbe.AssertCollected(BindView(form, keepAliveBy: null), "the View bound to a longer-lived Form");

            GC.KeepAlive(form);
        });

    [Fact]
    public void AViewSomethingStillReferencesIsDeliberatelyNotCollectable() =>
        StaThread.Run(() =>
        {
            var form = LongLivedForm();
            var holder = new List<object>();

            GcProbe.AssertAlive(BindView(form, keepAliveBy: holder), "a View the caller still holds");

            GC.KeepAlive(holder);
            GC.KeepAlive(form);
        });

    private static Form LongLivedForm()
    {
        var form = new Form();
        form.DefineField("Email", "", new RequiredAttribute());
        form.DefineField("Password", "", new RequiredAttribute());
        return form;
    }

    // Valee.Field and Valee.PasswordField are the two places that record the bound control, and
    // Valee.Form is what reads it back - all three are set, so either recording path leaking fails
    // the test.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference BindView(Form form, List<object>? keepAliveBy)
    {
        var emailBox = new TextBox();
        var passwordBox = new PasswordBox();
        Valee.SetField(emailBox, form.Fields["Email"]);
        Valee.SetPasswordField(passwordBox, (Field<string>)form.Fields["Password"]);

        var view = new StackPanel();
        Valee.SetForm(view, form);
        view.Children.Add(emailBox);
        view.Children.Add(passwordBox);

        keepAliveBy?.Add(view);
        return new WeakReference(view);
    }
}
