using System;
using System.IO;
using FlawlessPictury.AppCore.CrossCutting;
using FlawlessPictury.AppCore.Plugins;

namespace FlawlessPictury.Infrastructure.Plugins
{
    /// <summary>
    /// Shared plugin runtime environment (directory + loader + catalog).
    /// </summary>
    public sealed class PluginEnvironment
    {
        private readonly PluginLoader _loader;
        private readonly PluginCatalog _catalog;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes the environment.
        /// </summary>
        /// <param name="pluginsDirectory">Directory where plugin DLLs are discovered.</param>
        /// <param name="loader">Plugin loader implementation.</param>
        /// <param name="catalog">Shared in-memory plugin catalog.</param>
        /// <param name="logger">Logger.</param>
        public PluginEnvironment(string pluginsDirectory, PluginLoader loader, PluginCatalog catalog, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(pluginsDirectory))
            {
                throw new ArgumentException("Plugins directory is required.", nameof(pluginsDirectory));
            }

            PluginsDirectory = pluginsDirectory;
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the directory where plugin DLLs are loaded from.
        /// </summary>
        public string PluginsDirectory { get; }

        /// <summary>
        /// Gets the shared in-memory plugin catalog.
        /// </summary>
        public PluginCatalog Catalog => _catalog;

        /// <summary>
        /// Reloads plugins from <see cref="PluginsDirectory"/> into the shared <see cref="Catalog"/>.
        /// </summary>
        /// <remarks>
        /// We avoid logging the directory path here to prevent duplicated "Loading plugins from ..." lines.
        /// PluginLoader logs the directory path as part of its robust discovery behavior.
        /// </remarks>
        public PluginLoadResult Reload()
        {
            try
            {
                Directory.CreateDirectory(PluginsDirectory);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warn, "Could not create Plugins directory.", ex);
            }

            // Single, high-level message (directory details are logged by PluginLoader).
            _logger.Log(LogLevel.Info, "Loading plugins.");

            return _loader.LoadFromDirectory(PluginsDirectory, _catalog);
        }
    }
}
