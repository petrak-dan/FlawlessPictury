using System;
using System.IO;
using System.Linq;
using System.Reflection;
using FlawlessPictury.AppCore.CrossCutting;
using FlawlessPictury.AppCore.Plugins;

namespace FlawlessPictury.Infrastructure.Plugins
{
    /// <summary>
    /// Loads plugin assemblies from a directory and registers them into a <see cref="PluginCatalog"/>.
    /// </summary>
    public sealed class PluginLoader
    {
        private const string PluginInterfaceFullName = "FlawlessPictury.AppCore.Plugins.IPlugin";

        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a loader.
        /// </summary>
        /// <param name="logger">Optional logger for diagnostics.</param>
        public PluginLoader(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Loads plugin DLLs from <paramref name="pluginsDirectory"/> and registers them into <paramref name="catalog"/>.
        /// </summary>
        public PluginLoadResult LoadFromDirectory(string pluginsDirectory, PluginCatalog catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));

            var result = new PluginLoadResult();

            if (string.IsNullOrWhiteSpace(pluginsDirectory))
            {
                result.Warnings.Add("Plugins directory is empty. No plugins were loaded.");
                return result;
            }

            if (!Directory.Exists(pluginsDirectory))
            {
                result.Warnings.Add("Plugins directory not found: " + pluginsDirectory);
                return result;
            }

            _logger?.Log(LogLevel.Info, "Loading plugins from: " + pluginsDirectory);

            // Use a resolver scoped to this plugins directory to support private dependencies.
            using (var resolver = new PluginAssemblyResolver(pluginsDirectory))
            {
                resolver.Register();

                string[] dlls;
                try
                {
                    dlls = Directory.GetFiles(pluginsDirectory, "*.dll", SearchOption.TopDirectoryOnly);
                }
                catch (Exception ex)
                {
                    _logger?.Log(LogLevel.Warn, "Failed to enumerate plugin directory: " + pluginsDirectory, ex);
                    result.Errors.Add(new PluginLoadError(pluginsDirectory, "Failed to enumerate plugin directory.", ex));
                    return result;
                }

                if (dlls.Length == 0)
                {
                    result.Warnings.Add("No DLLs were found in the plugins directory.");
                    return result;
                }

                for (int i = 0; i < dlls.Length; i++)
                {
                    var dllPath = dlls[i];

                    try
                    {
                        if (!AppearsToContainPlugins(dllPath, pluginsDirectory, result))
                        {
                            continue;
                        }

                        LoadSingleAssembly(dllPath, catalog, result);
                    }
                    catch (Exception ex)
                    {
                        // Intent: Catch-all to keep loading other plugins.
                        _logger?.Log(LogLevel.Error, "Unexpected error while loading a plugin DLL: " + dllPath, ex);
                        result.Errors.Add(new PluginLoadError(dllPath, "Unexpected error while loading plugin.", ex));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Determines whether a DLL likely contains IPlugin implementations without loading it into the runtime context.
        /// </summary>
        private bool AppearsToContainPlugins(string dllPath, string pluginsDirectory, PluginLoadResult result)
        {
            if (string.IsNullOrWhiteSpace(dllPath) || !File.Exists(dllPath))
            {
                return false;
            }

            // Reflection-only assembly resolver that probes the plugin folder.
            ResolveEventHandler roHandler = (sender, args) =>
            {
                try
                {
                    var requestedAssemblyName = new AssemblyName(args.Name);
                    var name = requestedAssemblyName.Name;
                    if (string.IsNullOrWhiteSpace(name)) return null;

                    try
                    {
                        return Assembly.ReflectionOnlyLoad(args.Name);
                    }
                    catch
                    {
                    }

                    var applicationBaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    var sharedCandidate = Path.Combine(applicationBaseDirectory, name + ".dll");
                    if (File.Exists(sharedCandidate))
                    {
                        return Assembly.ReflectionOnlyLoadFrom(sharedCandidate);
                    }

                    var pluginCandidate = Path.Combine(pluginsDirectory, name + ".dll");
                    if (File.Exists(pluginCandidate))
                    {
                        return Assembly.ReflectionOnlyLoadFrom(pluginCandidate);
                    }
                }
                catch
                {
                }

                return null;
            };

            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += roHandler;

            try
            {
                Assembly roAsm;
                try
                {
                    roAsm = Assembly.ReflectionOnlyLoadFrom(dllPath);
                }
                catch (BadImageFormatException ex)
                {
                    // Native DLL / wrong bitness / not .NET.
                    result.Warnings.Add("Ignoring non-.NET DLL in plugins folder: " + Path.GetFileName(dllPath));
                    result.Errors.Add(new PluginLoadError(dllPath, "DLL is not a valid .NET assembly.", ex));
                    return false;
                }
                catch (FileLoadException ex)
                {
                    result.Errors.Add(new PluginLoadError(dllPath, "Failed to reflection-load assembly.", ex));
                    return false;
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new PluginLoadError(dllPath, "Failed to reflection-load assembly.", ex));
                    return false;
                }

                Type[] types;
                try
                {
                    types = roAsm.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).ToArray();
                }
                catch
                {
                    types = new Type[0];
                }

                for (int i = 0; i < types.Length; i++)
                {
                    var t = types[i];
                    if (t == null) continue;
                    if (!t.IsClass || t.IsAbstract) continue;

                    var ifaces = t.GetInterfaces();
                    for (int j = 0; j < ifaces.Length; j++)
                    {
                        var it = ifaces[j];
                        if (it != null && string.Equals(it.FullName, PluginInterfaceFullName, StringComparison.Ordinal))
                        {
                            return true;
                        }
                    }
                }

                // Not an error: could be a dependency/core assembly.
                result.Warnings.Add("Ignoring non-plugin assembly in Plugins folder: " + Path.GetFileName(dllPath));
                return false;
            }
            finally
            {
                AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= roHandler;
            }
        }

        private void LoadSingleAssembly(string dllPath, PluginCatalog catalog, PluginLoadResult result)
        {
            // Basic path safety: ensure it exists.
            if (string.IsNullOrWhiteSpace(dllPath) || !File.Exists(dllPath))
            {
                result.Errors.Add(new PluginLoadError(dllPath, "Plugin DLL path is invalid or does not exist."));
                return;
            }

            Assembly asm;
            try
            {
                asm = Assembly.LoadFrom(dllPath);
            }
            catch (BadImageFormatException ex)
            {
                // Common case: native DLL or wrong bitness.
                result.Errors.Add(new PluginLoadError(dllPath, "DLL is not a valid .NET plugin assembly.", ex));
                return;
            }

            Type[] types;
            try
            {
                types = asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Some types may still be loadable; capture loader exceptions for diagnosis.
                types = ex.Types.Where(t => t != null).ToArray();
                result.Errors.Add(new PluginLoadError(dllPath, "Some types could not be loaded from plugin assembly.", ex));
            }

            bool anyPluginFound = false;

            for (int t = 0; t < types.Length; t++)
            {
                var type = types[t];
                if (type == null) continue;

                if (!typeof(IPlugin).IsAssignableFrom(type))
                {
                    continue;
                }

                if (type.IsAbstract || type.IsInterface)
                {
                    continue;
                }

                // Require a public parameterless constructor for plugin activation.
                var ctor = type.GetConstructor(Type.EmptyTypes);
                if (ctor == null || !ctor.IsPublic)
                {
                    result.Errors.Add(new PluginLoadError(
                        dllPath,
                        "Type '" + type.FullName + "' implements IPlugin but does not have a public parameterless constructor."));
                    continue;
                }

                anyPluginFound = true;

                try
                {
                    var plugin = (IPlugin)Activator.CreateInstance(type);

                    var meta = plugin.GetMetadata();
                    if (meta == null)
                    {
                        result.Errors.Add(new PluginLoadError(dllPath, "Plugin '" + type.FullName + "' returned null metadata."));
                        continue;
                    }

                    // Warn on duplicates. Catalog.Register() will overwrite.
                    var existing = catalog.FindPlugin(meta.PluginId);
                    if (existing != null)
                    {
                        result.Warnings.Add("Duplicate plugin id '" + meta.PluginId + "' detected. The later plugin overwrote the previous one.");
                    }

                    catalog.Register(plugin);
                    result.LoadedPlugins.Add(meta);

                    _logger?.Log(LogLevel.Info, "Loaded plugin: " + meta.DisplayName + " (" + meta.PluginId + ")");
                }
                catch (Exception ex)
                {
                    _logger?.Log(LogLevel.Error, "Failed to instantiate plugin type '" + type.FullName + "'.", ex);
                    result.Errors.Add(new PluginLoadError(dllPath, "Failed to instantiate plugin type '" + type.FullName + "'.", ex));
                }
            }

            if (!anyPluginFound)
            {
                // Should be rare because we pre-filtered, but keep as a safety net.
                result.Warnings.Add("No IPlugin implementations found in: " + Path.GetFileName(dllPath));
            }
        }
    }
}
