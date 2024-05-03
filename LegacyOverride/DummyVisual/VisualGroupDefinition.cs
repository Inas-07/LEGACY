using System.Collections.Generic;
using LEGACY.LegacyOverride.DummyVisual.VisualGOAnimation;
using Localization;

namespace LEGACY.LegacyOverride.DummyVisual
{
    public class VisualGroupDefinition
    {
        public string WorldEventObjectFilter { get; set; } = string.Empty;

        public VisualTemplateType VisualType { get; set; } = VisualTemplateType.SENSOR;

        public VisualAnimationType InitialAnimation { get; set; } = VisualAnimationType.OFF;

        public float InitialPlayDelay { get; set; } = 0.0f;

        public LocalizedText Text { get; set; } = null;

        public bool DisplayCylinder { get; set; } = false;

        public List<VisualSequence> VisualSequences { get; set; } = new() { new() };

        public VisualAnimationConfig AnimationConfig { get; set; } = new();

    }
}
