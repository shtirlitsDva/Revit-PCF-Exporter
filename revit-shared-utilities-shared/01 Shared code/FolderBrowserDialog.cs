using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace Shared
{
    public class FolderBrowserDialog : IDisposable
    {
        public string SelectedPath { get; set; }

        public DialogResult ShowDialog()
        {
            IntPtr pidl = IntPtr.Zero;
            SHGetFolderLocation(IntPtr.Zero, 0, IntPtr.Zero, 0, out pidl);
            try
            {
                return ShowDialog(pidl);
            }
            finally
            {
                if (pidl != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(pidl);
                }
            }
        }

        private DialogResult ShowDialog(IntPtr pidl)
        {
            IntPtr pidlBrowse = IntPtr.Zero;
            BROWSEINFO bi = new BROWSEINFO();
            bi.pidlRoot = pidl;
            bi.lpszTitle = "Select a folder";
            bi.ulFlags = BIF_RETURNONLYFSDIRS | BIF_NEWDIALOGSTYLE;

            pidlBrowse = SHBrowseForFolder(ref bi);
            if (pidlBrowse != IntPtr.Zero)
            {
                try
                {
                    if (SHGetPathFromIDList(pidlBrowse, out string selectedPath))
                    {
                        SelectedPath = selectedPath;
                        return DialogResult.OK;
                    }
                }
                finally
                {
                    Marshal.FreeCoTaskMem(pidlBrowse);
                }
            }
            return DialogResult.Cancel;
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SHBrowseForFolder(ref BROWSEINFO lpbi);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool SHGetPathFromIDList(IntPtr pidl, out string pszPath);

        [DllImport("shell32.dll")]
        private static extern int SHGetFolderLocation(IntPtr hwndOwner, int nFolder, IntPtr hToken, uint dwFlags, out IntPtr ppidl);

        private const int BIF_RETURNONLYFSDIRS = 0x0001;
        private const int BIF_NEWDIALOGSTYLE = 0x0040;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct BROWSEINFO
        {
            public IntPtr hwndOwner;
            public IntPtr pidlRoot;
            public string lpszTitle;
            public string lpfn;
            public int lParam;
            public int iImage;
            public int ulFlags;
        }
    }
}