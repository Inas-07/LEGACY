using HarmonyLib;
using SNetwork;
using GTFO.API;
using GameEvent;
using FloLib.Infos;
using LEGACY.Utils;
using LEGACY.LegacyOverride.Restart;


namespace LEGACY.ExtraEvents.Patches
{
    [HarmonyPatch]
    internal static class SeamlessRestart
    {
        public static bool SeamlessRestartEnabled { get; internal set; } = false;

        public static bool HasCheckpoint => SNet.IsMaster && SNet.Capture.GotBuffer(eBufferType.Checkpoint);

        private static bool s_expeditionFail_Entered = false;

        [HarmonyPrefix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(GS_ExpeditionFail), nameof(GS_ExpeditionFail.Enter))]
        private static bool Pre_GS_ExpeditionFail_Enter(GS_ExpeditionFail __instance)
        {
            if (!HasCheckpoint || !SeamlessRestartEnabled) return true;

            __instance.IsReadyToGoToAfterLevel = false;
            __instance.m_isReadyToGoToAfterLevelTimer = 0f;
            MusicManager.Machine.ChangeState(MUS_State.Silence);
            GameEventManager.PostEvent(eGameEvent.gs_ExpeditionFail, AnalyticsManager.GetExpeditionEndPayload());
            s_expeditionFail_Entered = true;

            //CM_PageRestart.SetPageActive(true);
            return false;
        }

        [HarmonyPostfix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(GS_ExpeditionFail), nameof(GS_ExpeditionFail.Update))]
        private static void Post_GS_ExpeditionFail_Update(GS_ExpeditionFail __instance)
        {
            if(!s_expeditionFail_Entered || !__instance.IsReadyToGoToAfterLevel) return;

            if (SNet.IsMaster)
            {
                CheckpointManager.ReloadCheckpoint();
                SNet.Sync.SessionCommand(eSessionCommandType.ForceStartPlaying, 0);
                SNet.Sync.StartRecallCheckpointWithAllSyncedPlayers();
            }

            s_expeditionFail_Entered = false;
        }

        // local player 会重建，没法播放动画
        private static void OnRecallComplete(eBufferType eBufferType)
        {
            //CM_PageRestart.SetPageActive(false);
        }

        private static void Clear()
        {
            SeamlessRestartEnabled = false;
            s_expeditionFail_Entered = false;
        }

        static SeamlessRestart()
        {
            LevelAPI.OnBuildStart += Clear;
            LevelAPI.OnLevelCleanup += Clear;
            SNet_Events.OnRecallComplete += new System.Action<eBufferType>(OnRecallComplete);
        }
    }
}
