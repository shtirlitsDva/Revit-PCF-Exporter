#nullable enable
using System;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using Shared;

namespace MEPUtils.App
{
    /// <summary>
    /// Connects/disconnects MEP connectors. The underlying helper returns void
    /// and expects the caller to own the transaction (as the legacy RibbonPanel
    /// host did), so we open one here.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [DevReloadButton(Text = "Cons",
        Tooltip = @"Zero elements selected -> Connect ALL unconnected connectors

One element selected -> Connect the element to adjacent
                        elements

One element selected + CTRL -> Disconnect the element

One element selected + SHIFT -> Connect a special pipe accessory
                                support if placed at connection

Two elements selected -> If disconnected - connect
                         If connected - disconnect

More than two elements selected + CTRL
                         -> Disconnect all selected elements",
        Icon16 = "ImgConnectConnectors16.png", Icon32 = "ImgConnectConnectors32.png",
        Panel = "MEP", Order = 0)]
    public class ConnectConnectorsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                Document doc = commandData.Application.ActiveUIDocument.Document;
                using (Transaction trans = new Transaction(doc))
                {
                    trans.Start("Connect the Connectors!");
                    MEPUtils.ConnectConnectors.ConnectTheConnectors(commandData);
                    trans.Commit();
                }
                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException) { return Result.Cancelled; }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }

    /// <summary>
    /// Toggles pipe-insulation visibility in the current view. Helper returns
    /// void and needs a caller-owned transaction.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [DevReloadButton(Text = "Visible",
        Tooltip = "Toggle visibility of pipe insulation in current view.",
        Icon16 = "ImgPipeInsulationVisibility16.png", Icon32 = "ImgPipeInsulationVisibility32.png",
        Panel = "MEP", Order = 1)]
    public class PipeInsulationVisibilityCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                Document doc = commandData.Application.ActiveUIDocument.Document;
                using (Transaction trans = new Transaction(doc))
                {
                    trans.Start("Toggle Pipe Insulation visibility!");
                    MEPUtils.PipeInsulationVisibility.TogglePipeInsulationVisibility(commandData);
                    trans.Commit();
                }
                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException) { return Result.Cancelled; }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }

    /// <summary>Places pipe supports. Helper manages its own transactions and returns a Result.</summary>
    [Transaction(TransactionMode.Manual)]
    [DevReloadButton(Text = "Supports",
        Tooltip = "Place supports on selected pipes.",
        Icon16 = "ImgPlaceSupport16.png", Icon32 = "ImgPlaceSupport32.png",
        Panel = "MEP", Order = 2)]
    public class PlaceSupportsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return MEPUtils.PlaceSupport.PlaceSupport.StartPlaceSupportsProcedure(commandData);
        }
    }

    /// <summary>
    /// Populates PED data on Olets. The helper is an instance void method; the
    /// transaction group/transaction scaffolding is owned here (as it was in the
    /// legacy host).
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [DevReloadButton(Text = "PED",
        Tooltip = "LMB: Append PED data.\nCtrl+LMB: Overwrite PED data.",
        Icon16 = "ImgPED16.png", Icon32 = "ImgPED32.png",
        Panel = "MEP", Order = 3)]
    public class PedCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            using (TransactionGroup txGp = new TransactionGroup(doc))
            {
                txGp.Start("Initialize PED data");

                using (Transaction trans = new Transaction(doc))
                {
                    trans.Start("Populate Olets");
                    var ped = new MEPUtils.PED.InitPED();
                    ped.processOlets(commandData);
                    trans.Commit();
                }

                txGp.Assimilate();
            }

            return Result.Succeeded;
        }
    }

    /// <summary>Sets the Mark parameter on selected elements. Helper returns a Result.</summary>
    [Transaction(TransactionMode.Manual)]
    [DevReloadButton(Text = "Mark",
        Tooltip = "Sets the mark of selected element(s).",
        Icon16 = "ImgSetMark16.png", Icon32 = "ImgSetMark32.png",
        Panel = "MEP", Order = 5)]
    public class SetMarkCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return MEPUtils.SetMark.SetMark.SetMarkExecute(commandData);
        }
    }

    /// <summary>Opens the modeless 3D-rotation window. Helper returns a Result.</summary>
    [Transaction(TransactionMode.Manual)]
    [DevReloadButton(Text = "3D Rotate",
        Tooltip = "Rotate selected element(s) around their own X/Y/Z axes,\nor around a picked linear axis. Opens a modeless window.",
        Icon16 = "Img3DRotate16.png", Icon32 = "Img3DRotate32.png",
        Panel = "MEP", Order = 6)]
    public class Element3DRotationCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return MEPUtils.Element3DRotation.Element3DRotationApp.Launch(commandData);
        }
    }
}
