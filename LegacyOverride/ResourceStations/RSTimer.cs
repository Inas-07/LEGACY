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
        public float StartTime { get; private set; } = 0f;

        public float EndTime { get; private set; } = 0f;

        public bool HasOnGoingTimer { get; private set; } = false;

        public float RemainingTime => HasOnGoingTimer ? Math.Max(EndTime - Clock.Time, 0f): 0f;

        private Action<float> OnProgress;

        private Action OnTimerEnd;

        private void Update()
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel || !HasOnGoingTimer) return;

            float time = Clock.Time;
            if (OnProgress != null)
            {
                OnProgress((time - StartTime) / (EndTime - StartTime));
            }

            if (time < EndTime) return;

            EndTime = 0f;
            HasOnGoingTimer = false;
            OnTimerEnd?.Invoke();
        }

        public void StartTimer(float time)
        {
            if (time <= 0f)
            {
                LegacyLogger.Error("StartTimer: time is not positive!");
                return;
            }

            if(HasOnGoingTimer)
            {
                LegacyLogger.Error("StartTimer: this timer is yet ended!");
                return;
            }

            StartTime = Clock.Time;
            EndTime = StartTime + time;
            HasOnGoingTimer = true;
        }

        private void OnDestroy() 
        {
            EndTime = 0f;
            HasOnGoingTimer = false;
            OnTimerEnd = null;
        }

        private static List<GameObject> TimerGOs { get; } = new();

        public static RSTimer Instantiate(Action<float> onProgress, Action actionOnEnd)
        {
            GameObject timerGO = new();
            var timer = timerGO.AddComponent<RSTimer>();
            timer.OnProgress = onProgress;
            timer.OnTimerEnd = actionOnEnd;
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
