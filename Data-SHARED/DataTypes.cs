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

namespace Data
{
    [SettingsSerializeAs(SettingsSerializeAs.Xml)]
    [Serializable]
    public class ParameterImpression : IEquatable<ParameterImpression>
    {
        public int ElementId { get; private set; }
        public int HashCode { get; private set; }
        public string ParameterType
        {
            get
            {
                if (IsShared) return "Shared Parameter";
                else if (IsBuiltIn) return "Built In Parameter";
                else return "";
            }
        }
        public BuiltInParameter BuiltInParameter { get; private set; }
        public bool IsBuiltIn { get; private set; }
        public bool IsShared { get; private set; }
        private Guid guid;
        public Guid Guid
        {
            get
            {
                if (IsShared) return guid;
                else throw new Exception("Access to GUID on a BuiltInParameter detected! Fix code!");
            }
            private set { guid = value; }
        }
        public string Name { get; private set; }
        public ParameterImpression() { }
        public ParameterImpression(Parameter p)
        {
            ElementId = p.Id.IntegerValue;
            IsShared = p.IsShared;
            if (p.IsShared) Guid = p.GUID;
            InternalDefinition definition = (InternalDefinition)p.Definition;
            Name = definition.Name;
            HashCode = p.Id.GetHashCode();
            if (definition.BuiltInParameter == BuiltInParameter.INVALID) IsBuiltIn = false;
            else
            {
                IsBuiltIn = true;
                BuiltInParameter = definition.BuiltInParameter;
            }
        }
        public bool Equals(ParameterImpression other)
        {
            if (other == null) return false;
            if (this.ElementId == other.ElementId &&
                this.HashCode == other.HashCode &&
                this.IsBuiltIn == other.IsBuiltIn &&
                this.IsShared == other.IsShared &&
                this.Name == other.Name) return true;
            else return false;
        }
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            ParameterImpression pi = obj as ParameterImpression;
            if (pi == null) return false;
            else return Equals(pi);
        }
        public static bool operator == (ParameterImpression pi1, ParameterImpression pi2)
        {
            if (((object)pi1) == null || ((object)pi2) == null) return object.Equals(pi1, pi2);
            return pi1.Equals(pi2);
        }
        public static bool operator != (ParameterImpression pi1, ParameterImpression pi2)
        {
            if (((object)pi1) == null || ((object)pi2) == null) return ! object.Equals(pi1, pi2);
            return ! pi1.Equals(pi2);
        }
    }

    [SettingsSerializeAs(SettingsSerializeAs.Xml)]
    public class Grouping
    {
        private List<ParameterImpression> parameterList;
        public List<ParameterImpression> ParameterList
        {
            get { return parameterList != null ? parameterList : new List<ParameterImpression>(); }
            set { parameterList = value; }
        }
        public Grouping() { }
        //public Grouping(List<ParameterImpression> parameterList)
        //{
        //    ParameterList = parameterList;
        //}
        //ParameterList = new List<ParameterImpression>();
    }
}
