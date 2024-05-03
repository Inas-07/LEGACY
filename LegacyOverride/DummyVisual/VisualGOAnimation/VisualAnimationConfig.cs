using LEGACY.LegacyOverride.DummyVisual.VisualGOAnimation.AnimationConfig;

namespace LEGACY.LegacyOverride.DummyVisual.VisualGOAnimation
{
    public class VisualAnimationConfig
    {
        public DirectionalConfig Directional { get; set; } = new();

        public BlinkConfig Blink { get; set; } = new();
    }
}
