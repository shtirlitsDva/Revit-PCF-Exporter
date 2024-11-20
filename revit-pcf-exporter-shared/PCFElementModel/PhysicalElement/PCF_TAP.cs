using Autodesk.Revit.DB;

using System;
using System.Collections.Generic;
using System.Text;

using plst = PCF_Functions.Parameters;
using pdef = PCF_Functions.ParameterDefinition;
using System.Linq;
using Shared;
using mp = Shared.MepUtils;
using Autodesk.Revit.DB.Plumbing;

namespace PCF_Model
{
    internal class PCF_TAP : PcfPhysicalElement
    {
        public PCF_TAP(Element element) : base(element) {}

        public void ProcessTaps()
        {
            if (!Cons.Primary.IsConnected)
                throw new Exception($"TAP Element {Element.Id} is not connected on primary connector!");
            if (!Cons.Secondary.IsConnected)
                throw new Exception($"TAP Element {Element.Id} is not connected on secondary connector!");

            //Assume only one connection on each connector
            var refCons = mp.GetAllConnectorsFromConnectorSet(Cons.Primary.AllRefs);
            //Assume connected item on primary connector is a pipe
            var tappedPipeElement = refCons.First().Owner;
            if (!tappedPipeElement.IsType<Pipe>())
                throw new Exception($"TAP Element {Element.Id} is not connected to a pipe!");

            //Assume connected item on secondary is the tapping element
            refCons = mp.GetAllConnectorsFromConnectorSet(Cons.Secondary.AllRefs);
            var tappingElementUCI = refCons.First().Owner.UniqueId.ToLower();

            Parameter tapsPar = tappedPipeElement.LookupParameter("PCF_ELEM_TAPS");
            if (tapsPar == null)
                throw new Exception(
                    $"Parameter PCF_ELEM_TAPS not found on element {tappedPipeElement.Id}!\n" +
                    $"PCF_ELEM_TYPE TAP will not work without this parameter.\n" +
                    $"Either use another element type or import PCF parameters anew\n" +
                    $"to update parameter bindings.");
            string raw = tapsPar.AsString();
            if (raw.IsNoE()) tapsPar.Set(tappingElementUCI);
            else
            {
                var contents = raw.Split(';').Select(x => x.ToLower()).ToList();
                if (contents.Contains(tappingElementUCI)) return;
                contents.Add(tappingElementUCI);
                tapsPar.Set(string.Join(";", contents));
            }
        }

        protected override StringBuilder WriteSpecificData()
        {
            throw new System.Exception(
                $"TAP Element {Element.Id} encountered.\n" +
                $"These should be removed from the IPcfElement collection\n" +
                $"before processing the collection.\n" +
                $"Programming error!");
        }
    }
}
