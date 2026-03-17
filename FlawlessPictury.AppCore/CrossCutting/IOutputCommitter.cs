using System.Threading;
using System.Threading.Tasks;

namespace FlawlessPictury.AppCore.CrossCutting
{
    /// <summary>
    /// Copies produced files from an internal output folder into the final output folder, applying collision handling.
    /// </summary>
    public interface IOutputCommitter
    {
        Task<OutputCommitResult> CommitFileAsync(string sourceFilePath, string destinationDirectory, string preferredFileName, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Result of committing one file to the final output directory.
    /// </summary>
    public sealed class OutputCommitResult
    {
        public OutputCommitResult(string destinationFilePath)
        {
            DestinationFilePath = destinationFilePath;
        }

        public string DestinationFilePath { get; }
    }
}
