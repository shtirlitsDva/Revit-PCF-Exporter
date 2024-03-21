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

                using (Transaction tx = new Transaction(doc))
                {
                    tx.Start("Add parameters");

                    List<(string Group, string ParName, BuiltInParameterGroup gp)> parNamesToAdd = 
                        new List<(string Group, string ParName, BuiltInParameterGroup gp)>
                    {
                        ("900 SCHEDULE", "DRI.Management.Schedule DN-Number", BuiltInParameterGroup.PG_IDENTITY_DATA),
                        ("900 SCHEDULE", "DRI.Management.Schedule DN-Number2", BuiltInParameterGroup.PG_IDENTITY_DATA),
                        ("900 SCHEDULE", "DRI.Management.Schedule Funktion", BuiltInParameterGroup.PG_IDENTITY_DATA),
                        ("900 SCHEDULE", "DRI.Management.Schedule Aktuator", BuiltInParameterGroup.PG_IDENTITY_DATA),
                        ("900 SCHEDULE", "DRI.Management.Schedule Betjening", BuiltInParameterGroup.PG_IDENTITY_DATA),
                        ("900 SCHEDULE", "DRI.Management.Schedule Tilslutning", BuiltInParameterGroup.PG_IDENTITY_DATA),
                        ("900 SCHEDULE", "DRI.Management.Schedule Type", BuiltInParameterGroup.PG_IDENTITY_DATA),
                        ("900 SCHEDULE", "DRI.Management.Schedule Tryktrin", BuiltInParameterGroup.PG_IDENTITY_DATA),
                        ("900 SCHEDULE", "DRI.Management.Schedule Fabrikat", BuiltInParameterGroup.PG_IDENTITY_DATA),
                        ("900 SCHEDULE", "DRI.Management.Schedule Produkt", BuiltInParameterGroup.PG_IDENTITY_DATA),
                        ("100 MECHANICAL", "Component Name", BuiltInParameterGroup.PG_IDENTITY_DATA),
                        ("100 MECHANICAL", "Component Class1", BuiltInParameterGroup.PG_IDENTITY_DATA),
                        ("100 MECHANICAL", "Component Class2", BuiltInParameterGroup.PG_IDENTITY_DATA),
                        ("100 MECHANICAL", "Component Class3", BuiltInParameterGroup.PG_IDENTITY_DATA),
                        ("800 PED", "PED_ELEM_DIMO", BuiltInParameterGroup.PG_CONSTRAINTS),
                        ("800 PED", "PED_ELEM_DIMO1", BuiltInParameterGroup.PG_CONSTRAINTS),
                        ("800 PED", "PED_ELEM_MATERIAL", BuiltInParameterGroup.PG_CONSTRAINTS),
                        ("800 PED", "PED_ELEM_MODEL", BuiltInParameterGroup.PG_CONSTRAINTS),
                        ("800 PED", "PED_ELEM_SCHEDULE", BuiltInParameterGroup.PG_CONSTRAINTS),
                        ("800 PED", "PED_ELEM_STANDARD", BuiltInParameterGroup.PG_CONSTRAINTS),
                        ("800 PED", "PED_ELEM_TYPE", BuiltInParameterGroup.PG_CONSTRAINTS),
                        ("800 PED", "PED_ELEM_THKT", BuiltInParameterGroup.PG_CONSTRAINTS),
                        ("800 PED", "PED_ELEM_THKT1", BuiltInParameterGroup.PG_CONSTRAINTS),
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
                            fm.AddParameter(def, pair.gp, false);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(ex.Message);
                        }
                    }

                    tx.Commit();
                    return Result.Succeeded;
                }
            }

            catch (Autodesk.Revit.Exceptions.OperationCanceledException) { return Result.Cancelled; }

            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
