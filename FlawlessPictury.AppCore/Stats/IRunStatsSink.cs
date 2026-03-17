namespace FlawlessPictury.AppCore.Stats
{
    /// <summary>
    /// Optional sink for structured run stats. Hosts may provide an implementation;
    /// steps and the pipeline engine remain functional when no sink is configured.
    /// </summary>
    public interface IRunStatsSink
    {
        void Emit(StatsEvent statsEvent);
    }
}
