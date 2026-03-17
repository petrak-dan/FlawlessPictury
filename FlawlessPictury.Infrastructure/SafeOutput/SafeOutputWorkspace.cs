using System;
using System.IO;

namespace FlawlessPictury.Infrastructure.SafeOutput
{
    /// <summary>
    /// Per-run workspace for SafeOutput (stage/out/tmp).
    /// </summary>
    public sealed class SafeOutputWorkspace
    {
        private SafeOutputWorkspace(string runRoot, string stageDirectory, string internalOutputDirectory, string tempDirectory)
        {
            RunRoot = runRoot;
            StageDirectory = stageDirectory;
            InternalOutputDirectory = internalOutputDirectory;
            TempDirectory = tempDirectory;
        }

        public string RunRoot { get; }
        public string StageDirectory { get; }
        public string InternalOutputDirectory { get; }
        public string TempDirectory { get; }

        public static SafeOutputWorkspace Create(string baseTempRoot)
        {
            if (string.IsNullOrWhiteSpace(baseTempRoot))
            {
                baseTempRoot = Path.Combine(Path.GetTempPath(), "FlawlessPictury");
            }

            Directory.CreateDirectory(baseTempRoot);

            var runId = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fffffff") + "_" + Guid.NewGuid().ToString("N");
            var runRoot = Path.Combine(baseTempRoot, "Runs", runId);

            var stage = Path.Combine(runRoot, "stage");
            var outDir = Path.Combine(runRoot, "out");
            var tmp = Path.Combine(runRoot, "tmp");

            Directory.CreateDirectory(stage);
            Directory.CreateDirectory(outDir);
            Directory.CreateDirectory(tmp);

            return new SafeOutputWorkspace(runRoot, stage, outDir, tmp);
        }

        public void TryCleanup()
        {
            try
            {
                if (Directory.Exists(RunRoot))
                {
                    Directory.Delete(RunRoot, recursive: true);
                }
            }
            catch
            {
            }
        }
    }
}
