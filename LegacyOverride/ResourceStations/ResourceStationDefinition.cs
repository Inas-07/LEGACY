using ExtraObjectiveSetup;
using ExtraObjectiveSetup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEGACY.LegacyOverride.ResourceStations
{
    public enum StationType
    {
        MEDI,
        AMMO,
        TOOL,
    }

    public class SupplyUplimit 
    {
        public float Medi { get; set; } = 0.6f;

        public float AmmoStandard { get; set; } = 1.0f;

        public float AmmoSpecial { get; set; } = 1.0f;

        public float Tool { get; set; } = 0.0f;
    }


    public class SupplyEfficiency 
    {
        public float Medi { get; set; } = 0.2f;

        public float AmmoStandard { get; set; } = 0.15f;
        
        public float AmmoSpecial { get; set; } = 0.15f;

        public float Tool { get; set; } = 0.0f;
    }


    public class ResourceStationDefinition: ExtraObjectiveSetup.BaseClasses.GlobalZoneIndex
    {
        public int AreaIndex { get; set; } = 0;

        public string WorldEventObjectFilter { get; set; } = string.Empty;

        public StationType StationType { get; set; } = StationType.AMMO;

        public Vec3 Position { get; set; } = new();

        public Vec3 Rotation { get; set; } = new();

        public float InteractDuration { get; set; } = 2.5f;

        public SupplyEfficiency SupplyEfficiency { get; set; } = new();

        public SupplyUplimit SupplyUplimit { get; set; } = new();

        public int AllowedUseTimePerCooldown { get; set; } = ResourceStation.UNLIMITED_USE_TIME;

        public float CooldownTime { get; set; } = 3f;
    }
}
