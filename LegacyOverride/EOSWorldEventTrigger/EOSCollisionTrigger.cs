//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using UnityEngine;

//namespace LEGACY.LegacyOverride.EOSWorldEventTrigger
//{
//    public class EOSCollisionTrigger : MonoBehaviour
//    {
//        public static float CHECK_INTERVAL { get; } = 0.1f;

//        private float nextCheckTime = float.NaN;

//        public Vector3 Scale { get; private set; } = Vector3.zero;

//        private Vector3 Position => gameObject.transform.position;

//        private int last_playersInSensor = 0;

//        internal void Setup(SensorSettings sensorSettings)
//        {
//            this.sensorSettings = sensorSettings;
//        }

//        void Update()
//        {
//            if (GameStateManager.CurrentStateName != eGameStateName.InLevel) return;

//            if (!float.IsNaN(nextCheckTime) && Clock.Time < nextCheckTime) return;

//            nextCheckTime = Clock.Time + CHECK_INTERVAL;
//            int current_playersInSensor = 0;
//            foreach (var player in PlayerManager.PlayerAgentsInLevel)
//            {
//                if (player.Owner.IsBot || !player.Alive) continue;

//                if ((this.Position - player.Position).magnitude < sensorSettings.Radius)
//                {
//                    current_playersInSensor++;
//                }
//            }

//            if (current_playersInSensor > last_playersInSensor)
//            {
//                SecuritySensorManager.Current.TriggerSensor(gameObject.Pointer);
//            }

//            last_playersInSensor = current_playersInSensor;
//        }

//        void OnDestroy()
//        {

//        }
//    }

//}
