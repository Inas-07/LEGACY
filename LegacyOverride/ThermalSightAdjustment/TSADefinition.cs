using ExtraObjectiveSetup.Utils;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;


namespace LEGACY.LegacyOverride.ThermalSightAdjustment
{
    public class TSADefinition
    {
        public uint GearPID { get; set; }

        [JsonPropertyName("DistanceFalloff")]
        [Range(0f, 1f)]
        public float HeatFalloff { get; set; } = 0.01f;

        [Range(0f, 1f)]
        public float FogFalloff { get; set; } = 0.1f;

        [JsonPropertyName("PixelZoom")]
        [Range(0f, 1f)]
        public float Zoom { get; set; } = 0.8f;

        [JsonPropertyName("AspectRatioAdjust")]
        [Range(0f, 2f)]
        public float RatioAdjust { get; set; } = 1f;

        [Range(0f, 1f)]
        public float DistortionCenter { get; set; } = 0.5f;

        public float DistortionScale { get; set; } = 1.0f;

        public float DistortionSpeed { get; set; } = 1.0f;

        public float DistortionSignalSpeed { get; set; } = 0.025f;

        [Range(0f, 1f)]
        public float DistortionMin { get; set; } = 0.01f;

        [Range(0f, 1f)]
        public float DistortionMax { get; set; } = 0.4f;

        [JsonPropertyName("AmbientTemperature")]
        [Range(0f, 1f)]
        public float AmbientTemp { get; set; } = 0.15f;
        
        [JsonPropertyName("BackgroundTemperature")]
        [Range(0f, 1f)]
        public float BackgroundTemp { get; set; } = 0.05f;

        [Range(0f, 10f)]
        public float AlbedoColorFactor { get; set; } = 0.5f;

        [Range(0f, 10f)]
        public float AmbientColorFactor { get; set; } = 5f;

        public float OcclusionHeat { get; set; } = 0.5f;

        public float BodyOcclusionHeat { get; set; } = 2.5f;

        [Range(0f, 1f)]
        public float ScreenIntensity { get; set; } = 0.2f;

        [Range(0f, 1f)]
        public float OffAngleFade { get; set; } = 0.95f;

        [Range(0f, 1f)]
        public float Noise { get; set; } = 0.1f;

        [JsonPropertyName("MinShadowEnemyDistortion")]
        [Range(0f, 1f)]
        public float DistortionMinShadowEnemies { get; set; } = 0.2f;

        [JsonPropertyName("MaxShadowEnemyDistortion")]
        [Range(0f, 1f)]
        public float DistortionMaxShadowEnemies { get; set; } = 1f;

        [Range(0f, 1f)]
        public float DistortionSignalSpeedShadowEnemies { get; set; } = 1f;

        public float ShadowEnemyFresnel { get; set; } = 10f;

        [Range(0f, 1f)]
        public float ShadowEnemyHeat { get; set; } = 0.1f;

        public EOSColor ReticuleColorA { get; set; } = new() { r = 1f, g = 1f, b = 1f, a = 1f };
        
        public EOSColor ReticuleColorB { get; set; } = new() { r = 1f, g = 1f, b = 1f, a = 1f };
        
        public EOSColor ReticuleColorC { get; set; } = new() { r = 1f, g = 1f, b = 1f, a = 1f };

        [Range(0f, 20f)]
        public float SightDirt { get; set; } = 0f;

        public bool LitGlass { get; set; } = false;

        public bool ClipBorders { get; set; } = true;

        public Vec4 AxisX { get; set; } = new();

        public Vec4 AxisY { get; set; } = new();
        
        public Vec4 AxisZ { get; set; } = new();

        public bool Flip { get; set; } = true;

        [JsonPropertyName("Distance1")]
        [Range(0f, 100f)]
        public float ProjDist1 { get; set; } = 100f;

        [JsonPropertyName("Distance2")]
        [Range(0f, 100f)]
        public float ProjDist2 { get; set; } = 66f;

        [JsonPropertyName("Distance3")]
        [Range(0f, 100f)]
        public float ProjDist3 { get; set; } = 33f;

        [JsonPropertyName("Size1")]
        [Range(0f, 3f)]
        public float ProjSize1 { get; set; } = 1f;

        [JsonPropertyName("Size2")]
        [Range(0f, 3f)]
        public float ProjSize2 { get; set; } = 1f;

        [JsonPropertyName("Size3")]
        [Range(0f, 3f)]
        public float ProjSize3 { get; set; } = 1f;

        [JsonPropertyName("Zeroing")]
        [Range(-1f, 1f)]
        public float ZeroOffset { get; set; } = 0f;

        public TSADefinition() { }
    }
}
