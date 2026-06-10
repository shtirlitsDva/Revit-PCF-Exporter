using System.Collections.Generic;

namespace PcfExporter.Configuration
{
    /// <summary>
    /// Persistence boundary for <see cref="PcfConfiguration"/>.
    /// </summary>
    public interface IConfigurationStore
    {
        /// <summary>
        /// Loads the persisted configuration, or defaults when nothing is persisted yet.
        /// Malformed entries fall back to their defaults but are NEVER silent: every
        /// affected key is reported in <see cref="ConfigurationLoadResult.MalformedKeys"/>
        /// and the UI must surface them (user decision 2026-06-10).
        /// </summary>
        ConfigurationLoadResult Load();
        void Save(PcfConfiguration configuration);
    }

    public sealed class ConfigurationLoadResult
    {
        public ConfigurationLoadResult(PcfConfiguration configuration, IReadOnlyList<string> malformedKeys)
        {
            Configuration = configuration;
            MalformedKeys = malformedKeys;
        }

        public PcfConfiguration Configuration { get; }
        /// <summary>Setting keys whose persisted value could not be parsed (defaults were used).</summary>
        public IReadOnlyList<string> MalformedKeys { get; }
    }
}
