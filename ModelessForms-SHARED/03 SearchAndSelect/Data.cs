using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.UI.Selection;
using Shared;

namespace Data
{
    public class ElementImpression
    {
        public int ElementId { get; private set; }
        public string FamilyName { get; private set; }
        public string TypeName { get; private set; }
        public string CategoryName { get; private set; }
        public int CategoryNumber { get; private set; }
        internal Grouping Grouping { get; private set; }
        internal List<string> Values { get; private set; }
        public ElementImpression(Element e, Grouping grouping)
        {
            ElementId = e.Id.IntegerValue;
            TypeName = e.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsValueString();
            FamilyName = e.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM).AsValueString();
            CategoryName = e.Category.Name;
            CategoryNumber = e.Category.Id.IntegerValue;
            Grouping = grouping;

            Values = new List<string>(Grouping.ParameterList.Count);

            foreach (ParameterImpression pi in Grouping.ParameterList)
            {
                Parameter par = null;
                if (pi.IsBuiltIn) par = e.get_Parameter(pi.BuiltInParameter);
                else if (pi.IsShared) par = e.get_Parameter(pi.Guid);
                if (par != null)
                {
                    string value = par.ToValueString2();
                    if (string.IsNullOrEmpty(value))
                    {
                        Values.Add("<+>");
                    }
                    else Values.Add(par.ToValueString2());
                }
                else Values.Add("<+>");
            }
        }
    }

    public class ParameterImpressionComparer : IEqualityComparer<ParameterImpression>
    {
        public bool Equals(ParameterImpression x, ParameterImpression y)
        {
            return null != x && null != y && x.ElementId == y.ElementId;
        }

        public int GetHashCode(ParameterImpression x)
        {
            return x.HashCode;
        }
    }

    //[SettingsSerializeAs(SettingsSerializeAs.Xml)]
    //public class Grouping
    //{
    //    private List<ParameterImpression> parameterList;
    //    public List<ParameterImpression> ParameterList
    //    {
    //        get { return parameterList != null ? parameterList : new List<ParameterImpression>(); }
    //        set { parameterList = value; }
    //    }
    //    public Grouping() {  }
    //    //public Grouping(List<ParameterImpression> parameterList)
    //    //{
    //    //    ParameterList = parameterList;
    //    //}
    //    //ParameterList = new List<ParameterImpression>();
    //}

    //public sealed class GroupingSettings
    //{
    //    [UserScopedSetting]
    //    [SettingsSerializeAs(SettingsSerializeAs.Xml)]
    //    [DefaultSettingValue("")]
    //    public Grouping GroupingSetting
    //    {
    //        get { return (Grouping)this["GroupingSetting"]; }
    //        set { this["GroupingSetting"] = value; }
    //    }
    //    public GroupingSettings() { }
    //    public GroupingSettings(Grouping grouping) { GroupingSetting = grouping; }
    //}

    public class ParameterTypeGroup
    {
        public string Name { get; set; }
        public BindingList<ParameterImpression> ParameterList { get; set; }
        public ParameterTypeGroup(string name, BindingList<ParameterImpression> parameterList)
        {
            Name = name; ParameterList = parameterList;
        }
    }
}
