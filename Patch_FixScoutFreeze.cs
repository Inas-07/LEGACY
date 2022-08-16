using HarmonyLib;
using Enemies;

namespace LEGACY.Patch
{
    [HarmonyPatch]
    internal class Patch_FixScoutFreeze
    {
        //private static AgentTarget replacedAgent = null;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ES_ScoutScream), nameof(ES_ScoutScream.CommonUpdate))]
        private static bool Prefix_Debug(ES_ScoutScream __instance)
        {
            if (__instance.m_ai.Target == null) return false;
            //__instance.m_ai.Target = GOUtil.
            return true;
        }
    }
}
