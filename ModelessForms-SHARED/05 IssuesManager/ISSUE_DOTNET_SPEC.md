<project>
ISSUE Tool - Revit Issue Documentation Plugin
</project>

<overview>
A Revit add-in for rapidly documenting model issues with screenshots, voice dictation, and element tracking. Issues are organized into collections and can be published to PDF. The tool runs as a modeless window allowing users to work in Revit while documenting issues.
</overview>

<technology-stack>
- Language: C# (.NET Framework 4.8 for Revit 2022-2024, or .NET 8 for Revit 2025+)
- UI Framework: WPF with XAML
- Revit API: Autodesk.Revit.DB, Autodesk.Revit.UI
- Speech Recognition: Azure Cognitive Services Speech SDK (azure-cognitiveservices-speech NuGet package)
- PDF Generation: iTextSharp or QuestPDF
- JSON Serialization: System.Text.Json or Newtonsoft.Json
</technology-stack>

<features>
<feature name="Screenshot Capture">
- User clicks "Add Screenshot" button
- Main window hides
- Full-screen transparent overlay appears for region selection
- User draws rectangle to select area
- Screenshot is captured and saved as PNG
- Multiple screenshots per issue supported
- Thumbnails displayed in issue detail panel
</feature>

<feature name="Voice Dictation">
- Azure Cognitive Services Speech-to-Text
- User clicks "Voice" button, speaks into microphone
- Recognized text appends to description field
- Requires Azure Speech resource (key + region)
- Settings stored in user configuration
</feature>

<feature name="Element Tracking">
- When creating new issue, capture currently selected Revit elements
- Store element UniqueId (GUID), not ElementId (ElementId changes between sessions)
- Display element category and truncated GUID in UI
</feature>

<feature name="Collections">
- Issues organized into named collections (folders)
- User can create, switch, delete collections
- Each collection stored as separate JSON file with images subfolder
- Default collection created on first run
</feature>

<feature name="PDF Export">
- One issue per A4 page
- Header: Collection name, Issue number, Date
- Body: Screenshot(s) scaled to fit, Description text
- Footer: Related element GUIDs
- All issues in collection exported to single PDF
</feature>

<feature name="Modeless Window">
- Window stays open while user works in Revit
- Must use IExternalEventHandler pattern for Revit API calls
- Window can be minimized, moved, resized
- Dark theme UI matching modern Revit aesthetic
</feature>
</features>

<data-model>
<class name="Settings">
```csharp
public class Settings
{
    public string BaseFolder { get; set; }           // Root folder for all collections
    public string SpeechProvider { get; set; }       // "windows" or "azure"
    public string DefaultCollection { get; set; }    // Last used collection name
    public string AzureSpeechKey { get; set; }       // Azure Speech resource key
    public string AzureSpeechRegion { get; set; }    // Azure region (e.g., "eastus")
}
```
Location: %APPDATA%/IssueTracker/settings.json
</class>

<class name="Issue">
```csharp
public class Issue
{
    public string Id { get; set; }                   // Unique ID: "issue_20260117_143052_123456"
    public string Description { get; set; }          // User-entered description
    public List<string> ElementGuids { get; set; }   // Revit element UniqueIds
    public List<string> Screenshots { get; set; }    // Screenshot filenames
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}
```
</class>

<class name="Collection">
```csharp
public class Collection
{
    public string Name { get; set; }
    public DateTime Created { get; set; }
    public List<Issue> Issues { get; set; }
}
```
Location: {BaseFolder}/{CollectionName}/collection.json
Images: {BaseFolder}/{CollectionName}/images/*.png
</class>
</data-model>

<folder-structure>
```
{BaseFolder}/
├── Collection1/
│   ├── collection.json
│   └── images/
│       ├── issue_20260117_143052_123456_001.png
│       └── issue_20260117_143052_123456_002.png
├── Collection2/
│   ├── collection.json
│   └── images/
└── ...
```
</folder-structure>

<ui-layout>
<window title="Issue Tracker" size="1000x700" minsize="800x500">
```
┌─────────────────────────────────────────────────────────────────────┐
│ Collection: [ComboBox▼] [New Collection] [Delete] _____ [Settings] │
├────────────────────────┬────────────────────────────────────────────┤
│                        │                                            │
│  Issues List           │  Issue Details                             │
│  ┌──────────────────┐  │  ┌──────────────────────────────────────┐  │
│  │ Issue #20260117  │  │  │ Description:                         │  │
│  │ Wall intersect...│  │  │ ┌──────────────────────────┐ [Voice] │  │
│  ├──────────────────┤  │  │ │ (multiline textbox)      │ [Clear] │  │
│  │ Issue #20260117  │  │  │ │                          │         │  │
│  │ Missing door...  │  │  │ └──────────────────────────┘         │  │
│  ├──────────────────┤  │  │                                      │  │
│  │ Issue #20260117  │  │  │ Screenshots:                         │  │
│  │ Level offset...  │  │  │ ┌─────┬─────┬─────┐ [Add]            │  │
│  │                  │  │  │ │thumb│thumb│thumb│ [Remove]         │  │
│  │                  │  │  │ └─────┴─────┴─────┘                  │  │
│  │                  │  │  │                                      │  │
│  │                  │  │  │ Related Elements:                    │  │
│  │                  │  │  │ ┌──────────────────────────────────┐ │  │
│  │                  │  │  │ │ Wall [abc123...]                 │ │  │
│  │                  │  │  │ │ Door [def456...]                 │ │  │
│  └──────────────────┘  │  │ └──────────────────────────────────┘ │  │
│                        │  └──────────────────────────────────────┘  │
├────────────────────────┴────────────────────────────────────────────┤
│ [New Issue] [Delete Issue] [Save]                 [Publish to PDF]  │
└─────────────────────────────────────────────────────────────────────┘
```
</window>

<overlay name="Screenshot Selection">
- Fullscreen transparent window (AllowsTransparency=True, WindowStyle=None)
- Semi-transparent dark overlay (#80000000)
- User draws rectangle with mouse
- Selected region shown with blue border
- Dimensions displayed near selection
- ESC to cancel, release mouse to capture
- Hint text: "Click and drag to select region. Press ESC to cancel."
</overlay>

<overlay name="Settings Dialog">
- Modal overlay (not separate window - inline Border with dark background)
- Storage folder path + Browse button
- Voice Recognition dropdown (Windows Speech / Azure Cognitive Services)
- Azure settings panel (Key, Region) - shown only when Azure selected
- Save / Cancel buttons
</overlay>

<overlay name="Input Dialog">
- Modal overlay for text input (collection name)
- Label, TextBox, OK/Cancel buttons
- Used instead of MessageBox.Show input dialogs
</overlay>
</ui-layout>

<color-scheme>
```csharp
// Dark theme colors
BackgroundDark    = #1E1E1E  // Window background
BackgroundMedium  = #252526  // Panel backgrounds
BackgroundLight   = #333337  // Input field backgrounds
BorderBrush       = #3F3F46  // Borders
ForegroundLight   = #F1F1F1  // Primary text
ForegroundDim     = #A0A0A0  // Secondary text
AccentBlue        = #007ACC  // Selection, primary buttons
AccentGreen       = #2D5A2D  // Save button, voice button
AccentRed         = #6E1E1E  // Delete buttons
AccentPurple      = #5B2C6F  // Publish button
```
</color-scheme>

<revit-integration>
<external-event-handler>
Modeless windows cannot directly call Revit API. Use IExternalEventHandler pattern:

```csharp
public class GetSelectionHandler : IExternalEventHandler
{
    public List<string> Guids { get; private set; }
    public Action<List<string>> Callback { get; set; }

    public void Execute(UIApplication app)
    {
        var uidoc = app.ActiveUIDocument;
        var doc = uidoc.Document;
        var selection = uidoc.Selection.GetElementIds();

        Guids = new List<string>();
        foreach (var elemId in selection)
        {
            var elem = doc.GetElement(elemId);
            if (elem != null)
                Guids.Add(elem.UniqueId);
        }

        Callback?.Invoke(Guids);
    }

    public string GetName() => "Get Selection Handler";
}

// In main window:
private ExternalEvent _selectionEvent;
private GetSelectionHandler _selectionHandler;

// Initialize:
_selectionHandler = new GetSelectionHandler();
_selectionEvent = ExternalEvent.Create(_selectionHandler);

// When "New Issue" clicked:
_selectionHandler.Callback = OnSelectionReceived;
_selectionEvent.Raise();
```
</external-event-handler>

<addin-manifest>
```xml
<?xml version="1.0" encoding="utf-8"?>
<RevitAddIns>
  <AddIn Type="Command">
    <Name>Issue Tracker</Name>
    <Assembly>IssueTracker.dll</Assembly>
    <FullClassName>IssueTracker.Command</FullClassName>
    <ClientId>GUID-HERE</ClientId>
    <Text>Issue Tracker</Text>
    <Description>Document model issues with screenshots and voice</Description>
    <VisibilityMode>AlwaysVisible</VisibilityMode>
    <VendorId>YourCompany</VendorId>
    <VendorDescription>Your Company Name</VendorDescription>
  </AddIn>
</RevitAddIns>
```
</addin-manifest>
</revit-integration>

<screenshot-capture>
```csharp
public class ScreenshotCapture
{
    public byte[] CaptureRegion(int x, int y, int width, int height)
    {
        using (var bitmap = new Bitmap(width, height))
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(x, y, 0, 0, new Size(width, height));
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
        }
    }
}

// Selection overlay is a WPF Window with:
// - WindowStyle="None"
// - AllowsTransparency="True"
// - Background="#01000000" (nearly transparent)
// - Topmost="True"
// - WindowState="Maximized"
// - Mouse events for drawing selection rectangle
```
</screenshot-capture>

<azure-speech>
```csharp
// NuGet: Microsoft.CognitiveServices.Speech

public class AzureSpeechService
{
    private readonly string _key;
    private readonly string _region;

    public AzureSpeechService(string key, string region)
    {
        _key = key;
        _region = region;
    }

    public async Task<string> RecognizeOnceAsync()
    {
        var config = SpeechConfig.FromSubscription(_key, _region);
        config.SpeechRecognitionLanguage = "en-US";

        using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        using var recognizer = new SpeechRecognizer(config, audioConfig);

        var result = await recognizer.RecognizeOnceAsync();

        return result.Reason switch
        {
            ResultReason.RecognizedSpeech => result.Text,
            ResultReason.NoMatch => throw new Exception("No speech detected"),
            ResultReason.Canceled => throw new Exception(
                CancellationDetails.FromResult(result).ErrorDetails),
            _ => throw new Exception("Unknown error")
        };
    }
}
```
</azure-speech>

<pdf-export>
```csharp
// Using QuestPDF (recommended) or iTextSharp

public void ExportToPdf(Collection collection, string outputPath, string baseFolder)
{
    Document.Create(container =>
    {
        foreach (var issue in collection.Issues)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20, Unit.Millimetre);

                page.Header().Text($"Issue #{issue.Id}").Bold().FontSize(16);

                page.Content().Column(col =>
                {
                    // Screenshots
                    foreach (var screenshot in issue.Screenshots)
                    {
                        var imgPath = Path.Combine(baseFolder, collection.Name, "images", screenshot);
                        if (File.Exists(imgPath))
                            col.Item().Image(imgPath).FitWidth();
                    }

                    // Description
                    col.Item().Text("Description:").Bold();
                    col.Item().Text(issue.Description ?? "(No description)");

                    // Elements
                    if (issue.ElementGuids?.Any() == true)
                    {
                        col.Item().Text("Related Elements:").Bold();
                        foreach (var guid in issue.ElementGuids)
                            col.Item().Text($"• {guid}");
                    }
                });

                page.Footer().Text($"Created: {issue.Created:yyyy-MM-dd}");
            });
        }
    }).GeneratePdf(outputPath);
}
```
</pdf-export>

<first-run>
1. Check if settings.json exists
2. If not, show folder browser dialog to select base folder
3. If user cancels, do not open main window
4. Create "Default" collection if none exist
5. Save settings
</first-run>

<error-handling>
- Wrap all Revit API calls in try-catch
- Wrap all UI event handlers in try-catch
- Log errors to file or Revit journal
- Never let exceptions crash Revit
- Show user-friendly error messages
</error-handling>

<known-challenges>
1. Modeless windows require IExternalEventHandler for Revit API access
2. Screenshot overlay must be a separate WPF window, not a dialog
3. Azure Speech SDK requires proper async/await handling
4. JSON serialization must handle null values gracefully
5. PDF images must be scaled to fit page while maintaining aspect ratio
6. Element GUIDs are stable across sessions; ElementIds are not
</known-challenges>

<project-structure>
```
IssueTracker/
├── IssueTracker.sln
├── IssueTracker/
│   ├── IssueTracker.csproj
│   ├── Command.cs                 // Revit IExternalCommand entry point
│   ├── App.cs                     // Optional: IExternalApplication for ribbon
│   ├── Views/
│   │   ├── MainWindow.xaml        // Main modeless window
│   │   ├── MainWindow.xaml.cs
│   │   ├── ScreenshotOverlay.xaml // Fullscreen selection overlay
│   │   └── ScreenshotOverlay.xaml.cs
│   ├── Models/
│   │   ├── Issue.cs
│   │   ├── Collection.cs
│   │   └── Settings.cs
│   ├── Services/
│   │   ├── StorageService.cs      // JSON file operations
│   │   ├── ScreenshotService.cs   // Screen capture
│   │   ├── SpeechService.cs       // Azure Speech
│   │   └── PdfExportService.cs    // PDF generation
│   ├── Handlers/
│   │   └── GetSelectionHandler.cs // IExternalEventHandler
│   └── Resources/
│       └── Styles.xaml            // Dark theme styles
└── IssueTracker.addin             // Revit add-in manifest
```
</project-structure>

<nuget-packages>
- Microsoft.CognitiveServices.Speech (Azure Speech SDK)
- QuestPDF (PDF generation) or iTextSharp
- Newtonsoft.Json (JSON serialization) - optional, can use System.Text.Json
</nuget-packages>

<testing-checklist>
- [ ] First run shows folder selection dialog
- [ ] Canceling folder selection closes app without error
- [ ] Create new collection creates folder and JSON file
- [ ] Delete collection removes folder and all contents
- [ ] Switch collections loads correct issues
- [ ] New Issue captures selected Revit elements
- [ ] New Issue with no selection prompts user
- [ ] Screenshot overlay appears fullscreen
- [ ] Drawing selection shows rectangle with dimensions
- [ ] ESC cancels screenshot
- [ ] Captured screenshot saves to images folder
- [ ] Multiple screenshots per issue work
- [ ] Remove screenshot deletes file
- [ ] Voice button records and transcribes speech
- [ ] Voice text appends to description
- [ ] Save button persists changes to JSON
- [ ] Publish to PDF creates valid PDF file
- [ ] PDF contains all issues with images and text
- [ ] Window can be moved, resized, minimized
- [ ] Closing window doesn't crash Revit
- [ ] Reopening tool shows previous state
</testing-checklist>
