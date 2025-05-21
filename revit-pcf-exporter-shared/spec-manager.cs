using System;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace SpecManager
{
    public static class SpecManager
    {
        private static ISpecRepository _repository;
        static SpecManager() => LoadPipeTypeData();
        private static void LoadPipeTypeData()
        {
            // Access the embedded resources in the assembly
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames(); // Lists all embedded resources

            // Filter to only CSV resources in the "PipeSpecs" folder
            var csvResources = new List<string>();
            foreach (var resourceName in resourceNames)
            {
                if (resourceName.Contains(".PipeSpecs.") && resourceName.EndsWith(".csv"))
                {
                    csvResources.Add(resourceName);
                }
            }

            // Load the CSV data from embedded resources
            _repository = new SpecRepository();
            _repository.Initialize(new SpecDataLoaderCSV().Load(csvResources));
        }
        public static string GetWALLTHICKNESS(string specName, string size)
        {
            ISpec spec = _repository.GetSpec(specName);
            if (spec == null) return "";
            //if (spec.HasSize(size)) return $"    WALL-THICKNESS {spec.GetWallThickness(size)}\n";
            if (spec.HasSize(size)) return $"    COMPONENT-ATTRIBUTE1 {spec.GetWallThickness(size)}\n";
            else return "";
        }
    }
    public interface ISpecRepository
    {
        void Initialize(Dictionary<string, ISpec> pipeTypeDict);
        ISpec GetSpec(string specName);
    }
    public class SpecRepository : ISpecRepository
    {
        private Dictionary<string, ISpec> _specDictionary = new Dictionary<string, ISpec>();
        public ISpec GetSpec(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            if (_specDictionary.ContainsKey(name)) return _specDictionary[name];
            else return null;
        }
        public void Initialize(Dictionary<string, ISpec> pipeTypeDict)
        {
            _specDictionary = pipeTypeDict;
        }
    }
    public class SpecDataLoaderCSV
    {
        public Dictionary<string, ISpec> Load(IEnumerable<string> resourceNames)
        {
            Dictionary<string, ISpec> dict = new Dictionary<string, ISpec>();
            var assembly = Assembly.GetExecutingAssembly();

            foreach (var resourceName in resourceNames)
            {
                if (string.IsNullOrEmpty(resourceName)) continue;

                // Read the embedded resource stream
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) continue;

                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string csvContent = reader.ReadToEnd();
                        string name = GetResourceFileNameWithoutExtension(resourceName); // Extract name

                        // Convert CSV content to a DataTable
                        DataTable table = Shared.DataHandler.ReadCsvToDataTable(csvContent, name);
                        dict.Add(name, new Spec(name, table));
                    }
                }
            }

            return dict;
        }

        private static string GetResourceFileNameWithoutExtension(string resourceName)
        {
            // Extract the file name from the resource name by splitting on dots
            var parts = resourceName.Split('.');

            // Assume the last part is the file extension, the second-to-last is the file name
            string fileName = parts[parts.Length - 2];
            return fileName;
        }
    }
    public interface ISpec{
        string Name { get; }
        bool HasSize(string size);
        string GetWallThickness(string size);
    }
    public class Spec : ISpec
    {
        private string _name;
        public string Name => _name;
        private Dictionary<string, string> _wthkDict;
        public bool HasSize(string size) => _wthkDict.ContainsKey(size);
        public string GetWallThickness(string size)
        {
            if (string.IsNullOrEmpty(size)) return "";
            if (_wthkDict.ContainsKey(size)) return _wthkDict[size];
            else return "";
        }
        public Spec(string name, DataTable table)
        {
            _name = name;
            _wthkDict = new Dictionary<string, string>();
            foreach (DataRow row in table.Rows) 
            { 
                _wthkDict.Add(row["DN"].ToString(), row["WTHK"].ToString());
            }
        }
    }
}