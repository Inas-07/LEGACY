using System.Collections.Generic;
using Localization;

namespace LEGACY.LegacyOverride.DummyVisual
{
    public class VisualGroupDefinition
    {
        public string WorldEventObjectFilter { get; set; } = string.Empty;

        public MaterialType VisualType { get; set; } = MaterialType.Sensor;

        public VisualType InitialVisual { get; set; } = DummyVisual.VisualType.OFF;

        public List<VisualSequence> VisualSequences { get; set; } = new() { new() };

        public LocalizedText Text { get; set; } = null;

        public bool DisplayCylinder { get; set; } = false;

        public DirectionalConfig DirectionalConfig { get; set; } = new();

        public BlinkConfig BlinkConfig { get; set; } = new();

    }
}
