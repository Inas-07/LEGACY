namespace LEGACY.LegacyOverride.DummyVisual
{
    public class DirectionalConfig
    {
        public float ShowHideTime { get; set; } = 5.0f;

        public float DelayPerGO { get; set; } = 0.15f; // waiting time for showing the next GO in the sequence after this GO has been active.

        public int WindowSize { get; set; } = 4;

        public int WindowInterval { get; set; } = 2;

        public float UpdateInterval { get; set; } = 0.125f;
    }
}
