using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using PcfExporter.Configuration;
using PcfExporter.Context;
using PcfExporter.Orchestration;
using PcfExporter.Services;

using dh = Shared.DataHandler;

namespace PcfExporter.UI.ViewModels
{
    /// <summary>
    /// The window's ViewModel. Configuration-mapped properties mirror
    /// <see cref="PcfConfiguration"/> one-to-one by name (a convention test enforces
    /// this); commands delegate to the services through the Revit executor.
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        private readonly IRevitExecutor _executor;
        private readonly IConfigurationStore _store;
        private readonly IDialogService _dialogs;
        private readonly IPcfExportService _exportService;
        private readonly IParameterBindingService _bindingService;
        private readonly IParameterPopulationService _populationService;
        private readonly IParameterReportService _reportService;
        private readonly IScheduleService _scheduleService;

        private bool _loading;

        public MainViewModel(
            IRevitExecutor executor,
            IConfigurationStore store,
            IDialogService dialogs)
            : this(executor, store, dialogs,
                  new PcfExportService(),
                  new ParameterBindingService(),
                  new ParameterPopulationService(),
                  new ParameterReportService(),
                  new ScheduleService())
        { }

        public MainViewModel(
            IRevitExecutor executor,
            IConfigurationStore store,
            IDialogService dialogs,
            IPcfExportService exportService,
            IParameterBindingService bindingService,
            IParameterPopulationService populationService,
            IParameterReportService reportService,
            IScheduleService scheduleService)
        {
            _executor = executor;
            _store = store;
            _dialogs = dialogs;
            _exportService = exportService;
            _bindingService = bindingService;
            _populationService = populationService;
            _reportService = reportService;
            _scheduleService = scheduleService;

            ConfigurationLoadResult loaded = _store.Load();
            _configLoadWarnings = loaded.MalformedKeys;
            LoadConfiguration(loaded.Configuration);
        }

        private readonly System.Collections.Generic.IReadOnlyList<string> _configLoadWarnings;

        #region Configuration-mapped properties (names mirror PcfConfiguration)
        [ObservableProperty] private ExportScope _scope;
        [ObservableProperty] private string _selectedSystemAbbreviation = "";
        [ObservableProperty] private BoreUnits _boreUnits;
        [ObservableProperty] private CoordsUnits _coordsUnits;
        [ObservableProperty] private WeightUnits _weightUnits;
        [ObservableProperty] private WeightLengthUnits _weightLengthUnits;
        [ObservableProperty] private OutputEncodingChoice _outputEncoding;
        [ObservableProperty] private string _outputDirectory = "";
        [ObservableProperty] private string _elementsExcelPath = "";
        [ObservableProperty] private string _ldtPath = "";
        [ObservableProperty] private string _projectIdentifier = "";
        [ObservableProperty] private string _diameterLimit = "0";
        [ObservableProperty] private string _specFilter = "";
        [ObservableProperty] private bool _exportToIsogen;
        [ObservableProperty] private bool _exportToCii;
        [ObservableProperty] private bool _overwrite;
        #endregion

        #region UI state
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _statusText = "Ready.";
        [ObservableProperty] private string _diameterLimitError = "";
        public ObservableCollection<string> Pipelines { get; } = new ObservableCollection<string>();
        public bool IsSpecificPipeline => Scope == ExportScope.SpecificPipeline;
        partial void OnScopeChanged(ExportScope value) => OnPropertyChanged(nameof(IsSpecificPipeline));
        partial void OnDiameterLimitChanged(string value)
        {
            if (TryParseDiameterLimit(value, out double parsed))
            {
                _lastValidDiameterLimit = parsed;
                DiameterLimitError = "";
            }
            else
            {
                DiameterLimitError =
                    $"Not a number — keeping the last valid value ({_lastValidDiameterLimit.ToString(CultureInfo.InvariantCulture)}).";
            }
        }
        #endregion

        /// <summary>
        /// User decision 2026-06-10: while the diameter-limit text is invalid, exports
        /// are blocked and persistence keeps this last valid value (never a silent 0).
        /// </summary>
        private double _lastValidDiameterLimit;

        private bool _saveFailureReported;

        protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            //Persist every configuration change immediately (matches the old
            //user.config behavior, minus the control-name vocabulary).
            if (_loading) return;
            if (e.PropertyName == nameof(IsBusy) || e.PropertyName == nameof(StatusText) ||
                e.PropertyName == nameof(IsSpecificPipeline) || e.PropertyName == nameof(DiameterLimitError)) return;
            SaveGuarded();
        }

        /// <summary>
        /// Saves loudly but without crashing Revit: a locked settings file (second Revit
        /// instance, antivirus, synced AppData) must not throw out of a binding setter.
        /// The failure is surfaced in the status bar and once per session as a dialog.
        /// </summary>
        private void SaveGuarded()
        {
            try
            {
                _store.Save(BuildConfiguration());
            }
            catch (Exception ex)
            {
                StatusText = "Could not save settings: " + ex.Message;
                if (!_saveFailureReported)
                {
                    _saveFailureReported = true;
                    _dialogs.ShowError("Saving settings failed", ex);
                }
            }
        }

        private void LoadConfiguration(PcfConfiguration cfg)
        {
            _loading = true;
            try
            {
                Scope = cfg.Scope;
                SelectedSystemAbbreviation = cfg.SelectedSystemAbbreviation;
                BoreUnits = cfg.BoreUnits;
                CoordsUnits = cfg.CoordsUnits;
                WeightUnits = cfg.WeightUnits;
                WeightLengthUnits = cfg.WeightLengthUnits;
                OutputEncoding = cfg.OutputEncoding;
                OutputDirectory = cfg.OutputDirectory;
                ElementsExcelPath = cfg.ElementsExcelPath;
                LdtPath = cfg.LdtPath;
                ProjectIdentifier = cfg.ProjectIdentifier;
                DiameterLimit = cfg.DiameterLimit.ToString(CultureInfo.InvariantCulture);
                SpecFilter = cfg.SpecFilter;
                ExportToIsogen = cfg.ExportToIsogen;
                ExportToCii = cfg.ExportToCii;
                Overwrite = cfg.Overwrite;
            }
            finally { _loading = false; }
        }

        public PcfConfiguration BuildConfiguration() => new PcfConfiguration
        {
            Scope = Scope,
            SelectedSystemAbbreviation = SelectedSystemAbbreviation ?? "",
            BoreUnits = BoreUnits,
            CoordsUnits = CoordsUnits,
            WeightUnits = WeightUnits,
            WeightLengthUnits = WeightLengthUnits,
            OutputEncoding = OutputEncoding,
            OutputDirectory = OutputDirectory ?? "",
            ElementsExcelPath = ElementsExcelPath ?? "",
            LdtPath = LdtPath ?? "",
            ProjectIdentifier = ProjectIdentifier ?? "",
            DiameterLimit = TryParseDiameterLimit(DiameterLimit, out double dl) ? dl : _lastValidDiameterLimit,
            SpecFilter = SpecFilter ?? "",
            ExportToIsogen = ExportToIsogen,
            ExportToCii = ExportToCii,
            Overwrite = Overwrite
        };

        /// <summary>Nominal sizes are integers in practice, but parse defensively and culture-tolerantly.</summary>
        public static double ParseDiameterLimit(string text) =>
            TryParseDiameterLimit(text, out double value) ? value : 0;

        public static bool TryParseDiameterLimit(string text, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text)) return true; //empty means "no limit"
            string normalized = text.Trim().Replace(',', '.');
            return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        /// <summary>Called by the window after it is shown: fills the pipeline combo.</summary>
        public async Task InitializeAsync()
        {
            if (_configLoadWarnings.Count > 0)
            {
                string keys = string.Join(", ", _configLoadWarnings);
                StatusText = "Settings file had malformed entries: " + keys;
                _dialogs.ShowInfo("Settings file had malformed entries",
                    "These settings could not be read and were reset to their defaults:\n\n" +
                    keys + "\n\nFile: " + FileConfigurationStore.DefaultPath());
            }
            await RefreshPipelinesAsync();
        }

        #region Commands
        [RelayCommand]
        private async Task RefreshPipelines() => await RefreshPipelinesAsync();

        private async Task RefreshPipelinesAsync()
        {
            await RunGuarded("Reading pipelines", async () =>
            {
                var names = await _executor.RunAsync("Read pipelines", ctx =>
                    Shared.MepUtils.GetDistinctPhysicalPipingSystemTypeNames(ctx.Doc, true));

                string remembered = SelectedSystemAbbreviation;
                Pipelines.Clear();
                foreach (string name in names) Pipelines.Add(name);

                SelectedSystemAbbreviation = Pipelines.Contains(remembered)
                    ? remembered
                    : Pipelines.FirstOrDefault() ?? "";
                StatusText = $"{Pipelines.Count} pipelines found.";
            });
        }

        [RelayCommand]
        private async Task ImportParameters()
        {
            await RunGuarded("Importing PCF parameters", async () =>
            {
                string feedback = await _executor.RunAsync("Import PCF parameters",
                    ctx => _bindingService.CreateAllBindings(ctx));
                _dialogs.ShowInfo("Import PCF parameters", feedback);
                StatusText = "PCF parameters imported.";
            });
        }

        [RelayCommand]
        private async Task DeleteParameters()
        {
            await RunGuarded("Deleting PCF parameters", async () =>
            {
                string feedback = await _executor.RunAsync("Delete PCF parameters",
                    ctx => _bindingService.DeleteAllBindings(ctx));
                _dialogs.ShowInfo("Delete PCF parameters", feedback);
                StatusText = "PCF parameters deleted.";
            });
        }

        [RelayCommand]
        private void SelectElementsExcel()
        {
            string path = _dialogs.OpenFile(
                "Select Excel file with ELEMENT parameter setup",
                "Excel files (*.xlsx;*.xls)|*.xlsx;*.xls");
            if (path != null) ElementsExcelPath = path;
        }

        [RelayCommand]
        private void SelectLdt()
        {
            string path = _dialogs.OpenFile(
                "Select Excel file with PIPELINE parameter setup (LDT)",
                "Excel files (*.xlsx;*.xls)|*.xlsx;*.xls");
            if (path != null) LdtPath = path;
        }

        [RelayCommand]
        private void SelectOutputDirectory()
        {
            string path = _dialogs.PickFolder("Select output folder for PCF files");
            if (path != null) OutputDirectory = path;
        }

        [RelayCommand]
        private async Task PopulateElements()
        {
            await RunGuarded("Populating ELEMENT parameters", async () =>
            {
                DataTable table = ReadTable(ElementsExcelPath, "Elements");
                PcfConfiguration cfg = BuildConfiguration();
                string feedback = await _executor.RunAsync("Populate ELEMENT parameters",
                    ctx => _populationService.PopulateElements(ctx, cfg, table));
                _dialogs.ShowInfo("Populate ELEMENT parameters", feedback);
                StatusText = "ELEMENT parameters populated.";
            });
        }

        [RelayCommand]
        private async Task PopulatePipelines()
        {
            await RunGuarded("Populating PIPELINE parameters", async () =>
            {
                DataTable table = ReadTable(LdtPath, "Pipelines");
                PcfConfiguration cfg = BuildConfiguration();
                string feedback = await _executor.RunAsync("Populate PIPELINE parameters",
                    ctx => _populationService.PopulatePipelines(ctx, cfg, table));
                _dialogs.ShowInfo("Populate PIPELINE parameters", feedback);
                StatusText = "PIPELINE parameters populated.";
            });
        }

        [RelayCommand]
        private async Task ExportUndefinedElements()
        {
            await RunGuarded("Exporting undefined elements", async () =>
            {
                DataTable table = ReadTable(ElementsExcelPath, "Elements");
                PcfConfiguration cfg = BuildConfiguration();
                DataTable missing = await _executor.RunAsync("Export undefined elements",
                    ctx => _reportService.UndefinedElements(ctx, cfg, table));
                if (missing.Rows.Count == 0)
                {
                    StatusText = "All elements defined. Note: this does not validate correctness of definitions.";
                    _dialogs.ShowInfo("Undefined elements",
                        "All ELEMENTS defined!\nNote: This does not validate\ncorrectness of definition.");
                }
                else
                {
                    StatusText = $"{missing.Rows.Count} undefined element type(s) — copy the rows into the Elements workbook.";
                    _dialogs.ShowTables("Undefined elements", new[] { missing });
                }
            });
        }

        [RelayCommand]
        private async Task ExportUndefinedPipelines()
        {
            await RunGuarded("Exporting undefined pipelines", async () =>
            {
                DataTable table = ReadTable(LdtPath, "Pipelines");
                PcfConfiguration cfg = BuildConfiguration();
                DataTable missing = await _executor.RunAsync("Export undefined pipelines",
                    ctx => _reportService.UndefinedPipelines(ctx, cfg, table));
                if (missing.Rows.Count == 0)
                {
                    StatusText = "All pipelines defined in the LDT workbook.";
                    _dialogs.ShowInfo("Undefined pipelines",
                        "All PIPELINES defined for this PROJECT-IDENTIFIER!");
                }
                else
                {
                    StatusText = $"{missing.Rows.Count} undefined pipeline(s) — copy the rows into the LDT workbook.";
                    _dialogs.ShowTables("Undefined pipelines", new[] { missing });
                }
            });
        }

        [RelayCommand]
        private async Task CreateSchedules()
        {
            await RunGuarded("Creating schedules", async () =>
            {
                await _executor.RunAsync("Create PCF schedules", ctx =>
                {
                    _scheduleService.CreatePcfSchedules(ctx);
                    return true;
                });
                _dialogs.ShowInfo("Schedules", "Schedules created successfully!");
                StatusText = "Schedules created.";
            });
        }

        [RelayCommand]
        private async Task ExportCurrentValues()
        {
            await RunGuarded("Showing current parameter values", async () =>
            {
                System.Collections.Generic.IReadOnlyList<DataTable> tables =
                    await _executor.RunAsync("Export parameter values",
                        ctx => _reportService.CurrentValues(ctx));
                _dialogs.ShowTables("Current parameter values", tables);
                StatusText = "Current parameter values shown — copy rows into the workbooks as needed.";
            });
        }

        [RelayCommand]
        private async Task Export()
        {
            await RunGuarded("Exporting PCF", async () =>
            {
                PcfConfiguration cfg = BuildConfiguration();
                ValidateForExport(cfg);
                ExportResult result = await _executor.RunAsync("PCF export",
                    ctx => _exportService.Export(ctx, cfg));
                StatusText = $"Exported {result.ElementCount} elements to " +
                             $"{result.WrittenFiles.Count} file(s).";
                _dialogs.ShowInfo("PCF export",
                    "PCF data exported successfully!\n\n" + string.Join("\n", result.WrittenFiles));
            });
        }
        #endregion

        private void ValidateForExport(PcfConfiguration cfg)
        {
            if (string.IsNullOrWhiteSpace(cfg.OutputDirectory) || !Directory.Exists(cfg.OutputDirectory))
                throw new InvalidOperationException(
                    "Output folder is not set or does not exist. Choose an output folder first.");
            if (cfg.Scope == ExportScope.SpecificPipeline && string.IsNullOrEmpty(cfg.SelectedSystemAbbreviation))
                throw new InvalidOperationException(
                    "No pipeline selected. Choose a pipeline to export.");
            if (!TryParseDiameterLimit(DiameterLimit, out _))
                throw new InvalidOperationException(
                    "Diameter limit is not a valid number. Correct it before exporting.");
        }

        private static DataTable ReadTable(string excelPath, string sheetName)
        {
            if (string.IsNullOrEmpty(excelPath) || !File.Exists(excelPath))
                throw new InvalidOperationException(
                    $"Excel file is not set or does not exist:\n{excelPath}\n" +
                    "Select the workbook first.");
            DataSet dataSet = dh.ReadExcelToDataSet(excelPath);
            DataTable table = dh.ReadDataTable(dataSet, sheetName);
            if (table == null)
                throw new InvalidOperationException(
                    $"Workbook {excelPath} has no sheet named {sheetName}.");
            return table;
        }

        private async Task RunGuarded(string description, Func<Task> work)
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusText = description + "…";
            try
            {
                await work();
            }
            catch (OperationCanceledException)
            {
                //Window closed while work was queued — nothing to report to anyone.
                StatusText = description + " cancelled.";
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                //User pressed Esc in a Revit pick/operation — quiet cancel, like the
                //old Result.Cancelled path.
                StatusText = description + " cancelled.";
            }
            catch (Exception ex)
            {
                StatusText = description + " failed.";
                _dialogs.ShowError(description + " failed", ex);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
