using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace NewRatkin
{
	/// <summary>
	/// Wyvern Fire 전용 Ability 클래스
	/// CompEquippableAbilityReloadable이 있고 ammoDef가 설정된 경우
	/// 쿨다운과 탄약 시스템을 완전히 분리하여 탄약 기반 재장전만 작동하도록 함
	/// </summary>
	public class Ability_WyvernFire : Ability
	{
		private bool shouldApplyCooldown = false;
		private bool wasOnCooldown = false;
		/// <summary>직전 틱 말미 기준 이 어빌리티 시전 잡 활성 여부. 워밍업 취소 시 PreCast가 안 돌아 잔류하는 PreIgnition 정리용.</summary>
		private bool prevAbilityCastingJob = false;
		public Ability_WyvernFire()
		{
		}

		public Ability_WyvernFire(Pawn pawn, AbilityDef def) : base(pawn, def)
		{
		}

	/// <summary>
	/// CanCast 오버라이드
	/// 쿨다운 중에는 탄약이 있어도 무조건 발사 불가
	/// </summary>
	public override AcceptanceReport CanCast
	{
		get
		{
			// 쿨다운 중에는 무조건 발사 불가
			if (this.OnCooldown)
			{
				return false;
			}

			// 기본 CanCast 체크 (탄약 확인 등)
			return base.CanCast;
		}
	}

		/// <summary>
		/// PreActivate 오버라이드
		/// 매 발사마다 쿨다운 시작, charge 자동 회복 방지
		/// </summary>
		protected override void PreActivate(LocalTargetInfo? target)
		{
			// charge 소모 (나중에 복원할 값 저장)
			int chargeBeforeConsume = this.RemainingCharges;
			if (this.UsesCharges)
			{
				this.RemainingCharges--;
			}
			int chargeAfterConsume = this.RemainingCharges;

			// 매번 쿨다운 시작 (cooldownPerCharge 설정과 무관하게)
			// 주의: StartCooldown()은 cooldownPerCharge: False일 때 charges를 maxCharges로 복원함!
			if (this.HasCooldown)
			{
				this.StartCooldown(this.def.cooldownTicksRange.RandomInRange);
				// Ability 쿨다운 시작 플래그 설정
				wasOnCooldown = true;
			}

			// StartCooldown()이 charge를 회복했다면, 소모된 상태로 복원
			if (this.RemainingCharges > chargeAfterConsume)
			{
				this.RemainingCharges = chargeAfterConsume;
			}

			// 기본 PreActivate의 나머지 로직 (로그 기록 등)
			Pawn pawn = this.ConstantCaster as Pawn;
			if (pawn != null)
			{
				Pawn_EquipmentTracker equipment = pawn.equipment;
				if (equipment != null)
				{
					equipment.Notify_AbilityUsed(this);
				}
			}

			if (this.def.writeCombatLog)
			{
				Find.BattleLog.Add(new BattleLogEntry_AbilityUsed(this.pawn, (target != null) ? target.GetValueOrDefault().Thing : null, this.def, RulePackDefOf.Event_AbilityUsed));
			}
		}

		/// <summary>
		/// AbilityTick 오버라이드
		/// StartCooldown에서 charge 자동 회복 방지 및 후딜레이 적용
		/// </summary>
		public override void AbilityTick()
		{
			bool wasCastingThisAbility = this.prevAbilityCastingJob;
			bool shouldPreventAutoRecharge = ShouldPreventAutoRecharge();
			int chargeBefore = this.RemainingCharges;

			// 기본 AbilityTick 호출
			base.AbilityTick();

			// 워밍업 중단·잡 종료 등으로 시전이 끊기면 vanilla는 preCast만 비우고 부착물은 남길 수 있음 → PreIgnition 전부 제거
			if (wasCastingThisAbility && !this.Casting)
			{
				CompAbilityEffect_WyvernFire.DestroyAllGunlancePreIgnitionOn(this.pawn);
			}
			this.prevAbilityCastingJob = this.Casting;

			// cooldownPerCharge: False일 때 StartCooldown()에서 charges = maxCharges로 회복하는 걸 방지
			if (shouldPreventAutoRecharge)
			{
				int chargeAfter = this.RemainingCharges;

				// charge가 증가했다면 원래 값으로 복원
				if (chargeAfter > chargeBefore)
				{
					this.RemainingCharges = chargeBefore;
				}
			}

			// WyvernFire 발사 후 후딜레이(cooldown) 적용
			// VerbTick에서 BurstingTick이 호출되어 state가 Idle로 변경된 후에 설정
			if (this.shouldApplyCooldown)
			{
				this.shouldApplyCooldown = false;
				ApplyWyvernFireCooldown();
			}

			// cooldownTicksRange 끝나고 나면 사운드 재생
			CheckAbilityCooldownEnd();
		}

		/// <summary>
		/// WyvernFire 발사 후 후딜레이(cooldown) 적용
		/// </summary>
		private void ApplyWyvernFireCooldown()
		{
			Pawn pawn = this.pawn;
			if (pawn == null || !pawn.Spawned || pawn.stances == null)
			{
				return;
			}

			// CompAbilityEffect_WyvernFire에서 meleeCooldownTime 가져오기
			CompAbilityEffect_WyvernFire wyvernFireEffect = null;
			foreach (CompAbilityEffect effect in this.EffectComps)
			{
				if (effect is CompAbilityEffect_WyvernFire)
				{
					wyvernFireEffect = effect as CompAbilityEffect_WyvernFire;
					break;
				}
			}

			if (wyvernFireEffect == null)
			{
				return;
			}

			float cooldownTime = wyvernFireEffect.GetMeleeCooldownTime();
			SoundDef cooldownEndSound = wyvernFireEffect.GetCooldownEndSound();

			// XML에서 설정한 값만 사용 (무기 tool cooldown 자동 사용 안 함)
			// 양수 값이면 해당 값 사용, 0 이하면 cooldown 없음
			if (cooldownTime > 0f)
			{
				// cooldownTime을 틱으로 변환 (1초 = 60틱)
				int cooldownTicks = Mathf.RoundToInt(cooldownTime * 60f);

				if (cooldownTicks > 0)
				{
					// Stance_Cooldown_WithSound 설정 (verb는 null로 설정 - ability이므로)
					// 쿨다운 종료 시 XML에서 지정한 사운드 자동 재생
					// 이 Stance가 활성화되면 Pawn.stances.FullBodyBusy = true가 됩니다
					pawn.stances.SetStance(new Stance_Cooldown_WithSound(
						cooldownTicks,
						LocalTargetInfo.Invalid,
						null,
						cooldownEndSound));
				}
			}
		}

		/// <summary>
		/// 후딜레이 적용 플래그 설정 (CompAbilityEffect_WyvernFire에서 호출)
		/// </summary>
		public void SetShouldApplyCooldown(bool value)
		{
			this.shouldApplyCooldown = value;
		}

		/// <summary>
		/// cooldownTicksRange 끝나고 나면 사운드 재생 체크
		/// </summary>
		private void CheckAbilityCooldownEnd()
		{
			Pawn pawn = this.pawn;
			if (pawn == null || !pawn.Spawned)
			{
				wasOnCooldown = false;
				return;
			}

			// 현재 Ability 쿨다운 상태 확인
			bool isOnCooldown = this.OnCooldown;

			// 이전에는 쿨다운이었는데 지금은 아닌 경우 = 쿨다운이 끝남
			if (wasOnCooldown && !isOnCooldown)
			{
				// RK_Sound_WyvernFireCoolDownEnd 재생
				if (RatkinSoundDefOf.RK_Sound_WyvernFireCoolDownEnd != null)
				{
					RatkinSoundDefOf.RK_Sound_WyvernFireCoolDownEnd.PlayOneShot(SoundInfo.InMap(new TargetInfo(pawn.Position, pawn.Map, false), MaintenanceType.None));
				}
			}

			wasOnCooldown = isOnCooldown;
		}

	/// <summary>
	/// AIGetAOETarget 오버라이드
	/// 자기 자신 제외, 적대 타겟만 검색
	/// </summary>
	public override LocalTargetInfo AIGetAOETarget()
	{
		// ai_SearchAOEForTargets가 true인 경우에만 AOE 타겟 검색
		if (this.def.ai_SearchAOEForTargets)
		{
			// 원형 반경 내 타겟 검색
			foreach (Thing thing in GenRadial.RadialDistinctThingsAround(this.pawn.Position, this.pawn.Map, this.verb.EffectiveRange, true))
			{
				// 자기 자신 제외
				if (thing == this.pawn)
				{
					continue;
				}
				
				// 적대 관계가 아니면 스킵
				if (!this.pawn.HostileTo(thing))
				{
					continue;
				}
				
				// ValidAOEAffectedTarget 체크 (private이므로 직접 구현)
				if (!IsValidAOETarget(thing))
				{
					continue;
				}
				
				// CompAbilityEffect들의 AICanTargetNow 체크
				bool allCompsValid = true;
				foreach (CompAbilityEffect comp in this.EffectComps)
				{
					if (!comp.AICanTargetNow(thing))
					{
						allCompsValid = false;
						break;
					}
				}
				
				if (allCompsValid)
				{
					return thing;
				}
			}
		}
		
		return LocalTargetInfo.Invalid;
	}
	
	/// <summary>
	/// ValidAOEAffectedTarget의 로직을 구현 (private이므로 직접 접근 불가)
	/// </summary>
	private bool IsValidAOETarget(Thing target)
	{
		// targetParams 체크
		if (!this.verb.targetParams.CanTarget(target, null))
		{
			return false;
		}
		
		// Fog 체크
		if (target.Fogged())
		{
			return false;
		}
		
		// CompAbilityEffect들의 Valid 체크
		LocalTargetInfo localTarget = new LocalTargetInfo(target);
		for (int i = 0; i < this.EffectComps.Count; i++)
		{
			if (!this.EffectComps[i].Valid(localTarget, false))
			{
				return false;
			}
		}
		
		return true;
	}

	/// <summary>
	/// AICanTargetNow 오버라이드
	/// AI가 타겟을 선택할 수 있는지 확인 (사거리 체크 포함)
	/// </summary>
	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		// 기본 조건 체크
		if (!base.AICanTargetNow(target))
		{
			return false;
		}
		
		// 사거리 체크 추가
		if (this.verb != null && !this.verb.CanHitTarget(target))
		{
			return false;
		}
		
		// 적대 관계 체크 (타겟이 Thing인 경우)
		if (target.HasThing && !this.pawn.HostileTo(target.Thing))
		{
			return false;
		}
		
		return true;
	}

	/// <summary>
	/// GetJob 오버라이드
	/// AI가 어빌리티를 사용하기 위한 Job 생성
	/// </summary>
	public override Job GetJob(LocalTargetInfo target, LocalTargetInfo destination)
	{
		return base.GetJob(target, destination);
	}

		/// <summary>
		/// CompEquippableAbilityReloadable이 있고 ammoDef가 설정된 경우인지 확인
		/// </summary>
		private bool ShouldPreventAutoRecharge()
		{
			// Ability의 소유자(Pawn) 확인
			if (this.pawn == null || this.pawn.equipment == null)
			{
				return false;
			}

			// 장착된 무기 확인
			ThingWithComps equipment = this.pawn.equipment.Primary;
			if (equipment == null)
			{
				return false;
			}

			// CompEquippableAbilityReloadable 확인
			CompEquippableAbilityReloadable reloadableComp = equipment.GetComp<CompEquippableAbilityReloadable>();
			if (reloadableComp == null)
			{
				return false;
			}

			// ammoDef가 설정되어 있고, 이 Ability가 reloadableComp의 Ability인지 확인
			return reloadableComp.Props.ammoDef != null && reloadableComp.AbilityForReading == this;
		}
	}
}
