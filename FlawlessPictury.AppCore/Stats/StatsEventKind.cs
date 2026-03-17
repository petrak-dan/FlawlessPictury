namespace FlawlessPictury.AppCore.Stats
{
    /// <summary>
    /// High-level event kinds emitted by the host runtime for reporting and analytics.
    /// </summary>
    public enum StatsEventKind
    {
        RunStarted = 0,
        FileStarted = 1,
        StepStarted = 2,
        StepCompleted = 3,
        CandidateEvaluated = 4,
        FileCompleted = 5,
        FileFailed = 6,
        RunCompleted = 7
    }
}
