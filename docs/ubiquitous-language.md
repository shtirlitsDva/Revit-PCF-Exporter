<document-info>
Ubiquitous language for the PCF Exporter. DRAFT extracted from the existing code/UI by Claude (2026-06-10) — needs ratification by the domain expert (you). Terms below are used verbatim in conversation, planning, code identifiers, and commit messages once ratified.
</document-info>

<domain-terms>
**PCF** — Piping Component File; the text format (ISOGEN/Alias) this add-in exports. One record per component, keyword-indented attributes.

**Pipeline** — one piping system, identified by its Revit *System Abbreviation* (e.g. `FVF`). Maps to `PIPELINE-REFERENCE` in PCF. In Revit terms: a `PipingSystemType`.

**System Abbreviation (SysAbbr)** — the Revit parameter that names a pipeline; the grouping key for everything in the export.

**Element** — a single exported component: pipe, fitting, accessory, or virtual element. Carries `PCF_ELEM_*` parameters.

**Element Type (PCF_ELEM_TYPE)** — the PCF component kind of an element: PIPE, ELBOW, TEE, FLANGE, FLANGE-BLIND, CAP, REDUCER-CONCENTRIC/-ECCENTRIC, COUPLING, UNION, OLET, TEE-STUB, VALVE, VALVE-3WAY, VALVE-ANGLE, INSTRUMENT, INSTRUMENT-3WAY, INSTRUMENT-DIAL, FILTER, GASKET, SUPPORT, FLOOR-SYMBOL, TAP, BOLT, MISC-COMPONENT, PIPE-BLOCK-FIXED. Drives the factory (`PcfElementFactory`).

**Physical element** — an exported element backed 1:1 by a Revit `Element` (`PcfPhysicalElement`).

**Virtual element** — an exported PCF record with **no own Revit element**, derived from physical ones (`PcfVirtualElement`): gasket implied by a flange's `Pakning` flag, field weld, iso split point, start point.

**Special (PCF_ELEM_SPECIAL)** — semicolon-separated markers on an element that spawn virtual elements: `START` (start point), `FW` (field weld), `SP` (iso split point).

**Tap / Tap connection** — a branch connection without a fitting; the tapping element connects onto the tapped element. Two mechanisms: legacy slots (`PCF_ELEM_TAP1..3`, set via the Taps ribbon command) and the TAP element type (`PCF_ELEM_TAPS`, UCI list).

**Olet** — a branch fitting welded onto a pipe; exported as TEE-STUB geometry with a computed center point on the host pipe axis.

**Spec (PCF_ELEM_SPEC / piping spec)** — the piping specification name of an element (e.g. `C02`, `EXISTING`). `EXISTING` elements are filtered out; `EXISTING-INCLUDE` elements export as dotted/undimensioned and excluded from the material list.

**Pipe spec table (PipeSpecs CSVs)** — embedded DN→wall-thickness tables per spec (`SpecManager`); source of `COMPONENT-ATTRIBUTE1` (wall thickness) on pipes.

**Material group** — elements grouped by identical `PCF_MAT_DESCR`; each group gets a `MATERIAL-IDENTIFIER` (PCF_MAT_ID) and a row in the MATERIALS section. Element instances get sequential `COMPONENT-IDENTIFIER` (PCF_ELEM_COMPID).

**Material description (PCF_MAT_DESCR)** — human-readable component description; for elbows it is suffixed with the bend angle to keep distinct angles in distinct material groups.

**Scope** — what gets exported: *all pipelines in one file*, *all pipelines in separate files*, *one specific pipeline*, or *current selection*.

**Diameter limit** — minimum nominal size; elements at/below it are excluded from export.

**Parameter domain** — which Revit object a PCF parameter binds to: ELEM (instances), PIPL (PipingSystemType), CTRL (exclusion flags PCF_ELEM_EXCL / PCF_PIPL_EXCL).

**Import / Populate / Export parameters** — the Setup workflow verbs: *Import* creates shared-parameter bindings in the project; *Populate* fills values from the Excel configuration workbook; *Export* dumps current values (or undefined families) to Excel.

**Elements workbook** — the Excel file with one row per Family+Type defining its ELEM parameter values (sheet `Elements`).

**LDT (workbook)** — the Excel file with per-pipeline title-block attributes for ISOGEN (sheet `Pipelines`, keyed by PROJECT-IDENTIFIER + LINE_NAME); written into the pipeline header as `ATTRIBUTE11..58`.

**PROJECT-IDENTIFIER (PCF_PROJID)** — the project key selecting rows in the LDT workbook.

**ISOGEN export** — export tuned for Plant 3D / ISOGEN iso generation: LDT attributes written, `ARGD` system excluded, ITEM-CODE entries omitted.

**CII export** — optional appended attributes for CAESAR II stress analysis (`PCF_*_CII_*` parameters). Noted in code as "not maintained".

**ARGD** — "Analysis Rigids" piping system; modeling aid, always excluded from ISOGEN export and parameter population.

**INSTR** — instrumentation pipes system abbreviation; always filtered out of exports.

**Pakning** (Danish: gasket) — Yes/No family parameter on flanges; when set, a virtual NN gasket is generated and the flange face is offset 1.5 mm.

**Continuation / Ends and connections** — per-pipeline records describing where a pipeline's free ends connect: `END-CONNECTION-PIPELINE` (another pipeline, or `EKSISTERENDE` for existing plant) and `END-CONNECTION-EQUIPMENT` (mechanical equipment, with TAG reference).

**Start point** — the pipeline's `START-CO-ORDS`; derived from the element marked `START` at its open/system-boundary connector. At most one per pipeline.

**Spindle direction** — cardinal direction of a valve spindle, read from a nested `Spindle direction` generic-model instance; exported as `SPINDLE-DIRECTION`.

**UCI** — Unique Component Identifier; the Revit `UniqueId`, written per element for traceability.

**SKEY (PCF_ELEM_SKEY)** — ISOGEN symbol key controlling the iso symbol used for a component.

**Preamble** — the PCF file header: units (`UNITS-BORE`, `UNITS-CO-ORDS`, weight/bolt units) and `ISOGEN-FILES`.
</domain-terms>

<naming-conventions-pending-ratification>
- Danish terms appearing in data/parameters (`Pakning`, `EKSISTERENDE`, `TEGN/KONTR/GODK` = drawn/checked/approved) stay as-is where they are project data contracts, but new code identifiers are English.
- Refactor identifiers should use these glossary terms: e.g. `ExportScope`, `Pipeline`, `MaterialGroup`, `VirtualElement`, `SpecTable` — not synonyms.
</naming-conventions-pending-ratification>
