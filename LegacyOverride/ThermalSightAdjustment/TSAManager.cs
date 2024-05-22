using ExtraObjectiveSetup.BaseClasses;
using ExtraObjectiveSetup.Expedition.Gears;
using ExtraObjectiveSetup.JSON;
using ExtraObjectiveSetup.Utils;
using GameData;
using GTFO.API;
using GTFO.API.Utilities;
using LEGACY.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LEGACY.LegacyOverride.ThermalSightAdjustment
{

    internal partial class TSAManager : GenericDefinitionManager<TSADefinition>
    {
        public static TSAManager Current { get; } = new();

        protected override string DEFINITION_NAME => "ThermalSight";

        //private Dictionary<uint, TSADefinition> ThermalGearDefs { get; } = new();

        private Dictionary<uint, Renderer[]> InLevelGearThermals { get; } = new();

        private HashSet<uint> ModifiedInLevelGearThermals { get; } = new();

        private HashSet<uint> ThermalOfflineGears { get; } = new();

        public uint CurrentGearPID { get; private set; } = 0u;

        private const bool OUTPUT_THERMAL_SHADER_SETTINGS_ON_WIELD = false;

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
                }
            }
            LegacyLogger.Debug($"Found OfflineGear with thermal sight, count: {ThermalOfflineGears.Count}");
        }

        private bool TryGetInLevelGearThermalRenderersFromItem(ItemEquippable item, uint gearPID, out Renderer[] renderers)
        {
            renderers = null;
            if (item.GearIDRange == null)
            {
                return false;
            }

            if(gearPID == 0)
            {
                gearPID = ExpeditionGearManager.GetOfflineGearPID(item.GearIDRange);
            }

            if (gearPID == 0 || !IsGearWithThermal(gearPID)) return false;

            bool shouldAdd = false;
            if(!InLevelGearThermals.ContainsKey(gearPID))
            {
                shouldAdd = true;
            }
            else
            {
                try
                {
                    var _ = InLevelGearThermals[gearPID][0].gameObject.transform.position;
                    shouldAdd = false;
                }
                catch
                {
                    ModifiedInLevelGearThermals.Remove(gearPID);
                    shouldAdd = true;
                }
            }

            if (shouldAdd)
            {
                renderers = item.GetComponentsInChildren<Renderer>(true)
                    ?.Where(x => x.sharedMaterial != null && x.sharedMaterial.shader != null)
                    .Where(x => x.sharedMaterial.shader.name.ToLower().Contains("Thermal".ToLower()))
                    .ToArray() ?? null;

                if (renderers != null)
                {
                    if (renderers.Length != 1)
                    {
                        LegacyLogger.Warning($"{item.PublicName} contains more than 1 thermal renderers!");
                    }

                    InLevelGearThermals[gearPID] = renderers;
                }
                else
                {
                    LegacyLogger.Debug($"{item.PublicName}: thermal renderer not found");
                    return false;
                }

                return true;
            }
            else
            {
                renderers = InLevelGearThermals[gearPID];
                return true;
            }
        }

        /// <summary>
        /// Apply the entire TSShader to this thermal sight.
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

            if(definitions.TryGetValue(gearPID, out var definition)
                && InLevelGearThermals.TryGetValue(gearPID, out var renderers))
            {
                var def = definition.Definition;
                TSShader shader = def.Shader;

                foreach (var r in renderers)
                {
                    foreach(var prop in shader.GetType().GetProperties())
                    {
                        var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                        var shaderProp = $"_{prop.Name}";
                        if (type == typeof(float))
                        {
                            r.material.SetFloat(shaderProp, (float)prop.GetValue(shader));
                        }
                        else if(type == typeof(EOSColor))
                        {
                            var value = (EOSColor)prop.GetValue(shader);
                            r.material.SetVector(shaderProp, value.ToUnityColor());
                        }
                        else if(type == typeof(bool))
                        {
                            var value = (bool)prop.GetValue(shader);
                            r.material.SetFloat(shaderProp, value ? 1.0f : 0.0f);
                        }
                        else if(type == typeof(Vec4))
                        {
                            var value = (Vec4)prop.GetValue(shader);
                            r.material.SetVector(shaderProp, value.ToVector4());
                        }
                        //LegacyLogger.Debug($"{shaderProp}");
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
            if(item.GearIDRange == null)
            {
                CurrentGearPID = 0;
                return;
            }

            CurrentGearPID = ExpeditionGearManager.GetOfflineGearPID(item.GearIDRange);
            TryGetInLevelGearThermalRenderersFromItem(item, CurrentGearPID, out var _);
            if (!TrySetThermalSightRenderer(CurrentGearPID) && OUTPUT_THERMAL_SHADER_SETTINGS_ON_WIELD)
            {
                if (InLevelGearThermals.TryGetValue(CurrentGearPID, out var renderers))
                {
                    foreach (var r in renderers)
                    {
                        TSShader tsshader = new();
                        foreach (var prop in tsshader.GetType().GetProperties())
                        {
                            var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                            var shaderProp = $"_{prop.Name}";
                            if (type == typeof(float))
                            {
                                prop.SetValue(tsshader, r.material.GetFloat(shaderProp));
                            }
                            else if (type == typeof(EOSColor))
                            {
                                var value = r.material.GetVector(shaderProp);
                                prop.SetValue(tsshader, new EOSColor() { r = value.x, g = value.y, b = value.z, a = value.w});
                            }
                            else if (type == typeof(bool))
                            {
                                prop.SetValue(tsshader, r.material.GetFloat(shaderProp) == 1.0f);
                            }
                            else if (type == typeof(Vec4))
                            {
                                var value = r.material.GetVector(shaderProp);
                                prop.SetValue(tsshader, new Vec4() { x = value.x, y = value.y, z = value.z, w = value.w });
                            }
                        }

                        LegacyLogger.Log($"GearPID: {CurrentGearPID}, shader setting:\n{EOSJson.Serialize(tsshader)}");
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

        internal void SetCurrentThermalSightSettings(float t)
        {
            if (!InLevelGearThermals.TryGetValue(CurrentGearPID, out var renderers) 
                || !definitions.TryGetValue(CurrentGearPID, out var def)) return;

            foreach (var r in renderers)
            {
                float OnAimZoom = def.Definition.Shader.Zoom;
                float OffAimZoom = def.Definition.OffAimPixelZoom;
                float zoom = Mathf.Lerp(OnAimZoom, OffAimZoom, t);
                
                r.material.SetFloat("_Zoom", zoom);
            }
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
