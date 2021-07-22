using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using HarmonyLib;
using UnityEngine;
using Verse.AI;
using Verse.Sound;
using Verse;


namespace NewRatkin
{
    [DefOf]
    public static class GunlanceDefOf
    {
        public static ThingDef GunlanceExplosion;
        public static ThingDef GunlancePreIgnition;
        public static ThingDef GunlanceAfterIgnition;
    }
    [DefOf]
    public static class RatkinSoundDefOf
    {
        public static SoundDef RK_Charge;
        public static SoundDef RK_Fire;
        public static SoundDef RK_OverHeat;
        public static SoundDef RK_Reload;
        public static SoundDef RK_WyvernFire;
    }
}
