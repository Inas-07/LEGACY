using AK;
using CellMenu;
using HarmonyLib;
using SNetwork;
using Localization;
using System.Collections;
using UnityEngine;
using LEGACY.Utils;
using GameData;

namespace LEGACY.LegacyOverride.Patches
{
    [HarmonyPatch]
    internal class RundownSelectionCustomization
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CM_PageRundown_New), nameof(CM_PageRundown_New.Setup))]
        private static void Post_CM_PageRundown_New(CM_PageRundown_New __instance)
        {
            var p = __instance;

            var db = GameDataBlockBase<GameSetupDataBlock>.GetBlock(1);

            if (db.RundownIdsToLoad.Count <= 1) return;

            switch (db.RundownIdsToLoad.Count)
            {
                case 2:
                    /*
                        [Warning:LEGACYCore] (-476.0, 45.0, 0.0)
                        [Warning:LEGACYCore] (317.0, -104.0, 0.0)
                     */
                    p.m_rundownSelectionPositions[0] = new(-320.0f, 75f, 0f);
                    p.m_rundownSelectionPositions[1] = new(320.0f, 0f, 0f);

                    var oriPos = Vector3.zero;
                    for (int i = db.RundownIdsToLoad.Count; i < p.m_rundownSelectionPositions.Count; i++)
                    {
                        oriPos = p.m_rundownSelectionPositions[i];
                        p.m_rundownSelectionPositions[i] = new(oriPos.x, -10000f, oriPos.z);
                    }

                    oriPos = p.m_textRundownHeaderTop.transform.position;
                    p.m_textRundownHeaderTop.transform.position = new(oriPos.x, -350f, oriPos.z); // CLCTR multithread text
                    break;
                default: break;
            }
        }

        private static void SetSelectionScale(CM_PageRundown_New p)
        {
            var db = GameDataBlockBase<GameSetupDataBlock>.GetBlock(1);

            if (db.RundownIdsToLoad.Count <= 1) return;

            switch (db.RundownIdsToLoad.Count)
            {
                case 2:
                    var r1 = p.m_rundownSelections[0];
                    var r2 = p.m_rundownSelections[1];

                    r1.transform.localScale = new Vector3(2.0f, 2.0f, 2.0f) * 1.625f;
                    r2.transform.localScale = new Vector3(1.25f, 1.25f, 1.25f) * 1.5f;
                    r1.m_rundownText.transform.localRotation = r2.m_rundownText.transform.localRotation = Quaternion.AngleAxis(45.0f, Vector3.right);

                    r1.m_rundownText.transform.localPosition = Vector3.up * 60f + Vector3.right * 20f;
                    r2.m_rundownText.transform.localPosition = Vector3.up * 85f + Vector3.left * 25f;

                    r1.transform.localRotation = r2.transform.localRotation = Quaternion.AngleAxis(-45.0f, Vector3.right);

                    r1.m_rundownText.text = r1TitleID != 0 ? Text.Get(r1TitleID) : "<size=50%><color=#00ae9d>[ LEGACY ]</color></size>";
                    r2.m_rundownText.text = r2TitleID != 0 ? Text.Get(r2TitleID) : "<size=80%><color=#009ad6>[ L-OMNI ]</color></size>";

                    void DestroyAltText(CM_RundownSelection s)
                    {
                        if (s.m_altText != null)
                        {
                            Object.Destroy(s.m_altText.gameObject);
                            s.m_altText = null;
                        }
                    }

                    DestroyAltText(r1);
                    DestroyAltText(r2);
                    break;
                default: break;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CM_PageRundown_New), nameof(CM_PageRundown_New.UpdateRundownSelectionButton))]
        private static void Post_UpdateRundownSelectionButton(CM_PageRundown_New __instance)
        {
            InitTitle();
            SetSelectionScale(__instance);
        }

        private static uint r1TitleID = 0;
        private static uint r2TitleID = 0;

        private static void InitTitle()
        {
            if (r1TitleID != 0) return;

            var db = GameDataBlockBase<TextDataBlock>.GetBlock("LEGACY_Title");
            if (db != null)
            {
                r1TitleID = db.persistentID;
            }

            db = GameDataBlockBase<TextDataBlock>.GetBlock("LEGACY-Omni_Title");
            if (db != null)
            {
                r2TitleID = db.persistentID;
            }
        }

        private static IEnumerator reverseReveal(CM_PageRundown_New p, bool hosting, Transform guixSurfaceTransform)
        {
            float arrowScale = p.m_tierSpacing * 5f * p.m_tierSpaceToArrowScale;
            if (hosting)
            {
                CoroutineManager.BlinkIn(p.m_buttonConnect, 0f, null);
                yield return new WaitForSeconds(0.1f);
                yield return new WaitForSeconds(0.24f);
                p.m_buttonConnect.SetVisible(false);
            }
            CM_PageBase.PostSound(EVENTS.MENU_SURFACE_LEVEL_MOVE_UP, "");
            //yield return CoroutineEase.EaseLocalPos(p.m_rundownHolder, Vector3.zero, new Vector3(0f, 650f, 0f), 0.5f, Easing.LinearTween, null, null);
            yield return CoroutineEase.EaseLocalPos(guixSurfaceTransform, Vector3.zero, new Vector3(0f, 650f, 0f), 0.5f, Easing.LinearTween, null, null);

            yield return new WaitForSeconds(0.1f);
            CM_PageBase.PostSound(EVENTS.MENU_SURFACE_LEVEL_SHRINK, "");
            CoroutineEase.EaseLocalScale(p.m_textRundownHeader.transform, Vector3.one, new Vector3(0.6f, 0.6f, 0.6f), 0.2f, Easing.LinearTween, null, null);
            yield return CoroutineEase.EaseLocalScale(guixSurfaceTransform, Vector3.one, new Vector3(0.2f, 0.2f, 0.2f), 0.2f, Easing.LinearTween, null, null);
            yield return new WaitForSeconds(0.1f);
            CoroutineEase.EaseLocalPos(p.m_textRundownHeader.transform, p.m_textRundownHeader.transform.localPosition, p.m_rundownHeaderPos, 0.2f, Easing.LinearTween, null, null);
            CoroutineManager.BlinkIn(p.m_rundownIntelButton, 0f, null);
            yield return new WaitForSeconds(0.2f);
            CM_PageBase.PostSound(EVENTS.MENU_SURFACE_LEVEL_TURN, "");
            yield return CoroutineEase.EaseLocalRot(guixSurfaceTransform, Vector3.zero, new Vector3(70f, 0f, 0f), 0.3f, Easing.LinearTween, null, null);
            p.m_verticalArrow.SetActive(true);
            yield return new WaitForSeconds(0.5f);
            CoroutineManager.BlinkIn(p.m_tierMarkerSectorSummary.gameObject, 0f);
            CM_PageBase.PostSound(EVENTS.MENU_RUNDOWN_DISC_APPEAR_5, "");
            yield return new WaitForSeconds(0.5f);

            CM_PageBase.PostSound(EVENTS.MENU_RUNDOWN_SPINE_START, "");
            CoroutineEase.EaseLocalScale(p.m_verticalArrow.transform, new Vector3(1f, 0f, 1f), new Vector3(1f, arrowScale, 1f), 4.3f, Easing.LinearTween, () => {
                CM_PageBase.PostSound(EVENTS.MENU_RUNDOWN_SPINE_STOP, "");
            }, null);
            float tierMarkerDelay = 0.6f;
            yield return new WaitForSeconds(0.2f);


            CM_PageBase.PostSound(EVENTS.MENU_RUNDOWN_DISC_APPEAR_3, "");
            p.m_guix_Tier3.gameObject.SetActive(true);
            for (int k = 0; k < p.m_expIconsTier3.Count; k++)
            {
                CoroutineManager.BlinkIn(p.m_expIconsTier3[k].gameObject, k * 0.1f);
            }
            if (p.m_expIconsTier3.Count > 0)
            {
                p.m_tierMarker3.SetVisible(true, tierMarkerDelay);
            }
            yield return new WaitForSeconds(1f);


            CM_PageBase.PostSound(EVENTS.MENU_RUNDOWN_DISC_APPEAR_5, "");
            p.m_guix_Tier5.gameObject.SetActive(true);
            for (int m = 0; m < p.m_expIconsTier5.Count; m++)
            {
                CoroutineManager.BlinkIn(p.m_expIconsTier5[m].gameObject, m * 0.1f);
            }
            if (p.m_expIconsTier5.Count > 0)
            {
                p.m_tierMarker5.SetVisible(true, tierMarkerDelay);
            }
            yield return new WaitForSeconds(1f);


            CM_PageBase.PostSound(EVENTS.MENU_RUNDOWN_DISC_APPEAR_4, "");
            p.m_guix_Tier4.gameObject.SetActive(true);
            for (int l = 0; l < p.m_expIconsTier4.Count; l++)
            {
                CoroutineManager.BlinkIn(p.m_expIconsTier4[l].gameObject, l * 0.1f);
            }
            if (p.m_expIconsTier4.Count > 0)
            {
                p.m_tierMarker4.SetVisible(true, tierMarkerDelay);
            }
            yield return new WaitForSeconds(1f);


            CM_PageBase.PostSound(EVENTS.MENU_RUNDOWN_DISC_APPEAR_2, "");
            p.m_guix_Tier2.gameObject.SetActive(true);
            for (int j = 0; j < p.m_expIconsTier2.Count; j++)
            {
                CoroutineManager.BlinkIn(p.m_expIconsTier2[j].gameObject, j * 0.1f);
            }
            if (p.m_expIconsTier2.Count > 0)
            {
                p.m_tierMarker2.SetVisible(true, tierMarkerDelay);
            }
            yield return new WaitForSeconds(1f);



            CM_PageBase.PostSound(EVENTS.MENU_RUNDOWN_DISC_APPEAR_1, "");
            p.m_guix_Tier1.gameObject.SetActive(true);
            for (int i = 0; i < p.m_expIconsTier1.Count; i++)
            {
                CoroutineManager.BlinkIn(p.m_expIconsTier1[i].gameObject, i * 0.1f);
            }
            if (p.m_expIconsTier1.Count > 0)
            {
                p.m_tierMarker1.SetVisible(true, tierMarkerDelay);
            }
            yield return new WaitForSeconds(1f);


            p.m_joinOnServerIdText.gameObject.SetActive(true);
            CoroutineManager.BlinkIn(p.m_aboutTheRundownButton, 0f, null);
            CoroutineManager.BlinkIn(p.m_discordButton, 0.1f, null);
            if (SNet.IsMaster || !SNet.IsInLobby)
            {
                CoroutineManager.BlinkIn(p.m_matchmakeAllButton, 0.2f, null);
            }
            p.m_selectionIsRevealed = true;
            p.CheckClipboard();
            yield break;
        }
    }
}
