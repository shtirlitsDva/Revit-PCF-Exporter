using System;
using System.IO;
using System.Collections.Generic;
using System.Data;

namespace SpecManager
{
    public static class SpecManager
    {
        private static ISpecRepository _repository;
        static SpecManager() => LoadPipeTypeData();
        private static void LoadPipeTypeData()
        {
            var paths = new List<string>()
            {
                @"X:\AC - Iso\PipeSpecs",
                @"C:\1\Norsyn\AC - Iso\PipeSpecs"
            };

            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    var csvs = Directory.EnumerateFiles(
                        path, "*.csv", SearchOption.TopDirectoryOnly);

                    _repository = new SpecRepository();
                    _repository.Initialize(new SpecDataLoaderCSV().Load(csvs));
                }
            }
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
        public Dictionary<string, ISpec> Load(IEnumerable<string> paths)
        {
            Dictionary<string, ISpec> dict = new Dictionary<string, ISpec>();
            foreach (var path in paths)
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                DataTable table = Shared.DataHandler.ReadCsvToDataTable(path, name);
                dict.Add(name, new Spec(name, table));
            }

            return dict;
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