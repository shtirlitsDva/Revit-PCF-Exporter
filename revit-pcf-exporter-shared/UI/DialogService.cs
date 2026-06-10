using System;
using System.Windows;

namespace PcfExporter.UI
{
    public sealed class DialogService : IDialogService
    {
        private readonly Func<Window> _ownerProvider;

        public DialogService(Func<Window> ownerProvider) => _ownerProvider = ownerProvider;

        public string OpenFile(string title, string filter)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = title,
                Filter = filter,
                CheckFileExists = true
            };
            return dialog.ShowDialog(_ownerProvider()) == true ? dialog.FileName : null;
        }

        public string PickFolder(string title)
        {
#if REVIT2022 || REVIT2024
            //.NET Framework WPF has no folder picker; WinForms' is the standard fallback.
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = title,
                ShowNewFolderButton = true
            })
            {
                var owner = new Win32Owner(
                    new System.Windows.Interop.WindowInteropHelper(_ownerProvider()).Handle);
                return dialog.ShowDialog(owner) == System.Windows.Forms.DialogResult.OK
                    ? dialog.SelectedPath : null;
            }
#else
            var dialog = new Microsoft.Win32.OpenFolderDialog { Title = title };
            return dialog.ShowDialog(_ownerProvider()) == true ? dialog.FolderName : null;
#endif
        }

#if REVIT2022 || REVIT2024
        /// <summary>WPF window handle as a WinForms dialog owner.</summary>
        private sealed class Win32Owner : System.Windows.Forms.IWin32Window
        {
            public Win32Owner(IntPtr handle) => Handle = handle;
            public IntPtr Handle { get; }
        }
#endif

        public void ShowInfo(string title, string message)
        {
            var window = new Views.MessageWindow(title, message, isError: false)
            {
                Owner = _ownerProvider()
            };
            window.ShowDialog();
        }

        public void ShowTables(string title, System.Collections.Generic.IReadOnlyList<System.Data.DataTable> tables)
        {
            //Modeless on purpose: the user arranges this window next to Excel and
            //copies rows over — the same workflow the live COM Excel used to serve.
            var window = new Views.TableWindow(title, tables)
            {
                Owner = _ownerProvider()
            };
            window.Show();
        }

        public void ShowError(string title, Exception exception)
        {
            //Full ToString on purpose: the user must be able to read AND copy
            //the exception text including the stack trace.
            var window = new Views.MessageWindow(title, exception.ToString(), isError: true)
            {
                Owner = _ownerProvider()
            };
            window.ShowDialog();
        }
    }
}
