using System;
using System.Globalization;
using System.Linq;
using System.Reflection;

using PcfExporter.Configuration;
using PcfExporter.Tests.Support;
using PcfExporter.UI.ViewModels;

using Xunit;

namespace PcfExporter.Tests.UI
{
    /// <summary>
    /// Pins the UI ⇄ engine contract: every PcfConfiguration property must have a
    /// same-named, bindable counterpart on MainViewModel, and the values must flow
    /// both ways (Load → VM, VM → BuildConfiguration). Add a setting and forget the
    /// UI (or vice versa) and these tests fail.
    /// </summary>
    public class ViewModelConfigurationSyncTests
    {
        private static MainViewModel CreateViewModel(InMemoryConfigurationStore store) =>
            new MainViewModel(
                new FakeRevitExecutor(), store, new FakeDialogService(),
                new FakeExportService(), new FakeBindingService(),
                new FakePopulationService(), new FakeReportService(), new FakeScheduleService());

        private static PropertyInfo[] ConfigProperties() =>
            typeof(PcfConfiguration)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToArray();

        [Fact]
        public void EveryConfigurationProperty_HasSameNamedViewModelProperty()
        {
            foreach (PropertyInfo cfgProp in ConfigProperties())
            {
                PropertyInfo vmProp = typeof(MainViewModel).GetProperty(
                    cfgProp.Name, BindingFlags.Public | BindingFlags.Instance);
                Assert.True(vmProp != null,
                    $"MainViewModel is missing a property named {cfgProp.Name} " +
                    "(every PcfConfiguration property must be editable in the UI).");
                Assert.True(vmProp.CanRead && vmProp.CanWrite,
                    $"MainViewModel.{cfgProp.Name} must be readable and writable for binding.");
            }
        }

        [Fact]
        public void LoadedConfiguration_AppearsInViewModel_AndRoundTripsThroughBuild()
        {
            PcfConfiguration original = NonDefaultConfiguration();
            var store = new InMemoryConfigurationStore { Stored = original };

            MainViewModel vm = CreateViewModel(store);
            PcfConfiguration rebuilt = vm.BuildConfiguration();

            foreach (PropertyInfo p in ConfigProperties())
            {
                object expected = p.GetValue(original);
                object actual = p.GetValue(rebuilt);
                Assert.True(Equals(expected, actual),
                    $"{p.Name}: loaded '{expected}' but BuildConfiguration returned '{actual}'.");
            }
        }

        [Fact]
        public void ChangingViewModelProperty_PersistsToStore()
        {
            var store = new InMemoryConfigurationStore();
            MainViewModel vm = CreateViewModel(store);

            vm.ProjectIdentifier = "PRJ-42";
            Assert.Equal("PRJ-42", store.Stored.ProjectIdentifier);

            vm.Scope = ExportScope.Selection;
            Assert.Equal(ExportScope.Selection, store.Stored.Scope);

            vm.DiameterLimit = "25";
            Assert.Equal(25d, store.Stored.DiameterLimit);
        }

        [Theory]
        [InlineData("0", 0d)]
        [InlineData("25", 25d)]
        [InlineData("32.5", 32.5d)]
        [InlineData("32,5", 32.5d)]   //tolerate comma decimal separator
        [InlineData(" 40 ", 40d)]
        [InlineData("", 0d)]
        [InlineData("garbage", 0d)]
        public void ParseDiameterLimit_IsCultureTolerant(string input, double expected)
        {
            Assert.Equal(expected, MainViewModel.ParseDiameterLimit(input));
        }

        /// <summary>
        /// User decision 2026-06-10: invalid diameter-limit text never persists as a
        /// silent 0 — the last valid value is kept and an error is shown.
        /// </summary>
        [Fact]
        public void InvalidDiameterLimit_KeepsLastValidValue_AndShowsError()
        {
            var store = new InMemoryConfigurationStore
            {
                Stored = new PcfConfiguration { DiameterLimit = 25 }
            };
            MainViewModel vm = CreateViewModel(store);
            Assert.Equal("", vm.DiameterLimitError);

            vm.DiameterLimit = "2x5";

            Assert.NotEqual("", vm.DiameterLimitError);
            Assert.Equal(25d, vm.BuildConfiguration().DiameterLimit);
            Assert.Equal(25d, store.Stored.DiameterLimit);

            //Recovery: a valid value clears the error and becomes the new last-valid.
            vm.DiameterLimit = "40";
            Assert.Equal("", vm.DiameterLimitError);
            Assert.Equal(40d, store.Stored.DiameterLimit);
        }

        private static PcfConfiguration NonDefaultConfiguration()
        {
            var cfg = new PcfConfiguration();
            var defaults = new PcfConfiguration();
            foreach (PropertyInfo p in ConfigProperties())
            {
                if (p.PropertyType == typeof(string))
                {
                    //DiameterLimit travels as text through the VM — keep numeric strings numeric.
                    p.SetValue(cfg, p.Name == "SelectedSystemAbbreviation" ? "FVF" : "v-" + p.Name);
                }
                else if (p.PropertyType == typeof(bool))
                    p.SetValue(cfg, !(bool)p.GetValue(defaults));
                else if (p.PropertyType == typeof(double))
                    p.SetValue(cfg, 15d);
                else if (p.PropertyType.IsEnum)
                    p.SetValue(cfg, Enum.GetValues(p.PropertyType).Cast<object>()
                        .First(v => !v.Equals(p.GetValue(defaults))));
            }
            return cfg;
        }
    }
}
