using System;
using System.Collections.Generic;
using FlawlessPictury.AppCore.CrossCutting;
using FlawlessPictury.AppCore.Plugins;
using FlawlessPictury.AppCore.Plugins.Pipeline;
using FlawlessPictury.AppCore.Stats;

namespace FlawlessPictury.AppCore.Plugins.Execution
{
    /// <summary>
    /// Provides the execution context for a pipeline step.
    /// </summary>
    public sealed class StepContext
    {
        /// <summary>
        /// Initializes a new context.
        /// </summary>
        public StepContext(
            WorkflowMode workflowMode,
            string workingDirectory,
            string tempDirectory,
            string outputDirectory,
            ILogger logger,
            IClock clock,
            IProcessRunner processRunner = null)
        {
            WorkflowMode = workflowMode;
            WorkingDirectory = workingDirectory;
            TempDirectory = tempDirectory;
            OutputDirectory = outputDirectory;
            Logger = logger;
            Clock = clock;
            ProcessRunner = processRunner;
            Environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Current workflow mode (safe output vs in-place).</summary>
        public WorkflowMode WorkflowMode { get; }

        /// <summary>Directory for step working files (may be inside temp).</summary>
        public string WorkingDirectory { get; }

        /// <summary>Directory for temporary/intermediate files.</summary>
        public string TempDirectory { get; }

        /// <summary>Directory where step-produced output artifacts should be written in safe mode.</summary>
        public string OutputDirectory { get; }

        /// <summary>Logger for diagnostics.</summary>
        public ILogger Logger { get; }

        /// <summary>Clock for timestamps and durations.</summary>
        public IClock Clock { get; }

        /// <summary>
        /// Optional process runner for steps that wrap external tools (ImageMagick, ffmpeg, ...).
        /// </summary>
        public IProcessRunner ProcessRunner { get; }

        /// <summary>
        /// Optional environment variables/settings for external tool invocations and host-provided metadata.
        /// </summary>
        public Dictionary<string, string> Environment { get; }

        /// <summary>
        /// Provides access to the loaded plugin catalog so orchestrator steps can inspect available capabilities.
        /// Hosts should set this. <see cref="Pipeline.PipelineExecutor"/> also sets it automatically when available.
        /// </summary>
        public IPluginCatalog PluginCatalog { get; set; }

        /// <summary>
        /// Provides a generic nested-execution API so orchestrator steps can ask the engine to run child work.
        /// Hosts should set this. <see cref="Pipeline.PipelineExecutor"/> also sets it automatically when available.
        /// </summary>
        public IStepExecutionHost ExecutionHost { get; set; }

        /// <summary>
        /// Declarative definition currently being executed. This lets orchestrator steps inspect
        /// their own mandatory child-step bindings without bypassing the engine.
        /// </summary>
        public PipelineStepDefinition CurrentStepDefinition { get; set; }

        /// <summary>
        /// Optional structured stats sink for reporting and analytics exports.
        /// </summary>
        public IRunStatsSink StatsSink { get; set; }

        /// <summary>
        /// Creates a child context for nested invocations, reusing shared services (logger, clock, process runner,
        /// plugin catalog, execution host, environment) while allowing different working/temp/output directories.
        /// </summary>
        public StepContext CreateChild(string workingDirectory, string tempDirectory, string outputDirectory)
        {
            var child = new StepContext(
                WorkflowMode,
                workingDirectory,
                tempDirectory,
                outputDirectory,
                Logger,
                Clock,
                ProcessRunner);

            foreach (var kv in Environment)
            {
                child.Environment[kv.Key] = kv.Value;
            }

            child.PluginCatalog = PluginCatalog;
            child.ExecutionHost = ExecutionHost;
            child.StatsSink = StatsSink;
            return child;
        }
    }
}
