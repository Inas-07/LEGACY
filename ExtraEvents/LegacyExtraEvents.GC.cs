using GameData;
using AK;
using ExtraObjectiveSetup.Instances;
using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using ExtraObjectiveSetup.BaseClasses;
using LevelGeneration;
using UnityEngine;

namespace LEGACY.ExtraEvents
{
    internal static partial class LegacyExtraEvents
    {
        private static IEnumerator Play(WardenObjectiveEventData e)
        {
            var gcInZone = PowerGeneratorInstanceManager.Current.GetInstancesInZone(e.DimensionIndex, e.Layer, e.LocalIndex);
            yield return new WaitForSeconds(4f);

            CellSound.Post(EVENTS.DISTANT_EXPLOSION_SEQUENCE);
            yield return new WaitForSeconds(2f);

            EnvironmentStateManager.AttemptSetExpeditionLightMode(false);
            CellSound.Post(EVENTS.LIGHTS_OFF_GLOBAL);
            yield return new WaitForSeconds(3f);

            int g = 0;
            while (g < gcInZone.Count)
            {
                gcInZone[g].TriggerPowerFailureSequence();
                yield return new WaitForSeconds(Random.Range(0.3f, 1f));
                int num = g + 1;
                g = num;
            }
            yield return new WaitForSeconds(4f);

            EnvironmentStateManager.AttemptSetExpeditionLightMode(true);
        }

        private static void PlayGCEndSequence(WardenObjectiveEventData e)
        {
            Coroutine val = CoroutineManager.StartCoroutine(CollectionExtensions.WrapToIl2Cpp(Play(e)), null);
            WorldEventManager.m_worldEventEventCoroutines.Add(val);
        }
    }
}