using BepInEx.Unity.IL2CPP.Utils.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LEGACY.Utils
{
    public delegate float EasingFunction(float t, float b, float c, float d);

    public delegate bool BoolCheck();

    internal static class CoroutineEase
    {
        private static IEnumerator DoEaseLocalScale(Transform trans, Vector3 startScale, Vector3 targetScale, float startTime, float duration, EasingFunction ease, Action onDone, BoolCheck checkAbort)
        {
            bool doAbort = false;
            while (Clock.Time < startTime + duration && !doAbort)
            {
                doAbort = (checkAbort != null && checkAbort());
                float t = ease(Clock.Time - startTime, 0f, 1f, duration);
                trans.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            trans.localScale = targetScale;
            if (!doAbort && onDone != null)
            {
                onDone();
            }
            yield break;
        }

        private static IEnumerator DoEaseLocalPos(Transform trans, Vector3 sourcePos, Vector3 targetPos, float startTime, float duration, EasingFunction ease, Action onDone, BoolCheck checkAbort)
        {
            bool doAbort = false;
            while (Clock.Time < startTime + duration && !doAbort)
            {
                doAbort = (checkAbort != null && checkAbort());
                float t = ease(Clock.Time - startTime, 0f, 1f, duration);
                trans.localPosition = Vector3.Lerp(sourcePos, targetPos, t);
                yield return null;
            }
            trans.localPosition = targetPos;
            if (!doAbort && onDone != null)
            {
                onDone();
            }
            yield break;
        }

        private static IEnumerator DoEaseLocalRot(Transform trans, Vector3 sourceEuler, Vector3 targetEuler, float startTime, float duration, EasingFunction ease, Action onDone, BoolCheck checkAbort)
        {
            bool doAbort = false;
            while (Clock.Time < startTime + duration && !doAbort)
            {
                doAbort = (checkAbort != null && checkAbort());
                float t = ease(Clock.Time - startTime, 0f, 1f, duration);
                trans.localEulerAngles = Vector3.Lerp(sourceEuler, targetEuler, t);
                yield return null;
            }
            trans.localEulerAngles = targetEuler;
            if (!doAbort && onDone != null)
            {
                onDone();
            }
            yield break;
        }

        internal static Coroutine EaseLocalScale(Transform trans, Vector3 startScale, Vector3 targetScale, float duration, EasingFunction ease = null, Action onDone = null, BoolCheck checkAbort = null)
        {
            EasingFunction ease2 = ease ?? new EasingFunction(Easing.EaseOutExpo);
            return CoroutineManager.StartCoroutine(DoEaseLocalScale(trans, startScale, targetScale, Clock.Time, duration, ease2, onDone, checkAbort).WrapToIl2Cpp(), null);
        }

        internal static Coroutine EaseLocalPos(Transform trans, Vector3 sourcePos, Vector3 targetPos, float duration, EasingFunction ease = null, Action onDone = null, BoolCheck checkAbort = null)
        {
            var ease2 = ease ?? new EasingFunction(Easing.EaseOutExpo);
            return CoroutineManager.StartCoroutine(DoEaseLocalPos(trans, sourcePos, targetPos, Clock.Time, duration, ease2, onDone, checkAbort).WrapToIl2Cpp(), null);
        }

        internal static Coroutine EaseLocalRot(Transform trans, Vector3 sourceEuler, Vector3 targetEuler, float duration, EasingFunction ease = null, Action onDone = null, BoolCheck checkAbort = null)
        {
            var ease2 = ease ?? new EasingFunction(Easing.EaseOutExpo);
            return CoroutineManager.StartCoroutine(DoEaseLocalRot(trans, sourceEuler, targetEuler, Clock.Time, duration, ease2, onDone, checkAbort).WrapToIl2Cpp(), null);
        }
    }
}
