<issue-tracker-rebuild>
Shared understanding for the Issue Tracker rebuild. Date: 2026-11-24.
Input: `docs/issue-tracker-review.md` and your revdiff notes on it.
Status: DRAFT. I write no code until you approve this file.
</issue-tracker-rebuild>

<decisions-from-user>
1. Fix all findings in the review. Rebuild the tool. Do not patch it.
2. Target is Revit 2025 only. It is urgent. Ignore 2022/2024.
3. Publish the 2025 version after the fixes.
4. Saved data uses a versioned format with incremental upgrade steps. Each change to the format adds one step (vN → vN+1). A load runs all the steps from the file's version to the current version.
5. One user at a time. No locking. If this becomes a problem later, a database will replace the files.
6. Any PDF library. The QuestPDF free licence probably does not apply.
7. Palette: "use the best one". Rethink the button colours in the shared theme.
8. Do not use the existing code in this repo as an example. It is not of good quality.
</decisions-from-user>

<decisions-i-made>
Each item is my recommendation. Annotate any item to change it.

D1. Palette: there is one palette file, `revit-shared-utilities-shared/02 Theme/Colors.xaml`. I choose its VALUES on quality: your ruleset palette (neutral cool gray `#1E2124` / `#2A2D31` / `#33373C`, accent `#4A6B8A`, status success/warning/error). Reasons: it is neutral gray like the Revit 2025 dark UI. The present `Colors.xaml` has a blue cast (`#20242B`, `#333944`) and a brighter accent (`#4C8DD1`) that competes with the content. Your ruleset palette also has the status colours that a Danger button needs.
I keep one set of semantic key names and rename them if they are not clear. Effect: the command palette and the Rotate window change to the new values. That is intended: one palette for the whole add-in.
The PCF exporter has its own second `Theme.xaml` with its own palette. That is a duplicate. I report it and do not merge it in this task, because the PCF exporter is not part of the 2025 Issue Tracker release.

D2. Buttons in the shared theme. Now every button is filled accent blue, so a window full of buttons is all blue and nothing stands out. New model:
- Implicit (default) Button = neutral: surface background, border, primary text.
- `Button.Primary` = accent fill. Use it for the one main action in a view (New Issue, OK).
- `Button.Danger` = neutral at rest, with a red border and red hover. Use it for Delete.
- No green or purple buttons. Colour carries a meaning (primary / danger), not decoration.
Effect outside this task: the command palette and the Rotate window then show neutral buttons. Where a button there is the main action, I add `Style="{StaticResource Button.Primary}"`. I will list each such change.

D3. The shared theme gets retemplated implicit styles for every control the Issue Tracker uses that the theme does not have now: ComboBox (+ its toggle button and popup), ComboBoxItem, ListBox, ListBoxItem, ScrollBar, ScrollViewer, GridSplitter, ToolTip, and TextBox focus/hover states. I first review the existing shared styles and the dark title bar code against WPF rules 1–5, and fix what is wrong. I do not copy them as they are.

D4. PDF library: PDFsharp + MigraDoc 6.x (MIT licence, supports .NET 8). No licence risk. One implementation, no `#if`.

D5. MVVM: CommunityToolkit.Mvvm (Microsoft, source generators). The View is real compiled XAML with no inline styles. The ViewModel has no WPF or Revit types, so tests can drive it.

D6. Dialogs: real modal owned Windows (themed, dark title bar), not overlay borders in the same grid. This fixes the keyboard focus problem (Tab and Enter reach the controls behind the overlay). There are three: Settings, New collection name, Collection properties.

D7. The Issue Tracker window gets the Revit main window as its Owner. Then it stays above Revit and has no separate taskbar button.

D8. Saving: auto-save. Each change is saved at once with an atomic write, and the "Save" button is removed. A failed save shows an error and keeps the data in memory. It does not report "saved". Reason: the old mix of "Save" plus hidden saves on each click had no clear meaning.

D9. Issue numbers: each collection gives its issues sequential numbers (1, 2, 3...). The list and the PDF show "#7 – first line of description". The upgrade step v0→v1 gives numbers to old issues in Created order.

D10. Revit 2022/2024: the Issue Tracker files and its ribbon button compile only for 2025 (a condition in `.projitems`, and `#if REVIT2025` at the button). The other ModelessForms tools stay as they are. The 2022 build is already broken for another reason (B3 in the review). I do not fix that here.
</decisions-i-made>

<modules>
All new code is in `ModelessForms-SHARED/05 IssuesManager/`. Each module has one small public interface.

<m1-issue-store>
Public interface (synchronous; single user):
- `IssueStore(string baseFolder)`
- `IReadOnlyList<string> ListCollections()`
- `Collection Load(string name)`
- `void Save(Collection c)` (atomic: write a temp file, then `File.Replace`)
- `Collection Create(string name)` (throws if the name is not valid or already exists)
- `void Delete(string name)`
- `string AddScreenshot(Collection c, Issue i, byte[] png)` returns the file name
- `void RemoveScreenshot(Collection c, Issue i, string file)`
- `string ScreenshotPath(Collection c, string file)`
Hidden inside: folder layout, JSON, the format version and the upgrade chain, name validation, unique screenshot names (GUID, so names never collide; fixes L2).
Errors: one exception type, `IssueStoreException`, with a clear message. No `catch { }`. A load that fails NEVER returns an empty collection (fixes D1).
Name: the folder name is the only source of the collection name. It is not stored in the JSON (fixes D6).
</m1-issue-store>

<m2-format-upgrades>
- The file has a `"FormatVersion": N` field. A missing field means v0 (all existing files).
- `IFormatUpgrade { int FromVersion; void Apply(JsonObject doc); }`. One class per step.
- The loader reads the file as a `JsonObject`, runs the steps in order, then deserializes it to the current model.
- After an upgrade, the loader saves a copy of the original as `collection.v0.bak.json` before the first save. Then a bad upgrade step cannot destroy old data.
- Step v0→v1: read both old date formats (`"\/Date(ms+zone)\/"` from net48 and ISO from net8), give numbers to the issues, remove `Name`.
- The same mechanism is used for `settings.json`.
</m2-format-upgrades>

<m3-settings>
- `SettingsStore`: load/save `%APPDATA%\Norsyn\IssueTracker\settings.json` (versioned, as in M2).
- The Azure key is encrypted with DPAPI (`ProtectedData`, CurrentUser scope) and entered in a PasswordBox.
- No silent default for the storage folder. If none is set, the Settings dialog opens. If the user cancels, the tool closes with a message (this matches the original spec).
- The old file `%APPDATA%\IssueTracker\settings.json` is read once by the v0 upgrade, if it exists.
</m3-settings>

<m4-revit-bridge>
- `RevitBridge` is the Issue Tracker's own ExternalEvent plus handler, with a queue of requests. It does not use the shared `Application.asyncCommand` field (fixes L5 for this tool).
- Requests: `GetSelectionAsync()` returns the UniqueIds, and `SelectAndShow(ids)`.
- No open document → the result is an error that the ViewModel shows (fixes L4).
- `SelectAndShow` reports how many elements it did not find.
- The shared `EventHandler.Execute` null-reference bug and the shared-slot race in the other ModelessForms tools: reported, not fixed (outside scope).
</m4-revit-bridge>

<m5-screen-capture>
- `ScreenCapture.CaptureRegionAsync()` returns `byte[]?` (null = the user cancelled).
- The overlay covers the full virtual screen (all monitors). It converts the selection to physical pixels from both corners with `PointToScreen`, so mixed DPI works.
- The overlay is made invisible and the capture runs after the next render with `DwmFlush()`, not `Thread.Sleep` (fixes L8).
- The main window is restored in a `finally` block, whatever closes the overlay (fixes L3). The overlay activates and focuses itself, so ESC works.
</m5-screen-capture>

<m6-dictation>
- `DictationService`: `StartAsync()`, `StopAsync()` (with a timeout), and the state `Idle / Recording / Stopping`.
- One error path: methods throw, and there are no extra error events (fixes the double message box).
- Text goes to the issue that was selected at START (fixes L6).
- A missing microphone gives an error, not a silent switch to the default microphone (rule 12).
- Packages: Microsoft.CognitiveServices.Speech; NAudio.Wasapi to list the microphones.
</m6-dictation>

<m7-pdf>
- `PdfReport.Export(Collection, IssueStore, string path)`: MigraDoc. A front page, then one page for each issue. Images fit both width and height (fixes L9).
- It runs on a background thread with a copy of the data. The window shows a busy state.
- A missing image file shows as a "missing image: <name>" line in the PDF. It is not skipped silently.
</m7-pdf>

<m8-ui>
- `IssueTrackerWindow.xaml` + `IssueTrackerViewModel`. Dialog windows (D6). Everything binds to the VM.
- `IssueViewModel` raises property-changed, so the list title updates while you type (fixes L1).
- The VM depends on small interfaces (`IIssueStore`, `IRevitBridge`, `IScreenCapture`, `IDictation`, `IPdfReport`, `IDialogs`), so it can be tested without WPF or Revit.
</m8-ui>

<m9-shared-theme>
Changes to `revit-shared-utilities-shared/02 Theme/`: D1, D2, D3. I check the existing code first.
</m9-shared-theme>

<m10-build-and-tests>
- `ModelessForms-2025.csproj`: add CommunityToolkit.Mvvm, Microsoft.CognitiveServices.Speech, NAudio.Wasapi, PDFsharp-MigraDoc.
- `.projitems`: the Issue Tracker files compile only for net8 (D10). The XAML is added as `<Page>`.
- New test project `ModelessForms-tests` (net8, xUnit). TDD for M1, M2 and M3 (no Revit needed). VM tests use fakes. One theme test: each control type in the Issue Tracker XAML has an implicit style with a Template in the shared theme, and the views have no inline Background/Foreground/hex.
- Delete: the old window code, the dead XAML, `Styles.xaml`, `ISSUE_DOTNET_SPEC.md` (this file replaces it), and the dead members listed in the review.
</m10-build-and-tests>
</modules>

<improvements-to-touched-shared-code>
Rule: REUSE the shared code (theme, `ThemeManager`, dark title bar) and improve it in place. Never make a local copy for the Issue Tracker.
The codebase is not exemplary. I improve the code that I touch. Each item is listed here, so nothing is changed silently:
- I1. `ModelessForms-SHARED/EventHandler.cs`: remove the unused `doc` read that throws with no document open (L4 for all ModelessForms tools). Give the handler a correct `GetName()`.
- I2. `ModelessForms-SHARED/Application.cs`: the Issue Tracker launch code is changed for the new window (owner, theme, `#if REVIT2025`). I clean up only the Issue Tracker parts of this file.
- I3. Shared theme (`02 Theme`): I review every existing style and `ThemeManager` against WPF rules 1–5 and fix what is wrong, not only the new styles.
- I4. The command palette and Rotate window: only the changes that D1/D2 need (Primary style on the main buttons). If they have other inline colours, I list them in the final report and do not fix them now.
Not in scope (I report it, the fix is your decision): the shared `asyncCommand` slot race in the other ModelessForms tools, the 2022 `#nullable` build error, and the duplicate PCF theme.
</improvements-to-touched-shared-code>

<order-of-work>
1. M10 test project + M1/M2/M3 with TDD (the data-loss fixes first).
2. M9 shared theme.
3. M4, M5, M6, M7.
4. M8 UI. Remove the old code.
5. Build 2025 and run the tests. Then a smoke test in Revit 2025: I need you for this, or I use the rvt-mcp tool if you allow it.
6. Publish: I will ask you before I run any deploy script, and I will show you what it does.
</order-of-work>

<fallbacks-i-add>
Rule 12 inventory. These are the only defaults in the new design:
- F1. Missing `FormatVersion` → treated as v0. This is part of your upgrade design.
- F2. A missing screenshot file shows as a text line in the PDF and as a "missing" item in the UI. It is visible, not silent.
No other fallbacks. All other failures show an error to the user.
</fallbacks-i-add>

<implementation-notes>
Added during the build (2026-11-24). These are the places where the code differs from the text above.
Status: APPROVED by user 2026-11-24 (all of N1–N12, the fallbacks F3 and F5–F8, and the colours listed below, as written).
- N1 (M1): the interface is `IIssueStore` with `IssueCollection` (not `Collection`, which conflicts with a system type). It also has `DeleteIssue` and a `StorageFolder` property.
- N2 (M4): `SelectAndShow` is `SelectElementsAsync(ids)` and returns the number of missing elements. `GetSelectionAsync` is `GetSelectedElementUniqueIdsAsync`.
- N3 (M6): `IDictation` has one event, `Faulted`. A running recording can fail with no method call in progress (network, key, microphone). An event is the only way to report that. The view model shows ONE message for it and then stops without a second message, so the double message box (the M6 goal) does not occur. The state is `IsRecording` (bool) in the service. The view model has the "stopping" state: one shared stop task. While it runs, the dictation button is disabled, and a close waits for the same stop. So the text is never added twice.
- N4 (M7): `PdfReport.Export(collection, screenshotPath, outputPath)` takes a `Func<string,string>` instead of the store, so the PDF module does not depend on storage.
- N5 (D10): the `.projitems` condition is `$(DefineConstants.Contains('REVIT2025'))`. The ribbon button, the command class and the shutdown call use `#if REVIT2025`.
- N6 (M10): `ModelessForms-2025.csproj` gets `RuntimeIdentifier=win-x64`. Revit does not read an add-in's `deps.json`, so the Azure Speech native DLL must be next to `ModelessForms.dll`. Side effect: 4 system DLLs in the output become their Windows builds instead of "not supported" stubs (System.Data.OleDb, System.Diagnostics.EventLog, System.Diagnostics.PerformanceCounter, System.Security.Cryptography.Pkcs). The `runtimes\` folder is gone. The published folder `X:\AutoCAD DRI - Revit\Addins\2025\ModelessForms` already has these same 4 Windows DLLs (byte-equal), so for the users this is no change.
- N7 (M5): one overlay per monitor (not one window across all monitors). One WPF window cannot have two DPIs. A selection is on one monitor.
- N8: Revit shutdown saves synchronously first (`CloseForShutdown`), because Revit can end before an asynchronous close completes. While Revit exits, a failed save is reported ("changes are lost"), not asked about: Revit closes in any case. Known limit: if Revit exits WHILE a dictation records, the text of that recording can be lost (stopping takes up to 10 s, and Revit does not wait).
- N9 (M10): `PDFsharp-MigraDoc` is pinned to 6.2.0, the same PdfSharp version as the deployed PDFExporter and RibbonPanel add-ins. Revit 2025 loads one copy per assembly name for all add-ins, so a different version breaks the add-in that loads second.
- N10: `IIssueStore.DeleteIssue` / `RemoveScreenshot` save first, then delete the image files. A file that cannot be deleted does not undo the saved change; it is returned and the user is told where it is.
- N11: if closing the window fails with an exception, the user is asked "Close anyway?" (danger dialog). The window neither closes silently nor traps the user.
Fallbacks found in the build (rule 12, need a ruling):
- F3. At start, when the last-used collection no longer exists, the first collection in the list opens. The same happens after a change of the storage folder.
- F4. RESOLVED: a screenshot file that exists but cannot be decoded shows "Cannot show image" (visible, not silent).
- F7. The upgrade v0→v1 changes null or missing text fields to "" (old files written by the net48 serializer can have null). A current-format file with null fields, bad numbers or bad screenshot names is NOT repaired: the load stops with a "damaged" message and the file is not changed.
- F8. If the close path itself fails (the themed dialog throws), the plain Windows message box shows the error. It prevents an exception that could end Revit.
- N12: TextBox and PasswordBox get an explicit themed Cut/Copy/Paste/Select all menu (`ContextMenu.TextEdit`). The built-in WPF menu uses private classes that implicit styles cannot reach.
Colours outside the ruleset palette (need a ruling): `Accent.Translucent` (accent at 25 % opacity, capture selection), `Overlay.Dim` (black at 50 %, capture overlay), and the older `Axis.X/Y/Z` and `Palette.Star` (Rotate window, command palette). `Bg.SurfaceLo` is now the ruleset BackgroundDeep #1E2124, the same as the window background.
- F5. PDFsharp font set-up: if another add-in already configured PDFsharp fonts in the Revit process (read the setting first, or installed its own FontResolver), that configuration is kept. If it cannot supply Arial / Courier New, the export shows an error.
- F6. Dark title bar: on a Windows version without the DWM attribute, the system title bar stays (cosmetic).

Final conformance review (2026-11-24, after commit 85d19e7). User rulings and changes:
- N13 (D8), APPROVED as is: description typing is saved 1 s after the last key press, not at once. All other changes save at once. A close, a collection change or a Revit exit saves the pending text first. Limit: a Revit crash less than 1 s after a key press loses that text.
- D9 FIXED: the title is now "#7 – first line", as the spec says (it was "#7  first line").
- F9, APPROVED: `"FormatVersion": null` is read as v0, the same as a missing field (F1).
- F7 is now stricter: EVERY field of a current-format file is required. A missing field is "damaged", and the file is refused and not changed. Before, a missing field got the C# default value, and the next save wrote that default over the lost data.
- An old (v0) file with a value of the wrong JSON type (for example a number for "Created") is refused with the "cannot be read" message. Before, the error was lost in the drop-down binding.
- Removed fallbacks (user ruling: change, do not approve):
  - After a collection is deleted, NO other collection opens. The user picks the next one. F3 is still for start-up and a folder change only.
  - The upgrade of the old settings file refuses a field that is missing, null or not text, with a message that names the field. Nothing is changed to "".
  - "New issue": a selected Revit element that cannot be found is an error message. It is not dropped silently.
  - Dictation does not start when there is no `Faulted` subscriber or no UI thread. Before, a recording error could be dropped.
- After the new settings are saved, the plain-text Azure key is blanked in the old Issue Tracker's settings file (`%APPDATA%\IssueTracker\settings.json`). If that fails, the user gets a message that names the file to delete.
- `AtomicFile`: if the temporary file cannot be removed after a failed write, BOTH errors are reported, and the write error is kept as the inner exception.
- Revit 2025 runs on .NET 8: no reference may ask for a .NET framework assembly above version 8.0. The ProtectedData package is removed because the framework supplies it, and the old 9.0.5 System.* packages are now 8.0.x (fix d46adb0).
</implementation-notes>

<answers>
Status: APPROVED by user 2026-11-24.
Q1. Publish target: the add-in must end in `X:\AutoCAD DRI - Revit\Addins\2025`. The user does not know if `Deploy-NorsynApps.ps1` is the correct publish path. I check the script and confirm with the user before I copy anything.
Q2. No smoke test in Revit. Release without live testing (no time). Compensate: code with maximum care, use more tests, and run extra independent reviews before the release.
</answers>
