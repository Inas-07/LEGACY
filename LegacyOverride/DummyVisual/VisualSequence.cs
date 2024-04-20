using ExtraObjectiveSetup.Utils;

namespace LEGACY.LegacyOverride.DummyVisual
{
    public class VisualSequence
    {
        public float Radius { get; set; } = 3.2f;

        public Vec3 Color { get; set; } = new();

        public Vec3 StartPosition { get; set; } = new Vec3();

        public Vec3 Rotation { get; set; } = new Vec3();

        public Vec3 ExtendDirection { get; set; } = new();

        public float PlacementInterval { get; set; } = 6.4f;

        public int Count { get; set; } = 1;

    }
}
