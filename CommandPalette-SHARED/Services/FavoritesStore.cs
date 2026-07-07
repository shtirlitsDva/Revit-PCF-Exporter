#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Norsyn.CommandPalette.Services
{
    // Persists the user's favorites and mini-pane preferences to
    // %APPDATA%\Norsyn\command-palette.txt. Deliberately a trivial line format
    // (no JSON library) so the exact same source compiles and runs on the net48
    // year-builds without dragging in a serializer package that could clash with
    // Revit's own. Command ids never contain newlines, so no escaping is needed.
    // Writes are best-effort: a failed save must never break the pane.
    public sealed class FavoritesStore
    {
        private readonly string _path;
        private readonly object _gate = new object();
        private readonly HashSet<string> _favorites = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> _order = new List<string>();
        private bool _labelsOn = true;

        public FavoritesStore(string? path = null)
        {
            _path = path ?? DefaultPath();
            Load();
        }

        private static string DefaultPath()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Norsyn");
            return Path.Combine(dir, "command-palette.txt");
        }

        // Raised after any mutation so the favorites pane can refresh live.
        public event Action? Changed;

        public bool IsFavorite(string id)
        {
            lock (_gate) return _favorites.Contains(id);
        }

        // Favorite ids in the user's arranged order.
        public IReadOnlyList<string> Order
        {
            get { lock (_gate) return _order.ToList(); }
        }

        public bool LabelsOn
        {
            get { lock (_gate) return _labelsOn; }
            set
            {
                lock (_gate)
                {
                    if (_labelsOn == value) return;
                    _labelsOn = value;
                }
                SaveAndNotify();
            }
        }

        public void Toggle(string id)
        {
            lock (_gate)
            {
                if (_favorites.Remove(id)) _order.Remove(id);
                else { _favorites.Add(id); if (!_order.Contains(id)) _order.Add(id); }
            }
            SaveAndNotify();
        }

        // Persist a new arrangement (drag-reorder in the favorites pane).
        public void SetOrder(IEnumerable<string> ids)
        {
            lock (_gate)
            {
                _order.Clear();
                foreach (string id in ids)
                    if (_favorites.Contains(id) && !_order.Contains(id)) _order.Add(id);
            }
            SaveAndNotify();
        }

        private void SaveAndNotify()
        {
            Save();
            Changed?.Invoke();
        }

        private void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                List<string> lines;
                lock (_gate)
                {
                    lines = new List<string> { "labelsOn=" + (_labelsOn ? "1" : "0") };
                    lines.AddRange(_order.Select(id => "fav=" + id));
                }
                File.WriteAllLines(_path, lines);
            }
            catch
            {
                // Best-effort: never let a disk hiccup take down the UI.
            }
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(_path)) return;
                foreach (string raw in File.ReadAllLines(_path))
                {
                    string line = raw.Trim();
                    if (line.StartsWith("labelsOn=", StringComparison.Ordinal))
                        _labelsOn = line.Substring("labelsOn=".Length).Trim() != "0";
                    else if (line.StartsWith("fav=", StringComparison.Ordinal))
                    {
                        string id = line.Substring("fav=".Length);
                        if (id.Length > 0 && _favorites.Add(id)) _order.Add(id);
                    }
                }
            }
            catch
            {
                // Corrupt/unreadable → start fresh rather than crash.
            }
        }
    }
}
