using System;
using System.IO;
using System.Linq;
using System.Text;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;

using PcfExporter.Context;

using pdef = PcfExporter.Model.ParameterDefinition;
using plst = PcfExporter.Model.Parameters;

namespace PcfExporter.Services
{
    /// <summary>
    /// Creates and deletes the PCF shared-parameter bindings in the project.
    /// Returns human-readable feedback; never shows UI itself.
    /// </summary>
    public interface IParameterBindingService
    {
        string CreateAllBindings(IRevitContext ctx);
        string DeleteAllBindings(IRevitContext ctx);
    }

    public sealed class ParameterBindingService : IParameterBindingService
    {
        public string CreateAllBindings(IRevitContext ctx)
        {
            var feedback = new StringBuilder();
            feedback.Append(CreateElementBindings(ctx));
            feedback.Append(CreatePipelineBindings(ctx));
            return feedback.ToString();
        }

        private string CreateElementBindings(IRevitContext ctx)
        {
            Document doc = ctx.Doc;
            Application app = doc.Application;

            CategorySet catSet = app.Create.NewCategorySet();
            catSet.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_PipeCurves));
            catSet.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_PipeFitting));
            catSet.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_PipeAccessory));

            var query = plst.LPAll().Where(p =>
                p.Domain == Model.ParameterDomain.ELEM ||
                p.Name == "PCF_ELEM_EXCL");

            return WithTemporarySharedParameterFile(app, file =>
            {
                var sbFeedback = new StringBuilder();
                int count = 0;

                ctx.RunInTransaction("Bind element PCF parameters", () =>
                {
                    foreach (pdef parameter in query.ToList())
                    {
                        count++;
                        ExternalDefinition def = CreateDefinition(file, parameter, count);

                        BindingMap map = doc.ParameterBindings;
                        Binding binding = app.Create.NewInstanceBinding(catSet);

                        if (map.Contains(def))
                        {
                            sbFeedback.Append("Parameter " + parameter.Name + " already exists.\n");
                            continue;
                        }

                        map.Insert(def, binding, parameter.ParameterGroup);
                        if (map.Contains(def))
                        {
                            doc.Regenerate();
                            sbFeedback.Append("Parameter " + parameter.Name + " added to project.\n");
                            var spe = SharedParameterElement.Lookup(doc, def.GUID);
                            var internalDef = spe?.GetDefinition();
                            if (internalDef != null)
                            {
                                try { internalDef.SetAllowVaryBetweenGroups(doc, true); }
                                catch (Exception) { /* not applicable for all parameter types */ }
                            }
                        }
                        else sbFeedback.Append("Creation of parameter " + parameter.Name + " failed for some reason.\n");
                    }
                });

                return sbFeedback.ToString();
            });
        }

        private string CreatePipelineBindings(IRevitContext ctx)
        {
            Document doc = ctx.Doc;
            Application app = doc.Application;

            CategorySet catSet = app.Create.NewCategorySet();
            catSet.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_PipingSystem));

            var query = plst.LPAll().Where(p =>
                (p.Domain == Model.ParameterDomain.PIPL || p.Name == "PCF_PIPL_EXCL") &&
                p.ExportingTo != Model.ExportingTo.LDT);

            return WithTemporarySharedParameterFile(app, file =>
            {
                var sbFeedback = new StringBuilder();
                int count = 0;

                ctx.RunInTransaction("Bind pipeline PCF parameters", () =>
                {
                    foreach (pdef parameter in query.ToList())
                    {
                        count++;
                        ExternalDefinition def = CreateDefinition(file, parameter, count);

                        BindingMap map = doc.ParameterBindings;
                        Binding binding = app.Create.NewTypeBinding(catSet);

                        if (map.Contains(def))
                        {
                            sbFeedback.Append("Parameter " + parameter.Name + " already exists.\n");
                            continue;
                        }

                        map.Insert(def, binding, Model.Parameters.PipelineParameterGroup);
                        sbFeedback.Append(map.Contains(def)
                            ? "Parameter " + parameter.Name + " added to project.\n"
                            : "Creation of parameter " + parameter.Name + " failed for some reason.\n");
                    }
                });

                return sbFeedback.ToString();
            });
        }

        public string DeleteAllBindings(IRevitContext ctx)
        {
            var sbFeedback = new StringBuilder();
            ctx.RunInTransaction("Delete PCF parameters", () =>
            {
                foreach (pdef parameter in plst.LPAll())
                    RemoveSharedParameterBinding(ctx.Doc, parameter.Name, parameter.Type, sbFeedback);
            });
            return sbFeedback.ToString();
        }

        private static void RemoveSharedParameterBinding(
            Document doc, string name, ForgeTypeId type, StringBuilder sbFeedback)
        {
            BindingMap map = doc.ParameterBindings;
            DefinitionBindingMapIterator it = map.ForwardIterator();
            it.Reset();

            Definition def = null;
            while (it.MoveNext())
            {
                if (it.Key != null && it.Key.Name == name && type == it.Key.GetDataType())
                {
                    def = it.Key;
                    break;
                }
            }

            if (def == null) sbFeedback.Append("Parameter " + name + " does not exist.\n");
            else
            {
                map.Remove(def);
                sbFeedback.Append(map.Contains(def)
                    ? "Failed to delete parameter " + name + " for some reason.\n"
                    : "Parameter " + name + " deleted.\n");
            }
        }

        /// <summary>
        /// Shared parameters can only be created through a shared parameter file;
        /// a temporary file is swapped in and the user's file restored afterwards.
        /// </summary>
        private static string WithTemporarySharedParameterFile(
            Application app, Func<DefinitionFile, string> work)
        {
            string oriFile = app.SharedParametersFilename;
            string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()) + ".txt";
            using (File.Create(tempFile)) { }
            try
            {
                app.SharedParametersFilename = tempFile;
                DefinitionFile file = app.OpenSharedParameterFile();
                return work(file);
            }
            finally
            {
                app.SharedParametersFilename = oriFile;
                File.Delete(tempFile);
            }
        }

        private static ExternalDefinition CreateDefinition(DefinitionFile file, pdef parameter, int count)
        {
            var options = new ExternalDefinitionCreationOptions(parameter.Name, parameter.Type)
            {
                GUID = parameter.Guid
            };
            DefinitionGroup tempGroup = file.Groups.Create("TemporaryDefinitionGroup" + count);
            return tempGroup.Definitions.Create(options) as ExternalDefinition;
        }
    }
}
