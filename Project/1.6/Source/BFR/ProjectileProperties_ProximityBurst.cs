using RimWorld;
using UnityEngine;
using Verse;

namespace NewRatkin
{
    /// <summary>
    /// BFR HE탄 전용 Projectile 속성.
    /// preDetonationDistance: 목표로부터 기폭점까지 거리(0이면 적중 시 폭발).
    /// sectorAngle: 부채꼴 각도(도), sectorRadius: 반지름, damageAmountDirect/Explosion: 적중/폭발 피해.
    /// wallBreachRadius: 이 거리 이내는 벽 무시(최초 후폭발), 초과 시 벽에 막힘.
    /// </summary>
    public class ProjectileProperties_ProximityBurst : ProjectileProperties
    {
        public float preDetonationDistance = 0f;
        public float sectorAngle = 90f;
        public float sectorRadius = 1.7f;
        public float wallBreachRadius = 1.7f;
        public int damageAmountDirect = 40;
        public int damageAmountExplosion = 25;
        public DamageDef damageDefDirect;
        public DamageDef damageDefExplosion;
        public float armorPenetrationDirect = -1f;
        public float armorPenetrationExplosion = -1f;

        /// <summary>섹터 셀 이펙트용 Fleck. null이면 <see cref="FleckDefOf.ShotHit_Dirt"/>.</summary>
        public FleckDef sectorCellFleckDef;

        public int sectorEffectsPerCell = 2;
        public float sectorEffectNoiseRange = 0.3f;

        /// <summary>중심 폭발 커스텀 Fleck. null이면 RK_WyvernFireExplosion 이름 조회.</summary>
        public FleckDef centerExplosionFleckDef;

        /// <summary><see cref="GenExplosion.DoExplosion"/> 시각용 반경 등에 사용.</summary>
        public float centerExplosionVisualRadius = 1f;

        /// <summary>중심 Fleck 스케일 (기본: 기존 하드코딩과 동일).</summary>
        public Vector3 centerExplosionFleckScale = new Vector3(3f, 1f, 2f);

        /// <summary>중심 Fleck 색 (기본: 기존 하드코딩과 동일).</summary>
        public Color centerExplosionFleckColor = new Color(0.75f, 0.55f, 0.55f, 0.7f);
    }
}
