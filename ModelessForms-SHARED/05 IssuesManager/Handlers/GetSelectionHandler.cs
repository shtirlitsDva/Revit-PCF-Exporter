using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace ModelessForms.IssuesManager.Handlers
{
    public class GetSelectionHandler : IAsyncCommand
    {
        public List<string> Guids { get; private set; }
        public Action<List<string>> Callback { get; set; }

        public GetSelectionHandler()
        {
            Guids = new List<string>();
        }

        public void Execute(UIApplication uiApp)
        {
            Guids = new List<string>();

            try
            {
                var uidoc = uiApp.ActiveUIDocument;
                if (uidoc == null)
                {
                    Callback?.Invoke(Guids);
                    return;
                }

                var doc = uidoc.Document;
                var selection = uidoc.Selection.GetElementIds();

                foreach (var elemId in selection)
                {
                    var elem = doc.GetElement(elemId);
                    if (elem != null)
                        Guids.Add(elem.UniqueId);
                }
            }
            catch
            {
            }

            Callback?.Invoke(Guids);
        }
    }

    public class SelectElementsHandler : IAsyncCommand
    {
        private readonly List<string> _guids;

        public SelectElementsHandler(List<string> guids)
        {
            _guids = guids ?? new List<string>();
        }

        public void Execute(UIApplication uiApp)
        {
            try
            {
                var uidoc = uiApp.ActiveUIDocument;
                if (uidoc == null) return;

                var doc = uidoc.Document;
                var ids = new List<ElementId>();

                foreach (var guid in _guids)
                {
                    var elem = doc.GetElement(guid);
                    if (elem != null)
                        ids.Add(elem.Id);
                }

                if (ids.Count > 0)
                {
                    uidoc.Selection.SetElementIds(ids);
                    uidoc.ShowElements(ids);
                }
            }
            catch
            {
            }
        }
    }
}
