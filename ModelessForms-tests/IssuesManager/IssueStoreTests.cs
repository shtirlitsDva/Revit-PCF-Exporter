using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModelessForms.IssuesManager.Model;
using ModelessForms.IssuesManager.Storage;
using Xunit;

namespace ModelessForms.Tests.IssuesManager
{
    public sealed class IssueStoreTests : IDisposable
    {
        private readonly string _base;
        private readonly IssueStore _store;

        public IssueStoreTests()
        {
            _base = Path.Combine(Path.GetTempPath(), "IssueStoreTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_base);
            _store = new IssueStore(_base);
        }

        public void Dispose()
        {
            if (Directory.Exists(_base)) Directory.Delete(_base, recursive: true);
        }

        private string CollectionFile(string name) => Path.Combine(_base, name, "collection.json");

        // ---------- create / list / load ----------

        [Fact]
        public void Create_then_Load_roundtrips_collection_and_issues()
        {
            var c = _store.Create("Pumps");
            c.ProjectName = "P1";
            c.AuthorName = "A";
            var issue = c.AddIssue(new[] { "uid-1", "uid-2" }, new DateTime(2026, 1, 2, 3, 4, 5));
            issue.Description = "Valve clashes with beam";
            _store.Save(c);

            var loaded = _store.Load("Pumps");

            Assert.Equal("Pumps", loaded.Name);
            Assert.Equal("P1", loaded.ProjectName);
            Assert.Equal("A", loaded.AuthorName);
            var li = Assert.Single(loaded.Issues);
            Assert.Equal(issue.Id, li.Id);
            Assert.Equal(1, li.Number);
            Assert.Equal("Valve clashes with beam", li.Description);
            Assert.Equal(new[] { "uid-1", "uid-2" }, li.ElementUniqueIds);
            Assert.Equal(issue.Created, li.Created);
        }

        [Fact]
        public void ListCollections_returns_only_folders_with_collection_file_sorted()
        {
            _store.Create("b");
            _store.Create("A");
            Directory.CreateDirectory(Path.Combine(_base, "not-a-collection"));

            Assert.Equal(new[] { "A", "b" }, _store.ListCollections());
        }

        [Fact]
        public void Missing_base_folder_throws_and_is_not_created()
        {
            var missing = Path.Combine(_base, "nope");
            var store = new IssueStore(missing);

            Assert.Throws<IssueStoreException>(() => store.ListCollections());
            Assert.False(Directory.Exists(missing));
        }

        // ---------- D3: name collision, D6: invalid names ----------

        [Fact]
        public void Create_with_existing_name_throws_and_keeps_existing_data()
        {
            var c = _store.Create("Pumps");
            c.AddIssue(Array.Empty<string>(), DateTime.Now);
            _store.Save(c);

            Assert.Throws<IssueStoreException>(() => _store.Create("pumps"));
            Assert.Single(_store.Load("Pumps").Issues);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("a/b")]
        [InlineData("a:b")]
        [InlineData("..")]
        [InlineData("name.")]
        [InlineData("CON")]
        [InlineData("lpt1")]
        public void Create_with_invalid_name_throws(string name)
        {
            Assert.Throws<IssueStoreException>(() => _store.Create(name));
        }

        [Fact]
        public void Create_trims_the_name()
        {
            var c = _store.Create("  Pumps  ");
            Assert.Equal("Pumps", c.Name);
        }

        [Fact]
        public void Collection_name_is_not_stored_in_file_folder_is_the_only_source()
        {
            _store.Create("Pumps");
            Assert.DoesNotContain("\"Name\"", File.ReadAllText(CollectionFile("Pumps")));

            Directory.Move(Path.Combine(_base, "Pumps"), Path.Combine(_base, "Renamed"));
            var c = _store.Load("Renamed");
            Assert.Equal("Renamed", c.Name);

            _store.Save(c);
            Assert.False(Directory.Exists(Path.Combine(_base, "Pumps")));
        }

        // ---------- D1 / D5: failures are loud, never an empty collection ----------

        [Fact]
        public void Load_of_corrupt_file_throws_and_does_not_touch_the_file()
        {
            _store.Create("Pumps");
            File.WriteAllText(CollectionFile("Pumps"), "{ this is not json");

            Assert.Throws<IssueStoreException>(() => _store.Load("Pumps"));
            Assert.Equal("{ this is not json", File.ReadAllText(CollectionFile("Pumps")));
        }

        [Theory]
        [InlineData("Issues")]
        [InlineData("NextIssueNumber")]
        [InlineData("ProjectName")]
        [InlineData("Description")]
        [InlineData("Screenshots")]
        [InlineData("Modified")]
        public void Load_of_current_file_with_a_missing_field_throws_and_does_not_touch_the_file(string field)
        {
            var c = _store.Create("Pumps");
            c.AddIssue(new[] { "uid-1" }, DateTime.Now);
            _store.Save(c);
            var doc = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(CollectionFile("Pumps")))!.AsObject();
            var owner = doc.ContainsKey(field) ? doc : doc["Issues"]![0]!.AsObject();
            Assert.True(owner.Remove(field));
            var damaged = doc.ToJsonString();
            File.WriteAllText(CollectionFile("Pumps"), damaged);

            Assert.Throws<IssueStoreException>(() => _store.Load("Pumps"));
            Assert.Equal(damaged, File.ReadAllText(CollectionFile("Pumps")));
        }

        [Fact]
        public void Load_of_missing_collection_throws()
        {
            Assert.Throws<IssueStoreException>(() => _store.Load("Nope"));
        }

        [Fact]
        public void Load_of_file_from_newer_format_throws()
        {
            _store.Create("Pumps");
            File.WriteAllText(CollectionFile("Pumps"), "{\"FormatVersion\": 999}");

            var ex = Assert.Throws<IssueStoreException>(() => _store.Load("Pumps"));
            Assert.Contains("newer", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Save_of_locked_file_throws()
        {
            var c = _store.Create("Pumps");
            using (new FileStream(CollectionFile("Pumps"), FileMode.Open, FileAccess.Read, FileShare.None))
            {
                Assert.Throws<IssueStoreException>(() => _store.Save(c));
            }
        }

        [Fact]
        public void Atomic_write_reports_the_original_error_when_the_temp_file_cannot_be_removed()
        {
            var target = Path.Combine(_base, "file.json");
            using (new FileStream(target + ".tmp", FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                var ex = Assert.Throws<IOException>(() => AtomicFile.WriteAllText(target, "x"));
                Assert.IsAssignableFrom<IOException>(ex.InnerException);
                Assert.Contains("temporary file", ex.Message);
                Assert.Contains(ex.InnerException!.Message, ex.Message);
            }
        }

        [Fact]
        public void Save_leaves_no_temp_file()
        {
            var c = _store.Create("Pumps");
            _store.Save(c);

            Assert.Equal(new[] { "collection.json" },
                Directory.GetFiles(Path.Combine(_base, "Pumps")).Select(Path.GetFileName).ToArray());
        }

        [Fact]
        public void Save_of_deleted_collection_throws_and_does_not_recreate_it()
        {
            var c = _store.Create("Pumps");
            _store.Delete("Pumps");

            Assert.Throws<IssueStoreException>(() => _store.Save(c));
            Assert.False(Directory.Exists(Path.Combine(_base, "Pumps")));
        }

        // ---------- numbering ----------

        [Fact]
        public void Issue_numbers_are_sequential_and_never_reused_after_delete()
        {
            var c = _store.Create("Pumps");
            var i1 = c.AddIssue(Array.Empty<string>(), DateTime.Now);
            var i2 = c.AddIssue(Array.Empty<string>(), DateTime.Now);
            _store.Save(c);
            _store.DeleteIssue(c, i2);

            var reloaded = _store.Load("Pumps");
            var i3 = reloaded.AddIssue(Array.Empty<string>(), DateTime.Now);

            Assert.Equal(1, i1.Number);
            Assert.Equal(2, i2.Number);
            Assert.Equal(3, i3.Number);
        }

        // ---------- L2: screenshots ----------

        [Fact]
        public void Screenshot_names_never_collide_after_remove()
        {
            var c = _store.Create("Pumps");
            var issue = c.AddIssue(Array.Empty<string>(), DateTime.Now);
            _store.Save(c);

            var s1 = _store.AddScreenshot(c, issue, new byte[] { 1 });
            var s2 = _store.AddScreenshot(c, issue, new byte[] { 2 });
            _store.RemoveScreenshot(c, issue, s1);
            var s3 = _store.AddScreenshot(c, issue, new byte[] { 3 });

            Assert.Equal(new[] { s2, s3 }, issue.Screenshots);
            Assert.Equal(new byte[] { 2 }, File.ReadAllBytes(_store.ScreenshotPath(c, s2)));
            Assert.Equal(new byte[] { 3 }, File.ReadAllBytes(_store.ScreenshotPath(c, s3)));
            Assert.False(File.Exists(_store.ScreenshotPath(c, s1)));
            Assert.Equal(new[] { s2, s3 }, _store.Load("Pumps").Issues.Single().Screenshots);
        }

        [Fact]
        public void AddScreenshot_rejects_empty_data()
        {
            var c = _store.Create("Pumps");
            var issue = c.AddIssue(Array.Empty<string>(), DateTime.Now);
            Assert.Throws<ArgumentException>(() => _store.AddScreenshot(c, issue, Array.Empty<byte>()));
        }

        [Fact]
        public void DeleteIssue_removes_issue_and_its_screenshot_files()
        {
            var c = _store.Create("Pumps");
            var issue = c.AddIssue(Array.Empty<string>(), DateTime.Now);
            _store.Save(c);
            var s = _store.AddScreenshot(c, issue, new byte[] { 1 });

            _store.DeleteIssue(c, issue);

            Assert.Empty(_store.Load("Pumps").Issues);
            Assert.False(File.Exists(_store.ScreenshotPath(c, s)));
        }

        [Fact]
        public void DeleteIssue_with_a_locked_image_saves_and_reports_the_file()
        {
            var c = _store.Create("Pumps");
            var issue = c.AddIssue(Array.Empty<string>(), DateTime.Now);
            _store.Save(c);
            var s = _store.AddScreenshot(c, issue, new byte[] { 1 });

            IReadOnlyList<string> notDeleted;
            using (new FileStream(_store.ScreenshotPath(c, s), FileMode.Open, FileAccess.Read, FileShare.None))
                notDeleted = _store.DeleteIssue(c, issue);

            Assert.Empty(c.Issues);
            Assert.Empty(_store.Load("Pumps").Issues);
            Assert.Equal(new[] { _store.ScreenshotPath(c, s) }, notDeleted);
        }

        [Fact]
        public void RemoveScreenshot_with_a_locked_image_saves_and_reports_the_file()
        {
            var c = _store.Create("Pumps");
            var issue = c.AddIssue(Array.Empty<string>(), DateTime.Now);
            _store.Save(c);
            var s = _store.AddScreenshot(c, issue, new byte[] { 1 });

            IReadOnlyList<string> notDeleted;
            using (new FileStream(_store.ScreenshotPath(c, s), FileMode.Open, FileAccess.Read, FileShare.None))
                notDeleted = _store.RemoveScreenshot(c, issue, s);

            Assert.Empty(issue.Screenshots);
            Assert.Empty(_store.Load("Pumps").Issues.Single().Screenshots);
            Assert.Equal(new[] { _store.ScreenshotPath(c, s) }, notDeleted);
        }

        [Theory]
        [InlineData("../outside.png")]
        [InlineData("")]
        public void Load_refuses_a_collection_with_an_invalid_screenshot_name_and_keeps_the_file(string badName)
        {
            var c = _store.Create("Pumps");
            c.AddIssue(Array.Empty<string>(), DateTime.Now).Screenshots.Add(badName);
            _store.Save(c);
            var before = File.ReadAllText(CollectionFile("Pumps"));

            var ex = Assert.Throws<IssueStoreException>(() => _store.Load("Pumps"));

            Assert.Contains("damaged", ex.Message);
            Assert.Equal(before, File.ReadAllText(CollectionFile("Pumps")));
        }

        [Fact]
        public void Load_refuses_a_collection_with_a_null_description()
        {
            var c = _store.Create("Pumps");
            c.AddIssue(Array.Empty<string>(), DateTime.Now).Description = "x";
            _store.Save(c);
            var file = CollectionFile("Pumps");
            File.WriteAllText(file, File.ReadAllText(file).Replace("\"Description\": \"x\"", "\"Description\": null"));
            Assert.Contains("null", File.ReadAllText(file));

            Assert.Throws<IssueStoreException>(() => _store.Load("Pumps"));
        }

        [Fact]
        public void Delete_removes_folder()
        {
            _store.Create("Pumps");
            _store.Delete("Pumps");
            Assert.Empty(_store.ListCollections());
        }
    }
}
