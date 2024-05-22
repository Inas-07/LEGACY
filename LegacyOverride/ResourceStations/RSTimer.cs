using GTFO.API;
using Il2CppInterop.Runtime.Injection;
using LEGACY.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LEGACY.LegacyOverride.ResourceStations
{
    public class RSTimer : MonoBehaviour
    {
        private float endTime = 0f;

        private bool hasOnGoingTimer = false;

        public float RemainingTime => hasOnGoingTimer ? Math.Max(endTime - Clock.Time, 0f): 0f;

        private Action OnTimerEnd;

        private void Update()
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel) return;
            if (!hasOnGoingTimer || Clock.Time < endTime) return;

            endTime = 0f;
            hasOnGoingTimer = false;
            var action = OnTimerEnd;
            OnTimerEnd = null;
            action?.Invoke();
        }

        public void StartTimer(float time, Action onEnd = null)
        {
            if (time <= 0f)
            {
                LegacyLogger.Error("StartTimer: time is not positive!");
                return;
            }

            if(hasOnGoingTimer)
            {
                LegacyLogger.Error("StartTimer: this timer is yet ended!");
                return;
            }

            endTime = Clock.Time + time;
            OnTimerEnd = onEnd;
            hasOnGoingTimer = true;
        }

        private void OnDestroy() 
        {
            endTime = 0f;
            hasOnGoingTimer = false;
            OnTimerEnd = null;
        }

        private static List<GameObject> TimerGOs { get; } = new();

        public static RSTimer Instantiate()
        {
            GameObject timerGO = new();
            var timer = timerGO.AddComponent<RSTimer>();
            TimerGOs.Add(timerGO);
            return timer;
        }

        public static void DestroyAll()
        {
            TimerGOs.ForEach(Destroy);
            TimerGOs.Clear();
        }

        private RSTimer() { }

        static RSTimer()
        {
            ClassInjector.RegisterTypeInIl2Cpp<RSTimer>();

            LevelAPI.OnBuildStart += DestroyAll;
            LevelAPI.OnLevelCleanup += DestroyAll;
        }
    }
}
