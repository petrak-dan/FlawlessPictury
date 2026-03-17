using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using FlawlessPictury.AppCore.Plugins;
using FlawlessPictury.AppCore.Plugins.Pipeline;
using FlawlessPictury.Infrastructure.CrossCutting;
using FlawlessPictury.Infrastructure.ExternalTools;
using FlawlessPictury.Infrastructure.Plugins;
using FlawlessPictury.Infrastructure.Presets;
using FlawlessPictury.Infrastructure.SafeOutput;
using FlawlessPictury.Infrastructure.Stats;
using FlawlessPictury.Presentation.WinForms.CrossCutting;
using FlawlessPictury.Presentation.WinForms.Presenters;
using FlawlessPictury.Presentation.WinForms.Views;

namespace FlawlessPictury.Presentation.WinForms
{
    internal static class Program
    {
        private static int ReadIntSetting(string key, int defaultValue)
        {
            var raw = ConfigurationManager.AppSettings[key];
            int parsed;
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) && parsed > 0
                ? parsed
                : defaultValue;
        }

        private static bool ReadBoolSetting(string key, bool defaultValue)
        {
            var raw = ConfigurationManager.AppSettings[key];
            bool parsed;
            return bool.TryParse(raw, out parsed) ? parsed : defaultValue;
        }

        private static string ReadStringSetting(string key, string defaultValue)
        {
            var raw = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(raw) ? defaultValue : raw;
        }

        [STAThread]
        private static void Main()
        {
            // Must be configured before any Control is created.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Portable-mode base directory: anchor to running EXE folder (not current working directory).
            var baseDir = ExecutablePathResolver.GetExecutableDirectory();

            var logsDir = Path.Combine(baseDir, "Logs");
            var presetsDir = Path.Combine(baseDir, "Presets");
            var pluginsDir = Path.Combine(baseDir, "Plugins");
            var statsDir = Path.Combine(baseDir, ReadStringSetting("Stats.CsvDirectory", "Stats"));
            var maxConcurrentFiles = ReadIntSetting("Runner.MaxConcurrentFiles", 4);
            var statsEnabled = ReadBoolSetting("Stats.EnableCsv", true);

            const int MaxUiLogLines = 20000;
            const long MaxLogFileBytes = 5L * 1024L * 1024L;
            const int MaxLogFiles = 5;

            using (var runnerView = new RunnerForm())
            using (var logForm = new LogForm())
            using (var presetEditorView = new PresetEditorForm())
            using (var fileLogger = new RollingFileLogger(logsDir, "FlawlessPictury.log", MaxLogFileBytes, MaxLogFiles))
            using (var csvStats = statsEnabled ? new CsvStatsSink(statsDir) : null)
            {
                var dispatcher = new WinFormsUiDispatcher(runnerView);
                var logHub = new UiLogHub(dispatcher, maxBufferedLines: MaxUiLogLines, minimumUiLevel: FlawlessPictury.AppCore.CrossCutting.LogLevel.Debug);
                var logger = new CompositeLogger(logHub, fileLogger);

// Startup identity and diagnostics.
var appName = Application.ProductName;
var appVersion = Application.ProductVersion;

logger.Log(FlawlessPictury.AppCore.CrossCutting.LogLevel.Info, "Welcome to " + appName + " " + appVersion);
logger.Log(FlawlessPictury.AppCore.CrossCutting.LogLevel.Info, "Mode: Portable");

// Debug-only paths for troubleshooting (UI log filters Debug; file logs keep it).
logger.Log(FlawlessPictury.AppCore.CrossCutting.LogLevel.Debug, "ExePath: " + Application.ExecutablePath);
logger.Log(FlawlessPictury.AppCore.CrossCutting.LogLevel.Debug, "BaseDir: " + baseDir);
logger.Log(FlawlessPictury.AppCore.CrossCutting.LogLevel.Debug, "LogsDir: " + logsDir);
logger.Log(FlawlessPictury.AppCore.CrossCutting.LogLevel.Debug, "LogFile: " + Path.Combine(logsDir, "FlawlessPictury.log"));
logger.Log(FlawlessPictury.AppCore.CrossCutting.LogLevel.Debug, "PluginsDir: " + pluginsDir);
logger.Log(FlawlessPictury.AppCore.CrossCutting.LogLevel.Debug, "PresetsDir: " + presetsDir);
logger.Log(FlawlessPictury.AppCore.CrossCutting.LogLevel.Debug, "ToolsDir: " + Path.Combine(baseDir, "Tools"));
logger.Log(FlawlessPictury.AppCore.CrossCutting.LogLevel.Debug, "TempRunsDir: " + Path.Combine(Path.GetTempPath(), "FlawlessPictury", "Runs"));
logger.Log(FlawlessPictury.AppCore.CrossCutting.LogLevel.Info, "Parallel files: max=" + maxConcurrentFiles.ToString(CultureInfo.InvariantCulture));
if (statsEnabled && csvStats != null)
{
    logger.Log(FlawlessPictury.AppCore.CrossCutting.LogLevel.Info, "Stats CSV: " + csvStats.FilePath);
}

                var presetRepo = new FilePresetRepository(presetsDir);
                var presetWorkspace = new PresetWorkspace(presetRepo);

                var logPresenter = new LogPresenter(logForm, logHub, logger, maxUiLines: MaxUiLogLines);
                logPresenter.SetOwner(runnerView);

                // Global exception safety nets (log + auto-show log window).
                Application.ThreadException += (s, e) =>
                {
                    try
                    {
                        logger.Log(FlawlessPictury.AppCore.CrossCutting.LogLevel.Error, "Unhandled UI exception.", e.Exception);
                        logPresenter.Show(runnerView);
                    }
                    catch { }
                };

                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    try
                    {
                        var ex = e.ExceptionObject as Exception;
                        logger.Log(FlawlessPictury.AppCore.CrossCutting.LogLevel.Error, "Unhandled domain exception.", ex);
                        logPresenter.Show(runnerView);
                    }
                    catch { }
                };

                TaskScheduler.UnobservedTaskException += (s, e) =>
                {
                    try
                    {
                        logger.Log(FlawlessPictury.AppCore.CrossCutting.LogLevel.Error, "Unobserved task exception.", e.Exception);
                        e.SetObserved();
                        logPresenter.Show(runnerView);
                    }
                    catch { }
                };

                var clock = new SystemClock();

                // Plugin system
                var catalog = new PluginCatalog();
                var loader = new PluginLoader(logger);

                var pluginEnv = new PluginEnvironment(
                    pluginsDirectory: pluginsDir,
                    loader: loader,
                    catalog: catalog,
                    logger: logger);

                var presetEditorPresenter = new PresetEditorPresenter(presetEditorView, presetWorkspace, pluginEnv, logger);

                // Pipeline engine
                var executor = new PipelineExecutor(catalog);

                // Safe output
                var stager = new FileStager();
                var committer = new OutputCommitter(logger);

                // External tools
                var processRunner = new ExternalProcessRunner(logger);

                var runnerPresenter = new RunnerPresenter(
                    runnerView,
                    dispatcher,
                    logger,
                    logHub,
                    clock,
                    pluginEnv,
                    executor,
                    stager,
                    committer,
                    presetWorkspace,
                    processRunner,
                    maxConcurrentFiles,
                    csvStats);

                runnerPresenter.OpenLogWindowRequested += (s, e) =>
                {
                    logPresenter.Toggle(runnerView);
                };

                runnerPresenter.OpenPluginExplorerRequested += (s, e) =>
                {
                    presetEditorPresenter.Show(runnerView);
                };

                Application.Run(runnerView);
                logger.Log(FlawlessPictury.AppCore.CrossCutting.LogLevel.Info, "Application exiting.");
            }
        }
    }
}
