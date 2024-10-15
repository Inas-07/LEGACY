using Enemies;
using FX_EffectSystem;
using Gear;
using HarmonyLib;
using LEGACY.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LEGACY.LegacyOverride.GearFunctions
{
    [HarmonyPatch]
    internal static class Patch_BulletWeapon
    {
        private static uint GLUE_GUN_PID = 400;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.Fire))]
        private static void Post_BulletHit(BulletWeapon __instance)
        {
            if (__instance.ArchetypeData.persistentID != GLUE_GUN_PID) return;

            GameObject gameObject = Weapon.s_weaponRayData.rayHit.collider.gameObject;
            if (gameObject == null) return;

            IDamageable damageable = null;
            BulletWeapon.s_tempColliderInfo = gameObject.GetComponent<ColliderMaterial>();

            if (BulletWeapon.s_tempColliderInfo != null)
            {
                damageable = BulletWeapon.s_tempColliderInfo.Damageable;
            }

            if (damageable == null)
            {
                damageable = gameObject.GetComponent<IDamageable>();
            }

            if (damageable == null)
            {
                return;
            }

            var enemy = damageable.GetBaseAgent()?.TryCast<EnemyAgent>();
            if (enemy != null && enemy.Damage.AttachedGlueRel < 1f)
            {
                damageable.GlueDamage(100f);
            }
        }

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.BulletHit))]
        //private static void Post_BulletHit(Weapon.WeaponHitData weaponRayData, bool doDamage)
        //{
        //    GameObject gameObject = weaponRayData.rayHit.collider.gameObject;
        //    if (gameObject == null) return;

        //    IDamageable damageable = null;
        //    BulletWeapon.s_tempColliderInfo = gameObject.GetComponent<ColliderMaterial>();

        //    if (BulletWeapon.s_tempColliderInfo != null)
        //    {
        //        damageable = BulletWeapon.s_tempColliderInfo.Damageable;
        //    }

        //    if (damageable == null)
        //    {
        //        damageable = gameObject.GetComponent<IDamageable>();
        //    }

        //    if (damageable == null)
        //    {
        //        return;
        //    }

        //    if (doDamage)
        //    {
        //        var enemy = damageable.GetBaseAgent()?.TryCast<EnemyAgent>();
        //        if(enemy != null && enemy.Damage.AttachedGlueRel < 1f)
        //        {
        //            damageable.GlueDamage(100f);
        //        }
        //    }
        //}
    }
}
