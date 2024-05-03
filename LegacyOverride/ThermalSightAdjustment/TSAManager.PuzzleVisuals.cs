using ChainedPuzzles;
using ExtraObjectiveSetup.BaseClasses;
using GTFO.API.Utilities;
using LEGACY.LegacyOverride.EnemyTagger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LEGACY.LegacyOverride.ThermalSightAdjustment
{
    internal partial class TSAManager : GenericDefinitionManager<TSADefinition>
    {
        private class PuzzleVisualWrapper
        {
            internal GameObject GO { get; set; }

            internal Renderer Renderer { get; set; }

            internal float Intensity { get; set; }

            internal float BehindWallIntensity { get; set; }

            internal void SetIntensity(float t)
            {
                if (!GO.active) return;

                if (Intensity > 0.0f)
                {
                    Renderer.material.SetFloat("_Intensity", Intensity * t);
                }

                if (BehindWallIntensity > 0.0f)
                {
                    Renderer.material.SetFloat("_BehindWallIntensity", BehindWallIntensity * t);
                }
            }

            internal PuzzleVisualWrapper() { }
        }

        private List<PuzzleVisualWrapper> PuzzleVisuals { get; } = new();

        internal void RegisterPuzzleVisual(CP_Bioscan_Core core)
        {
            var components = core.gameObject.GetComponentsInChildren<Renderer>(true);

            if (components != null)
            {
                var renderers = components.Where(comp => comp.gameObject.name.Equals("Zone")).ToList();
                foreach (var r in renderers)
                {
                    var go = r.gameObject;
                    float intensity = r.material.GetFloat("_Intensity");
                    float behindWallIntensity = r.material.GetFloat("_BehindWallIntensity");

                    var wrapper = new PuzzleVisualWrapper()
                    {
                        GO = go,
                        Renderer = r,
                        Intensity = intensity,
                        BehindWallIntensity = behindWallIntensity,
                    };

                    PuzzleVisuals.Add(wrapper);
                }
            }
        }

        private void AddOBSVisualRenderers()
        {
            foreach (var go in EnemyTaggerSettingManager.Current.OBSVisuals)
            {
                var renderer = go.GetComponentInChildren<Renderer>();
                float intensity = renderer.material.GetFloat("_Intensity");
                float behindWallIntensity = -1.0f;
                PuzzleVisuals.Add(new()
                {
                    GO = go,
                    Renderer = renderer,
                    Intensity = intensity,
                    BehindWallIntensity = behindWallIntensity
                });
            }
        }

        internal void SetPuzzleVisualsIntensity(float t)
        {
            PuzzleVisuals.ForEach(v => v.SetIntensity(t));
        }

        private void CleanupPuzzleVisuals()
        {
            PuzzleVisuals.Clear();
        }
    }
}
