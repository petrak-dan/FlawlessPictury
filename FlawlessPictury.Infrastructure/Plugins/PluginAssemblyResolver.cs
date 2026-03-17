using System;
using System.IO;
using System.Reflection;

namespace FlawlessPictury.Infrastructure.Plugins
{
    /// <summary>
    /// Assembly resolver that probes a single plugin directory for missing dependencies.
    /// </summary>
    internal sealed class PluginAssemblyResolver : IDisposable
    {
        private readonly string _pluginDirectory;
        private readonly string _applicationBaseDirectory;
        private bool _isRegistered;

        /// <summary>
        /// Initializes the resolver.
        /// </summary>
        public PluginAssemblyResolver(string pluginDirectory)
        {
            _pluginDirectory = pluginDirectory;
            _applicationBaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        }

        /// <summary>
        /// Registers the resolver with the current AppDomain.
        /// </summary>
        public void Register()
        {
            if (_isRegistered)
            {
                return;
            }

            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            _isRegistered = true;
        }

        /// <summary>
        /// Unregisters the resolver.
        /// </summary>
        public void Dispose()
        {
            if (_isRegistered)
            {
                AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
                _isRegistered = false;
            }
        }

        private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                var requestedAssemblyName = new AssemblyName(args.Name);
                var requestedSimpleName = requestedAssemblyName.Name;
                if (string.IsNullOrWhiteSpace(requestedSimpleName))
                {
                    return null;
                }

                var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < loadedAssemblies.Length; i++)
                {
                    var loadedAssembly = loadedAssemblies[i];
                    if (loadedAssembly == null) continue;

                    var loadedName = loadedAssembly.GetName();
                    if (!string.Equals(loadedName.Name, requestedSimpleName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (string.Equals(loadedName.FullName, requestedAssemblyName.FullName, StringComparison.OrdinalIgnoreCase))
                    {
                        return loadedAssembly;
                    }
                }

                var sharedCandidatePath = Path.Combine(_applicationBaseDirectory, requestedSimpleName + ".dll");
                if (File.Exists(sharedCandidatePath))
                {
                    return Assembly.LoadFrom(sharedCandidatePath);
                }

                if (string.IsNullOrWhiteSpace(_pluginDirectory) || !Directory.Exists(_pluginDirectory))
                {
                    return null;
                }

                var pluginCandidatePath = Path.Combine(_pluginDirectory, requestedSimpleName + ".dll");
                if (!File.Exists(pluginCandidatePath))
                {
                    return null;
                }

                return Assembly.LoadFrom(pluginCandidatePath);
            }
            catch
            {
                // Intent: Never throw from AssemblyResolve; return null so default resolution can continue.
                return null;
            }
        }
    }
}
