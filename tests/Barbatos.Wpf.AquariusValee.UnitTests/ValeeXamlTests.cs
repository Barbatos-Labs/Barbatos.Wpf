using System.Windows;
using System.Windows.Controls;

namespace Barbatos.Wpf.AquariusValee.UnitTests;

public class ValeeXamlTests
{
    [Fact]
    public void ValeeFieldBindsTextBoxTextTwoWayWithLivePropertyChangedTrigger()
    {
        StaThread.Run(() =>
        {
            var field = new Field<string>("Name", "initial");
            var textBox = new TextBox();
            Valee.SetField(textBox, field);

            var window = new Window { Content = textBox };
            try
            {
                window.Show();
                StaThread.PumpDispatcher();

                Assert.Equal("initial", textBox.Text);

                textBox.Text = "typed";

                // No explicit UpdateSource() call - proving UpdateSourceTrigger is genuinely
                // PropertyChanged (live), not WPF's own TextBox.Text default of LostFocus.
                Assert.Equal("typed", field.Value);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void ValeeFieldWiresLostFocusToHandleBlur()
    {
        StaThread.Run(() =>
        {
            var field = new Field<string>("Name", "initial") { Triggers = ValidateTrigger.None };
            var textBox = new TextBox();
            Valee.SetField(textBox, field);

            var window = new Window { Content = textBox };
            try
            {
                window.Show();
                StaThread.PumpDispatcher();

                Assert.False(field.Meta.Touched);
                textBox.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent));

                Assert.True(field.Meta.Touched);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void ValeeFieldThrowsForPasswordBox()
    {
        StaThread.Run(() =>
        {
            var field = new Field<string>("Password", "");
            var passwordBox = new PasswordBox();

            Assert.Throws<InvalidOperationException>(() => Valee.SetField(passwordBox, field));
        });
    }

    [Fact]
    public void ValeePasswordFieldWiresPasswordChangedAndLostFocusWithoutCreatingABinding()
    {
        StaThread.Run(() =>
        {
            var field = new Field<string>("Password", "") { Triggers = ValidateTrigger.None };
            var passwordBox = new PasswordBox();
            Valee.SetPasswordField(passwordBox, field);

            var window = new Window { Content = passwordBox };
            try
            {
                window.Show();
                StaThread.PumpDispatcher();

                passwordBox.Password = "secret";

                Assert.Equal("secret", field.Value);

                passwordBox.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent));
                Assert.True(field.Meta.Touched);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void ValeeFormAndForResolveTheAmbientFieldViaAnAncestorWalkAtLoaded()
    {
        StaThread.Run(() =>
        {
            var form = new Form();
            var email = form.DefineField("Email", "start@example.com");

            var textBox = new TextBox();
            Valee.SetFor(textBox, "Email");

            var panel = new StackPanel();
            Valee.SetForm(panel, form);
            panel.Children.Add(textBox);

            var window = new Window { Content = panel };
            try
            {
                window.Show();
                StaThread.PumpDispatcher();

                Assert.Equal("start@example.com", textBox.Text);

                textBox.Text = "changed@example.com";
                Assert.Equal("changed@example.com", email.Value);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void ValeeForSurfacesAsADispatcherExceptionWhenNoAncestorFormIsSet()
    {
        // Mirrors RouterViewTests's identical regression test for the identical reason: Loaded
        // fires from deep inside WPF's own render pipeline, so Valee.For's own failure is
        // deferred via Dispatcher.BeginInvoke (see OnForElementLoaded's remarks) rather than
        // thrown synchronously - a plain StaThread.Run body never pumps long enough to observe
        // a BeginInvoke'd callback at all, so this specifically needs TestApplication's real,
        // continuously-running Dispatcher.Run() loop.
        Exception? observed = null;
        using var signal = new ManualResetEventSlim();

        void OnUnhandled(object? sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            observed = e.Exception;
            e.Handled = true;
            signal.Set();
        }

        TestApplication.Invoke(() =>
        {
            Application.Current.DispatcherUnhandledException += OnUnhandled;
            var textBox = new TextBox();
            Valee.SetFor(textBox, "Email");
            var window = new Window { Content = textBox };
            window.Show();
            window.Close();
        });

        try
        {
            Assert.True(signal.Wait(TimeSpan.FromSeconds(5)), "Expected the deferred failure to reach DispatcherUnhandledException.");
            Assert.IsType<InvalidOperationException>(observed);
        }
        finally
        {
            TestApplication.Invoke(() => Application.Current.DispatcherUnhandledException -= OnUnhandled);
        }
    }

    [Fact]
    public void ValeeFormFocusesTheFirstInvalidFieldsElementWhenHandleSubmitFails()
    {
        StaThread.Run(() =>
        {
            var form = new Form();
            var email = form.DefineField("Email", "", new RequiredAttribute());
            var password = form.DefineField("Password", "", new RequiredAttribute());

            var emailBox = new TextBox();
            var passwordBox = new TextBox();
            Valee.SetField(emailBox, email);
            Valee.SetField(passwordBox, password);

            var panel = new StackPanel();
            Valee.SetForm(panel, form);
            panel.Children.Add(emailBox);
            panel.Children.Add(passwordBox);

            var window = new Window { Content = panel };
            try
            {
                window.Show();
                StaThread.PumpDispatcher();

                // Both fields are empty/invalid - HandleSubmit should raise SubmitInvalid with
                // "Email" (registered first) and Valee.Form's own subscriber should focus its box.
                // Blocked on synchronously rather than awaited - this runs on a plain background
                // thread with no Dispatcher.Run() loop pumping it, same as every other headless
                // HandleSubmit test in FormTests.cs; RequiredAttribute validates synchronously so
                // there is no real async work here to deadlock against.
                form.HandleSubmit(_ => Task.CompletedTask).ExecuteAsync(null).GetAwaiter().GetResult();
                StaThread.PumpDispatcher();

                Assert.Same(emailBox, System.Windows.Input.Keyboard.FocusedElement);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void FieldsNativeINotifyDataErrorInfoImplementationDrivesWpfsOwnValidationAdorner()
    {
        StaThread.Run(() =>
        {
            var field = new Field<string>("Email", "", new RequiredAttribute());
            field.ValidateAsync().GetAwaiter().GetResult();
            Assert.NotEmpty(field.Errors);

            var textBox = new TextBox();
            Valee.SetField(textBox, field);

            var window = new Window { Content = textBox };
            try
            {
                window.Show();
                StaThread.PumpDispatcher();

                // A plain Binding to Field<T>.Value - via Valee.Field - already gets WPF's
                // native red-border adorner for free, with no converter/custom control, because
                // Field<T> implements INotifyDataErrorInfo directly and
                // Binding.ValidatesOnNotifyDataErrors defaults to true.
                Assert.True(System.Windows.Controls.Validation.GetHasError(textBox));
            }
            finally
            {
                window.Close();
            }
        });
    }
}
