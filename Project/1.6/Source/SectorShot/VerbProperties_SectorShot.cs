using Verse;

namespace NewRatkin
{
    /// <summary>
    /// 부채꼴(Sector) 즉시 AOE 피해용 Verb 속성.
    /// vertexOffset: 타겟에서 사수 방향으로 당기는 칸 수 (칸 단위, 좌표 오차 없음).
    /// </summary>
    public class VerbProperties_SectorShot : VerbProperties
    {
        public float sectorRadius = 5f;
        public float sectorAngle = 70f;
        public int vertexOffset = 1;
        public DamageDef sectorDamageDef;
        public int sectorDamageAmount = 8;
        public float sectorArmorPenetration = -1f;
        public int effectsPerCellMin = 1;
        public int effectsPerCellMax = 3;
        /// <summary>피해 반복 회수 범위 (각 대상마다 랜덤 결정).</summary>
        public int hitRepeatMin = 1;
        public int hitRepeatMax = 3;
        /// <summary>LOS 중첩 시 반복 회수 상한. baseRepeat + overlapCount를 이 값으로 캡.</summary>
        public int hitRepeatOverlapCap = 6;
        /// <summary>총구 이펙트 오프셋 (DrawPos에서 발사 방향으로의 거리, 셀 단위). 0.4 = 몸 앞쪽 총구 위치.</summary>
        public float muzzleEffectOffset = 0.4f;
    }
}
