using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data;

namespace ModelessForms.GeometryValidator
{
    public class ConnectorValidationContainer
    {
        public string SystemToValidate { get; set; }
        public List<ConnectorValidationResult> ValidationResult { get; set; } 
            = new List<ConnectorValidationResult>();
    }

    public class ConnectorValidationResult
    {
        public double LongestDist;
        public List<(string coords, ElementId id)> Data = new List<(string coords, ElementId id)> ();
    }

    public delegate void EventHandler(object source, MyEventArgs e);
    public class MyEventArgs : EventArgs
    {
        private string EventInfo;
        public MyEventArgs(string Text) => EventInfo = Text;
        public string GetInfo() => EventInfo;
    }
    public static class EventHelper
    {
        public static void Raise(this EventHandler eventHandler, object sender, MyEventArgs args)
        {
            if (eventHandler == null) return;
            eventHandler(sender, args);
        }
    }
}
