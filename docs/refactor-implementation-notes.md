<document-info>
Implementation notes for the deep-modules refactor + WPF port of the PCF exporter,
executed 2026-06-10 per the user-approved plan (docs/pcf-exporter-review-and-wpf-port-plan.md)
and the user's revdiff annotations. Read this before touching the new code.
</document-info>

<what-was-built>
The shared project was restructured into deep modules (folders = modules = namespaces `PcfExporter.*`):

| Module | Interface(s) | Contents |
|---|---|---|
| Configuration | `IConfigurationStore` | `PcfConfiguration` (immutable-ish settings snapshot, typed enums), `FileConfigurationStore` (key=value file in `%AppData%\PCF-Exporter\settings.cfg`; hand-rolled on purpose — no JSON package = no Revit add-in assembly conflicts) |
| Context | `IRevitContext`, `IRevitExecutor` | `RevitContext` (fresh per operation — stale documents impossible), `RevitExecutor` (ExternalEvent queue marshalling modeless-UI work into valid Revit API context; cancels queued work on dispose) |
| ElementSource | `IElementSource` | Scope-based collection (was copy-pasted 3×), all export filters, diameter-limit predicate |
| Model | `IPcfElement` | Element classes (physical + virtual), `PcfElementFactory` (session-injected), `PcfElementTypes` constants, `ParameterRegistry` (single GUID authority; commented-out definitions kept on purpose), `ExportSession` (per-run state incl. lazy spindle lookup), `BrokenPipesGroup` (DORMANT — kept per user instruction, see file header) |
| Writer | `ISpecTables` | `EndpointWriter` (one core + historical facade overloads), `PcfFormat` (pure), `DocumentComposer`/`CiiWriter`/`Plant3DItemCodeWriter`/`FilenameBuilder`, `PipelineHeaderWriter`, `StartPointWriter`, `EndsAndConnectionsWriter`, `TapsWriter`, `TagWriter`, `SpecTables` |
| Services | `IParameterBindingService`, `IParameterPopulationService`, `IParameterReportService`, `IScheduleService` | Setup-tab verbs; return feedback strings/counts/tables, never show UI |
| Output | `IOutputWriter` | File writing with configured encoding |
| Orchestration | `IPcfExportService` | The full export workflow incl. the all-separate-files loop (was in a button handler) |
| App | — | Ribbon, `PcfExporterCommand` (opens the modeless window), `TapsCommand`, `PcfWindowController` |
| UI | `IDialogService` | `MainViewModel` (CommunityToolkit MVVM; config-mapped properties mirror `PcfConfiguration` 1:1 by name — enforced by tests), `MainWindow`/`MessageWindow`/`TableWindow`, `Theme.xaml` (full ControlTemplate retemplates incl. DataGrid, AutoCAD-style dark palette, implicit styles), `DarkTitleBar` (DWM PInvoke, palette-sourced), converters, `DialogService` |

NO COM (user decision 2026-06-10, reversing the earlier keep-COM ruling): the live-Excel
flows became `TableWindow` grid views (read-only DataGrids; Ctrl+C / "Copy all" produce
tab-separated text that pastes straight into Excel). Reason: the hosts must build with
`dotnet build` (RevitDevReload), which cannot process `COMReference` (MSB4803). The former
Excel module (`LiveExcel`) is deleted; the dead `DataHandler.READExcel` COM method in
revit-shared-utilities-shared (zero callers) is deleted too.

The window is **modeless** (long-standing user wish): commands `await` the `RevitExecutor`, which executes on Revit's UI thread via ExternalEvent; continuations resume on the WPF dispatcher.

Projects: `revit-pcf-exporter-FORMS` and `revit-pcf-exporter-WPF` are deleted (sln updated). 2022/2024 csproj were converted to SDK-style **net48** (per-version multi-project structure preserved — the user's explicit decision); 2025 stays SDK net8. A new `revit-pcf-exporter-tests` project (xunit v3, net8) references the 2025 host assembly.
</what-was-built>

<bugs-fixed>
- Wall thickness never exported: old spec-manager called `File.Exists` on embedded-resource names AND passed CSV *content* to `DataHandler.ReadCsvToDataTable(path,…)` (which expects a file path) — doubly broken. `SpecTables` parses the embedded CSVs directly; regression-tested.
- Stale `DocumentManager` singleton + static `SpindleDict` → replaced by per-run `RevitContext`/`ExportSession`.
- Inline duplicated parameter GUIDs (PCF_ELEM_SPEC, PCF_PIPL_EXCL) → registry references; a test fails if any registry GUID is hardcoded elsewhere.
- `double.Parse` crash on diameter-limit keystrokes → tolerant parse + visible validation error.
- `SetWallThicknessPipes` (hardcoded table, broken TryGetValue) deleted — superseded by the spec system (user confirmed).
- Config-store unescape order bug found by review (corrupted `C:\norsyn`-style paths) → single-pass unescape + regression tests.
</bugs-fixed>

<output-differences-vs-legacy>
The refactor reproduces the legacy PCF byte stream (verified by an adversarial review diffing against git history), with these KNOWN, intentional differences:
1. **PIPE records gain `    COMPONENT-ATTRIBUTE1 <wthk>`** for specs C02/C03/C08/S02/S03 — the wall-thickness feature finally works. NOTE: `COMPONENT-ATTRIBUTE1` is also the CII design-pressure keyword; with *Export to CII* enabled a PIPE record can contain both meanings. CII is unmaintained and off by default; revisit the keyword if CII is ever resurrected.
2. **Plant3D ITEM-CODE/ITEM-DESCRIPTION and CII blocks** are written again (they existed in the pre-OOP exporter but were lost in the old OOP model): ITEM-CODE when NOT exporting to Isogen; CII per element when enabled.
3. **OLET centre-point** uses the congruent-rectangle construction again (`PCF_OLET`); the old OOP model had silently downgraded OLET to TEE-STUB's plain projection.
4. **ATTRIBUTE59/filename** computed once per file (old code recomputed per pipeline; last one won — same result unless the clock ticked mid-export).
5. **Pipe diameter limit in inch mode** now rounds to 3 decimals (was 0 for pipes but 3 for fittings — inconsistent). Mm mode (default) unchanged.
6. CENTRE-POINT for valve/instrument midpoints keeps the historical TWO-decimal format (quirk preserved deliberately — see `EndpointWriter.WriteCP(Connector,Connector)`).
7. 3-way valve/instrument CP stays at the connector midpoint (current production behavior; pre-OOP code used the family-instance location).
</output-differences-vs-legacy>

<silent-fallbacks-removed>
Per the no-silent-fallback rule, these now fail loudly instead of skipping: missing PCF parameter during population (names + selects the element), missing shared parameter when building schedules, missing PipingSystemType in CII export, missing PCF_MAT_ID/DESCR in ITEM-CODE writing, orphaned/duplicate spindle-direction instances, empty/missing spec tables at load, missing ribbon icon resources at startup. Settings-save failures surface in the status bar + one dialog per session (cannot crash Revit from a binding setter).
</silent-fallbacks-removed>

<fallback-decisions-by-user>
Every remaining fallback path was explicitly ruled on by the user (2026-06-10), after the
agent initially added some without consulting (a rule-12 violation, since corrected):
1. **Malformed settings.cfg entry** → the property default is used, AND the affected key
   names are reported (status bar + dialog when the window opens). `IConfigurationStore.Load`
   returns `ConfigurationLoadResult` with `MalformedKeys` so silence is impossible by type.
2. **Invalid diameter-limit text** → persistence keeps the LAST VALID value (never a silent 0);
   the red validation error says so explicitly and export is blocked while invalid.
3. **Populate skips** (element type / pipeline with no workbook row) → legacy define-as-you-go
   workflow preserved, but the feedback dialog now lists the count AND names of skipped items.
4. **Unknown spec → no wall-thickness line** → sanctioned earlier by the user ("the spec check
   is just a gate"); not every PCF_ELEM_SPEC value has a CSV table.
5. **LDT path unset during Isogen export → LDT block omitted** → legacy behavior, retained
   unchanged (not ruled on; flag to the user if it ever bites).
All are regression-tested except 5.
</fallback-decisions-by-user>

<tests>
39 tests (xunit v3, run `PCF-Exporter.Tests.exe` or via VS Test Explorer; everything builds with plain `dotnet build` since the COM removal):
- Configuration round-trip by reflection (new settings can't forget persistence), malformed-line fallback, Windows-path escaping regressions.
- UI⇄engine sync: every `PcfConfiguration` property has a same-named `MainViewModel` property; load→VM→`BuildConfiguration()` round-trip; VM change → store persisted.
- XAML: every `{Binding}` path resolves on the VM; theme has implicit + Template-overriding styles for all used controls; zero inline styling; zero color literals in views.
- Factory/registry sync: every element-type constant handled by the factory; registry GUIDs unique and never duplicated in source.
- Writer: preamble golden text, filename builder, spec tables (incl. the regression), point formatting.
What is NOT covered (requires Revit running): everything touching live `Document`/`Element` — collectors, transactions, element record geometry. Manual smoke test in Revit required before relying on output (see open follow-ups).
</tests>

<open-follow-ups>
1. **Manual smoke test in Revit 2025**: open the window, run import-parameters → populate → export on a known project, diff the PCF against a pre-refactor export (expect only the documented differences above).
2. The deploy manifest `Revit_Piping_Analysis.addin` paths are machine-specific (X:); the PCF entry now points at the 2025 Release output and class `PcfExporter.App.App` — verify on the deploy machine.
3. 2022/2024 hosts compile, but runtime verification needs those Revit versions.
4. Theme: TabControl assumes top tab placement; ComboBox is non-editable-only; ProgressBar determinate-only (none of these are currently used otherwise).
5. Future Revit add-in isolation (2026+ AssemblyLoadContext) may break the relative pack URI for Theme.xaml — mitigation documented in the WPF review (code-constructed singleton dictionary).
</open-follow-ups>
