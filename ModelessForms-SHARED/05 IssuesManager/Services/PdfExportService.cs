using System;
using System.IO;
using System.Linq;
using ModelessForms.IssuesManager.Models;

#if REVIT2025 || REVIT2026
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
#else
using iTextSharp.text;
using iTextSharp.text.pdf;
#endif

namespace ModelessForms.IssuesManager.Services
{
    public class PdfExportService
    {
        static PdfExportService()
        {
#if REVIT2025 || REVIT2026
            QuestPDF.Settings.License = LicenseType.Community;
#endif
        }

        public void ExportToPdf(Collection collection, string outputPath, string baseFolder)
        {
            if (collection == null || collection.Issues == null || collection.Issues.Count == 0)
                throw new InvalidOperationException("No issues to export.");

            var imagesFolder = Path.Combine(baseFolder, collection.Name, "images");

#if REVIT2025 || REVIT2026
            Document.Create(container =>
            {
                foreach (var issue in collection.Issues)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(20, Unit.Millimetre);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header().Column(headerCol =>
                        {
                            headerCol.Item().Text($"Collection: {collection.Name}").Bold().FontSize(14);
                            headerCol.Item().Text($"Issue: {issue.Id}").FontSize(12);
                            headerCol.Item().PaddingBottom(10).Text($"Created: {issue.Created:yyyy-MM-dd HH:mm}").FontSize(9).FontColor(Colors.Grey.Darken1);
                        });

                        page.Content().Column(col =>
                        {
                            if (issue.Screenshots != null && issue.Screenshots.Any())
                            {
                                col.Item().Text("Screenshots:").Bold().FontSize(11);
                                col.Item().PaddingBottom(5);

                                foreach (var screenshot in issue.Screenshots)
                                {
                                    var imgPath = Path.Combine(imagesFolder, screenshot);
                                    if (File.Exists(imgPath))
                                    {
                                        col.Item().PaddingBottom(5).Image(imgPath).FitWidth();
                                    }
                                }

                                col.Item().PaddingBottom(10);
                            }

                            col.Item().Text("Description:").Bold().FontSize(11);
                            col.Item().PaddingBottom(5);
                            col.Item().Text(string.IsNullOrWhiteSpace(issue.Description) ? "(No description)" : issue.Description);
                            col.Item().PaddingBottom(10);

                            if (issue.ElementGuids != null && issue.ElementGuids.Any())
                            {
                                col.Item().Text("Related Elements:").Bold().FontSize(11);
                                col.Item().PaddingBottom(5);
                                foreach (var guid in issue.ElementGuids)
                                {
                                    col.Item().Text($"• {guid}").FontSize(9);
                                }
                            }
                        });

                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                    });
                }
            }).GeneratePdf(outputPath);
#else
            using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var document = new iTextSharp.text.Document(PageSize.A4, 50, 50, 50, 50))
            {
                var writer = PdfWriter.GetInstance(document, fs);
                document.Open();

                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                var subHeaderFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                var smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.GRAY);

                bool firstIssue = true;
                foreach (var issue in collection.Issues)
                {
                    if (!firstIssue)
                        document.NewPage();
                    firstIssue = false;

                    document.Add(new Paragraph($"Collection: {collection.Name}", titleFont));
                    document.Add(new Paragraph($"Issue: {issue.Id}", headerFont));
                    document.Add(new Paragraph($"Created: {issue.Created:yyyy-MM-dd HH:mm}", smallFont));
                    document.Add(new Paragraph(" "));

                    if (issue.Screenshots != null && issue.Screenshots.Any())
                    {
                        document.Add(new Paragraph("Screenshots:", subHeaderFont));
                        document.Add(new Paragraph(" "));

                        foreach (var screenshot in issue.Screenshots)
                        {
                            var imgPath = Path.Combine(imagesFolder, screenshot);
                            if (File.Exists(imgPath))
                            {
                                try
                                {
                                    var img = iTextSharp.text.Image.GetInstance(imgPath);
                                    float maxWidth = document.PageSize.Width - document.LeftMargin - document.RightMargin;
                                    if (img.Width > maxWidth)
                                        img.ScaleToFit(maxWidth, img.Height * maxWidth / img.Width);
                                    document.Add(img);
                                    document.Add(new Paragraph(" "));
                                }
                                catch { }
                            }
                        }
                    }

                    document.Add(new Paragraph("Description:", subHeaderFont));
                    document.Add(new Paragraph(string.IsNullOrWhiteSpace(issue.Description) ? "(No description)" : issue.Description, normalFont));
                    document.Add(new Paragraph(" "));

                    if (issue.ElementGuids != null && issue.ElementGuids.Any())
                    {
                        document.Add(new Paragraph("Related Elements:", subHeaderFont));
                        foreach (var guid in issue.ElementGuids)
                        {
                            document.Add(new Paragraph($"• {guid}", smallFont));
                        }
                    }
                }

                document.Close();
            }
#endif
        }

        public bool IsSupported => true;
    }
}
