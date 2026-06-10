using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;

using PcfExporter.Configuration;
using PcfExporter.Context;
using PcfExporter.Writer;

using Shared;

namespace PcfExporter.Model
{
    /// <summary>
    /// Everything one export run needs, created fresh per run and passed down by
    /// constructor injection. Replaces the old global statics (InputVars,
    /// DocumentManager, static spindle cache) — and with them the whole class of
    /// stale-document bugs.
    /// </summary>
    public sealed class ExportSession
    {
        public IRevitContext Revit { get; }
        public PcfConfiguration Cfg { get; }
        public EndpointWriter EW { get; }
        public ISpecTables Specs { get; }
        public Document Doc => Revit.Doc;

        private Dictionary<ElementId, FamilyInstance> _spindles;

        public ExportSession(IRevitContext revit, PcfConfiguration cfg, ISpecTables specs)
        {
            Revit = revit ?? throw new ArgumentNullException(nameof(revit));
            Cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
            Specs = specs ?? throw new ArgumentNullException(nameof(specs));
            EW = new EndpointWriter(cfg);
        }

        /// <summary>
        /// Spindle-direction family instances keyed by their host (super component) id.
        /// Built lazily once per session — never cached across documents or runs.
        /// </summary>
        public IReadOnlyDictionary<ElementId, FamilyInstance> Spindles
        {
            get
            {
                if (_spindles == null)
                {
                    var spindles = new FilteredElementCollector(Doc)
                        .OfCategory(BuiltInCategory.OST_GenericModel)
                        .OfClass(typeof(FamilyInstance))
                        .Cast<FamilyInstance>()
                        .Where(x => x.FamilyAndTypeName() == "Spindle direction: Spindle direction")
                        .ToList();

                    //Modeling errors must be loud: a spindle with no host or a valve
                    //with two spindles would otherwise export an arbitrary direction.
                    var orphans = spindles.Where(x => x.SuperComponent == null).ToList();
                    if (orphans.Count > 0)
                        throw new InvalidOperationException(
                            "Spindle direction instance(s) without a host element: " +
                            string.Join(", ", orphans.Select(x => x.Id.ToString())));

                    var duplicates = spindles.GroupBy(x => x.SuperComponent.Id)
                        .Where(g => g.Count() > 1).ToList();
                    if (duplicates.Count > 0)
                        throw new InvalidOperationException(
                            "Multiple spindle direction instances on the same host element: " +
                            string.Join(", ", duplicates.Select(g => g.Key.ToString())));

                    _spindles = spindles.ToDictionary(x => x.SuperComponent.Id, x => x);
                }
                return _spindles;
            }
        }
    }
}
