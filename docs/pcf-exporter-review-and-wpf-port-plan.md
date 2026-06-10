<document-info>
Review of `revit-pcf-exporter-shared` + refactoring & WPF-port plan.
Author: Claude (autonomous session, 2026-06-10). Status: DRAFT — awaiting user review.
Scope: the PCF exporter only (shared, FORMS, WPF, per-version csproj). Other tools in the solution (NTR exporter, MEPUtils, …) are out of scope, but most findings apply to them too since they share the same patterns.
</document-info>

<executive-summary>
The PCF exporter works, but it is held together by global mutable state. Every architectural problem in the codebase traces back to one root cause: **configuration and Revit context flow through static fields** (`InputVars`, `Properties.Settings.Default`, `DocumentManager.Instance`) instead of being passed as arguments. This makes the code untestable, makes the UI and the engine invisibly coupled, and has already produced real defects (stale document, dead wall-thickness feature).

The good news: the codebase is small (~4,500 lines shared), the newer `PCFElementModel` OOP design is genuinely decent (interface + factory + polymorphic writers — you were already moving toward deep modules instinctively), and the UI surface is modest (~20 actions, ~25 settings). A full refactor to deep modules + WPF/MVVM is very achievable.

Proposed order of work:
1. **Phase 0 — Cleanup & bug fixes** (delete dead code, fix 2 confirmed bugs)
2. **Phase 1 — Kill global state** (introduce `PcfConfiguration` + `RevitContext`, mechanical sweep)
3. **Phase 2 — Deep modules** (split god-files into 6 modules behind interfaces)
4. **Phase 3 — WPF UI** (MVVM, CommunityToolkit, central Theme.xaml, full retemplating)
5. **Phase 4 — Tests** (UI↔engine sync tests, golden-file writer tests)

Several decisions are yours to make — listed in <open-questions> at the end.
</executive-summary>

<part-1-code-review>

<confirmed-bugs>
These are defects, not style issues. Per rule 4 I have **not** fixed anything — reporting first.

<bug-1-wall-thickness-never-exported>
**`spec-manager.cs:72`** — `SpecDataLoaderCSV.Load()` iterates embedded-resource names (e.g. `"PCF_Exporter.PipeSpecs.C02.csv"`) and then does:

```csharp
if (!File.Exists(resourceName)) continue;
```

`File.Exists` checks the **filesystem**, but these are manifest resource names, not paths — it returns false for every resource. Result: the spec dictionary is always empty, `GetWALLTHICKNESS` always returns `""`, and `COMPONENT-ATTRIBUTE1` (wall thickness) is silently never written for any pipe (`PCF_Pipe.cs:26`). The CSVs *are* correctly embedded (`revit-pcf-exporter-shared.projitems:73-77`), so the fix is simply deleting the `File.Exists` line. This is also a textbook example of why rule 12 (no silent fallbacks) exists — the empty-string fallback hid a total feature failure, possibly for years.
</bug-1-wall-thickness-never-exported>

<bug-2-stale-document-singleton>
**`DocumentManager.cs:15-21`** — `Initialize()` only assigns `Doc` if it is null:

```csharp
public void Initialize(Document document)
{
    if (Doc == null) { Doc = document; }
}
```

Revit add-ins live for the whole Revit session. Open project A, run the exporter, close A, open project B, run again → the singleton still holds project A's (now invalid) `Document`. Everything that reads `DocumentManager.Instance.Doc` (`PcfPhysicalElement`, `PcfVirtualElement`, `PcfElementFactory`) then operates on a disposed document — crash or, worse, wrong output.

Compounding it: **`PcfPhysicalElement.cs:28-34`** — `SpindleDict` is a `static` field whose initializer runs a `FilteredElementCollector` over that doc **once per Revit session** (at first use of the class). Spindles added after the first export are never seen; second-document scenarios crash. Also `.ToDictionary(x => x.SuperComponent.Id, …)` throws on a spindle without a super-component and on duplicate keys.
</bug-2-stale-document-singleton>

<bug-3-wallthk-dictionary-misses>
**`PCF_Functions.cs:829-833`** (`SetWallThicknessPipes`) — `pipeWallThk.TryGetValue(dia, out data)` return value is ignored; an unknown diameter sets the parameter to `null` instead of failing loudly. Two empty `if (element is Pipe) { }` / `if (element is FamilyInstance) { }` blocks sit just above (lines 819-827) — leftovers. (Note: this whole method may itself be dead — `WriteWallThickness`/`radioButton12` is read into `iv.WriteWallThickness` but I found no caller of `SetWallThicknessPipes`. Verify before fixing.)
</bug-3-wallthk-dictionary-misses>

<bug-4-fragile-ui-input>
**`Dark_PCF_Exporter_form.cs:408`** — `iv.DiameterLimit = double.Parse(darkTextBox22.Text);` on every keystroke: typing `"5."` or any non-numeric character throws an unhandled exception inside Revit. Also culture-sensitive (`,` vs `.`). Same pattern at line 130 on form load.
</bug-4-fragile-ui-input>

<bug-5-ncrash-on-missing-parameter>
**`PCF_EndsAndConnections.cs:124-125`** — `existingParameter.AsString()` NREs if the connected element lacks PCF parameters (e.g. a category the bindings were never applied to). The hard-coded GUID here is the same value as `Parameters.PCF_ELEM_SPEC` — see code-smells below.
</bug-5-ncrash-on-missing-parameter>
</confirmed-bugs>

<root-architectural-problem>
**Global mutable static state.** Three interlocking globals:

1. `InputVars` (`PCF_Functions.cs:31-94`) — a static settings bag. The WinForms form writes it field-by-field in event handlers; the export engine reads it from two dozen places. Nothing in any method signature tells you it depends on `InputVars.UNITS_BORE_MM`. Each unit is stored **three times** (`UNITS_BORE` string + `UNITS_BORE_MM` bool + `UNITS_BORE_INCH` bool) and the form must keep all three consistent by hand — 10 nearly identical radio-button handlers exist only to do this bookkeeping.

2. `PCF_Exporter.Properties.Settings.Default` — read **directly by the engine**, not just the UI: `PCF_Output.cs:19` chooses output encoding by reading `radioButton17UTF8_BOM`; `PCF_Pipeline.cs:56` reads `LDTPath`. Setting names are WinForms control names (`textBox5OutputPath`, `checkBox1Checked`) — the UI's private vocabulary has leaked through every layer down to the file writer. Renaming a button becomes a cross-cutting change.

3. `DocumentManager.Instance` — see bug 2.

Consequences: zero testability (you cannot call the exporter without standing up the whole static world inside Revit), invisible coupling (the "separate files" export works by the **form** mutating `iv.SysAbbr` in a loop — `Dark_PCF_Exporter_form.cs:291-298` — i.e. business workflow lives in a button handler), and an entire class of "works on my second run" bugs.

This is the keystone: fix this and testability, the WPF port, and the deep-module split all become straightforward. Fix anything else first and you'll be refactoring around a moving global target.
</root-architectural-problem>

<code-smells>
Reported per rule 3; locations given; no fixes applied.

<smell-two-parallel-export-engines>
The shared project contains **two complete element-writing implementations**: the old procedural writers (`PCF_Fittings.cs` 478 lines, `PCF_Pipes.cs`, `PCF_Accessories.cs`) and the new OOP `PCFElementModel` (factory + `ToPCFString()` polymorphism). `PCF_Main.cs` uses only the OOP model. The procedural writers are dead in the shared project — they are referenced only by the legacy `revit-pcf-exporter\` folder, which is **not in the solution** at all. ~900 lines of dead, confusing duplication.
</smell-two-parallel-export-engines>

<smell-god-file>
`PCF_Functions.cs` (942 lines) holds six unrelated things: `InputVars` (config), `Composer` (PCF text composition), `Filters` (element filtering), `EndWriter` (coordinate formatting), `ScheduleCreator` (Revit schedules), `ParameterDataWriter` + `BrokenPipesGroup` (misc). Classic grab-bag "Functions" file — each piece belongs to a different module.
</smell-god-file>

<smell-endwriter-copy-paste>
`EndWriter` has ~13 near-identical methods (`WriteEP1` ×3 overloads, `WriteEP2` ×3, `WriteEP3`, `WriteBP1`, `WriteCP` ×4, `WriteCO` ×4) differing only in keyword, end-parameter name, and whether bore/attributes are appended. One parameterized method (~20 lines) replaces ~290. Return types are inconsistent (`StringBuilder` vs `object` — `PCF_Functions.cs:346,381,501`).
</smell-endwriter-copy-paste>

<smell-duplicated-magic-guids>
Parameter GUIDs exist as named `ParameterDefinition`s in `PCF_ParameterData.cs`, yet are re-hardcoded inline: `Filters.PipingSystemAllowed` (`PCF_Functions.cs:282`, PCF_PIPL_EXCL) and `EndsAndConnections` (`PCF_EndsAndConnections.cs:124`, PCF_ELEM_SPEC). `PCF_ParameterData.cs:257` even carries the warning comment "If guid changes can break other methods!!" — the codebase knows.
</smell-duplicated-magic-guids>

<smell-triplicated-collector>
The scope-dependent element collection (the big `LogicalAndFilter`/`LogicalOrFilter` block keyed on `ExportAllOneFile`/`ExportAllSepFiles`/`ExportSelection`) is copy-pasted three times: `PCF_Main.cs:73-114`, `PCF_Parameters.cs:38-80`, `PCF_Parameters.cs:310-352`.
</smell-triplicated-collector>

<smell-excel-com-interop>
`PCF_Parameters.cs` writes Excel via COM Interop (`Microsoft.Office.Interop.Excel`): requires Excel installed on every machine, leaks COM objects (none are released; orphaned `EXCEL.EXE` processes), and forces a `COMReference` into every csproj. Reading already uses `ExcelDataReader` (good). Writing should use a file-based library (ClosedXML) — but note the current behavior intentionally opens a *visible live Excel window* for the user; replacing it changes UX → your call (see open questions).
</smell-excel-com-interop>

<smell-exception-handling>
`catch (Exception ex) { throw new Exception(ex.ToString()); }` (`PCF_Main.cs:457-460`, `PCF_Pipeline.cs:124-127`) destroys the exception type and stack trace. Deep engine code pops message boxes (`BuildingCoderUtilities.ErrorMsg/InfoMsg`) — presentation decisions made in the domain layer; the engine should throw/return results and let the UI decide how to show them.
</smell-exception-handling>

<smell-deferred-query-mutation>
`PopulateParameters.PopulateElementData` (`PCF_Parameters.cs:357-371`) builds LINQ queries over local variables (`eFamilyType`, `columnName`) and then mutates those variables inside a loop so deferred execution re-evaluates "correctly". It works, but it is a trap for every future reader (and `eQuery.First()` at line 416 throws if the family is missing from Excel). Same trick in `ExecuteMyCommand` where the `query` over `curDomain` is silently re-purposed for the second worksheet (`PCF_Parameters.cs:174-232`).
</smell-deferred-query-mutation>

<smell-static-form-fields>
`Dark_PCF_Exporter_form.cs:24-27` — `_commandData`, `_uiapp`, `_uidoc`, `_doc` are `static` on the form. Also `dataTableElements`/`dataTablePipelines` are `public static` and read from nowhere else. Instance state stored as statics = the same staleness hazard as bug 2.
</smell-static-form-fields>

<smell-naming-and-language>
Mixed Danish/English: `"Pakning"` (gasket) parameter lookups, `"EKSISTERENDE"` literal, `PCF_TEGN/KONTR/GODK`. File naming inconsistent (`spec-manager.cs` kebab-case vs PascalCase everywhere else). Namespaces are scattered (`PCF_Functions`, `PCF_Model`, `PCF_Exporter`, `SpecManager`, `GroupByCluster`, `Shared` in pcf files). Magic strings everywhere: element types (`"ELBOW"`, `"TAP"`…), specials (`"START"`, `"FW"`, `"SP"`), system filters (`"INSTR"`, `"ARGD"`), spec markers (`"EXISTING"`, `"EXISTING-INCLUDE"`) — all candidates for the ubiquitous-language glossary + constants/enums.
</smell-naming-and-language>

<smell-incomplete-gasket-feature>
`PCF_Fittings.cs:104-121,536-543` — gasket handling computes a modified position, assigns `var text = EndWriter.WriteEP1(...)` and **discards it**, then at the end writes only the literal line `"GASKET"` with no data. Half-finished feature inside dead code (the OOP model has `PCF_VIRTUAL_NN_GASKET` which appears to be the live replacement).
</smell-incomplete-gasket-feature>
</code-smells>

<dead-code-inventory>
Per rule 11 — flagged for deletion, your call. "Dead" = no live reference from any project in the `.sln`.

| Item | Evidence |
|---|---|
| `revit-pcf-exporter\` entire folder | Not in `.sln`; legacy pre-shared-project copy |
| `PCF_Fittings.cs`, `PCF_Pipes.cs`, `PCF_Accessories.cs` (shared) | Superseded by `PCFElementModel`; only the non-solution legacy folder calls them |
| `PCF_Supports.cs` (`SetSupportPipingSystem`) | Its only caller `SupportsCaller` is commented out (`App.cs:120-132`) |
| `BrokenPipesGroup` (`PCF_Functions.cs:850-1062`) | Only "caller" is the ~120-line commented `#region BrokenPipes` in `PCF_Main.cs:285-409` |
| `GroupByCluster.cs` | No references anywhere |
| `SharedStagingArea.cs` | Empty class |
| `ExportParameters.ExportUndefinedPipelines` (`PCF_Parameters.cs:248-284`) | Body is an empty `if` + commented block; UI handler `darkButton11_Click` is empty too |
| Commented-out regions | `PCF_Main.cs` old-filtering (~35 lines) + BrokenPipes (~120 lines); debug regions in `PCF_Fittings.cs` ×2 (~80 lines); commented parameter definitions in `PCF_ParameterData.cs`; assorted "Debugging" blocks in most files |
| `Output.OutputWriter` commented preamble, `PCF_Pipeline.cs` trailing commented debug block | Noise |
| `nul` file in repo root | Accidental redirect artifact |
| `Windows Logo.png`, `PCF_DEVELOPEMENT_01.xlsx` in shared project | Verify — look like leftovers; `LDT.xlsx` may be a live template |
| `revit-pcf-exporter-WPF\bin\`, `obj\`, and FORMS `bin\`/`obj\` committed to git | Build output in VCS; should be gitignored/removed |

Estimated total: ~1,200+ lines of dead code in the shared project alone.
</dead-code-inventory>

<what-is-good>
Worth saying explicitly, because the refactor should *preserve* these:

- **`PCFElementModel`** — `IPcfElement` + `PcfElementFactory` + abstract `PcfPhysicalElement`/`PcfVirtualElement` with `WriteSpecificData()` template method is a sound polymorphic design. This is the seed of the future PcfModel module.
- **`PCF_Filtering` + `FilterOptions`** — already an options-object pattern, easily testable once `InputVars` reads move out.
- **`ParameterDefinition`/`Parameters` registry** — declarative, reflection-enumerable (`LPAll()`), single source of truth for GUIDs (when not bypassed). Good bones.
- **`SpecManager`** — already interface-based (`ISpecRepository`, `ISpec`); just has the one fatal bug.
- The element-type → class mapping in the factory is exactly the kind of seam the sync-tests in Phase 4 can verify.
</what-is-good>
</part-1-code-review>

<part-2-target-architecture>

<deep-modules-design>
Six modules, each deep (small interface, substantial hidden implementation). Everything below the UI is **UI-framework-free** and, where possible, **Revit-free** (testable on plain .NET).

```
┌────────────────────────────────────────────────────────────┐
│ UI (WPF, MVVM)                 ViewModels + Views + Theme  │
└──────────────┬─────────────────────────────────────────────┘
               │ PcfConfiguration (immutable record)
┌──────────────▼─────────────────────────────────────────────┐
│ Orchestration   IPcfExportService / IParameterService      │
│                 runs: collect → filter → model → write → out│
└───┬──────────────┬──────────────┬──────────────┬───────────┘
    ▼              ▼              ▼              ▼
┌─────────┐  ┌───────────┐  ┌───────────┐  ┌───────────┐
│ Element │  │ PcfModel  │  │ PcfWriter │  │ Output    │
│ Source  │  │ (domain)  │  │ (pure)    │  │ (files)   │
└────┬────┘  └─────┬─────┘  └───────────┘  └───────────┘
     │             │              uses
┌────▼─────────────▼────┐  ┌───────────┐
│ RevitContext          │  │ Excel     │
│ (doc, uidoc, txns)    │  │ Gateway   │
└───────────────────────┘  └───────────┘
```

<module-1-configuration>
**Configuration** — replaces `InputVars` + direct `Settings.Default` reads.
- `PcfConfiguration` — immutable record with **typed, domain-named** properties: `ExportScope` (enum: AllInOneFile / AllSeparateFiles / SpecificPipeline / Selection), `BoreUnits` (enum Mm/Inch — one value, not three fields), `CoordsUnits`, `WeightUnits`, `WeightLengthUnits`, `OutputEncoding` (enum Ansi/Utf8Bom), `OutputDirectory`, `ElementsExcelPath`, `LdtPath`, `ProjectIdentifier`, `DiameterLimit` (double), `SpecFilter`, `ExportToIsogen`, `ExportToCii`, `Overwrite`, `SelectedSystemAbbreviation`.
- `IConfigurationStore` — `PcfConfiguration Load(); void Save(PcfConfiguration)`. Implementation: JSON file in `%AppData%\PCF-Exporter\settings.json` (replaces the WinForms `user.config` with its `radioButton17UTF8_BOM` names).
- The UI builds/edits a configuration; the engine receives it **as a constructor/method argument**. No engine code ever touches a settings store.
Interface size: 1 record + 2 methods. Depth: persistence, defaults, validation, migration all hidden.
</module-1-configuration>

<module-2-revit-context>
**RevitContext** — replaces `DocumentManager` and static `doc` properties.
- `IRevitContext` — `Document Doc`, `UIDocument UiDoc`, `ICollection<ElementId> Selection`, `T RunInTransaction<T>(string name, Func<T>)`.
- Constructed fresh per command invocation from `ExternalCommandData` and **passed down** — staleness becomes impossible by construction. `SpindleDict` becomes an instance-level lazy lookup built per export run.
</module-2-revit-context>

<module-3-element-source>
**ElementSource** — owns "which elements participate".
- `IElementSource` — `HashSet<Element> GetElements(ExportScope scope, string sysAbbr)`.
- Consolidates the three copy-pasted collector blocks + `PCF_Filtering`/`FilterOptions` (which moves here). `Filters.FilterDL` etc. live behind it.
</module-3-element-source>

<module-4-pcf-model>
**PcfModel (domain)** — the existing `PCFElementModel`, promoted.
- Keep `IPcfElement`, factory, physical/virtual hierarchy. Changes: inject `IRevitContext` + `PcfConfiguration` (no `DocumentManager`, no `InputVars`); make `IPcfElement` public; move element-type strings into a `PcfElementType` enum/constants shared with the factory (sync-testable).
- `ParameterDefinition`/`Parameters` registry stays here as the single GUID authority; the two inline-GUID bypasses are redirected to it.
</module-4-pcf-model>

<module-5-pcf-writer>
**PcfWriter (pure)** — all PCF text composition.
- `IPcfWriter` — `string ComposeDocument(PcfDocumentData data, PcfConfiguration cfg)`.
- Absorbs: `Composer` (preamble, materials, CII), `EndWriter` (collapsed to ~2 parameterized methods), `PCF_Pipeline.*` (pipeline header, filename, start point, ends/connections), `TapsWriter` output halves, `SpecManager`.
- Critical property: **no Revit types in its inputs** where feasible (points, sizes, parameter values arrive as plain data extracted by the model layer). This is what makes golden-file unit tests possible without Revit.
- Also fixes: filename/`iv.FullFileName` side-channel (`PCF_Pipeline_Filename.cs:54` currently smuggles the output path to `Output` via a global — becomes a return value).
</module-5-pcf-writer>

<module-6-parameter-and-excel>
**ParameterService + ExcelGateway**
- `IParameterService` — `CreateBindings()`, `DeleteBindings()`, `PopulateElements(table)`, `PopulatePipelines(table)`, `ExportToExcel()`, `ExportUndefined()`, `CreateSchedules()` — the Setup-tab verbs. Returns result objects (counts, messages); **no message boxes inside**.
- `IExcelGateway` — `DataSet Read(path)`, `void Write(path, tables)` — hides ExcelDataReader and (pending your decision) replaces COM Interop with ClosedXML.
- `Output` module: `IOutputWriter.Write(path, content, OutputEncoding)` — encoding from configuration, not from `Settings.Default`.
</module-6-parameter-and-excel>

<orchestration>
**PcfExportService** (the only thing UI export buttons call):
```csharp
ExportResult Export(IRevitContext ctx, PcfConfiguration cfg);
```
Owns the workflow currently spread between `PCF_Main.ExecuteMyCommand` and the form's `button6_Click` (including the all-separate-files loop, which moves out of the UI). Returns `ExportResult` (success/failures/written files) for the UI to present.
</orchestration>
</deep-modules-design>

<physical-layout>
Inside `revit-pcf-exporter-shared`, organized by module (namespaces follow folders, `PcfExporter.*`):
```
revit-pcf-exporter-shared/
  Configuration/   Context/   ElementSource/   Model/   Writer/
  Parameters/      Excel/     Output/          Orchestration/   App/
```
The per-version csproj (2022/2024/2025) stay as thin hosts. See open question 1 for the alternative single-csproj multi-targeting layout.
</physical-layout>
</part-2-target-architecture>

<part-3-wpf-port-plan>

<project-shape>
Your premise is correct: WPF XAML can live in the shared project (`.projitems` supports `<Page>` items), so **no separate UI project is needed** — Views, ViewModels, and `Theme.xaml` go into the shared project and compile per Revit version. The existing `revit-pcf-exporter-WPF` csproj then becomes redundant (it currently duplicates the 2025 host + links FORMS files; its `bin/obj` are committed). Plan: move its useful seeds (the started `PcfExporterViewModel`, window sketch) into the shared project, delete the WPF csproj, then delete `revit-pcf-exporter-FORMS` once parity is reached.

Caveats to accept: XAML designer support in shared projects is second-class (build-and-run preview instead), and CommunityToolkit.Mvvm + source generators require `LangVersion` ≥ 8 in the net48 hosts (works — toolkit targets netstandard2.0; the old-style 2022/2024 csproj need `<LangVersion>latest</LangVersion>` and the NuGet reference added to each host).
</project-shape>

<mvvm-structure>
- `MainWindow` (modal via `ShowDialog()` from `IExternalCommand`, same as today — modeless + ExternalEvent is deliberately **not** in scope; YAGNI until you ask for it).
- `MainViewModel` (CommunityToolkit `ObservableObject`) composed of section VMs mirroring today's tabs: `SetupViewModel` (parameter management/initialization, write mode, schedules), `ExportViewModel` (export actions, units, encoding, export-to toggles), `ScopeViewModel` (scope radios, pipeline combo, diameter limit, spec filter, project identifier), `HelpViewModel` (static content).
- VM properties map **1:1 to `PcfConfiguration` properties** (this exact correspondence is what Phase 4 tests verify). Radio-button groups bind to enums via a single `EnumToBoolConverter` — the 10 hand-written mutual-exclusion handlers disappear.
- Commands (`[RelayCommand]`) call `IPcfExportService`/`IParameterService` only — no engine logic in VMs beyond argument assembly and result presentation. The existing `PcfExporterViewModel` is a starting point but currently writes to `iv.*`/`Settings.Default` in property-changed handlers — that coupling goes away with Phase 1.
- `DiameterLimit` becomes a validated numeric binding (`ValidatesOnDataErrors`, culture-invariant) — fixes bug 4.
- Dialogs: file/folder pickers behind an `IDialogService` so VMs stay testable. net8 WPF has `OpenFolderDialog` built in; net48 hosts use the existing WindowsAPICodePack — hidden behind the same interface.
</mvvm-structure>

<theming>
Single `Theme.xaml` ResourceDictionary merged once at window level (Revit add-ins have no `App.xaml` — the window merges it; still "merged once").

- **Palette**: the neutral cool-gray AutoCAD-style palette from your global config (BackgroundDeep `#1E2124` → BackgroundHighlight `#4A5159`, TextPrimary `#E8E8E8`, AccentPrimary `#4A6B8A`, status colors) as named `SolidColorBrush` resources.
- **Full ControlTemplate retemplating** (not recoloring) for every control type the UI uses: `Button`, `RadioButton` (glyph + dot), `CheckBox` (box + check glyph), `TextBox`, `ComboBox` (**including dropdown Popup, toggle arrow, ComboBoxItem highlight**), `TabControl`/`TabItem` (tab chrome, selected/hover), `GroupBox` (header + border), `ScrollViewer`/`ScrollBar` (track, thumb, repeat buttons), `TextBlock`, `ToolTip`, `Separator`.
- **Implicit styles only** (no `x:Key` except named variants on top of implicit bases). **Zero inline styling** in views — enforced by a Phase 4 test.
- DarkUI (the WinForms dark lib) and `DarkRadioBox.cs` retire with the FORMS project.
</theming>

<ui-parity-checklist>
Every FORMS action must exist in WPF before FORMS is deleted (extracted from `Dark_PCF_Exporter_form.cs`):
import/delete parameter bindings; select Elements xlsx + LDT xlsx (persisted paths, auto-load tables); populate ELEM / PIPL parameters; export-undefined-elements; create schedules; export schedules to Excel; export PCF (all-one-file / all-separate / specific pipeline with combo populated from `GetDistinctPhysicalPipingSystemTypeNames` / selection); units radios ×4; encoding radios; overwrite/append; Isogen + CII checkboxes; diameter limit; spec filter; project identifier; output folder picker; help text with clickable links. Plus the conditional visibility (combo only when "specific pipeline").
</ui-parity-checklist>
</part-3-wpf-port-plan>

<part-4-test-plan>
"Tests to make sure the UI is in sync with the different parts of the app" — the drift points and how each is pinned. Test project: `revit-pcf-exporter-tests` (xUnit, net8.0-windows), referencing the shared code via the 2025 host or a dedicated test-compile; modules that are Revit-free (Configuration, Writer, theme/XAML checks) need no Revit at all.

<sync-tests>
1. **Configuration ⇄ persistence round-trip** — reflection over `PcfConfiguration` properties: serialize → deserialize → equal. Catches "added a setting, forgot persistence".
2. **ViewModel ⇄ Configuration sync** — convention test: every `PcfConfiguration` property has a corresponding VM property (name-mapped), and pushing a changed VM through `BuildConfiguration()` reflects every property. Catches "added a setting, forgot the UI" and vice versa.
3. **XAML ⇄ ViewModel binding validation** — parse all `.xaml` as XML, extract `{Binding Path}` expressions per view, resolve against the mapped VM type via reflection; fail on unknown paths. Catches typo'd/renamed bindings (WPF's silent-failure mode) without instantiating WPF.
4. **Theme coverage** — parse views: collect every control type used → assert `Theme.xaml` contains an implicit style **with a Template setter** for each; assert views contain zero `Background=`/`Foreground=`/`FontSize=`/`BorderBrush=`/`Padding=` attributes and zero inline `<Setter>` styling. Enforces the theming rules permanently.
5. **Factory ⇄ element-type sync** — every `PcfElementType` constant maps to a class in `PcfElementFactory` (call with each type, assert no `NotImplementedException`), and no unreachable cases.
6. **Parameter registry integrity** — `Parameters.LPAll()`: GUIDs unique, names unique, every CTRL/ELEM/PIPL parameter reachable from the binding-creation queries; grep-style test that no `new Guid("...")` literal in the codebase duplicates a registry GUID (kills the inline-GUID smell forever).
</sync-tests>

<engine-tests>
7. **PcfWriter golden files** — feed hand-built `PcfDocumentData` (plain data, no Revit) through `IPcfWriter`, compare against committed `.pcf` snapshots: preamble units variants, pipeline header, each element type's record shape, materials section, encoding. This is the safety net that lets the refactor proceed without a running Revit.
8. **SpecManager** — embedded CSVs load (regression for bug 1), known size → known wall thickness, unknown spec/size behavior **explicit** (throw or document — per no-silent-fallback rule, decide loudly).
9. **EndWriter formatting** — mm/inch point strings, rounding, invariant culture.
What stays untested (honestly): Revit-API-touching paths (collectors, transactions, bindings). Optionally coverable later with RevitTestFramework inside Revit — out of scope now.
</engine-tests>
</part-4-test-plan>

<part-5-phased-execution>
Each phase compiles + exports correctly before the next; golden-file tests (item 7) are created **early in Phase 2** against the *current* output so refactoring is verified against reality.

| Phase | Content | Risk |
|---|---|---|
| 0 | Delete dead code (inventory above, after your approval); fix bug 1 (spec-manager) + bug 3; gitignore bin/obj; remove `nul` | Low |
| 1 | `PcfConfiguration` + `IConfigurationStore` (JSON); `IRevitContext`; mechanical sweep replacing all `InputVars`/`Settings.Default`/`DocumentManager` reads; fixes bug 2 by construction | Medium — wide but mechanical |
| 2 | Capture golden files; split `PCF_Functions.cs`; consolidate `EndWriter`; extract the six modules + interfaces; orchestrator absorbs form workflow; exceptions/results instead of message boxes | Medium |
| 3 | WPF window + section VMs + `Theme.xaml` (full retemplate); parity checklist; delete FORMS + WPF-csproj + DarkUI | Medium |
| 4 | Test project: sync tests 1-6, engine tests 7-9 | Low |

Also in Phase 0: create `docs/ubiquitous-language.md` (glossary: PCF, pipeline/system abbreviation, ELEM/PIPL domains, spec, LDT, ISOGEN, CII, SKEY, tap, olet, virtual element, start point, field weld, split point, material group, COMPID…). It is a precondition for naming the modules consistently; I will draft it for your review.
</part-5-phased-execution>

<open-questions>
Decisions that are yours (rule: agent does not decide scope/strategy):

1. **Project structure**: (a) keep `shproj` + 3 host csproj and put WPF in the shared project (plan above, least disruption), or (b) modernize to a **single SDK-style csproj** multi-targeting net48/net8 with per-Revit-version configurations (today's standard for Revit add-ins, e.g. Nice3point template style; kills shproj weirdness entirely but restructures the build). I recommend (b) long-term but (a) keeps this refactor focused; (b) can be a later step.
2. **Revit version support**: must WPF UI + refactor support 2022/2024 (net48), or is 2025-only acceptable? Affects dialog code, LangVersion shims, and testing matrix.
3. **Settings migration**: migrate existing `user.config` values into the new JSON store once, or accept a one-time reset to defaults? (Reset is simpler; values are quick to re-enter.)
4. **Excel writing**: replace COM Interop with ClosedXML (file written + opened via shell) — accepting the UX change from "live Excel window appears" to "file is created and opened"? Strongly recommended (reliability), but it is a visible behavior change.
5. **Dead-code deletion**: approve the inventory wholesale, or item-by-item? Includes deleting the legacy `revit-pcf-exporter\` folder and the current `revit-pcf-exporter-WPF` project.
6. **`SetWallThicknessPipes` / `WriteWallThickness` setting**: appears dead (no caller found) — confirm it's abandoned or should be revived via SpecManager?
7. **Modal window confirmed?** Plan keeps `ShowDialog()` (modal) like today. Modeless (usable while working in Revit) requires ExternalEvent plumbing — only if you want it.
8. **Error UX**: plan replaces deep-code message boxes with result objects + a status area / summary dialog in WPF. OK?
</open-questions>
