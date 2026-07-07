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
    /// selection around each element's own X/Y/Z axes, around the selection's
    /// common geometric centre using the world X/Y/Z axes, or around a user
    /// picked linear axis. The window is modeless, so the actual document changes
    /// run through an <see cref="ExternalEvent"/> in a valid API context.
    /// </summary>
    public enum RotateMode
    {
        AroundItself,
        AroundCommonCenter,
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
                    case RotateMode.AroundCommonCenter:
                        RotateAroundCommonCenter(doc, ids);
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

        /// <summary>
        /// Rotates the whole selection as a rigid group around its common geometric
        /// centre, using the world X/Y/Z axes. Unlike <see cref="RotateAroundItself"/>
        /// this works for any element (pipes, ducts, instances alike) because it moves
        /// the elements about a shared point rather than each element's own basis.
        /// </summary>
        private void RotateAroundCommonCenter(Document doc, ICollection<ElementId> ids)
        {
            double[] angles = { AngleX, AngleY, AngleZ };
            if (angles.All(a => a == 0.0)) return;

            XYZ center = ComputeSelectionCenter(doc, ids);
            if (center == null)
            {
                TaskDialog.Show("3D Rotate",
                    "Could not determine a geometric centre for the selection.");
                return;
            }

            // World axes through the fixed common centre. The centre does not move
            // between the three rotations, so they can be applied sequentially.
            XYZ[] worldAxes = { XYZ.BasisX, XYZ.BasisY, XYZ.BasisZ };

            using (Transaction t = new Transaction(doc, "Rotate around common center"))
            {
                t.Start();
                for (int i = 0; i < 3; i++)
                {
                    if (angles[i] == 0.0) continue;
                    Line axis = Line.CreateBound(center, center + worldAxes[i]);
                    ElementTransformUtils.RotateElements(doc, ids, axis, angles[i]);
                }
                t.Commit();
            }
        }

        /// <summary>
        /// The centre of the combined model-coordinate bounding box of every element
        /// in the selection. Returns null if none of them expose a bounding box.
        /// </summary>
        private static XYZ ComputeSelectionCenter(Document doc, ICollection<ElementId> ids)
        {
            double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;
            bool any = false;

            foreach (ElementId id in ids)
            {
                BoundingBoxXYZ bb = doc.GetElement(id)?.get_BoundingBox(null);
                if (bb == null) continue;

                any = true;
                minX = Math.Min(minX, bb.Min.X);
                minY = Math.Min(minY, bb.Min.Y);
                minZ = Math.Min(minZ, bb.Min.Z);
                maxX = Math.Max(maxX, bb.Max.X);
                maxY = Math.Max(maxY, bb.Max.Y);
                maxZ = Math.Max(maxZ, bb.Max.Z);
            }

            if (!any) return null;
            return new XYZ((minX + maxX) / 2.0, (minY + maxY) / 2.0, (minZ + maxZ) / 2.0);
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
                _externalEvent?.Dispose();
                _externalEvent = null;
                _handler = null;
            };

            _window.Show();
            return Result.Succeeded;
        }

        /// <summary>
        /// Terminate-time cleanup (MEPUtils App.OnShutdown): closing the window
        /// runs its Closed handler, which disposes the ExternalEvent and clears
        /// the static keepers so a DevReload reload starts clean instead of
        /// leaving a stale window running old code. Must run on the UI thread —
        /// DevReload's teardown executes in Revit API context, which is that thread.
        /// </summary>
        public static void Shutdown()
        {
            _window?.Close();
        }
    }
}
