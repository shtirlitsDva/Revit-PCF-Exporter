using System;
using System.Windows;
using System.Windows.Interop;
using Shared.Theme;

namespace ModelessForms.IssuesManager.Views
{
    /// <summary>Themed message and confirmation dialog.</summary>
    public partial class MessageDialog : Window
    {
        private enum Kind { Info, Error, Confirm, Danger }

        private MessageDialog(string title, string message, Kind kind, string confirmText)
        {
            ThemeManager.Apply(this);
            InitializeComponent();
            Title = title;
            MessageText.Text = message;

            switch (kind)
            {
                case Kind.Info:
                case Kind.Error:
                    ConfirmButton.Content = "OK";
                    ConfirmButton.Style = ThemeManager.GetStyle("Button.Primary");
                    CancelButton.Visibility = Visibility.Collapsed;
                    ConfirmButton.IsCancel = true;
                    break;
                case Kind.Confirm:
                    ConfirmButton.Content = confirmText;
                    ConfirmButton.Style = ThemeManager.GetStyle("Button.Primary");
                    CancelButton.Content = "No";
                    break;
                case Kind.Danger:
                    // The safe choice is the default: Enter cancels, the destructive button needs a click.
                    ConfirmButton.Content = confirmText;
                    ConfirmButton.Style = ThemeManager.GetStyle("Button.Danger");
                    ConfirmButton.IsDefault = false;
                    CancelButton.Content = "Cancel";
                    CancelButton.IsDefault = true;
                    break;
            }
        }

        public static void ShowInfo(Window? owner, string title, string message, IntPtr revitWindow = default) =>
            DialogOwner.ShowModal(new MessageDialog(title, message, Kind.Info, "OK"), owner, revitWindow);

        public static void ShowError(Window? owner, string title, string message, IntPtr revitWindow = default) =>
            DialogOwner.ShowModal(new MessageDialog(title, message, Kind.Error, "OK"), owner, revitWindow);

        public static bool Confirm(Window? owner, string title, string message, IntPtr revitWindow = default) =>
            DialogOwner.ShowModal(new MessageDialog(title, message, Kind.Confirm, "Yes"), owner, revitWindow);

        public static bool ConfirmDanger(Window? owner, string title, string message, string confirmText, IntPtr revitWindow = default) =>
            DialogOwner.ShowModal(new MessageDialog(title, message, Kind.Danger, confirmText), owner, revitWindow);

        private void OnConfirm(object sender, RoutedEventArgs e) => DialogResult = true;

        private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
    }

    /// <summary>Shows a modal dialog over the Issue Tracker window, or over Revit when that window is not shown yet.</summary>
    internal static class DialogOwner
    {
        public static bool ShowModal(Window dialog, Window? owner, IntPtr revitWindow)
        {
            if (owner != null && owner.IsVisible)
            {
                dialog.Owner = owner;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                // CenterOwner needs a WPF owner; with a native (Revit) owner, centre on the screen.
                if (revitWindow != IntPtr.Zero) new WindowInteropHelper(dialog).Owner = revitWindow;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            return dialog.ShowDialog() == true;
        }
    }
}
