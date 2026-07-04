using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Interop;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace MEPUtils.Element3DRotation
{
    /// <summary>
    /// C# port of pyRevitMEP "3D Rotate" (Element3DRotation). Rotates the current
    /// selection either around each element's own X/Y/Z axes, or around a user
    /// picked linear axis. The window is modeless, so the actual document changes
    /// run through an <see cref="ExternalEvent"/> in a valid API context.
    /// </summary>
    public enum RotateMode
    {
        AroundItself,
        AroundAxis
    }

    /// <summary>Only allows elements whose location is a straight line (usable as an axis).</summary>
    public class AxisSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element element)
            => (element.Location as LocationCurve)?.Curve is Line;

        public bool AllowReference(Reference reference, XYZ position) => false;
    }

    /// <summary>
    /// Handles the rotation request raised by <see cref="RotateOptionsWindow"/>.
    /// The window fills in the mode and the angles (already in Revit internal
    /// units, i.e. radians) before raising the external event.
    /// </summary>
    public class RotateRequestHandler : IExternalEventHandler
    {
        public RotateMode Mode { get; set; }

        // Angles in internal units (radians).
        public double AngleX { get; set; }
        public double AngleY { get; set; }
        public double AngleZ { get; set; }
        public double AngleAxis { get; set; }

        public void Execute(UIApplication app)
        {
            try
            {
                UIDocument uidoc = app.ActiveUIDocument;
                if (uidoc == null) return;
                Document doc = uidoc.Document;

                ICollection<ElementId> ids = uidoc.Selection.GetElementIds();
                if (ids.Count == 0)
                {
                    TaskDialog.Show("3D Rotate", "Select one or more elements first.");
                    return;
                }

                if (doc.IsWorkshared)
                    WorksharingUtils.CheckoutElements(doc, ids);

                switch (Mode)
                {
                    case RotateMode.AroundItself:
                        RotateAroundItself(doc, ids);
                        break;
                    case RotateMode.AroundAxis:
                        RotateAroundAxis(uidoc, doc, ids);
                        break;
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                // User cancelled the axis pick - nothing to do.
            }
            catch (Exception ex)
            {
                TaskDialog.Show("3D Rotate - Error", ex.Message);
            }
        }

        public string GetName() => "Element 3D Rotation";

        private void RotateAroundItself(Document doc, ICollection<ElementId> ids)
        {
            double[] angles = { AngleX, AngleY, AngleZ };
            if (angles.All(a => a == 0.0)) return;

            var skipped = new List<ElementId>();

            using (Transaction t = new Transaction(doc, "Rotate around itself"))
            {
                t.Start();
                foreach (ElementId id in ids)
                {
                    // Only instances expose a transform (own basis). Pipes, ducts,
                    // etc. cannot be rotated around their own axes this way.
                    Transform tf = (doc.GetElement(id) as Instance)?.GetTransform();
                    if (tf == null)
                    {
                        skipped.Add(id);
                        continue;
                    }

                    for (int i = 0; i < 3; i++)
                    {
                        if (angles[i] == 0.0) continue;
                        Line axis = Line.CreateBound(tf.Origin, tf.Origin + tf.get_Basis(i));
                        ElementTransformUtils.RotateElement(doc, id, axis, angles[i]);
                    }
                }
                t.Commit();
            }

            if (skipped.Count > 0)
                TaskDialog.Show("3D Rotate",
                    $"{skipped.Count} element(s) have no instance transform " +
                    "(only family instances can rotate around their own axes) and were skipped.");
        }

        private void RotateAroundAxis(UIDocument uidoc, Document doc, ICollection<ElementId> ids)
        {
            Reference reference = uidoc.Selection.PickObject(
                ObjectType.Element,
                new AxisSelectionFilter(),
                "Select a linear element to use as rotation axis");

            Line axis = (doc.GetElement(reference).Location as LocationCurve)?.Curve as Line;
            if (axis == null)
            {
                TaskDialog.Show("3D Rotate", "Selected element does not provide a straight axis.");
                return;
            }

            using (Transaction t = new Transaction(doc, "Rotate around axis"))
            {
                t.Start();
                ElementTransformUtils.RotateElements(doc, ids, axis, AngleAxis);
                t.Commit();
            }

            uidoc.Selection.SetElementIds(ids);
        }
    }

    /// <summary>
    /// Entry point used by the ribbon command. Shows the modeless rotation window
    /// (or re-focuses it if already open) and owns the single shared external event.
    /// </summary>
    public static class Element3DRotationApp
    {
        private static RotateOptionsWindow _window;
        private static ExternalEvent _externalEvent;
        private static RotateRequestHandler _handler;

        public static Result Launch(ExternalCommandData commandData)
        {
            UIApplication uiapp = commandData.Application;

            if (_window != null)
            {
                _window.Activate();
                return Result.Succeeded;
            }

            _handler = new RotateRequestHandler();
            _externalEvent = ExternalEvent.Create(_handler);
            _window = new RotateOptionsWindow(uiapp, _externalEvent, _handler);

            // Parent to the Revit main window so the modeless window behaves.
            new WindowInteropHelper(_window) { Owner = uiapp.MainWindowHandle };

            _window.Closed += (s, e) =>
            {
                _window = null;
                _externalEvent = null;
                _handler = null;
            };

            _window.Show();
            return Result.Succeeded;
        }
    }
}
