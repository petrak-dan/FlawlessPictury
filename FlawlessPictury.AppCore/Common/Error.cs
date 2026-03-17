using System;

namespace FlawlessPictury.AppCore.Common
{
    /// <summary>
    /// Represents a structured error that can be returned from AppCore services without throwing exceptions.
    /// </summary>
    /// <remarks>
    /// Use <see cref="Error"/> for expected failures (validation, incompatible pipeline, missing tool).
    /// Throw exceptions only for unexpected failures (bugs, IO corruption, etc.), and translate at boundaries.
    /// </remarks>
    public sealed class Error
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Error"/> class.
        /// </summary>
        /// <param name="code">A stable code used for programmatic handling and UI mapping.</param>
        /// <param name="message">A human-readable message suitable for display.</param>
        /// <param name="details">Optional technical details for logs (avoid secrets).</param>
        public Error(string code, string message, string details = null)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Details = details;
        }

        /// <summary>Stable error identifier (e.g., "validation.required", "pipeline.incompatible").</summary>
        public string Code { get; }

        /// <summary>User-facing message describing the error.</summary>
        public string Message { get; }

        /// <summary>Optional extra details for logs/debugging. Avoid secrets and sensitive paths.</summary>
        public string Details { get; }

        /// <summary>
        /// Creates a validation error.
        /// </summary>
        public static Error Validation(string message, string details = null)
        {
            // Intent: Standardize validation errors so UI can present consistent messaging.
            return new Error("validation", message, details);
        }

        /// <summary>
        /// Creates an error indicating a requested operation is not supported or not available.
        /// </summary>
        public static Error NotSupported(string message, string details = null)
        {
            return new Error("not_supported", message, details);
        }

        /// <summary>
        /// Creates an error indicating an external dependency is missing (e.g., an external tool).
        /// </summary>
        public static Error DependencyMissing(string message, string details = null)
        {
            return new Error("dependency_missing", message, details);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(Details)
                ? $"{Code}: {Message}"
                : $"{Code}: {Message} ({Details})";
        }
    }
}
