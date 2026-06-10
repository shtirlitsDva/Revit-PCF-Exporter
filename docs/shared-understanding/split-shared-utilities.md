<proposal-split-revit-shared-utilities>

<problem>
`revit-shared-utilities-SHARED.shproj` mixes two unrelated things:

1. **General utilities** — `01 Shared code\` (Filter/Output/Transformation/MepUtils in `Shared.cs`, `Utils.cs`, dialogs, `Reflector`, `LinqExtensions`) and `MoreLinq\`.
2. **A complete Revit add-in** ("Analysis Tools") — `App.cs` (ribbon `IExternalApplication` + `FormCaller` command), the `Tools` WinForms dialog (`Tools.cs/.designer.cs/.resx`, `ToolsClass.cs`), the tool implementations (`02 Connector position validation`, `03 Element coordinates`, `04 Get element by UCI`, `05 Get elements UCI`), `Settings.Designer.cs`, and the ribbon icons in `Resources\`.

Because 20 csprojs (PCF, NTR, MEPUtils, ModelessForms, PDFExporter, and the Analysis Tools host itself — ×2022/2024/2025) import this shproj, **every plugin DLL embeds `Shared.App`** — an `IExternalApplication` it never uses. Revit ignores it (the .addin manifest names the entry class), but DevReload scans the DLL for any `IExternalApplication` and ran it → "panel 'Tools' already exists" crash on first hot-load of PCF.
</problem>

<verified-facts>
- No consumer outside the shared library references the Tools dialog, `AnalysisTools`, or any class in folders 02–05. (MEPUtils' `InputBoxBasic`/`connectorSpatialGroup` hits are its own local copies, not references.)
- Dependency direction is strictly one-way: App/Tools/02–05 → `01 Shared code` + `MoreLinq`. No reverse references. (`Settings` hits in `Utils.cs` are Revit API `doc.Settings`, unrelated.)
- Only `revit-shared-utilities-2022/2024/2025.csproj` (producing `mg.revit-shared-utilities.dll`, the "Analysis Tools" add-in) needs the app/tools part.
</verified-facts>

<proposed-split>
Keep the existing shproj's identity as the utilities part — then the 17 consumer csprojs need **zero changes**:

| Project | Contents | Consumers |
|---|---|---|
| `revit-shared-utilities-SHARED` (existing, stripped) | `01 Shared code\`, `MoreLinq\` | all 20 csprojs, unchanged imports |
| `revit-shared-utilities-tools-SHARED` (new) | folders 02–05, `Tools.cs/.designer.cs/.resx`, `ToolsClass.cs`, `Settings.Designer.cs`, **`FormCaller` command (split out of App.cs into its own file)** | only `revit-shared-utilities-20XX` |
| `revit-shared-utilities-app-SHARED` (new) | `App.cs` (ribbon `IExternalApplication` only), `Resources\AnalysisTools16/32.png` | only `revit-shared-utilities-20XX` |

`revit-shared-utilities-2022/2024/2025.csproj` import all three shprojs → output DLL is byte-for-byte equivalent in behavior; the installed "Analysis Tools" add-in keeps working unchanged.

Why `FormCaller` moves to the tools project: it is the `IExternalCommand` entry to the dialog. With it separated from the ribbon `App`, a future dev-loadable variant (DevReload) can expose the Tools commands with **no** `IExternalApplication` in the assembly at all.

Why 3-way instead of 2-way (tools+app together): the app project is one file + two icons, and separating it is precisely what makes the assembly hot-load-clean. If you never intend to hot-load Analysis Tools itself, 2-way is simpler — your call.
</proposed-split>

<effects-on-devreload>
After the split, `PCF-Exporter.dll` contains exactly one `IExternalApplication` (`PcfExporter.App.App`) → DevReload runs the right one. Note its `OnStartup` still creates a "PCF Tools" ribbon panel; ribbon panels survive ALC unload, so a *second* hot-load in one session may still collide. The DevReload-side hardening (best-effort `OnStartup`, explicit app-class selection) remains recommended, but is now orthogonal.
</effects-on-devreload>

<migration-steps>
1. New shproj pair `revit-shared-utilities-tools-SHARED` + `revit-shared-utilities-app-SHARED` (folders, .shproj, .projitems), added to the solution.
2. Move the files listed above out of `revit-shared-utilities-SHARED.projitems` into the new projitems (resx/designer pairs move together). Split `FormCaller` out of `App.cs`.
3. Add the two new shproj imports to `revit-shared-utilities-2022/2024/2025.csproj`.
4. Build the full solution for all three Revit years; verify `mg.revit-shared-utilities.dll` still exposes `Shared.App` + `Shared.FormCaller` and PCF/NTR/MEPUtils/ModelessForms/PDFExporter build clean.
5. E2E: DevReload hot-load of `revit-pcf-exporter-2025` → confirm `PcfExporter.App.App` is the app that runs.
</migration-steps>

<dead-code-found>
Flagged per rule-11, separate commit if you agree to delete:
- `revit-shared-utilities\` (full legacy copy incl. own csproj) — not in the solution.
- `MEPUtils\` (same) — not in the solution; its local `InputBoxBasic`/`connectorSpatialGroup` copies are what made the grep noisy.
- `Revit_Piping_Analysis.addin` in repo root references `X:\GitHub\...` paths while the repo lives on `H:` — stale manifest template.
</dead-code-found>

<open-questions>
1. 3-way split as proposed, or 2-way (tools+app in one shproj)?
2. Naming OK (`revit-shared-utilities-tools-SHARED` / `-app-SHARED`), or prefer e.g. `analysis-tools-SHARED`?
3. Delete the legacy `revit-shared-utilities\` and `MEPUtils\` folders?
</open-questions>

</proposal-split-revit-shared-utilities>
