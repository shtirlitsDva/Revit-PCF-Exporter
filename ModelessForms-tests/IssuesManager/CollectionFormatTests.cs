using System;
using System.IO;
using System.Linq;
using ModelessForms.IssuesManager.Storage;
using Xunit;

namespace ModelessForms.Tests.IssuesManager
{
    /// <summary>
    /// v0 = the files written by the old Issue Tracker (no FormatVersion field).
    /// Revit 2024 (net48) wrote them with DataContractJsonSerializer, Revit 2025 with
    /// System.Text.Json. Both shapes must upgrade to the current format.
    /// </summary>
    public sealed class CollectionFormatTests : IDisposable
    {
        private readonly string _base;
        private readonly IssueStore _store;

        public CollectionFormatTests()
        {
            _base = Path.Combine(Path.GetTempPath(), "CollectionFormatTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_base);
            _store = new IssueStore(_base);
        }

        public void Dispose()
        {
            if (Directory.Exists(_base)) Directory.Delete(_base, recursive: true);
        }

        // Old code inserted new issues at index 0, so the newest issue is first.
        private const string V0_SystemTextJson = @"{
  ""Name"": ""OldName"",
  ""Created"": ""2026-01-10T08:00:00.0000000+01:00"",
  ""Issues"": [
    {
      ""Id"": ""issue_20260117_143052_bbbbbb"",
      ""Description"": ""second"",
      ""ElementGuids"": [ ""uid-b"" ],
      ""Screenshots"": [ ""issue_20260117_143052_bbbbbb_001.png"" ],
      ""Created"": ""2026-01-17T14:30:52.0000000+01:00"",
      ""Modified"": ""2026-01-18T09:00:00.0000000+01:00""
    },
    {
      ""Id"": ""issue_20260115_100000_aaaaaa"",
      ""Description"": ""first"",
      ""ElementGuids"": [],
      ""Screenshots"": [],
      ""Created"": ""2026-01-15T10:00:00.0000000+01:00"",
      ""Modified"": ""2026-01-15T10:00:00.0000000+01:00""
    }
  ],
  ""ProjectName"": ""Proj"",
  ""AuthorName"": ""Auth""
}";

        // DataContractJsonSerializer: alphabetical members, \/Date(ms+zone)\/ dates, null strings.
        private const string V0_DataContract =
            @"{""AuthorName"":null,""Created"":""\/Date(1768028400000+0100)\/""," +
            @"""Issues"":[{""Created"":""\/Date(1768656652000+0100)\/"",""Description"":null," +
            @"""ElementGuids"":[""uid-1"",""uid-2""],""Id"":""issue_20260117_143052_cccccc""," +
            @"""Modified"":""\/Date(1768656652000+0100)\/"",""Screenshots"":[]}]," +
            @"""Name"":""Default"",""ProjectName"":""P""}";

        private void WriteV0(string name, string json)
        {
            Directory.CreateDirectory(Path.Combine(_base, name));
            File.WriteAllText(Path.Combine(_base, name, "collection.json"), json);
        }

        [Fact]
        public void V0_from_System_Text_Json_upgrades()
        {
            WriteV0("Pumps", V0_SystemTextJson);

            var c = _store.Load("Pumps");

            Assert.Equal("Pumps", c.Name); // folder wins over the stored "OldName"
            Assert.Equal("Proj", c.ProjectName);
            Assert.Equal("Auth", c.AuthorName);
            Assert.Equal(2, c.Issues.Count);

            // Numbered by Created, list ordered by number.
            Assert.Equal("first", c.Issues[0].Description);
            Assert.Equal(1, c.Issues[0].Number);
            Assert.Equal("second", c.Issues[1].Description);
            Assert.Equal(2, c.Issues[1].Number);

            Assert.Equal(new[] { "uid-b" }, c.Issues[1].ElementUniqueIds);
            Assert.Equal(new[] { "issue_20260117_143052_bbbbbb_001.png" }, c.Issues[1].Screenshots);
            Assert.Equal(new DateTimeOffset(2026, 1, 17, 14, 30, 52, TimeSpan.FromHours(1)).LocalDateTime,
                c.Issues[1].Created);

            var next = c.AddIssue(Array.Empty<string>(), DateTime.Now);
            Assert.Equal(3, next.Number);
        }

        [Fact]
        public void V0_from_DataContractJsonSerializer_upgrades()
        {
            WriteV0("Default", V0_DataContract);

            var c = _store.Load("Default");

            Assert.Equal(string.Empty, c.AuthorName);
            Assert.Equal("P", c.ProjectName);
            Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1768028400000).LocalDateTime, c.Created);
            var i = Assert.Single(c.Issues);
            Assert.Equal(string.Empty, i.Description);
            Assert.Equal(1, i.Number);
            Assert.Equal(new[] { "uid-1", "uid-2" }, i.ElementUniqueIds);
            Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1768656652000).LocalDateTime, i.Created);
        }

        [Fact]
        public void Upgrade_keeps_a_backup_of_the_original_file()
        {
            WriteV0("Pumps", V0_SystemTextJson);

            _store.Load("Pumps");

            var backup = Path.Combine(_base, "Pumps", "collection.v0.bak.json");
            Assert.Equal(V0_SystemTextJson, File.ReadAllText(backup));
        }

        [Fact]
        public void Upgrade_does_not_overwrite_an_existing_backup()
        {
            WriteV0("Pumps", V0_SystemTextJson);
            var backup = Path.Combine(_base, "Pumps", "collection.v0.bak.json");
            File.WriteAllText(backup, "older backup");

            _store.Load("Pumps");

            Assert.Equal("older backup", File.ReadAllText(backup));
        }

        [Fact]
        public void Saved_file_is_current_format_and_loads_without_upgrade()
        {
            WriteV0("Pumps", V0_SystemTextJson);
            var c = _store.Load("Pumps");
            _store.Save(c);

            var json = File.ReadAllText(Path.Combine(_base, "Pumps", "collection.json"));
            Assert.Contains("\"FormatVersion\": 1", json);
            Assert.DoesNotContain("ElementGuids", json);

            var again = _store.Load("Pumps");
            Assert.Equal(c.Issues.Select(i => i.Id), again.Issues.Select(i => i.Id));
        }

        [Fact]
        public void V0_issue_without_id_is_rejected()
        {
            WriteV0("Pumps", @"{""Issues"":[{""Description"":""x"",""ElementGuids"":[],""Screenshots"":[]," +
                             @"""Created"":""2026-01-15T10:00:00+01:00"",""Modified"":""2026-01-15T10:00:00+01:00""}]}");

            Assert.Throws<IssueStoreException>(() => _store.Load("Pumps"));
        }
    }
}
