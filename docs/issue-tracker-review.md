<issue-tracker-review>
Review of `ModelessForms-SHARED/05 IssuesManager` (Issue Tracker). Date: 2026-11-24.
Scope: all 13 files (3248 lines), the wiring in `Application.cs` / `EventHandler.cs`, and the build of each Revit version.
Labels: CONFIRMED = I proved it (build or exact code path). PLAUSIBLE = the code shows it, but I did not run it in Revit.
</issue-tracker-review>

<summary>
1. The tool does not compile for Revit 2025 (CONFIRMED by build) or Revit 2022 (missing references). Only 2024 can build it.
2. There are 5 paths that can make issue data disappear without a warning. All of them come from `catch { }` in `StorageService` plus "save on every click".
3. The XAML is dead. The window builds all of its UI in C# with 89 inline colours. Nothing is retemplated, the title bar is not dark, and the shared `ThemeManager` is not used.
4. The code is one 1200-line code-behind class. There is no MVVM and there are no tests.
Recommendation: rebuild the window on the shared theme as MVVM, and fix the storage module first, because it can destroy user data.
</summary>

<part-1-xaml-and-theme>

<finding id="X1" severity="high" status="CONFIRMED">
The XAML files are dead code. `IssueTrackerWindow.xaml` (274 lines), `ScreenshotOverlay.xaml` (50) and `Styles.xaml` (154) are not in `ModelessForms-SHARED.projitems` as `<Page>`. Only the `.xaml.cs` files compile. The classes are `partial` but have no other part. `BuildUI()` builds the full window in C# again.
Effect: the styles in `Styles.xaml` (including its 2 ControlTemplates) are never applied. Any edit to the XAML has no effect.
</finding>

<finding id="X2" severity="high" status="CONFIRMED">
No centralized style dictionary is used. `IssueTrackerWindow.xaml.cs` has 81 inline colour calls (`ColorConverter.ConvertFromString("#...")`, `Color.FromArgb`, `Brushes.White`). `ScreenshotOverlay.xaml.cs` has 8. Background, Foreground, BorderBrush, Padding and FontSize are set on each control instance. This breaks WPF rule 1 (centralized styles only).
`CreateDarkListBoxItemStyle()` and `CreateDarkComboBoxItemStyle()` build styles in code and are called again for each control.
</finding>

<finding id="X3" severity="high" status="CONFIRMED in code, PLAUSIBLE on screen">
No control is retemplated (WPF rule 3). The live window uses these control types, all with the default Aero2 template:
- ComboBox (x3): the toggle button and dropdown chrome stay light. Setting `Background` on a ComboBox has no effect on Windows 10/11.
- ComboBoxItem: only recoloured by setters. The default template keeps its own highlight border.
- ListBox (x3), ListBoxItem: setters only. The default template draws its own selected/hover chrome, so the `IsSelected` trigger is partly hidden by the system highlight.
- Button (x15): setters only. The default template ignores `Background` on mouse-over and pressed, so the buttons turn light blue on hover.
- ScrollBar: light (in the description TextBox and the 3 ListBoxes).
- TextBox: no focus/hover template, so the default blue focus border shows.
- GridSplitter, ToolTip: default.
</finding>

<finding id="X4" severity="medium" status="CONFIRMED">
No dark title bar. Neither window calls `DwmSetWindowAttribute`. The repo already has this in `revit-shared-utilities-shared/02 Theme/ThemeManager.cs` (`ThemeManager.Apply(window)`, attribute 20 with fallback 19). ModelessForms 2022/2024/2025 already import that shared project, so the window can use it now.
</finding>

<finding id="X5" severity="medium" status="CONFIRMED">
The shared theme does not cover all the controls this window needs. `revit-shared-utilities-shared/02 Theme/Theme.xaml` has implicit styles only for Window, TextBlock, TextBox, Button and GroupBox. ComboBox, ComboBoxItem, ListBox, ListBoxItem, ScrollBar, GridSplitter and ToolTip are missing. To meet rule 3, add these to the SHARED theme, not to a local Issue Tracker dictionary.
</finding>

<finding id="X6" severity="low" status="CONFIRMED">
There are three different palettes:
- Issue Tracker: VS-Code colours (`#1E1E1E`, `#007ACC`, green `#2D5A2D`, purple `#5B2C6F`).
- Shared `Colors.xaml`: `#20242B`, accent `#4C8DD1`.
- Your WPF ruleset default: `#1E2124`, accent `#4A6B8A`.
Your ruleset says "if a project has its own brand palette, prefer that". I think the shared `Colors.xaml` is that palette. You must decide this (see question Q1).
</finding>

<finding id="X7" severity="low" status="CONFIRMED">
`ScreenshotOverlay` colours (selection blue, dim overlay) are inline. These are overlay-specific. Put them into the theme as named brushes.
</finding>

</part-1-xaml-and-theme>

<part-2-build>

<finding id="B1" severity="critical" status="CONFIRMED (build)">
Revit 2025 does not compile. MSBuild errors: `QuestPDF` not found, `Microsoft.CognitiveServices` not found, `NAudio` not found. `ModelessForms-2025.csproj` has no PackageReference for QuestPDF, Microsoft.CognitiveServices.Speech or NAudio.
</finding>

<finding id="B2" severity="critical" status="PLAUSIBLE">
Revit 2022 cannot compile it either. `ModelessForms-2022.csproj` has no reference to iTextSharp, Speech or NAudio. I could not confirm with a build, because 2022 stops earlier on a different error (see B3).
</finding>

<finding id="B3" severity="medium" status="CONFIRMED (build), outside scope">
ModelessForms-2022 fails on C# 7.3: `#nullable` in `MEPUtils-SHARED/App/CategoryCommands.cs`, `RibbonButtons.cs` and `revit-shared-utilities-shared/01 Shared code/DevReloadButtonAttribute.cs` (error CS8370). This comes from the recent command-palette work, not the Issue Tracker. Set `<LangVersion>` in the 2022 csproj, or remove `#nullable` from shared code.
</finding>

<finding id="B4" severity="medium" status="CONFIRMED">
There are two PDF implementations of the same page layout: QuestPDF (2025) and iTextSharp 5 (2022/2024), in one `#if` file. They will drift apart (DRY). Licences:
- iTextSharp 5 is AGPL.
- QuestPDF "Community" is free only for organisations with less than USD 1M annual revenue. The code sets `LicenseType.Community` without a check.
Use one library on all versions. QuestPDF targets netstandard2.0, so it also runs on net48 (Professional licence if Norsyn is above the limit). PDFsharp/MigraDoc (MIT) is the free option.
</finding>

<finding id="B5" severity="low" status="CONFIRMED">
`#if REVIT2025 || REVIT2026`: there is no 2026 project.
</finding>

</part-2-build>

<part-3-data-loss>
All 5 items come from one root cause: `StorageService` swallows every exception (`catch { }`) and returns an empty `new Collection(name)`. The window then saves that empty collection on top of the real file. This is a silent fallback (your rule 12) in its most harmful form.

<finding id="D1" severity="critical" status="CONFIRMED (code path)">
A corrupt or unreadable `collection.json` destroys all issues. `LoadCollection` fails, returns an empty collection, and the next `SaveCollection` (any list click, window close) overwrites the file. A file locked by another user, antivirus or OneDrive gives the same result.
</finding>

<finding id="D2" severity="critical" status="CONFIRMED (code path)">
Revit 2024 and Revit 2025 write different JSON. net48 uses `DataContractJsonSerializer` (dates as `"\/Date(1737120000000+0100)\/"`). net8 uses `System.Text.Json` (ISO dates). Neither can read the other's dates. If one person opens the same collection in Revit 2024 and in 2025, D1 occurs and the data is lost. `settings.json` in `%APPDATA%\IssueTracker` is shared by all Revit versions too, so the settings (Azure key, folder) reset when you change version.
</finding>

<finding id="D3" severity="critical" status="CONFIRMED (code path)">
"New collection" with an existing name overwrites that collection. `NewCollectionButton_Click` calls `SaveCollection(new Collection(name))` without checking if the folder exists. The issues are lost. The images stay as orphan files.
</finding>

<finding id="D4" severity="high" status="CONFIRMED (code path)">
Changing the storage folder copies the open collection into the new folder. `SaveSettingsButton_Click` sets `_settings.BaseFolder` first, then `LoadCollections()` → `LoadCollection()` → `SaveCollection()`. This writes the OLD collection's JSON (without images) into the NEW folder. If the new folder has a collection with the same name, that collection is overwritten.
</finding>

<finding id="D5" severity="high" status="CONFIRMED (code path)">
"Save" always says "Collection saved.", even when the write failed. `File.WriteAllText` is not atomic, so a crash during a write leaves a truncated file, and then D1 occurs.
</finding>

<finding id="D6" severity="medium" status="CONFIRMED">
The collection has two names that can disagree: the folder name (used to load) and `Collection.Name` in the JSON (used to save). If someone renames the folder, saves go to the old folder name. A name that has only invalid characters (for example `///`) becomes `""`, and `collection.json` is written into the base folder.
</finding>

</part-3-data-loss>

<part-4-logic>

<finding id="L1" severity="high" status="CONFIRMED (code)">
The issue list shows empty rows. `IssuesListBox.DisplayMemberPath = "GetDisplayTitle"`, but `GetDisplayTitle` is a METHOD. A binding path can only read a property, so each row binds to nothing. The title also cannot refresh when the description changes, because `Issue` has no `INotifyPropertyChanged` and the list is a `List<Issue>` with `Items.Refresh()`.
Also: the title fallback `Id.Substring(6, 8)` is the date, so all issues made on the same day get the same title. It throws if the `Id` is short.
</finding>

<finding id="L2" severity="high" status="CONFIRMED (code path)">
Screenshot file names collide. The name index is `Screenshots.Count + 1`. Example: add 3 screenshots (001, 002, 003), remove 001, add a new one → index 3 → `_003.png` is overwritten. Now two entries point to one file. Remove one, and the other screenshot loses its file.
</finding>

<finding id="L3" severity="high" status="PLAUSIBLE">
The main window can stay hidden permanently. `AddScreenshotButton_Click` calls `Hide()`, and only the `ScreenshotCaptured` event calls `Show()`. If the overlay closes in a different way (Alt+F4, focus lost), the window stays hidden. The ribbon button then calls `Activate()` on a hidden window, which does not show it. The user must restart Revit. ESC can also fail if the overlay does not get keyboard focus.
</finding>

<finding id="L4" severity="high" status="CONFIRMED (code)">
New Issue does nothing when no document is open. `EventHandler.Execute` reads `app.ActiveUIDocument.Document` before it calls the command. With no document this throws a NullReferenceException. The null check in `GetSelectionHandler` is never reached, the callback never runs, and the user gets no message.
</finding>

<finding id="L5" severity="medium" status="CONFIRMED (code)">
All modeless forms share one `Application.asyncCommand` field. If two forms raise their event before Revit runs it, the last one wins and the first command is lost. This is a pre-existing architecture smell in ModelessForms, not only the Issue Tracker.
</finding>

<finding id="L6" severity="medium" status="CONFIRMED (code)">
Speech errors show two message boxes. `StartRecordingAsync` raises `OnError` (MessageBox 1) and then rethrows (MessageBox 2 in `VoiceButton_Click`). Other speech problems:
- If an error stops the session, the button stays on "■".
- `StopRecordingAsync` waits for `SessionStopped` with no timeout, so the button can stay on "..." forever.
- Dictated text goes into the issue that is selected when recognition ENDS, not the issue where it started.
- Saving settings while recording disposes the active recognizer.
</finding>

<finding id="L7" severity="medium" status="CONFIRMED (code)">
Silent fallbacks (rule 12):
- If the chosen microphone is missing, the default microphone is used with no message.
- `Settings()` sets `BaseFolder` to `Documents\IssueTracker`, and `GetCollectionNames` creates the folder. The first-run check `IsNullOrEmpty(BaseFolder)` is therefore never true. "Cancel" in Settings never closes the tool, and the tool silently uses Documents.
- Capture failure returns `null` and nothing happens.
- PDF export skips missing images.
- `SelectElementsHandler` skips elements it cannot find and does not tell the user.
</finding>

<finding id="L8" severity="medium" status="PLAUSIBLE">
Screenshot capture is limited and fragile:
- The overlay is `Maximized`, so it covers only one monitor. You cannot capture the second monitor.
- Width is scaled by the DPI of the overlay window, which is wrong with mixed-DPI monitors.
- `Thread.Sleep(100)` on the UI thread is a timing hack to let the overlay fade.
</finding>

<finding id="L9" severity="medium" status="PLAUSIBLE">
The QuestPDF export fails on tall screenshots. `.Image(path).FitWidth()` throws a layout exception when the scaled image is taller than the page. `FitArea()` is correct. iTextSharp scales by width only.
</finding>

<finding id="L10" severity="medium" status="CONFIRMED">
The Azure Speech key is stored as plain text in `%APPDATA%\IssueTracker\settings.json` and shown in a plain TextBox. Use DPAPI (`ProtectedData`) and a PasswordBox.
</finding>

<finding id="L11" severity="low" status="CONFIRMED">
- `IssueTrackerWindow` has no `Owner` (the Revit main window). It can go behind Revit and shows a separate taskbar button.
- Each click in the issue list writes the full JSON to disk on the Revit UI thread. `Modified` is set to "now" on each save, even with no change, so the value has no meaning.
- PDF export runs on the Revit UI thread, so Revit freezes during a large export.
- `OnSelectionReceived` adds the issue to the collection that is open when Revit answers, which can be a different collection.
- "New Issue" with no selection makes an issue with no elements and no prompt (the spec says to prompt).
- The PDF header shows the raw id `issue_20260117_...`, not an issue number.
- If `BaseFolder` is a shared network folder, the last person to save wins. There is no locking. (See Q3.)
</finding>

</part-4-logic>

<part-5-dead-code-and-smells>
Dead code to delete (your decision):
- `Views/IssueTrackerWindow.xaml`, `Views/ScreenshotOverlay.xaml`, `Views/Styles.xaml`: not compiled (X1).
- `IssueTrackerWindow._handler`: assigned, never read.
- `IssueTrackerWindow._isNewCollectionFlow`: written, never read.
- `PdfExportService.IsSupported`: always `true`, so the "requires Revit 2025" branch in `PublishPdfButton_Click` cannot run. Its message is also wrong.
- `SpeechService.OnPartialResult`: no subscriber. The return value of `StopRecordingAsync` is never used.
- `GetSelectionHandler.Guids`: public, but only the callback is used.
- `EventHandler.Execute`: local `doc` is unused (and causes L4).
- `ISSUE_DOTNET_SPEC.md`: out of date (`SpeechProvider`, "windows" speech, sequential issue numbers). Update it, or replace it with `docs/shared-understanding/issue-tracker.md` after we agree on the design.

Smells:
- God class: `IssueTrackerWindow.xaml.cs` (1200 lines) mixes UI construction, state, persistence calls, speech, capture and PDF. Not testable.
- Shallow services built with `new` in the window and no interfaces, so there is no test seam. `ScreenshotService.GetScreenshotPath` is only a `Path.Combine`.
- `catch { }` in 11 places. The spec requires "log errors"; NLog is already in the 2025 project.
- Overlay "dialogs" (Settings, Input, Collection settings) are borders in the same grid. They do not block keyboard focus, so Tab and Enter still reach the controls behind them.
</part-5-dead-code-and-smells>

<proposed-direction>
I suggest this order. Each step is a separate deliverable. I do not start any of it until you agree.
1. Storage module (deep module, one boundary): one JSON serializer for all versions, atomic write (temp file + replace), loud failures (no `catch { }`), no overwrite on load failure, a name-collision check, and the folder as the one source of truth for the name. Use TDD: this module needs no Revit, so a test project can cover D1–D6 and L2.
2. Build: add the missing packages for 2022/2025, and choose one PDF library.
3. Shared theme: add retemplated implicit styles for ComboBox, ListBox(Item), ScrollBar, GridSplitter and ToolTip to `revit-shared-utilities-shared/02 Theme`.
4. View: replace `BuildUI()` with real compiled XAML plus a ViewModel, use `ThemeManager.Apply(window)` (dark title bar), and set the Revit owner.
5. Fix the remaining logic items (L3–L11).
</proposed-direction>

<questions-for-user>
Q1. Palette: use the shared `Colors.xaml` (command palette colours), or your ruleset default? Do you want to keep the green/red/purple button colours as status colours?
Q2. Existing data: do users have real collections now (2024 format)? If yes, step 1 needs a migration reader for the old `\/Date()\/` format.
Q3. Is `BaseFolder` a shared network folder that more people use at the same time? If yes, we need locking or one file per issue.
Q4. PDF: does Norsyn qualify for the free QuestPDF Community licence, or do we use PDFsharp/MigraDoc (MIT)?
Q5. Is Revit 2022 still a target? If not, we can drop it and remove some of the `#if` code.
</questions-for-user>
