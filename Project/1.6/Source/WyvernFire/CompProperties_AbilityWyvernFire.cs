using System;
using Verse;
using RimWorld;

namespace NewRatkin
{
	public class CompProperties_AbilityWyvernFire : CompProperties_AbilityEffect
	{
		public float range;

		public float lineWidthEnd;

		public DamageDef damageDef;

		public int damAmount = -1;

		public float armorPenetration = -1f;

		public ThingDef filthDef;

		public EffecterDef effecterDef;

		public bool canHitFilledCells;

		/// <summary>
		/// WyvernFire 발사 후 적용할 후딜레이(cooldown) 시간 (초 단위)
		/// 이 시간 동안 Pawn이 움직이지 못합니다 (FullBodyBusy = true)
		/// 
		/// 설정값:
		/// - 양수 값: 해당 초만큼 cooldown 적용 (예: 3 = 3초간 못 움직임)
		/// - 0 이하: cooldown 없음 (즉시 다시 사용 가능, 기본값)
		/// </summary>
		public float meleeCooldownTime = 0f;

		/// <summary>
		/// meleeCooldownTime이 끝날 때 재생할 사운드
		/// null이면 사운드를 재생하지 않습니다
		/// </summary>
		public SoundDef cooldownEndSound;

		public CompProperties_AbilityWyvernFire()
		{
			this.compClass = typeof(CompAbilityEffect_WyvernFire);
		}
	}
}
