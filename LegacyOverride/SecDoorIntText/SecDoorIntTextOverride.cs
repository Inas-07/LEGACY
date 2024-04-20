using Localization;

namespace LEGACY.LegacyOverride.SecDoorIntText
{
    public enum GlitchMode
    {
        None,
        Style1,
        Style2
    }

    public class SecDoorIntTextOverride : ExtraObjectiveSetup.BaseClasses.GlobalZoneIndex
    {
        public LocalizedText Prefix { get; set; } = null;

        public LocalizedText Postfix { get; set; } = null;

        public LocalizedText TextToReplace { get; set; } = null;
 
        public GlitchMode GlitchMode { get; set; } = GlitchMode.None; 
    }
}
