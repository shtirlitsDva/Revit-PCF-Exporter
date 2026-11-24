using System;
using System.IO;
using System.Linq;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using MigraDoc.Rendering;
using ModelessForms.IssuesManager.Model;
using ModelessForms.IssuesManager.ViewModels;
using PdfSharp.Fonts;

namespace ModelessForms.IssuesManager.Pdf
{
    /// <summary>
    /// Writes a collection as an A4 PDF (MigraDoc / PDFsharp, MIT licence): a front
    /// page, then one page per issue in number order with description, screenshots
    /// (scaled to fit the page in both directions) and related element ids.
    /// A missing or unreadable screenshot is printed as a visible line, never skipped.
    /// </summary>
    public sealed class PdfReport : IPdfReport
    {
        private static readonly Unit PageMargin = Unit.FromMillimeter(20);
        private static readonly Unit ContentWidth = Unit.FromMillimeter(210 - 2 * 20);
        private static readonly Unit MaxImageHeight = Unit.FromMillimeter(190);
        private static readonly Color MutedText = new Color(0x6B, 0x70, 0x79);
        private static readonly Color DangerText = new Color(0xB9, 0x4A, 0x4A);

        private static readonly object FontSetupLock = new object();
        private static bool _fontsReady;

        public void Export(IssueCollection collection, Func<string, string> screenshotPath, string outputPath)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            EnsureFonts();

            var document = BuildDocument(collection, screenshotPath);
            var renderer = new PdfDocumentRenderer { Document = document };
            renderer.RenderDocument();
            renderer.PdfDocument.Save(outputPath);
        }

        /// <summary>
        /// PDFsharp 6 (core build) must be told where fonts come from; on Windows it
        /// can use the installed Windows fonts. The setting is process-wide (all add-ins
        /// in the Revit process share PDFsharp 6.2.0). Behaviour of 6.2.0 (checked):
        /// writing true again is allowed; a write after another component READ the value
        /// throws InvalidOperationException; if another add-in installed its own
        /// FontResolver, the write does not throw and that resolver is used. In the last
        /// two cases another configuration stays in force (fallback F5); if it cannot
        /// supply Arial / Courier New, rendering throws and the user sees an export error.
        /// </summary>
        private static void EnsureFonts()
        {
            lock (FontSetupLock)
            {
                if (_fontsReady) return;
                try
                {
                    GlobalFontSettings.UseWindowsFontsUnderWindows = true;
                }
                catch (InvalidOperationException)
                {
                    // Already configured by another component in this process (see above).
                }
                _fontsReady = true;
            }
        }

        private static Document BuildDocument(IssueCollection collection, Func<string, string> screenshotPath)
        {
            var document = new Document();
            document.Info.Title = $"Issues – {collection.Name}";
            document.Info.Author = collection.AuthorName;
            document.Info.Subject = collection.ProjectName;

            var normal = document.Styles[StyleNames.Normal]!;
            normal.Font.Name = "Arial"; // PDFsharp Windows-font mode supports only standard families
            normal.Font.Size = 10;

            var heading = document.Styles.AddStyle("IssueHeading", StyleNames.Normal);
            heading.Font.Size = 16;
            heading.Font.Bold = true;
            heading.ParagraphFormat.SpaceAfter = Unit.FromPoint(2);

            var label = document.Styles.AddStyle("Label", StyleNames.Normal);
            label.Font.Bold = true;
            label.Font.Size = 11;
            label.ParagraphFormat.SpaceBefore = Unit.FromPoint(10);
            label.ParagraphFormat.SpaceAfter = Unit.FromPoint(3);

            var section = document.AddSection();
            section.PageSetup = document.DefaultPageSetup.Clone();
            section.PageSetup.PageFormat = PageFormat.A4;
            section.PageSetup.TopMargin = PageMargin;
            section.PageSetup.BottomMargin = PageMargin;
            section.PageSetup.LeftMargin = PageMargin;
            section.PageSetup.RightMargin = PageMargin;

            var footer = section.Footers.Primary.AddParagraph();
            footer.Format.Alignment = ParagraphAlignment.Center;
            footer.Format.Font.Size = 8;
            footer.Format.Font.Color = MutedText;
            footer.AddText($"{collection.Name}   ·   Page ");
            footer.AddPageField();
            footer.AddText(" of ");
            footer.AddNumPagesField();

            AddFrontPage(section, collection);

            foreach (var issue in collection.Issues.OrderBy(i => i.Number))
            {
                section.AddPageBreak();
                AddIssuePage(section, issue, screenshotPath);
            }

            return document;
        }

        private static void AddFrontPage(Section section, IssueCollection collection)
        {
            var spacer = section.AddParagraph();
            spacer.Format.SpaceBefore = Unit.FromMillimeter(70);

            var title = section.AddParagraph("ISSUES");
            title.Format.Alignment = ParagraphAlignment.Center;
            title.Format.Font.Size = 40;
            title.Format.Font.Bold = true;

            var name = section.AddParagraph(collection.Name);
            name.Format.Alignment = ParagraphAlignment.Center;
            name.Format.Font.Size = 22;
            name.Format.SpaceBefore = Unit.FromMillimeter(12);

            var details = section.AddParagraph();
            details.Format.Alignment = ParagraphAlignment.Center;
            details.Format.Font.Size = 12;
            details.Format.SpaceBefore = Unit.FromMillimeter(14);
            if (collection.ProjectName.Length > 0) { details.AddText("Project: " + collection.ProjectName); details.AddLineBreak(); }
            if (collection.AuthorName.Length > 0) { details.AddText("Author: " + collection.AuthorName); details.AddLineBreak(); }
            details.AddText($"Issues: {collection.Issues.Count}");
            details.AddLineBreak();
            details.AddText($"Date: {DateTime.Now:yyyy-MM-dd}");
        }

        private static void AddIssuePage(Section section, Issue issue, Func<string, string> screenshotPath)
        {
            section.AddParagraph(issue.FormatTitle(), "IssueHeading");

            var meta = section.AddParagraph($"Created {issue.Created:yyyy-MM-dd HH:mm}   ·   Id {issue.Id}");
            meta.Format.Font.Size = 8;
            meta.Format.Font.Color = MutedText;

            section.AddParagraph("Description", "Label");
            if (string.IsNullOrWhiteSpace(issue.Description))
            {
                var none = section.AddParagraph("(no description)");
                none.Format.Font.Color = MutedText;
            }
            else
            {
                var text = section.AddParagraph();
                var lines = issue.Description.Replace("\r\n", "\n").Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    if (i > 0) text.AddLineBreak();
                    text.AddText(lines[i]);
                }
            }

            if (issue.Screenshots.Count > 0)
            {
                section.AddParagraph("Screenshots", "Label");
                foreach (var file in issue.Screenshots)
                    AddScreenshot(section, screenshotPath(file), file);
            }

            if (issue.ElementUniqueIds.Count > 0)
            {
                section.AddParagraph("Related elements (Revit UniqueId)", "Label");
                foreach (var id in issue.ElementUniqueIds)
                {
                    var p = section.AddParagraph(id);
                    p.Format.Font.Size = 8;
                    p.Format.Font.Name = "Courier New";
                }
            }
        }

        private static void AddScreenshot(Section section, string path, string fileName)
        {
            if (!File.Exists(path))
            {
                AddProblemLine(section, $"Missing screenshot file: {fileName}");
                return;
            }

            if (!PngSize.TryRead(path, out int widthPx, out int heightPx))
            {
                AddProblemLine(section, $"Screenshot is not a readable PNG file: {fileName}");
                return;
            }

            // Fit inside the content width AND the maximum height, keeping the aspect
            // ratio; never enlarge beyond the screen size (96 dpi = 0.75 pt per pixel).
            double naturalWidth = widthPx * 0.75;
            double naturalHeight = heightPx * 0.75;
            double scale = Math.Min(1.0, Math.Min(ContentWidth.Point / naturalWidth, MaxImageHeight.Point / naturalHeight));

            var paragraph = section.AddParagraph();
            paragraph.Format.SpaceAfter = Unit.FromPoint(6);
            Image image = paragraph.AddImage(path);
            image.LockAspectRatio = true;
            image.Width = Unit.FromPoint(naturalWidth * scale);
            image.Height = Unit.FromPoint(naturalHeight * scale);
        }

        private static void AddProblemLine(Section section, string text)
        {
            var p = section.AddParagraph(text);
            p.Format.Font.Color = DangerText;
            p.Format.Font.Italic = true;
        }
    }

    /// <summary>Reads the pixel size from a PNG file header (the IHDR chunk).</summary>
    internal static class PngSize
    {
        private static readonly byte[] Signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        public static bool TryRead(string path, out int width, out int height)
        {
            width = height = 0;
            var header = new byte[24];
            using (var stream = File.OpenRead(path))
            {
                if (stream.Read(header, 0, header.Length) != header.Length) return false;
            }

            for (int i = 0; i < Signature.Length; i++)
                if (header[i] != Signature[i]) return false;

            width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
            height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
            return width > 0 && height > 0;
        }
    }
}
