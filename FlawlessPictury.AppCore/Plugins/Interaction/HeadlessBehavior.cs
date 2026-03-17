namespace FlawlessPictury.AppCore.Plugins.Interaction
{
    /// <summary>
    /// Defines behavior for interactive requests when running without UI (batch mode).
    /// </summary>
    public enum HeadlessBehavior
    {
        /// <summary>Fail the pipeline with an error.</summary>
        Fail = 0,

        /// <summary>Proceed using default values/choices supplied in the request.</summary>
        UseDefaults = 1,

        /// <summary>Skip the step.</summary>
        SkipStep = 2
    }
}
