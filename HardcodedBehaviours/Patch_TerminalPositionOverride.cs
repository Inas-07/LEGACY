using HarmonyLib;
using LevelGeneration;
using GameData;

namespace LEGACY.HardcodedBehaviours
{
    [HarmonyPatch]
    class Patch_TerminalPositionOverride
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LG_ComputerTerminal), nameof(LG_ComputerTerminal.Setup))]
        private static void Post_ChangeTerminalPosition(LG_ComputerTerminal __instance)
        {
            //if (RundownManager.ActiveExpedition.MainLayerData.ObjectiveData.DataBlockId != 30000u) return;

            //switch (__instance.SpawnNode.m_zone.LocalIndex)
            //{
            //    case eLocalZoneIndex.Zone_17:
            //        __instance.transform.position = new() { x = 337.4885f, y = -29.9895f, z = 191.8546f };
            //        __instance.transform.Rotate(0f, -90f, 0f);
            //        break;

            //    case eLocalZoneIndex.Zone_13:
            //        __instance.transform.position = new() { x = 559.4702f, y = -71.9855f, z = 192.0474f };
            //        __instance.transform.Rotate(0f, -90f, 0f);
            //        break;
            //}
        }
    }
}
