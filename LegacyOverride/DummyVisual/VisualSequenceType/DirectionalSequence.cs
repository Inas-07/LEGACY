using ExtraObjectiveSetup.Utils;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LEGACY.LegacyOverride.DummyVisual.VisualSequenceType
{
    public class DirectionalSequence
    {
        public Vec3 StartPosition { get; set; } = new Vec3();

        public Vec3 Rotation { get; set; } = new Vec3();

        public Vec3 ExtendDirection { get; set; } = new();

        public float PlacementInterval { get; set; } = 6.4f;

        public int Count { get; set; } = 1;

        public List<GameObject> Generate(GameObject template)
        {
            var curPos = StartPosition.ToVector3();
            var rot = Rotation.ToQuaternion();
            var dir = ExtendDirection.ToVector3().normalized;

            List<GameObject> seq = new List<GameObject>();
            for (int i = 0; i < Count; i++)
            {
                var go = GameObject.Instantiate(template);
                go.transform.SetPositionAndRotation(curPos, rot);

                go.SetActiveRecursively(true);

                curPos += dir * PlacementInterval;
                seq.Add(go);
            }

            return seq;
        }
    }

}
