<handoff>

<purpose>
Continuation doc for the next session (written 2026-06-11). Goal of the next
session: execute the shared-library split described in
`docs/shared-understanding/split-shared-utilities.md` (same folder as this
file — read it first, it contains the full verified analysis, the split
table, migration steps, and open questions).
</purpose>

<context-chain>
Why this work exists, shortest path:

1. DevReload (repo `shtirlitsDva/DevReload`) hot-loads Revit plugins via
   collectible ALC. Hot-loading `revit-pcf-exporter-2025` failed on FIRST
   load with "panel 'Tools' already exists".
2. Root cause (fully verified, not hypothesis): DevReload's
   `StartPluginApp` scans the plugin DLL for ALL `IExternalApplication`
   types and runs `appTypes[0]`. PCF-Exporter.dll embeds `Shared.App`
   (linked in via `revit-shared-utilities-SHARED.shproj`) in addition to
   its real entry `PcfExporter.App.App`. Normal Revit loading never runs
   `Shared.App` (the .addin manifest names only `FullClassName`), but
   DevReload picked it → it created ribbon panel "Tools" → collided with
   the panel already created at Revit startup by the machine-scope addin
   `C:\ProgramData\Autodesk\Revit\Addins\2025\NsRevitAddins-2025.addin`
   (entry `Shared.App` in `mg.revit-shared-utilities.dll`).
3. Fix direction chosen by user: split the shared library so plugin DLLs
   stop embedding the Analysis Tools add-in. See the proposal doc.
</context-chain>

<state-of-the-proposal>
The proposal was reviewed via revdiff on 2026-06-11; the user quit without
annotations (no objections recorded). The three open questions at the
bottom of the proposal were NOT explicitly answered — confirm them before
executing:

1. 3-way split (utilities / tools / app) vs 2-way (utilities / tools+app).
2. Naming of the new shprojs.
3. Deletion of legacy `revit-shared-utilities\` and `MEPUtils\` folders
   (have csprojs but are NOT in the solution — dead code).
</state-of-the-proposal>

<machine-state-warning>
On the home machine (H:), the Revit-PCF-Exporter working tree has
UNCOMMITTED in-flight changes that predate this analysis: modified
`Revit-PCF-Exporter.sln`, `Revit_Piping_Analysis.addin`, several
`revit-pcf-exporter-20XX.csproj`, and staged deletions of the whole
`revit-pcf-exporter-FORMS\` and `revit-pcf-exporter-WPF\` projects.
These were NOT committed with this handoff — if the next session runs on
a different machine, that state will be missing. Reconcile before doing
the split (the split touches the .sln and csprojs, so colliding with
those in-flight edits is likely).
</machine-state-warning>

<related-devreload-state>
DevReload repo: all session work committed and pushed (`eefdaf0` on
master — dark title bar, theme retemplates, DBG/REL toggle fix, settings
persistence, TargetInvocationException unwrapping, selectable log).
Tests 18/18 on net8 + net48. See that commit's message for details.

Two DevReload-side decisions remain OPEN (user deferred, then pivoted to
the library split):
1. Make plugin `OnStartup` failures best-effort (log + continue loading
   commands) instead of fatal.
2. Stop running `appTypes[0]` blindly when a DLL has multiple
   `IExternalApplication`s — add an explicit `AppClassName` to
   `RevitPluginEntry` / card UI. (`RevitPluginManager.StartPluginApp` is
   the relevant code.)
Even after the split these are worth doing: PCF's own
`PcfExporter.App.App.OnStartup` creates a "PCF Tools" ribbon panel, and
ribbon panels survive ALC unload → second hot-load in one session may
still collide.
</related-devreload-state>

<verification-for-the-split>
After implementing: build the full solution for 2022/2024/2025; verify
`mg.revit-shared-utilities.dll` still exposes `Shared.App` +
`Shared.FormCaller`; then E2E hot-load via DevReload:
`revit-cli.exe deploy --rvt 2025`, `start --rvt 2025 --watch-dialogs
--wait-pipe 300`, load the PCF plugin, confirm `PcfExporter.App.App` is
the app that runs (DevReload log now prints full exception detail on
failure). Scripted variant: `pwsh scripts/Test-RevitE2E.ps1 -RevitYear
2025` in the DevReload repo.
</verification-for-the-split>

<suggested-skills>
- `revdiff:revdiff` — review the split diff before committing (user's
  standard review flow; launch with a persistent Monitor).
- No grill needed: shared understanding is already captured in the
  proposal doc; only the three open questions need answers.
</suggested-skills>

</handoff>
