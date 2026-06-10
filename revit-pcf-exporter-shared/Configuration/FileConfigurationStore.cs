using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PcfExporter.Configuration
{
    /// <summary>
    /// Persists <see cref="PcfConfiguration"/> as a simple key=value file in %AppData%\PCF-Exporter.
    /// A hand-rolled format is used deliberately: Revit add-ins share one AppDomain/process with
    /// every other installed add-in, and bringing a JSON package along (System.Text.Json or
    /// Newtonsoft on net48) is a known source of assembly version conflicts. The format is
    /// reflection-driven, so adding a property to PcfConfiguration is automatically persisted —
    /// the round-trip test in the test project guards this.
    /// </summary>
    public sealed class FileConfigurationStore : IConfigurationStore
    {
        private readonly string _path;

        public FileConfigurationStore() : this(DefaultPath()) { }
        public FileConfigurationStore(string path) => _path = path;

        public static string DefaultPath() => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PCF-Exporter", "settings.cfg");

        private static IEnumerable<PropertyInfo> PersistedProperties() =>
            typeof(PcfConfiguration)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite && p.CanRead);

        public ConfigurationLoadResult Load()
        {
            var cfg = new PcfConfiguration();
            var malformed = new List<string>();
            if (!File.Exists(_path)) return new ConfigurationLoadResult(cfg, malformed);

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string line in File.ReadAllLines(_path))
            {
                int idx = line.IndexOf('=');
                if (idx <= 0) continue;
                values[line.Substring(0, idx)] = Unescape(line.Substring(idx + 1));
            }

            foreach (PropertyInfo p in PersistedProperties())
            {
                if (!values.TryGetValue(p.Name, out string raw)) continue;
                if (p.PropertyType == typeof(string))
                {
                    p.SetValue(cfg, raw);
                    continue;
                }
                object parsed = Parse(raw, p.PropertyType);
                if (parsed != null) p.SetValue(cfg, parsed);
                else malformed.Add(p.Name); //default stays in place — reported, not silent
            }
            return new ConfigurationLoadResult(cfg, malformed);
        }

        public void Save(PcfConfiguration configuration)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            var lines = PersistedProperties()
                .Select(p => $"{p.Name}={Escape(Format(p.GetValue(configuration)))}");
            File.WriteAllLines(_path, lines);
        }

        private static string Format(object value)
        {
            switch (value)
            {
                case null: return "";
                case double d: return d.ToString("R", CultureInfo.InvariantCulture);
                case bool b: return b ? "true" : "false";
                default: return value.ToString();
            }
        }

        /// <summary>Null means unparseable — the caller records the key as malformed.</summary>
        private static object Parse(string raw, Type type)
        {
            try
            {
                if (type == typeof(bool))
                {
                    if (string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase)) return true;
                    if (string.Equals(raw, "false", StringComparison.OrdinalIgnoreCase)) return false;
                    return null;
                }
                if (type == typeof(double)) return double.Parse(raw, CultureInfo.InvariantCulture);
                if (type == typeof(int)) return int.Parse(raw, CultureInfo.InvariantCulture);
                if (type.IsEnum) return Enum.Parse(type, raw, ignoreCase: true);
            }
            catch (Exception)
            {
                return null;
            }
            return null;
        }

        private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\r", "\\r").Replace("\n", "\\n");

        /// <summary>
        /// Single left-to-right scan. Sequential Replace calls would corrupt Windows
        /// paths: "C:\norsyn" escapes to "C:\\norsyn", and a naive Replace("\\n","\n")
        /// would then turn it into "C:" + newline + "orsyn".
        /// </summary>
        private static string Unescape(string s)
        {
            var sb = new System.Text.StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] != '\\' || i + 1 >= s.Length)
                {
                    sb.Append(s[i]);
                    continue;
                }
                char next = s[++i];
                switch (next)
                {
                    case '\\': sb.Append('\\'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    default: sb.Append('\\').Append(next); break;
                }
            }
            return sb.ToString();
        }
    }
}
