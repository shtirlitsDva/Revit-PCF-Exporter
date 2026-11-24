using System;
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
            catch (Exception writeError) when (writeError is IOException || writeError is UnauthorizedAccessException)
            {
                // The write or the replace failed: remove the temp file so no stale .tmp
                // files accumulate. If that fails too, report BOTH problems; the write
                // error stays the inner exception, so it is never hidden.
                try
                {
                    if (File.Exists(temp)) File.Delete(temp);
                }
                catch (Exception deleteError) when (deleteError is IOException || deleteError is UnauthorizedAccessException)
                {
                    throw new IOException(
                        $"{writeError.Message} The temporary file could not be removed either: {temp} ({deleteError.Message})",
                        writeError);
                }
                throw;
            }
        }
    }
}
