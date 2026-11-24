using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ModelessForms.IssuesManager.Model;
using ModelessForms.IssuesManager.Pdf;
using ModelessForms.Tests.Theme;
using PdfSharp.Pdf.IO;
using Xunit;

namespace ModelessForms.Tests.IssuesManager
{
    public sealed class PdfReportTests : IDisposable
    {
        private readonly string _dir;

        public PdfReportTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "PdfReportTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
        }

        private string WritePng(string name, int width, int height)
        {
            var path = Path.Combine(_dir, name);
            ThemeTests.RunSta(() =>
            {
                var pixels = new byte[width * height * 4];
                for (int i = 0; i < pixels.Length; i += 4) { pixels[i] = 200; pixels[i + 3] = 255; }
                var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var stream = File.Create(path);
                encoder.Save(stream);
            });
            return path;
        }

        [Fact]
        public void Export_writes_a_pdf_with_front_page_and_one_page_per_issue()
        {
            WritePng("wide.png", 1600, 900);
            WritePng("tall.png", 300, 4000); // taller than the page: must be scaled down, not fail

            var c = new IssueCollection { Name = "Pumps", Created = DateTime.Now, ProjectName = "P", AuthorName = "A" };
            var i1 = c.AddIssue(new[] { "uid-1" }, DateTime.Now);
            i1.Description = "First line\r\nSecond line with æøå";
            i1.Screenshots.Add("wide.png");
            i1.Screenshots.Add("missing.png");
            var i2 = c.AddIssue(Array.Empty<string>(), DateTime.Now);
            i2.Screenshots.Add("tall.png");
            c.AddIssue(Array.Empty<string>(), DateTime.Now);

            var output = Path.Combine(_dir, "out.pdf");
            new PdfReport().Export(c, f => Path.Combine(_dir, f), output);

            Assert.True(File.Exists(output));
            using var pdf = PdfReader.Open(output, PdfDocumentOpenMode.Import);
            Assert.Equal(1 + 3, pdf.PageCount);
        }

        [Fact]
        public void A_file_that_is_not_png_is_reported_not_fatal()
        {
            File.WriteAllText(Path.Combine(_dir, "bad.png"), "not a png");
            var c = new IssueCollection { Name = "X", Created = DateTime.Now };
            c.AddIssue(Array.Empty<string>(), DateTime.Now).Screenshots.Add("bad.png");

            var output = Path.Combine(_dir, "out.pdf");
            new PdfReport().Export(c, f => Path.Combine(_dir, f), output);

            Assert.True(new FileInfo(output).Length > 0);
        }

        [Fact]
        public void PngSize_reads_the_header()
        {
            var path = WritePng("s.png", 123, 45);
            Assert.True(PngSize.TryRead(path, out int w, out int h));
            Assert.Equal((123, 45), (w, h));
        }
    }
}
