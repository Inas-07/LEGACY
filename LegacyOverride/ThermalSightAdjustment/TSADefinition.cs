using ExtraObjectiveSetup.Utils;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;


namespace LEGACY.LegacyOverride.ThermalSightAdjustment
{
    public class TSADefinition
    {
        public float OffAimPixelZoom { get; set; } = 1.0f;

        public TSShader Shader { get; set; } = new();
    }
}
