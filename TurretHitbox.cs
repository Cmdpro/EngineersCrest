using GlobalEnums;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EngineersCrest
{
    public class TurretHitbox : HeroBox
    {
        public Turret turret;
        public override void Awake()
        {
            base.Awake();
            heroCtrl = null;
            turret = GetComponentInParent<Turret>();
        }
        public void TurretCheckForDamage(GameObject otherGameObject)
        {
            if (FSMUtility.ContainsFSM(otherGameObject, "damages_hero"))
            {
                PlayMakerFSM fsm = FSMUtility.LocateFSM(otherGameObject, "damages_hero");
                int damage = FSMUtility.GetInt(fsm, "damageDealt");
                //HazardType type = (HazardType)FSMUtility.GetInt(fsm, "hazardType");
                CollisionSide side = (otherGameObject.transform.position.x > base.transform.position.x) ? CollisionSide.right : CollisionSide.left;
                turret.Hurt(damage, side);
                return;
            }
            DamageHero component = otherGameObject.GetComponent<DamageHero>();
            this.TakeDamageFromDamager(component, otherGameObject);
        }
        public void TurretApplyBufferedHit()
        {
            turret.Hurt(damageDealt, collisionSide);
            this.lastDamageHero = null;
            this.lastDamagingObject = null;
            this.isHitBuffered = false;
        }
    }
    [HarmonyPatch]
    public class TurretHitboxPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(HeroBox), "CheckForDamage")]
        static bool TurretCheckForDamage(HeroBox __instance, GameObject otherGameObject)
        {
            if (__instance is TurretHitbox turret)
            {
                turret.TurretCheckForDamage(otherGameObject);
                return false;
            }
            return true;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(HeroBox), "ApplyBufferedHit")]
        static bool TurretApplyBufferedHit(HeroBox __instance)
        {
            if (__instance is TurretHitbox turret)
            {
                turret.TurretApplyBufferedHit();
                return false;
            }
            return true;
        }
    }
}
