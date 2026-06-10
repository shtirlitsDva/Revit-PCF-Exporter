using System.Threading;
using System.Windows;

namespace PcfExporter.UI
{
    /// <summary>
    /// Clipboard writes with retry: the Win32 clipboard is routinely locked by
    /// clipboard managers/RDP, so a single attempt fails sporadically.
    /// </summary>
    public static class ClipboardText
    {
        public static bool TrySet(string text)
        {
            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    Clipboard.SetDataObject(text, true);
                    return true;
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    Thread.Sleep(50);
                }
            }
            return false;
        }
    }
}
