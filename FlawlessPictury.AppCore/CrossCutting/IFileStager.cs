using System.Threading;
using System.Threading.Tasks;

namespace FlawlessPictury.AppCore.CrossCutting
{
    /// <summary>
    /// Stages an input file into a working directory (SafeOutput).
    /// </summary>
    public interface IFileStager
    {
        /// <summary>
        /// Copies (stages) an existing input file into a staging directory and returns the staged file path.
        /// </summary>
        Task<string> StageAsync(string inputFilePath, string stagingDirectory, CancellationToken cancellationToken);
    }
}
