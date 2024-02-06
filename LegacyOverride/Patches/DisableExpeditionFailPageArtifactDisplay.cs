using AK;
using BoosterImplants;
using CellMenu;
using HarmonyLib;
using SNetwork;
using Localization;
using System.Collections;
using UnityEngine;
using BepInEx.Unity.IL2CPP.Utils.Collections;

namespace LEGACY.LegacyOverride.Patches
{
    [HarmonyPatch]
    internal class DisableExpeditionFailPageArtifactDisplay
    {
        private class CustomExpeditionFailedSequence
        {
            private CM_PageExpeditionFail page;

            public CustomExpeditionFailedSequence(CM_PageExpeditionFail page)
            {
                this.page = page;
            }

            public IEnumerator CustomFailedSequence()
            {
                yield return new WaitForSeconds(0.5f);
                page.m_bgCortex_text.gameObject.SetActive(false);
                page.m_bgCortex_textBoxSmall.SetActive(false);
                page.m_bgCortex_textBoxLarge.SetActive(false);
                page.m_returnToLobby_text.gameObject.SetActive(false);
                CM_PageBase.PostSound(EVENTS.PLAY_01_FIRST_TEXT_APPEAR, "");
                yield return CoroutineManager.BlinkIn(page.m_bgCortex_logo, 0f);
                yield return new WaitForSeconds(0.3f);
                CM_PageBase.PostSound(EVENTS.PLAY_01_FIRST_TEXT_APPEAR, "");
                yield return CoroutineManager.BlinkIn(page.m_bgCortex_text, 0f, null);
                yield return new WaitForSeconds(0.4f);
                CoroutineManager.BlinkIn(page.m_bgCortex_textBoxSmall, 0f);
                CoroutineManager.BlinkIn(page.m_bgCortex_textBoxLarge, 0.15f);
                yield return new WaitForSeconds(0.5f);
                yield return CoroutineManager.BlinkOut(page.m_bgCortex_textBoxLarge, 0f);
                yield return CoroutineManager.BlinkOut(page.m_bgCortex_textBoxSmall, 0f);
                page.m_bgCortex_text.gameObject.SetActive(false);
                yield return CoroutineManager.BlinkOut(page.m_bgCortex_logo, 0f);
                yield return new WaitForSeconds(1f);
                CM_PageBase.PostSound(EVENTS.PLAY_06_MAIN_MENU_LAUNCH, "");
                page.m_bgScare.gameObject.SetActive(true);
                page.m_bgScare.Play();
                yield return new WaitForSeconds(0.5f);
                page.m_bgScare.gameObject.SetActive(false);
                yield return CoroutineManager.BlinkIn(page.m_missionFailed_text.gameObject, 0f);
                yield return new WaitForSeconds(0.2f);
                //yield return CoroutineManager.BlinkIn(page.m_ArtifactInventoryDisplay.gameObject, 0f);
                //yield return CoroutineManager.BlinkIn(page.m_artifactInfo_text.gameObject, 0f); 
                //yield return new WaitForSeconds(0.2f);
                yield return CoroutineManager.BlinkIn(page.m_returnToLobby_text.gameObject, 0f);
                yield break;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CM_PageExpeditionFail), nameof(CM_PageExpeditionFail.OnEnable))]
        private static bool Pre_CM_PageExpeditionFail(CM_PageExpeditionFail __instance)
        {
            if (__instance.m_isSetup)
            {
                CellSound.StopAll();
                CellSound.AllSoundsStoppedThisSession = true;
                CM_PageBase.PostSound(EVENTS.MUSIC_EXPEDITION_FAILED, "");
                __instance.m_ArtifactInventoryDisplay.Setup();
                __instance.m_ArtifactInventoryDisplay.SetArtifactValuesFromInventory(BoosterImplantManager.ArtifactInventory);
                __instance.m_missionFailed_text.gameObject.SetActive(false);
                __instance.m_artifactInfo_text.gameObject.SetActive(false);
                __instance.m_btnRestartCheckpoint.SetText(Text.Get(916U));
                if (SNet.IsMaster)
                {
                    __instance.m_btnRestartCheckpoint.SetButtonEnabled(true);
                    __instance.m_btnGoToLobby.SetButtonEnabled(true);
                    __instance.m_btnGoToLobby.SetText(Text.Get(917U));
                }
                else
                {
                    __instance.m_btnRestartCheckpoint.SetButtonEnabled(false);
                    __instance.m_btnGoToLobby.SetButtonEnabled(false);
                    __instance.m_btnGoToLobby.SetText(Text.Get(918U));
                }
                __instance.m_btnGoToLobby.gameObject.SetActive(false);
                __instance.m_btnRestartCheckpoint.gameObject.SetActive(false);
                __instance.m_lobbyButtonVisible = false;
                __instance.m_showLobbybuttonTimer = Clock.Time + 3f;
                __instance.m_ArtifactInventoryDisplay.SetVisible(false);

                // the only modified part is here
                __instance.StartPageTransitionRoutine(new CustomExpeditionFailedSequence(__instance).CustomFailedSequence().WrapToIl2Cpp());
            }

            return false;
        }
    }
}
