using ChainedPuzzles;
using Enemies;
using ExtraObjectiveSetup.BaseClasses;
using ExtraObjectiveSetup.Expedition.Gears;
using ExtraObjectiveSetup.JSON;
using ExtraObjectiveSetup.Utils;
using GameData;
using GTFO.API;
using GTFO.API.Utilities;
using Il2CppSystem.Security.Cryptography;
using LEGACY.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LEGACY.LegacyOverride.ThermalSightAdjustment
{
    internal partial class TSAManager : GenericExpeditionDefinitionManager<TSADefinition>
    {
        public static TSAManager Current { get; } = new();

        protected override string DEFINITION_NAME => "ThermalSight";

        private Dictionary<uint, TSADefinition> ThermalGearDefs { get; } = new();

        private Dictionary<uint, Renderer[]> InLevelGearThermals { get; } = new();

        private HashSet<uint> ModifiedInLevelGearThermals { get; } = new();

        private HashSet<uint> ThermalOfflineGears { get; } = new();

        public uint CurrentGearPID { get; private set; } = 0u;

        private const bool OUTPUT_THERMAL_SHADER_SETTINGS_ON_WIELD = true;

        protected override void AddDefinitions(GenericExpeditionDefinition<TSADefinition> definitions)
        {
            base.AddDefinitions(definitions);

            foreach (var def in definitions.Definitions)
            {
                ThermalGearDefs[def.GearPID] = def;
            }
        }

        protected override void FileChanged(LiveEditEventArgs e)
        {
            base.FileChanged(e);
            InitThermalOfflineGears();
            CleanupInLevelGearThermals(keepCurrentGear: true);
            TrySetThermalSightRenderer(CurrentGearPID);
        }

        public override void Init()
        {
            InitThermalOfflineGears();
        }

        /// <summary>
        /// Find all thermal gears and add them to `ThermalGearDefs` with a null def.
        /// </summary>
        private void InitThermalOfflineGears()
        {
            ThermalOfflineGears.Clear();
            foreach (var b in GameDataBlockBase<PlayerOfflineGearDataBlock>.GetAllBlocks())
            {
                if (b.name.ToLowerInvariant().EndsWith("_t"))
                {
                    ThermalOfflineGears.Add(b.persistentID);
                    LegacyLogger.Debug($"Found OfflineGear with thermal sight - {b.name}");
                }
            }
        }

        private void TryAddInLevelGearThermals(ItemEquippable item, uint gearPID = 0)
        {
            if (item.GearIDRange == null)
            {
                return;
            }

            if(gearPID == 0)
            {
                gearPID = ExpeditionGearManager.GetOfflineGearPID(item.GearIDRange);
            }

            if (gearPID == 0 || !IsGearWithThermal(gearPID)) return;

            bool shouldAdd = false;
            if(!InLevelGearThermals.ContainsKey(gearPID))
            {
                shouldAdd = true;
            }
            else
            {
                try
                {
                    InLevelGearThermals[gearPID][0].gameObject.SetActive(true); // see if it's destroyed
                    shouldAdd = false;
                }
                catch
                {
                    shouldAdd = true;
                }
            }

            if (shouldAdd)
            {
                var renderers = item.GetComponentsInChildren<Renderer>(true);
                if (renderers != null)
                {
                    var tRender = renderers
                        .Where(x => x.sharedMaterial != null && x.sharedMaterial.shader != null)
                        .Where(x => x.sharedMaterial.shader.name.ToLower().Contains("Thermal".ToLower()))
                        .ToArray();

                    if (tRender != null)
                    {
                        if (tRender.Length != 1)
                        {
                            LegacyLogger.Warning($"{item.PublicName} contains more than 1 thermal renderer!");
                        }

                        InLevelGearThermals[gearPID] = tRender;
                    }
                }
                else
                {
                    LegacyLogger.Debug($"{item.PublicName}: thermal renderer not found");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gearPID"></param>
        /// <returns> `true` if the gear has thermal sight and its def is found, otherwise false. </returns>
        internal bool TrySetThermalSightRenderer(uint gearPID = 0u)
        {
            if(gearPID == 0u)
            {
                gearPID = CurrentGearPID;
            }

            if (!IsGearWithThermal(gearPID)) return false;

            if (ModifiedInLevelGearThermals.Contains(gearPID)) return true;

            if(ThermalGearDefs.TryGetValue(gearPID, out var def)
                && InLevelGearThermals.TryGetValue(gearPID, out var renderers))
            {
                LegacyLogger.Debug($"TrySetThermalSightRenderer: {gearPID}, renderer count: {renderers.Length}, setting...");
                foreach (var r in renderers)
                {
                    foreach(var prop in def.GetType().GetProperties())
                    {
                        var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                        var shaderProp = $"_{prop.Name}";
                        if (type == typeof(float))
                        {
                            r.material.SetFloat(shaderProp, (float)prop.GetValue(def));
                        }
                        else if(type == typeof(EOSColor))
                        {
                            var value = (EOSColor)prop.GetValue(def);
                            r.material.SetVector(shaderProp, value.ToUnityColor());
                        }
                        else if(type == typeof(bool))
                        {
                            var value = (bool)prop.GetValue(def);
                            r.material.SetFloat(shaderProp, value ? 1.0f : 0.0f);
                        }
                        else if(type == typeof(Vec4))
                        {
                            var value = (Vec4)prop.GetValue(def);
                            r.material.SetVector(shaderProp, value.ToVector4());
                        }
                        LegacyLogger.Debug($"prop: {shaderProp}");
                    }
                }

                ModifiedInLevelGearThermals.Add(gearPID);
                return true;
            }
            else
            {
                return false;
            }
        }

        internal void OnPlayerItemWielded(FirstPersonItemHolder fpsItemHolder, ItemEquippable item)
        {
            CurrentGearPID = ExpeditionGearManager.GetOfflineGearPID(item.GearIDRange);
            TryAddInLevelGearThermals(item, CurrentGearPID);
            SetPuzzleVisualsIntensity(1f);
            if (!TrySetThermalSightRenderer(CurrentGearPID) && OUTPUT_THERMAL_SHADER_SETTINGS_ON_WIELD)
            {
                if (InLevelGearThermals.TryGetValue(CurrentGearPID, out var renderers))
                {
                    LegacyLogger.Debug($"OnPlayerItemWielded: {CurrentGearPID}, renderer count: {renderers.Length}, setting...");
                    foreach (var r in renderers)
                    {
                        TSADefinition rDef = new() { GearPID = CurrentGearPID };
                        foreach (var prop in rDef.GetType().GetProperties())
                        {
                            var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                            var shaderProp = $"_{prop.Name}";
                            if (type == typeof(float))
                            {
                                prop.SetValue(rDef, r.material.GetFloat(shaderProp));
                            }
                            else if (type == typeof(EOSColor))
                            {
                                var value = r.material.GetVector(shaderProp);
                                prop.SetValue(rDef, new EOSColor() { r = value.x, g = value.y, b = value.z, a = value.w});
                            }
                            else if (type == typeof(bool))
                            {
                                prop.SetValue(rDef, r.material.GetFloat(shaderProp) == 1.0f);
                            }
                            else if (type == typeof(Vec4))
                            {
                                var value = r.material.GetVector(shaderProp);
                                prop.SetValue(rDef, new Vec4() { x = value.x, y = value.y, z = value.z, w = value.w });
                            }
                        }

                        LegacyLogger.Log(EOSJson.Serialize(rDef));
                    }
                }
            }
        }

        public bool IsGearWithThermal(uint gearPID) => ThermalOfflineGears.Contains(gearPID);

        private void CleanupInLevelGearThermals(bool keepCurrentGear = false)
        {
            if(!keepCurrentGear || !InLevelGearThermals.ContainsKey(CurrentGearPID))
            {
                InLevelGearThermals.Clear();
            }
            else
            {
                var renderers = InLevelGearThermals[CurrentGearPID];
                InLevelGearThermals.Clear();
                InLevelGearThermals[CurrentGearPID] = renderers;
            }

            ModifiedInLevelGearThermals.Clear();
        }

        private void Clear()
        {
            CurrentGearPID = 0;
            CleanupInLevelGearThermals();
            CleanupPuzzleVisuals();
        }

        private TSAManager() 
        {
            LevelAPI.OnBuildStart += Clear;
            LevelAPI.OnLevelCleanup += Clear;
            LevelAPI.OnEnterLevel += AddOBSVisualRenderers;
        }

        static TSAManager() { }
    }
}
