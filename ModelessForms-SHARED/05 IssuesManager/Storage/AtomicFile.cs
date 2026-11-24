using System.IO;
using System.Text;

namespace ModelessForms.IssuesManager.Storage
{
    /// <summary>
    /// Writes a file so that a crash or a failed write never leaves a half-written
    /// target: the text goes to a temp file next to the target, which then replaces
    /// the target in one file-system operation.
    /// </summary>
    internal static class AtomicFile
    {
        /// <summary>
        /// Writes <paramref name="text"/> to <paramref name="path"/>. The folder must
        /// exist. IO errors are not caught: the caller decides how to report them.
        /// </summary>
        public static void WriteAllText(string path, string text)
        {
            string temp = path + ".tmp";
            try
            {
                File.WriteAllText(temp, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

                if (File.Exists(path))
                    File.Replace(temp, path, destinationBackupFileName: null);
                else
                    File.Move(temp, path);
            }
            finally
            {
                // Only reached with the temp file still present when the write or the
                // replace failed; remove it so no stale .tmp files accumulate.
                if (File.Exists(temp)) File.Delete(temp);
            }
        }
    }
}
