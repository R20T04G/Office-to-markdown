using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using System.Buffers.Binary;
using System.Text;
using UglyToad.PdfPig;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// ==========================================
// DOCUMENT CONVERSION ENDPOINT
// ==========================================
app.MapPost("/api/convert", async (IFormFile? file) =>
{
    if (file == null || file.Length == 0)
    {
        return Results.BadRequest("No file was uploaded.");
    }

    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

    await using var uploadedFile = new MemoryStream();
    await file.CopyToAsync(uploadedFile);
    var fileBytes = uploadedFile.ToArray();

    return extension switch
    {
        ".docx" => ConvertWordDocument(new MemoryStream(fileBytes), file.FileName),
        ".xlsx" => ConvertSpreadsheet(new MemoryStream(fileBytes), file.FileName),
        ".pptx" => ConvertPresentation(new MemoryStream(fileBytes), file.FileName),
        ".pdf" => ConvertPdf(new MemoryStream(fileBytes), file.FileName),
        _ => Results.BadRequest("Supported formats are .docx, .xlsx, .pptx, and .pdf.")
    };
})
.DisableAntiforgery();

app.Run();

static IResult ConvertWordDocument(Stream stream, string fileName)
{
    try
    {
        using var wordDocument = WordprocessingDocument.Open(stream, false);
        var body = wordDocument.MainDocumentPart?.Document.Body;

        if (body == null)
        {
            return Results.BadRequest("Could not read document body.");
        }

        var markdownBuilder = new StringBuilder();
        AppendConversionHeader(markdownBuilder, fileName, "Word document", new[]
        {
            "- Conversion mode: paragraph extraction",
            "- Output format: Markdown",
            "- AI-friendly structure: converted headings and paragraphs into readable text"
        });

        foreach (var paragraph in body.Elements<Paragraph>())
        {
            var text = paragraph.InnerText;
            if (!string.IsNullOrWhiteSpace(text))
            {
                markdownBuilder.AppendLine(text);
                markdownBuilder.AppendLine();
            }
        }

        var markdown = markdownBuilder.ToString().TrimEnd();

        return Results.Ok(new
        {
            message = "Conversion successful!",
            markdown
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error processing document: {ex.Message}");
    }
}

static IResult ConvertSpreadsheet(Stream stream, string fileName)
{
    try
    {
        using var spreadsheetDocument = SpreadsheetDocument.Open(stream, false);
        var workbookPart = spreadsheetDocument.WorkbookPart;

        if (workbookPart?.Workbook?.Sheets == null)
        {
            return Results.BadRequest("Could not read workbook sheets.");
        }

        var markdownBuilder = new StringBuilder();
        AppendConversionHeader(markdownBuilder, fileName, "Excel workbook", new[]
        {
            "- Conversion mode: worksheet table extraction",
            "- Output format: Markdown",
            "- AI-friendly structure: each sheet is converted into a markdown table"
        });

        var sharedStringTable = workbookPart.SharedStringTablePart?.SharedStringTable;
        var sheetCount = 0;

        foreach (var sheet in workbookPart.Workbook.Sheets.OfType<Sheet>())
        {
            var relationshipId = sheet.Id?.Value;
            if (string.IsNullOrWhiteSpace(relationshipId))
            {
                continue;
            }

            sheetCount++;
            var worksheetPart = (WorksheetPart)workbookPart.GetPartById(relationshipId);
            var sheetRows = ExtractWorksheetRows(worksheetPart, sharedStringTable);

            markdownBuilder.AppendLine($"## Sheet {sheetCount}: {sheet.Name}");
            markdownBuilder.AppendLine();

            if (sheetRows.Count == 0)
            {
                markdownBuilder.AppendLine("_No readable rows were found in this sheet._");
                markdownBuilder.AppendLine();
                continue;
            }

            markdownBuilder.AppendLine(BuildMarkdownTable(sheetRows));
            markdownBuilder.AppendLine();
        }

        if (sheetCount == 0)
        {
            return Results.BadRequest("The workbook did not contain any readable sheets.");
        }

        var markdown = markdownBuilder.ToString().TrimEnd();

        return Results.Ok(new
        {
            message = "Conversion successful!",
            markdown
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error processing spreadsheet: {ex.Message}");
    }
}

static IResult ConvertPresentation(Stream stream, string fileName)
{
    try
    {
        using var presentationDocument = PresentationDocument.Open(stream, false);
        var presentationPart = presentationDocument.PresentationPart;
        var slideIdList = presentationPart?.Presentation?.SlideIdList;

        if (presentationPart == null || slideIdList == null)
        {
            return Results.BadRequest("Could not read presentation slides.");
        }

        var markdownBuilder = new StringBuilder();
        AppendConversionHeader(markdownBuilder, fileName, "PowerPoint presentation", new[]
        {
            "- Conversion mode: slide text extraction",
            "- Output format: Markdown",
            "- AI-friendly structure: each slide becomes a titled bullet list"
        });

        var slideNumber = 0;
        foreach (var slideId in slideIdList.Elements<SlideId>())
        {
            var relationshipId = slideId.RelationshipId?.Value;
            if (string.IsNullOrWhiteSpace(relationshipId))
            {
                continue;
            }

            slideNumber++;
            var slidePart = (SlidePart)presentationPart.GetPartById(relationshipId);
            var slideText = ExtractSlideText(slidePart);

            markdownBuilder.AppendLine($"## Slide {slideNumber}: {(slideText.FirstOrDefault() ?? "Untitled Slide")}");
            markdownBuilder.AppendLine();

            var slideContent = slideText.Skip(1).ToList();
            if (slideContent.Count == 0)
            {
                markdownBuilder.AppendLine("_No readable text was found on this slide._");
                markdownBuilder.AppendLine();
                continue;
            }

            foreach (var line in slideContent)
            {
                markdownBuilder.AppendLine($"- {line}");
            }

            markdownBuilder.AppendLine();
        }

        var markdown = markdownBuilder.ToString().TrimEnd();

        return Results.Ok(new
        {
            message = "Conversion successful!",
            markdown
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error processing presentation: {ex.Message}");
    }
}

static IResult ConvertPdf(Stream stream, string fileName)
{
    try
    {
        var markdownBuilder = new StringBuilder();
        using var pdfDocument = PdfDocument.Open(stream);

        AppendConversionHeader(markdownBuilder, fileName, "PDF document", new[]
        {
            "- Conversion mode: page text extraction",
            "- Output format: Markdown",
            "- AI-friendly structure: each page becomes a labeled section"
        });

        var pageCount = 0;
        foreach (var page in pdfDocument.GetPages())
        {
            pageCount++;
            var pageText = page.Text.Trim();

            markdownBuilder.AppendLine($"## Page {pageCount}");
            markdownBuilder.AppendLine();

            if (string.IsNullOrWhiteSpace(pageText))
            {
                markdownBuilder.AppendLine("_No readable text was found on this page._");
                markdownBuilder.AppendLine();
                continue;
            }

            markdownBuilder.AppendLine(pageText);
            markdownBuilder.AppendLine();
        }

        if (pageCount == 0)
        {
            return Results.BadRequest("The PDF did not contain any readable pages.");
        }

        var markdown = markdownBuilder.ToString().TrimEnd();

        return Results.Ok(new
        {
            message = "Conversion successful!",
            markdown
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error processing PDF file: {ex.Message}");
    }
}

static void AppendConversionHeader(StringBuilder markdownBuilder, string fileName, string formatLabel, IEnumerable<string> summaryLines)
{
    markdownBuilder.AppendLine("# DocuMark Conversion Log");
    markdownBuilder.AppendLine();
    markdownBuilder.AppendLine($"- Source file: `{fileName}`");
    markdownBuilder.AppendLine($"- Input type: {formatLabel}");
    markdownBuilder.AppendLine("- Generated by: DocuMark");

    foreach (var line in summaryLines)
    {
        markdownBuilder.AppendLine(line);
    }

    markdownBuilder.AppendLine();
    markdownBuilder.AppendLine("---");
    markdownBuilder.AppendLine();
    markdownBuilder.AppendLine("## Extracted Content");
    markdownBuilder.AppendLine();
}

static List<List<string>> ExtractWorksheetRows(WorksheetPart worksheetPart, SharedStringTable? sharedStringTable)
{
    var rows = new List<List<string>>();
    var sheetData = worksheetPart.Worksheet.Elements<SheetData>().FirstOrDefault();

    if (sheetData == null)
    {
        return rows;
    }

    foreach (var row in sheetData.Elements<Row>())
    {
        var rowValues = new List<string>();

        foreach (var cell in row.Elements<Cell>())
        {
            var columnIndex = GetColumnIndexFromCellReference(cell.CellReference?.Value);
            while (rowValues.Count <= columnIndex)
            {
                rowValues.Add(string.Empty);
            }

            rowValues[columnIndex] = GetCellText(cell, sharedStringTable);
        }

        while (rowValues.Count > 0 && string.IsNullOrWhiteSpace(rowValues[^1]))
        {
            rowValues.RemoveAt(rowValues.Count - 1);
        }

        if (rowValues.Any(value => !string.IsNullOrWhiteSpace(value)))
        {
            rows.Add(rowValues);
        }
    }

    return rows;
}

static string GetCellText(Cell cell, SharedStringTable? sharedStringTable)
{
    if (cell.DataType?.Value == CellValues.SharedString)
    {
        if (int.TryParse(cell.CellValue?.Text ?? cell.CellValue?.InnerText, out var sharedStringIndex) && sharedStringTable != null)
        {
            var sharedStringItem = sharedStringTable.Elements<SharedStringItem>().ElementAtOrDefault(sharedStringIndex);
            return sharedStringItem?.InnerText ?? string.Empty;
        }

        return cell.CellValue?.Text ?? string.Empty;
    }

    if (cell.DataType?.Value == CellValues.InlineString)
    {
        return cell.InnerText;
    }

    if (cell.DataType?.Value == CellValues.Boolean)
    {
        return cell.CellValue?.Text == "1" ? "TRUE" : "FALSE";
    }

    if (cell.CellValue != null)
    {
        return cell.CellValue.Text;
    }

    return cell.InnerText;
}

static string BuildMarkdownTable(List<List<string>> rows)
{
    if (rows.Count == 0)
    {
        return string.Empty;
    }

    var columnCount = rows.Max(row => row.Count);
    if (columnCount == 0)
    {
        return string.Empty;
    }

    var normalizedRows = rows
        .Select(row => row.Concat(Enumerable.Repeat(string.Empty, columnCount - row.Count)).ToList())
        .ToList();

    var headerRow = normalizedRows[0];
    var dataRows = normalizedRows.Skip(1).ToList();

    if (dataRows.Count == 0)
    {
        headerRow = Enumerable.Range(1, columnCount).Select(column => $"Column {column}").ToList();
        dataRows = normalizedRows;
    }

    var markdownTable = new StringBuilder();
    markdownTable.AppendLine("| " + string.Join(" | ", headerRow.Select(EscapeMarkdownCell)) + " |");
    markdownTable.AppendLine("| " + string.Join(" | ", Enumerable.Repeat("---", columnCount)) + " |");

    foreach (var row in dataRows)
    {
        markdownTable.AppendLine("| " + string.Join(" | ", row.Select(EscapeMarkdownCell)) + " |");
    }

    return markdownTable.ToString().TrimEnd();
}

static string EscapeMarkdownCell(string value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return string.Empty;
    }

    return value
        .Replace("|", "\\|")
        .Replace("\r\n", "<br>")
        .Replace("\r", "<br>")
        .Replace("\n", "<br>")
        .Trim();
}

static int GetColumnIndexFromCellReference(string? cellReference)
{
    if (string.IsNullOrWhiteSpace(cellReference))
    {
        return 0;
    }

    var columnIndex = 0;
    foreach (var character in cellReference)
    {
        if (!char.IsLetter(character))
        {
            break;
        }

        columnIndex = (columnIndex * 26) + (char.ToUpperInvariant(character) - 'A' + 1);
    }

    return Math.Max(columnIndex - 1, 0);
}

static List<string> ExtractSlideText(SlidePart slidePart)
{
    return slidePart.Slide.Descendants<A.Paragraph>()
        .Select(paragraph => paragraph.InnerText.Trim())
        .Where(text => !string.IsNullOrWhiteSpace(text))
        .ToList();
}