using ExtraObjectiveSetup.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LEGACY.LegacyOverride.DummyVisual.VisualSequenceType
{
    public class CircularSequence
    {
        public Vec3 Origin { get; set; } = new Vec3();

        public Vec3 Rotation { get; set; } = new Vec3();

        public float CircleRadius { get; set; } = 6.4f;

        public int Count { get; set; } = 1;

        public bool GenerateFullCircle { get; set; } = false;

        public float AngleDegreeInterval { get; set; } = 60f; 

        public float ToRadian(float degrees) => (float)(degrees * Math.PI / 180f);

        public List<GameObject> Generate(GameObject template)
        {
            float AngleInterval = GenerateFullCircle ? 360f / Count : AngleDegreeInterval;

            List<GameObject> seq = new List<GameObject>();
            GameObject tempParent = new();
            tempParent.transform.SetPositionAndRotation(Origin.ToVector3(), Quaternion.identity);
            for (int i = 0; i < Count; i++)
            {
                var go = GameObject.Instantiate(template);
                float angle = AngleInterval * i;
                float radian = ToRadian(angle);

                // generate circle on x-z plane
                var pos = new Vector3(
                    Origin.x + Mathf.Cos(radian) * CircleRadius, 
                    Origin.y, 
                    Origin.z + Mathf.Sin(radian) * CircleRadius);

                go.transform.SetPositionAndRotation(pos, Quaternion.identity);
                go.SetActiveRecursively(true);

                seq.Add(go);

                go.transform.SetParent(tempParent.transform);
            }

            var rot = Rotation.ToQuaternion();
            tempParent.transform.SetPositionAndRotation(tempParent.transform.position, rot);
            foreach (var go in seq)
            {
                go.transform.SetParent(null);
            }

            GameObject.Destroy(tempParent);
            return seq;
        }
    }
}
