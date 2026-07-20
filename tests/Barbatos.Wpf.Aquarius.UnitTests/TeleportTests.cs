using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Barbatos.Wpf.Aquarius.UnitTests;

public partial class TeleportTests
{
    [Fact]
    public void ContentMovesIntoTheRegisteredHostOnceBothAreLoaded()
    {
        StaThread.Run(() =>
        {
            var hostName = $"Host-{Guid.NewGuid()}";
            var host = new Grid();
            TeleportHost.SetRegisterHost(host, hostName);

            var content = new Border();
            var teleport = new Teleport { To = hostName, Content = content };

            var window = new Window { Content = new StackPanel { Children = { host, teleport } }, Width = 200, Height = 100 };
            window.Show();
            StaThread.PumpDispatcher();

            Assert.Same(content, host.Children.OfType<Border>().SingleOrDefault());
            Assert.Null(teleport.Content);

            window.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void DisabledKeepsContentLocal()
    {
        StaThread.Run(() =>
        {
            var hostName = $"Host-{Guid.NewGuid()}";
            var host = new Grid();
            TeleportHost.SetRegisterHost(host, hostName);

            var content = new Border();
            var teleport = new Teleport { To = hostName, Disabled = true, Content = content };

            var window = new Window { Content = new StackPanel { Children = { host, teleport } }, Width = 200, Height = 100 };
            window.Show();
            StaThread.PumpDispatcher();

            Assert.Same(content, teleport.Content);
            Assert.False(host.Children.Contains(content));

            window.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void HostRegisteringAfterTeleportLoadsStillGetsPickedUp()
    {
        StaThread.Run(() =>
        {
            var hostName = $"Host-{Guid.NewGuid()}";
            var content = new Border();
            var teleport = new Teleport { To = hostName, Content = content };

            var root = new StackPanel { Children = { teleport } };
            var window = new Window { Content = root, Width = 200, Height = 100 };
            window.Show();
            StaThread.PumpDispatcher();

            // Teleport is mounted, but its host doesn't exist yet - content stays local.
            Assert.Same(content, teleport.Content);

            var host = new Grid();
            root.Children.Add(host);
            TeleportHost.SetRegisterHost(host, hostName);
            StaThread.PumpDispatcher();

            Assert.Same(content, host.Children.OfType<Border>().SingleOrDefault());
            Assert.Null(teleport.Content);

            window.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void ContentMovesBackAndForthBetweenHostsInTwoDifferentWindows()
    {
        StaThread.Run(() =>
        {
            var mainHostName = $"MainDock-{Guid.NewGuid()}";
            var dialogHostName = $"DialogDock-{Guid.NewGuid()}";

            var mainHost = new Grid();
            TeleportHost.SetRegisterHost(mainHost, mainHostName);
            var content = new TextBox { Text = "docked" };

            // The Teleport wrapper (the "source") lives in the parent window and stays
            // mounted for the whole test - a real dockable panel must follow this same
            // shape: the <aq:Teleport> element itself belongs somewhere stable (the main
            // window), never inside the transient dialog it sometimes renders into.
            var teleport = new Teleport { To = mainHostName, Content = content };
            var parentRoot = new StackPanel { Children = { mainHost, teleport } };
            var parentWindow = new Window { Content = parentRoot, Width = 200, Height = 100 };
            parentWindow.Show();
            StaThread.PumpDispatcher();

            Assert.Same(content, mainHost.Children.OfType<TextBox>().SingleOrDefault());

            // A separate "child dialog" Window with its own registered host.
            var dialogHost = new Grid();
            TeleportHost.SetRegisterHost(dialogHost, dialogHostName);
            var dialogWindow = new Window { Content = dialogHost, Width = 150, Height = 80 };
            dialogWindow.Show();
            StaThread.PumpDispatcher();

            // "Undock": retarget the same Teleport at the dialog's host.
            teleport.To = dialogHostName;
            StaThread.PumpDispatcher();

            Assert.False(mainHost.Children.Contains(content));
            Assert.Same(content, dialogHost.Children.OfType<TextBox>().SingleOrDefault());
            Assert.Equal("docked", content.Text); // same instance - state survived the move

            content.Text = "floating";

            // "Redock": retarget back at the main window's host.
            teleport.To = mainHostName;
            StaThread.PumpDispatcher();

            Assert.False(dialogHost.Children.Contains(content));
            Assert.Same(content, mainHost.Children.OfType<TextBox>().SingleOrDefault());
            Assert.Equal("floating", content.Text); // state survived the second move too

            dialogWindow.Close();
            StaThread.PumpDispatcher();
            parentWindow.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void ClosingTheDialogWhileFloatingAutomaticallyReturnsContentHome()
    {
        StaThread.Run(() =>
        {
            var mainHostName = $"MainDock-{Guid.NewGuid()}";
            var dialogHostName = $"DialogDock-{Guid.NewGuid()}";

            var mainHost = new Grid();
            TeleportHost.SetRegisterHost(mainHost, mainHostName);
            var content = new TextBox { Text = "docked" };
            var teleport = new Teleport { To = mainHostName, Content = content };
            var parentRoot = new StackPanel { Children = { mainHost, teleport } };
            var parentWindow = new Window { Content = parentRoot, Width = 200, Height = 100 };
            parentWindow.Show();
            StaThread.PumpDispatcher();

            var dialogHost = new Grid();
            TeleportHost.SetRegisterHost(dialogHost, dialogHostName);
            var dialogWindow = new Window { Content = dialogHost, Width = 150, Height = 80 };
            dialogWindow.Show();
            StaThread.PumpDispatcher();

            teleport.To = dialogHostName; // undock
            StaThread.PumpDispatcher();
            Assert.Same(content, dialogHost.Children.OfType<TextBox>().SingleOrDefault());

            // Close the dialog without first flipping Teleport.To back to the main host -
            // the app "forgot" to hand the panel home before the floating window went away.
            dialogWindow.Close();
            StaThread.PumpDispatcher();

            // TeleportHost.HostUnregistered is the safety net: content comes home onto the
            // Teleport control itself instead of being torn down with the closed dialog.
            Assert.Same(content, teleport.Content);
            Assert.False(mainHost.Children.Contains(content));
            Assert.Equal("docked", content.Text); // never recreated - same instance throughout

            // `To` itself is left pointing at the now-gone dialog host - the app can still
            // explicitly redock by reassigning it, same as any other retarget.
            teleport.To = mainHostName;
            StaThread.PumpDispatcher();
            Assert.Same(content, mainHost.Children.OfType<TextBox>().SingleOrDefault());

            parentWindow.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void ReopeningAHostUnderTheSameNameAutomaticallyReFloatsContentThere()
    {
        StaThread.Run(() =>
        {
            var mainHostName = $"MainDock-{Guid.NewGuid()}";
            var dialogHostName = $"DialogDock-{Guid.NewGuid()}";

            var mainHost = new Grid();
            TeleportHost.SetRegisterHost(mainHost, mainHostName);
            var content = new Border();
            var teleport = new Teleport { To = mainHostName, Content = content };
            var parentRoot = new StackPanel { Children = { mainHost, teleport } };
            var parentWindow = new Window { Content = parentRoot, Width = 200, Height = 100 };
            parentWindow.Show();
            StaThread.PumpDispatcher();

            var firstDialogHost = new Grid();
            TeleportHost.SetRegisterHost(firstDialogHost, dialogHostName);
            var firstDialogWindow = new Window { Content = firstDialogHost, Width = 150, Height = 80 };
            firstDialogWindow.Show();
            StaThread.PumpDispatcher();

            teleport.To = dialogHostName;
            StaThread.PumpDispatcher();
            Assert.Same(content, firstDialogHost.Children.OfType<Border>().SingleOrDefault());

            // Closed without redocking first - content comes home per the safety net.
            firstDialogWindow.Close();
            StaThread.PumpDispatcher();
            Assert.Same(content, teleport.Content);

            // Reopen "the same kind of dialog" - a fresh Window registering the same name.
            var secondDialogHost = new Grid();
            var secondDialogWindow = new Window { Content = secondDialogHost, Width = 150, Height = 80 };
            secondDialogWindow.Show();
            TeleportHost.SetRegisterHost(secondDialogHost, dialogHostName);
            StaThread.PumpDispatcher();

            // `To` was never reset, so the content automatically re-floats into the new dialog.
            Assert.Same(content, secondDialogHost.Children.OfType<Border>().SingleOrDefault());

            secondDialogWindow.Close();
            StaThread.PumpDispatcher();
            parentWindow.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void ContentBoundViaInheritedDataContextKeepsWorkingAcrossWindowsWhenBothSetIt()
    {
        StaThread.Run(() =>
        {
            var viewModel = new NotesViewModel { Notes = "typed value" };

            var mainHostName = $"MainDock-{Guid.NewGuid()}";
            var dialogHostName = $"DialogDock-{Guid.NewGuid()}";

            var mainHost = new Grid();
            TeleportHost.SetRegisterHost(mainHost, mainHostName);

            var textBox = new TextBox();
            textBox.SetBinding(TextBox.TextProperty, new Binding(nameof(NotesViewModel.Notes)));
            var teleport = new Teleport { To = mainHostName, Content = textBox };

            var parentRoot = new StackPanel { Children = { mainHost, teleport } };
            // Only the *Window* needs an explicit DataContext - everything below it inherits.
            var parentWindow = new Window { Content = parentRoot, DataContext = viewModel, Width = 200, Height = 100 };
            parentWindow.Show();
            StaThread.PumpDispatcher();

            Assert.Equal("typed value", textBox.Text);

            var dialogHost = new Grid();
            TeleportHost.SetRegisterHost(dialogHost, dialogHostName);
            var dialogWindow = new Window { Content = dialogHost, DataContext = viewModel, Width = 150, Height = 80 };
            dialogWindow.Show();
            StaThread.PumpDispatcher();

            teleport.To = dialogHostName;
            StaThread.PumpDispatcher();

            Assert.Same(textBox, dialogHost.Children.OfType<TextBox>().SingleOrDefault());
            Assert.Equal("typed value", textBox.Text); // same source object - binding still resolves

            dialogWindow.Close();
            StaThread.PumpDispatcher();
            parentWindow.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void ContentBoundViaInheritedDataContextLosesItsValueIfTheNewWindowDoesNotSetDataContext()
    {
        StaThread.Run(() =>
        {
            // The gotcha this documents: Teleport still moves the exact same TextBox
            // instance correctly (proven below), but DataContext is NOT part of what
            // travels with content - it's inherited from whichever Window the content
            // currently sits under. Forgetting to set DataContext on the second Window (an
            // easy mistake for a dockable-panel dialog) silently breaks any binding that
            // relied on inheritance, even though nothing about Teleport itself is broken.
            var viewModel = new NotesViewModel { Notes = "typed value" };

            var mainHostName = $"MainDock-{Guid.NewGuid()}";
            var dialogHostName = $"DialogDock-{Guid.NewGuid()}";

            var mainHost = new Grid();
            TeleportHost.SetRegisterHost(mainHost, mainHostName);

            var textBox = new TextBox();
            textBox.SetBinding(TextBox.TextProperty, new Binding(nameof(NotesViewModel.Notes)));
            var teleport = new Teleport { To = mainHostName, Content = textBox };

            var parentRoot = new StackPanel { Children = { mainHost, teleport } };
            var parentWindow = new Window { Content = parentRoot, DataContext = viewModel, Width = 200, Height = 100 };
            parentWindow.Show();
            StaThread.PumpDispatcher();

            Assert.Equal("typed value", textBox.Text);

            var dialogHost = new Grid();
            TeleportHost.SetRegisterHost(dialogHost, dialogHostName);
            var dialogWindow = new Window { Content = dialogHost, Width = 150, Height = 80 }; // no DataContext - the mistake
            dialogWindow.Show();
            StaThread.PumpDispatcher();

            teleport.To = dialogHostName;
            StaThread.PumpDispatcher();

            // Teleport did its job - it's still the exact same TextBox...
            Assert.Same(textBox, dialogHost.Children.OfType<TextBox>().SingleOrDefault());
            // ...but the binding lost its source and no longer reads "typed value".
            Assert.NotEqual("typed value", textBox.Text);

            dialogWindow.Close();
            StaThread.PumpDispatcher();
            parentWindow.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void UnloadingRestoresContentLocallyAndDetachesFromTheHost()
    {
        StaThread.Run(() =>
        {
            var hostName = $"Host-{Guid.NewGuid()}";
            var host = new Grid();
            TeleportHost.SetRegisterHost(host, hostName);

            var content = new Border();
            var teleport = new Teleport { To = hostName, Content = content };

            var root = new StackPanel { Children = { host, teleport } };
            var window = new Window { Content = root, Width = 200, Height = 100 };
            window.Show();
            StaThread.PumpDispatcher();

            Assert.True(host.Children.Contains(content));

            root.Children.Remove(teleport);
            StaThread.PumpDispatcher();

            Assert.False(host.Children.Contains(content));
            Assert.Same(content, teleport.Content);

            window.Close();
            StaThread.PumpDispatcher();
        });
    }

    private sealed partial class NotesViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _notes = "";
    }
}
