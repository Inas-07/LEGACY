using ExtraObjectiveSetup.Utils;
using LEGACY.LegacyOverride.DummyVisual.VisualSequenceType;

namespace LEGACY.LegacyOverride.DummyVisual
{
    public enum VSequenceType
    { 
        DIRECTIONAL,
        CIRCULAR
    }


    public class VisualSequence
    {
        public float VisualRadius { get; set; } = 3.2f;

        public Vec3 VisualColor { get; set; } = new();

        public VSequenceType SequenceType { get; set; } = VSequenceType.DIRECTIONAL;

        public DirectionalSequence DirectionalSequence { get; set; } = new();

        public CircularSequence CircularSequence { get; set; } = new();
    }
}
