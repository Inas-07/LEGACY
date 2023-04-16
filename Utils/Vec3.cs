using UnityEngine;

namespace LEGACY.Utils
{
    public class Vec3
    {
        public float x { get; set; }

        public float y { get; set; }

        public float z { get; set; }

        public Vector3 ToVector3() => new Vector3(x, y, z);

        public Quaternion ToQuaternion() => Quaternion.Euler(x, y, z);
    }
}
