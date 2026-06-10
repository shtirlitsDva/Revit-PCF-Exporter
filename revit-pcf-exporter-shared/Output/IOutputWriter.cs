using System.Text;

using PcfExporter.Configuration;

namespace PcfExporter.Output
{
    /// <summary>Writes a composed PCF document to disk.</summary>
    public interface IOutputWriter
    {
        void Write(string fullPath, StringBuilder content, OutputEncodingChoice encoding);
    }

    public sealed class FileOutputWriter : IOutputWriter
    {
        public void Write(string fullPath, StringBuilder content, OutputEncodingChoice encoding)
        {
            Encoding enc = encoding == OutputEncodingChoice.Utf8Bom
                ? Encoding.UTF8
                : Encoding.Default;

            using (var w = new System.IO.StreamWriter(fullPath, false, enc))
            {
                w.Write(content.ToString());
            }
        }
    }
}
