using RimWorld;
using UnityEngine;
using Verse;

namespace NewRatkin
{
	/// <summary>
	/// 볼터 대구경 탄환: <see cref="ProjectileProperties_Bolter"/>에 스턴·EMP 확률·보너스 수치.
	/// </summary>
	public class Bullet_Bolter : Bullet
	{
		private static readonly float DefaultStunChance = 0.3f;
		private static readonly float DefaultStunSec = 2f;
		private static readonly float DefaultEmpChance = 1f;
		private static readonly float DefaultEmpDamage = 6f;

		private ProjectileProperties_Bolter BolterProps => def.projectile as ProjectileProperties_Bolter;

		protected override void Impact(Thing hitThing, bool blockedByShield = false)
		{
			// 실드에 막힌 경우, Destroy 전에 투사체·경로 기준으로 어떤 Comp가 막았는지 식별 (CheckIntercept 재호출 금지: 부수효과)
			CompProjectileInterceptor shieldAtIntercept = null;
			if (blockedByShield)
			{
				shieldAtIntercept = FindFirstInterceptorMatchingShieldBlock();
			}

			base.Impact(hitThing, blockedByShield);

			// 직접 맞은 메카노이드: 보너스 EMP
			if (hitThing is Pawn mechPawn
				&& mechPawn.RaceProps.IsMechanoid
				&& !mechPawn.Dead
				&& !blockedByShield)
			{
				ApplyExtraEmpTo(mechPawn);
			}

			// 투사체 인터셉트(에너지 실드 등)에 막힌 경우: 실드(부착 틱)에 보너스 EMP
			if (shieldAtIntercept != null)
			{
				ApplyExtraEmpTo(shieldAtIntercept.parent);
			}

			// 30% 스턴 — 실드/헛방에는 적용하지 않음
			if (blockedByShield)
			{
				return;
			}

			if (hitThing == null)
			{
				return;
			}

			ProjectileProperties_Bolter p = BolterProps;
			float stunCh = p != null ? p.stunOnHitChance : DefaultStunChance;
			if (!Rand.Chance(stunCh))
			{
				return;
			}

			if (!(hitThing is Pawn target) || target.Dead)
			{
				return;
			}

			StunHandler stunner = target.stances?.stunner;
			if (stunner == null)
			{
				return;
			}

			float stunSec = p != null ? p.stunDurationSeconds : DefaultStunSec;
			int ticks = stunSec.SecondsToTicks();
			stunner.StunFor(ticks, launcher, addBattleLog: true, showMote: true, disableRotation: false);
		}

		private void ApplyExtraEmpTo(Thing target)
		{
			if (target is null || target.Destroyed || !target.Spawned)
			{
				return;
			}

			ProjectileProperties_Bolter p = BolterProps;
			float empCh = p != null ? p.extraEmpChance : DefaultEmpChance;
			if (!Rand.Chance(empCh))
			{
				return;
			}

			float empAmt = p != null ? p.extraEmpDamage : DefaultEmpDamage;

			Pawn instigatorPawn = launcher as Pawn;
			bool instigatorGuilty = instigatorPawn == null || !instigatorPawn.Drafted;
			DamageInfo dinfo = new DamageInfo(
				DamageDefOf.EMP,
				empAmt,
				0.5f,
				-1f,
				launcher,
				null,
				equipmentDef,
				DamageInfo.SourceCategory.ThingOrUnknown,
				intendedTarget.Thing,
				instigatorGuilty,
				true,
				QualityCategory.Normal,
				true,
				false);
			target.TakeDamage(dinfo);
		}

		/// <summary>CompProjectileInterceptor.CheckIntercept(…)의 조건만 복제 (부수효과 없음). 실제로 막은 첫 방패와 동일한 순서로 탐지.</summary>
		private CompProjectileInterceptor FindFirstInterceptorMatchingShieldBlock()
		{
			if (Map is null)
			{
				return null;
			}

			Vector3 newExactPos = ExactPosition;
			Vector3 v = (destination - origin).Yto0();
			if (v.sqrMagnitude < 1E-4f)
			{
				return null;
			}

			float step = Mathf.Max(0.1f, def.projectile.SpeedTilesPerTick);
			Vector3 lastExactPos = newExactPos - v.normalized * step;

			foreach (Thing t in Map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor))
			{
				CompProjectileInterceptor comp = t == null ? null : t.TryGetComp<CompProjectileInterceptor>();
				if (comp == null)
				{
					continue;
				}

				if (ShieldInterceptConditionsMatch(this, comp, lastExactPos, newExactPos))
				{
					return comp;
				}
			}

			return null;
		}

		private static bool ShieldInterceptConditionsMatch(Projectile projectile, CompProjectileInterceptor comp, Vector3 lastExactPos, Vector3 newExactPos)
		{
			CompProperties_ProjectileInterceptor props = comp.Props;
			Thing parent = comp.parent;
			Vector3 vector = parent.Position.ToVector3Shifted();
			float horiz = (newExactPos.x - vector.x) * (newExactPos.x - vector.x) + (newExactPos.z - vector.z) * (newExactPos.z - vector.z);
			float maxDist = props.radius + projectile.def.projectile.SpeedTilesPerTick + 0.1f;
			if (horiz > maxDist * maxDist)
			{
				return false;
			}

			if (!comp.Active)
			{
				return false;
			}

			if (!CompProjectileInterceptor.InterceptsProjectile(props, projectile))
			{
				return false;
			}

			// private debug 플래그는 접근 불가: 일반 플레이는 아래로 충분
			if (projectile.Launcher == null && !props.interceptNonHostileProjectiles)
			{
				return false;
			}

			if (parent.Faction != null)
			{
				if (projectile.Launcher != null && projectile.Launcher.Spawned
					&& !projectile.Launcher.HostileTo(parent.Faction))
				{
					return false;
				}

				if (projectile.Launcher != null && !projectile.Launcher.Spawned
					&& !projectile.Launcher.Faction.HostileTo(parent.Faction))
				{
					return false;
				}
			}

			if (!props.interceptOutgoingProjectiles)
			{
				if ((new Vector2(vector.x, vector.z) - new Vector2(lastExactPos.x, lastExactPos.z)).sqrMagnitude
					<= props.radius * props.radius)
				{
					return false;
				}
			}

			return GenGeo.IntersectLineCircleOutline(
				new Vector2(vector.x, vector.z),
				props.radius,
				new Vector2(lastExactPos.x, lastExactPos.z),
				new Vector2(newExactPos.x, newExactPos.z));
		}
	}
}
