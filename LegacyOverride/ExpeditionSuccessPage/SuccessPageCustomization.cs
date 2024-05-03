using Localization;

namespace LEGACY.LegacyOverride.ExpeditionSuccessPage
{
    public class SuccessPageCustomization
    {
        public string WorldEventObjectFilter { get; set; } = string.Empty;

        public LocalizedText PageHeader { get; set; } = null; // 'new' crashes on startup 

        public uint OverrideSuccessMusic { get; set; } = 0;

        public bool SetAsDefaultCustomization { get; set; } = false;
    }
}
