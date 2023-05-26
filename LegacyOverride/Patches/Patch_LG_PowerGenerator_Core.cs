using HarmonyLib;
using System.Collections.Generic;
using LevelGeneration;
using LEGACY.Utils;
using GameData;
using LEGACY.LegacyOverride.PowerGenerator.IndividualGenerator;

namespace LEGACY.LegacyOverride.Patches
{
    [HarmonyPatch]
    class Patch_LG_PowerGenerator_Core
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LG_PowerGenerator_Core), nameof(LG_PowerGenerator_Core.Setup))]
        private static void Post_PowerGenerator_Setup(LG_PowerGenerator_Core __instance)
        {
            if (IndividualGeneratorOverrideManager.Current.IsGCGenerator(__instance)) return;

            uint zoneInstanceIndex = IndividualGeneratorOverrideManager.Current.Register(__instance);

            List<ZoneGenerators> levelPGConfigs = IndividualGeneratorOverrideManager.Current.GetLevelPowerGeneratorOverride(RundownManager.ActiveExpedition.LevelLayoutData);

            // modify position / rotation if config specified. 
            if (levelPGConfigs == null || levelPGConfigs.Count < 1) return;

            int i = levelPGConfigs.FindIndex((zonePGConfig) =>
                __instance.SpawnNode.m_zone.LocalIndex == zonePGConfig.LocalIndex &&
                __instance.SpawnNode.m_zone.Layer.m_type == zonePGConfig.LayerType &&
                __instance.SpawnNode.m_dimension.DimensionIndex == zonePGConfig.DimensionIndex
            );

            if (i == -1) return;

            ZoneGenerators zonePGConfig = levelPGConfigs[i];

            i = zonePGConfig.IndividualGeneratorsInZone.FindIndex((config) => config.PowerGeneratorIndex == zoneInstanceIndex);

            if (i == -1) return;

            IndividualGenerator PGConfig = zonePGConfig.IndividualGeneratorsInZone[i];

            var position = PGConfig.Position.ToVector3();
            var rotation = PGConfig.Rotation.ToQuaternion();

            if (position != UnityEngine.Vector3.zero)
            {
                __instance.transform.position = position;
                __instance.transform.rotation = rotation;

                __instance.m_sound.UpdatePosition(position);

                LegacyLogger.Debug($"LG_PowerGenerator_Core: modified position / rotation");
            }

            if (PGConfig.ForceAllowPowerCellInsertion)
            {
                __instance.SetCanTakePowerCell(true);
            }

            if (PGConfig.EventsOnInsertCell != null && PGConfig.EventsOnInsertCell.Count > 0)
            {
                __instance.OnSyncStatusChanged += new System.Action<ePowerGeneratorStatus>((status) => {
                    if (status == ePowerGeneratorStatus.Powered)
                    {
                        PGConfig.EventsOnInsertCell.ForEach(e => WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(e, eWardenObjectiveEventTrigger.None, true));
                    }
                }); 
            }

            LegacyLogger.Debug($"LG_PowerGenerator_Core: overriden, instance {zoneInstanceIndex} in {zonePGConfig.LocalIndex}, {zonePGConfig.LayerType}, {zonePGConfig.DimensionIndex}");
        }
    }
}
