using System.Threading;
using System.Threading.Tasks;
using FlawlessPictury.AppCore.Common;

namespace FlawlessPictury.AppCore.CrossCutting
{
    /// <summary>
    /// Abstraction for executing external tools (ImageMagick, ffmpeg, exiftool, ...).
    ///
    /// Design goals:
    /// - UI-agnostic and host-agnostic.
    /// - Supports cancellation and optional timeouts.
    /// - Captures stdout/stderr for logging and debugging.
    /// </summary>
    public interface IProcessRunner
    {
        /// <summary>
        /// Runs an external process with the supplied options.
        /// </summary>
        Task<Result<ProcessExecutionResult>> RunAsync(ProcessExecutionOptions options, CancellationToken cancellationToken);
    }
}
