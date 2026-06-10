using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using PcfExporter.Configuration;
using PcfExporter.Context;
using PcfExporter.UI;

namespace PcfExporter.Tests.Support
{
    /// <summary>Executor fake: runs nothing, returns defaults (UI-only tests).</summary>
    public sealed class FakeRevitExecutor : IRevitExecutor
    {
        public Task RunAsync(string name, Action<IRevitContext> work) => Task.CompletedTask;
        public Task<T> RunAsync<T>(string name, Func<IRevitContext, T> work) => Task.FromResult(default(T));
    }

    public sealed class InMemoryConfigurationStore : IConfigurationStore
    {
        public PcfConfiguration Stored { get; set; } = new PcfConfiguration();
        public List<string> MalformedKeys { get; } = new List<string>();
        public int SaveCount { get; private set; }
        public ConfigurationLoadResult Load() => new ConfigurationLoadResult(Stored.Clone(), MalformedKeys);
        public void Save(PcfConfiguration configuration)
        {
            Stored = configuration.Clone();
            SaveCount++;
        }
    }

    public sealed class FakeDialogService : IDialogService
    {
        public List<string> Errors { get; } = new List<string>();
        public List<string> Infos { get; } = new List<string>();
        public List<(string Title, IReadOnlyList<System.Data.DataTable> Tables)> ShownTables { get; }
            = new List<(string, IReadOnlyList<System.Data.DataTable>)>();
        public string NextFile { get; set; }
        public string NextFolder { get; set; }

        public string OpenFile(string title, string filter) => NextFile;
        public string PickFolder(string title) => NextFolder;
        public void ShowInfo(string title, string message) => Infos.Add(title + ": " + message);
        public void ShowError(string title, Exception exception) => Errors.Add(title + ": " + exception);
        public void ShowTables(string title, IReadOnlyList<System.Data.DataTable> tables) =>
            ShownTables.Add((title, tables));
    }

    public static class RepoPaths
    {
        /// <summary>Walks up from the test assembly until the shared project folder is found.</summary>
        public static string SharedProjectDir()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                string candidate = Path.Combine(dir.FullName, "revit-pcf-exporter-shared");
                if (Directory.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
            throw new InvalidOperationException(
                "Could not locate revit-pcf-exporter-shared above " + AppContext.BaseDirectory);
        }
    }
}
