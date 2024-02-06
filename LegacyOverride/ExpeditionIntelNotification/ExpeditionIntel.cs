
using Localization;

namespace LEGACY.LegacyOverride.ExpeditionIntelNotification
{
    public class ExpeditionIntel
    {
        public bool BlinkInContent { get; set; } = true;

        public float BlinkTimeInterval { get; set; } = 0.2f;

        public LocalizedText Header { get; set; } = null;

        public LocalizedText Text { get; set; } = null;
    }
}
