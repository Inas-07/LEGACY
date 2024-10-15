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
        private float startTime = 0f;

        private float endTime = 0f;

        private bool hasOnGoingTimer = false;

        public float RemainingTime => hasOnGoingTimer ? Math.Max(endTime - Clock.Time, 0f): 0f;

        private Action<float> OnProgress;

        private Action OnTimerEnd;

        private void Update()
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel || !hasOnGoingTimer) return;

            float time = Clock.Time;
            if (OnProgress != null)
            {
                OnProgress((time - startTime) / (endTime - startTime));
            }

            if (time < endTime) return;

            endTime = 0f;
            hasOnGoingTimer = false;
            OnTimerEnd?.Invoke();
        }

        public void StartTimer(float time)
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

            startTime = Clock.Time;
            endTime = startTime + time;
            hasOnGoingTimer = true;
        }

        private void OnDestroy() 
        {
            endTime = 0f;
            hasOnGoingTimer = false;
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
