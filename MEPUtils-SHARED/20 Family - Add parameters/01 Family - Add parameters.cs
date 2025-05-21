using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

using Microsoft.WindowsAPICodePack.Dialogs;
using MoreLinq;
using Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Input;
using dbg = Shared.Dbg;
using fi = Shared.Filter;
using lad = MEPUtils.CreateInstrumentation.ListsAndDicts;
using mp = Shared.MepUtils;
using tr = Shared.Transformation;
using Autodesk.Revit.Attributes;

namespace MEPUtils.FamilyTools.AddParameters
{
    [Transaction(TransactionMode.Manual)]
    public class Family_AddParameters : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            Document doc = commandData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;
            try
            {
                if (!doc.IsFamilyDocument) throw new Exception("Works only in family docs!");

                FamilyManager fm = doc.FamilyManager;

                uidoc.Application.Application.SharedParametersFilename =
                    @"X:\AutoCAD DRI - Revit\Shared parameters\DAMGAARD SHARED PARAMETERS.txt";
                DefinitionFile defFile = uidoc.Application.Application.OpenSharedParameterFile();
                DefinitionGroups groups = defFile.Groups;

#if REVIT2025
                ForgeTypeId idGp = GroupTypeId.IdentityData;
                var dimGp = GroupTypeId.Geometry;

                using (Transaction tx = new Transaction(doc))
                {
                    tx.Start("Add parameters");

                    List<(string Group, string ParName, ForgeTypeId gp, bool isInstance)> parNamesToAdd =
                        new List<(string Group, string ParName, ForgeTypeId gp, bool isInstance)>
                    {
                        ("900 SCHEDULE", "DRI.Management.Schedule DN-Number", idGp, true),
                        ("900 SCHEDULE", "DRI.Management.Schedule DN-Number2", idGp, true),
                        ("900 SCHEDULE", "DRI.Management.Schedule Funktion", idGp, false),
                        ("900 SCHEDULE", "DRI.Management.Schedule Aktuator", idGp, false),
                        ("900 SCHEDULE", "DRI.Management.Schedule Betjening", idGp, false),
                        ("900 SCHEDULE", "DRI.Management.Schedule Tilslutning", idGp, false),
                        ("900 SCHEDULE", "DRI.Management.Schedule Type", idGp, false),
                        ("900 SCHEDULE", "DRI.Management.Schedule Tryktrin", idGp, false),
                        ("900 SCHEDULE", "DRI.Management.Schedule Fabrikat", idGp, false),
                        ("900 SCHEDULE", "DRI.Management.Schedule Produkt", idGp, false),
                        ("100 MECHANICAL", "Component Name", idGp, false),
                        ("100 MECHANICAL", "Component Class1", idGp, false),
                        ("100 MECHANICAL", "Component Class2", idGp, false),
                        ("100 MECHANICAL", "Component Class3", idGp, false),
                        ("800 PED", "PED_ELEM_DIMO", idGp, true),
                        ("800 PED", "PED_ELEM_DIMO1", idGp, true),
                        ("800 PED", "PED_ELEM_MATERIAL", idGp, false),
                        ("800 PED", "PED_ELEM_MODEL", idGp, false),
                        ("800 PED", "PED_ELEM_SCHEDULE", idGp, false),
                        ("800 PED", "PED_ELEM_STANDARD", idGp, false),
                        ("800 PED", "PED_ELEM_TYPE", idGp, false),
                        ("800 PED", "PED_ELEM_THKT", idGp, true),
                        ("800 PED", "PED_ELEM_THKT1", idGp, true),
                    };

                    foreach (var pair in parNamesToAdd)
                    {
                        DefinitionGroup group = groups.get_Item(pair.Group);
                        Definitions defs = group.Definitions;
                        ExternalDefinition def = defs.get_Item(pair.ParName) as ExternalDefinition;
                        try
                        {
                            var parameter = fm.get_Parameter(pair.ParName);
                            if (parameter != null) continue;
                            fm.AddParameter(def, pair.gp, pair.isInstance);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(ex.Message);
                        }
                    }
                    tx.Commit();
                    return Result.Succeeded;
                }
#else
                var idGp = BuiltInParameterGroup.PG_IDENTITY_DATA;
                var dimGp = BuiltInParameterGroup.PG_GEOMETRY;

                using (Transaction tx = new Transaction(doc))
                {
                    tx.Start("Add parameters");

                    List<(string Group, string ParName, BuiltInParameterGroup gp, bool isInstance)> parNamesToAdd =
                        new List<(string Group, string ParName, BuiltInParameterGroup gp, bool isInstance)>
                    {
                        ("900 SCHEDULE", "DRI.Management.Schedule DN-Number", idGp, true),
                        ("900 SCHEDULE", "DRI.Management.Schedule DN-Number2", idGp, true),
                        ("900 SCHEDULE", "DRI.Management.Schedule Funktion", idGp, false),
                        ("900 SCHEDULE", "DRI.Management.Schedule Aktuator", idGp, false),
                        ("900 SCHEDULE", "DRI.Management.Schedule Betjening", idGp, false),
                        ("900 SCHEDULE", "DRI.Management.Schedule Tilslutning", idGp, false),
                        ("900 SCHEDULE", "DRI.Management.Schedule Type", idGp, false),
                        ("900 SCHEDULE", "DRI.Management.Schedule Tryktrin", idGp, false),
                        ("900 SCHEDULE", "DRI.Management.Schedule Fabrikat", idGp, false),
                        ("900 SCHEDULE", "DRI.Management.Schedule Produkt", idGp, false),
                        ("100 MECHANICAL", "Component Name", idGp, false),
                        ("100 MECHANICAL", "Component Class1", idGp, false),
                        ("100 MECHANICAL", "Component Class2", idGp, false),
                        ("100 MECHANICAL", "Component Class3", idGp, false),
                        ("800 PED", "PED_ELEM_DIMO", idGp, true),
                        ("800 PED", "PED_ELEM_DIMO1", idGp, true),
                        ("800 PED", "PED_ELEM_MATERIAL", idGp, false),
                        ("800 PED", "PED_ELEM_MODEL", idGp, false),
                        ("800 PED", "PED_ELEM_SCHEDULE", idGp, false),
                        ("800 PED", "PED_ELEM_STANDARD", idGp, false),
                        ("800 PED", "PED_ELEM_TYPE", idGp, false),
                        ("800 PED", "PED_ELEM_THKT", idGp, true),
                        ("800 PED", "PED_ELEM_THKT1", idGp, true),
                    };

                    foreach (var pair in parNamesToAdd)
                    {
                        DefinitionGroup group = groups.get_Item(pair.Group);
                        Definitions defs = group.Definitions;
                        ExternalDefinition def = defs.get_Item(pair.ParName) as ExternalDefinition;
                        try
                        {
                            var parameter = fm.get_Parameter(pair.ParName);
                            if (parameter != null) continue;
                            fm.AddParameter(def, pair.gp, pair.isInstance);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(ex.Message);
                        }
                    }
                    tx.Commit();
                    return Result.Succeeded;
                }
#endif
            }

            catch (Autodesk.Revit.Exceptions.OperationCanceledException) { return Result.Cancelled; }

            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
