using System;
using System.Collections.Generic;
using System.Data;

namespace PcfExporter.UI
{
    /// <summary>
    /// File pickers and message surfaces, abstracted so ViewModels stay testable.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>Returns the chosen file path or null when cancelled.</summary>
        string OpenFile(string title, string filter);
        /// <summary>Returns the chosen folder path or null when cancelled.</summary>
        string PickFolder(string title);
        void ShowInfo(string title, string message);
        /// <summary>Error dialog with copyable message text including the stack trace.</summary>
        void ShowError(string title, Exception exception);
        /// <summary>Modeless grid window (one tab per table) the user copies rows from.</summary>
        void ShowTables(string title, IReadOnlyList<DataTable> tables);
    }
}
