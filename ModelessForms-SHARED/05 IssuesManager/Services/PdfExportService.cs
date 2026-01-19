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
                // Front page
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20, Unit.Millimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Content().AlignCenter().AlignMiddle().Column(col =>
                    {
                        col.Item().AlignCenter().Text("ISSUES").Bold().FontSize(48);
                        col.Item().PaddingTop(40);
                        col.Item().AlignCenter().Text(collection.Name).FontSize(24);
                        col.Item().PaddingTop(30);
                        if (!string.IsNullOrWhiteSpace(collection.ProjectName))
                        {
                            col.Item().AlignCenter().Text($"Project: {collection.ProjectName}").FontSize(14);
                            col.Item().PaddingTop(10);
                        }
                        if (!string.IsNullOrWhiteSpace(collection.AuthorName))
                        {
                            col.Item().AlignCenter().Text($"Author: {collection.AuthorName}").FontSize(14);
                            col.Item().PaddingTop(10);
                        }
                        col.Item().PaddingTop(20);
                        col.Item().AlignCenter().Text($"Date: {DateTime.Now:yyyy-MM-dd}").FontSize(14);
                    });
                });

                // Issue pages
                foreach (var issue in collection.Issues)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(20, Unit.Millimetre);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header().Column(headerCol =>
                        {
                            headerCol.Item().Text($"Issue: {issue.Id}").Bold().FontSize(14);
                            headerCol.Item().PaddingBottom(10).Text($"Created: {issue.Created:yyyy-MM-dd HH:mm}").FontSize(9).FontColor(Colors.Grey.Darken1);
                        });

                        page.Content().Column(col =>
                        {
                            // Description first
                            col.Item().Text("Description:").Bold().FontSize(11);
                            col.Item().PaddingBottom(5);
                            col.Item().Text(string.IsNullOrWhiteSpace(issue.Description) ? "(No description)" : issue.Description);
                            col.Item().PaddingBottom(10);

                            // Screenshots second
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

                var bigTitleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 48);
                var collectionNameFont = FontFactory.GetFont(FontFactory.HELVETICA, 24);
                var frontPageFont = FontFactory.GetFont(FontFactory.HELVETICA, 14);
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
                var subHeaderFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                var smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.GRAY);

                // Front page
                var frontTable = new PdfPTable(1);
                frontTable.WidthPercentage = 100;
                frontTable.DefaultCell.Border = Rectangle.NO_BORDER;
                frontTable.DefaultCell.HorizontalAlignment = Element.ALIGN_CENTER;
                frontTable.DefaultCell.VerticalAlignment = Element.ALIGN_MIDDLE;

                // Spacer to push content toward center
                var spacerCell = new PdfPCell(new Phrase(" ")) { Border = Rectangle.NO_BORDER, FixedHeight = 200 };
                frontTable.AddCell(spacerCell);

                var issuesCell = new PdfPCell(new Phrase("ISSUES", bigTitleFont)) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_CENTER };
                frontTable.AddCell(issuesCell);

                frontTable.AddCell(new PdfPCell(new Phrase(" ")) { Border = Rectangle.NO_BORDER, FixedHeight = 40 });

                var collectionCell = new PdfPCell(new Phrase(collection.Name, collectionNameFont)) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_CENTER };
                frontTable.AddCell(collectionCell);

                frontTable.AddCell(new PdfPCell(new Phrase(" ")) { Border = Rectangle.NO_BORDER, FixedHeight = 30 });

                if (!string.IsNullOrWhiteSpace(collection.ProjectName))
                {
                    var projectCell = new PdfPCell(new Phrase($"Project: {collection.ProjectName}", frontPageFont)) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_CENTER };
                    frontTable.AddCell(projectCell);
                    frontTable.AddCell(new PdfPCell(new Phrase(" ")) { Border = Rectangle.NO_BORDER, FixedHeight = 10 });
                }

                if (!string.IsNullOrWhiteSpace(collection.AuthorName))
                {
                    var authorCell = new PdfPCell(new Phrase($"Author: {collection.AuthorName}", frontPageFont)) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_CENTER };
                    frontTable.AddCell(authorCell);
                    frontTable.AddCell(new PdfPCell(new Phrase(" ")) { Border = Rectangle.NO_BORDER, FixedHeight = 10 });
                }

                frontTable.AddCell(new PdfPCell(new Phrase(" ")) { Border = Rectangle.NO_BORDER, FixedHeight = 20 });

                var dateCell = new PdfPCell(new Phrase($"Date: {DateTime.Now:yyyy-MM-dd}", frontPageFont)) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_CENTER };
                frontTable.AddCell(dateCell);

                document.Add(frontTable);

                // Issue pages
                foreach (var issue in collection.Issues)
                {
                    document.NewPage();

                    document.Add(new Paragraph($"Issue: {issue.Id}", titleFont));
                    document.Add(new Paragraph($"Created: {issue.Created:yyyy-MM-dd HH:mm}", smallFont));
                    document.Add(new Paragraph(" "));

                    // Description first
                    document.Add(new Paragraph("Description:", subHeaderFont));
                    document.Add(new Paragraph(string.IsNullOrWhiteSpace(issue.Description) ? "(No description)" : issue.Description, normalFont));
                    document.Add(new Paragraph(" "));

                    // Screenshots second
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
