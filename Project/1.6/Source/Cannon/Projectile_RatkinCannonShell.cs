using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NewRatkin
{
	/// <summary>
	/// 랫킨 포탄: 직격 시 폭발. 빈 지면 첫 착탄 시 <see cref="ProjectileProperties_RatkinCannonShell"/>의 반경·피해로 폭발 후 같은 방향으로 비행 거리 절반 비행(상한 <see cref="ProjectileProperties_RatkinCannonShell.groundBounceMaxDistance"/>).
	/// 지면 도탄 전·후 모두 동일하게 가로채기(실드·벽·폰 등)를 적용하며, 가로채기 후보는 <see cref="ThingCategory.Building"/>과 <see cref="Pawn"/>만이다.
	/// 지면 도탄 이후 구간은 가로채기·착탄에서 <c>Rand.Chance</c> 없이, 바닐라와 동일한 가중치가 양수이면 첫 후보에 히트한다.
	/// </summary>
	public class Projectile_RatkinCannonShell : Projectile_Explosive
	{
		private const float MinBounceLegCells = 18f;

		private static readonly FieldInfo ExplosiveTicksToDetonationField =
			typeof(Projectile_Explosive).GetField("ticksToDetonation", BindingFlags.Instance | BindingFlags.NonPublic);

		private static readonly List<IntVec3> ShellInterceptCheckedCells = new List<IntVec3>();

		private ProjectileProperties_RatkinCannonShell ShellProps =>
			def.projectile as ProjectileProperties_RatkinCannonShell;

		private float GroundTouchExplosionRadiusCells =>
			Mathf.Max(0.01f, ShellProps?.groundTouchExplosionRadius ?? def.projectile.explosionRadius);

		private float GroundBounceMaxDistanceCells =>
			Mathf.Max(0f, ShellProps?.groundBounceMaxDistance ?? 13f);

		private bool didGroundBounce;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref didGroundBounce, "rkCannonShellGroundBounce", false);
		}

		/// <summary>가로채기·착탄: 폰과 건물(ThingCategory.Building)만 림 기본 <see cref="Projectile.CanHit"/>와 함께 허용.</summary>
		/// <summary>
		/// 바닐라 <see cref="VerbUtility.InterceptChanceFactorFromDistance"/>는 발사점에서 5칸 이내면 0을 줘 가로채기가 통째로 스킵된다.
		/// 지면 도탄 직후 짧은 잔여 구간은 <c>origin</c>이 튀긴 지점이라 그대로 두면 로그처럼 연속 <c>distFactor=0</c>이 나온다.
		/// </summary>
		private float ShellInterceptDistanceFactor(IntVec3 cell)
		{
			float raw = VerbUtility.InterceptChanceFactorFromDistance(origin, cell);
			if (!didGroundBounce)
			{
				return raw;
			}

			return Mathf.Max(raw, 1f);
		}

		private bool ShellCanHit(Thing thing)
		{
			if (thing == null || thing.def == null)
			{
				return false;
			}

			if (thing is Pawn)
			{
				return base.CanHit(thing);
			}

			if (thing.def.category == ThingCategory.Building)
			{
				return base.CanHit(thing);
			}

			return false;
		}

		protected override void TickInterval(int delta)
		{
			RunTickIntervalShellProjectile(delta);
		}

		protected override void ImpactSomething()
		{
			if (def.projectile.flyOverhead)
			{
				RoofDef roofDef = Map.roofGrid.RoofAt(Position);
				if (roofDef != null)
				{
					if (roofDef.isThickRoof)
					{
						if (!def.projectile.soundHitThickRoof.NullOrUndefined())
						{
							def.projectile.soundHitThickRoof.PlayOneShot(new TargetInfo(Position, Map));
						}

						Destroy(DestroyMode.Vanish);
						return;
					}

					if (Position.GetEdifice(Map) == null || Position.GetEdifice(Map).def.Fillage != FillCategory.Full)
					{
						RoofCollapserImmediate.DropRoofInCells(Position, Map, null);
					}
				}
			}

			if (!usedTarget.HasThing || !ShellCanHit(usedTarget.Thing))
			{
				List<Thing> list = VerbUtility.ThingsToHit(Position, Map, ShellCanHit);
				if (!didGroundBounce)
				{
					list.Shuffle();
				}

				for (int i = 0; i < list.Count; i++)
				{
					Thing thing = list[i];
					Pawn pawn = thing as Pawn;
					float num;
					if (pawn != null)
					{
						num = 0.5f * Mathf.Clamp(pawn.BodySize, 0.1f, 2f);
						if (pawn.GetPosture() != PawnPosture.Standing && (origin - destination).MagnitudeHorizontalSquared() >= 20.25f)
						{
							num *= 0.5f;
						}

						if (launcher != null && pawn.Faction != null && launcher.Faction != null && !pawn.Faction.HostileTo(launcher.Faction))
						{
							num *= ShellInterceptDistanceFactor(Position);
						}
					}
					else
					{
						num = 1.5f * thing.def.fillPercent;
					}

					bool hit = didGroundBounce ? num > 1E-05f : Rand.Chance(num);
					if (hit)
					{
						Impact(thing, false);
						return;
					}
				}

				Impact(null, false);
				return;
			}

			Pawn pawn2 = usedTarget.Thing as Pawn;
			if (!didGroundBounce && pawn2 != null && pawn2.GetPosture() != PawnPosture.Standing
				&& (origin - destination).MagnitudeHorizontalSquared() >= 20.25f && !Rand.Chance(0.5f))
			{
				Impact(null, false);
				return;
			}

			Impact(usedTarget.Thing, false);
		}

		/// <summary>Projectile.TickInterval과 동일하되 가로채기만 <see cref="ShellCanHit"/> 사용(지면 도탄 후에도 동일).</summary>
		private void RunTickIntervalShellProjectile(int delta)
		{
			if (AllComps != null)
			{
				for (int i = 0; i < AllComps.Count; i++)
				{
					AllComps[i].CompTickInterval(delta);
				}
			}

			lifetime -= delta;
			if (landed)
			{
				return;
			}

			Vector3 exactPosition = ExactPosition;
			ticksToImpact -= delta;
			if (!ExactPosition.InBounds(Map))
			{
				ticksToImpact += delta;
				Position = ExactPosition.ToIntVec3();
				Destroy(DestroyMode.Vanish);
				return;
			}

			Vector3 exactPosition2 = ExactPosition;
			bool intercepted = ShellCheckForFreeInterceptBetween(exactPosition, exactPosition2);
			if (intercepted)
			{
				return;
			}

			Position = ExactPosition.ToIntVec3();
			// 세그먼트 샘플링이 건너뛴 칸(한 틱에 여러 칸 등) 보완: 현재 격자에 있는 히트 대상 매 틱 검사
			if (Position.InBounds(Map) && ShellTryInterceptCell(Position, skipIfDestinationCell: false))
			{
				return;
			}

			if (ticksToImpact <= 0)
			{
				if (DestinationCell.InBounds(Map))
				{
					Position = DestinationCell;
				}

				ImpactSomething();
				return;
			}

			TickExplosiveDetonationOnly(delta);
		}

		/// <summary><see cref="Projectile_Explosive"/> 지연 기폭 필드만 바닐라 순서에 맞춰 처리.</summary>
		private void TickExplosiveDetonationOnly(int delta)
		{
			if (ExplosiveTicksToDetonationField == null)
			{
				return;
			}

			int td = (int)ExplosiveTicksToDetonationField.GetValue(this);
			if (td <= 0)
			{
				return;
			}

			td -= delta;
			ExplosiveTicksToDetonationField.SetValue(this, td);
			if (td <= 0)
			{
				Explode();
			}
		}

		private bool ShellCheckForFreeInterceptBetween(Vector3 lastExactPos, Vector3 newExactPos)
		{
			if (lastExactPos == newExactPos)
			{
				return false;
			}

			List<Thing> list = Map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor);
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].TryGetComp<CompProjectileInterceptor>().CheckIntercept(this, lastExactPos, newExactPos))
				{
					Impact(null, true);
					return true;
				}
			}

			IntVec3 intVec = lastExactPos.ToIntVec3();
			IntVec3 intVec2 = newExactPos.ToIntVec3();
			if (intVec2 == intVec)
			{
				return false;
			}

			if (!intVec.InBounds(Map) || !intVec2.InBounds(Map))
			{
				return false;
			}

			if (intVec2.AdjacentToCardinal(intVec))
			{
				return ShellTryInterceptCell(intVec2, skipIfDestinationCell: true);
			}

			float distF = ShellInterceptDistanceFactor(intVec2);
			if (distF <= 0f)
			{
				return false;
			}

			Vector3 vector = lastExactPos;
			Vector3 v = newExactPos - lastExactPos;
			Vector3 b = v.normalized * 0.2f;
			int num = (int)(v.MagnitudeHorizontal() / 0.2f);
			ShellInterceptCheckedCells.Clear();
			int num2 = 0;
			for (;;)
			{
				vector += b;
				IntVec3 intVec3 = vector.ToIntVec3();
				if (!ShellInterceptCheckedCells.Contains(intVec3))
				{
					if (ShellTryInterceptCell(intVec3, skipIfDestinationCell: true))
					{
						break;
					}

					ShellInterceptCheckedCells.Add(intVec3);
				}

				num2++;
				if (num2 > num)
				{
					return false;
				}

				if (intVec3 == intVec2)
				{
					return false;
				}
			}

			return true;
		}

		/// <param name="skipIfDestinationCell">바닐라 가로채기: 목적지 칸은 최종 <see cref="ImpactSomething"/>에 맡김.</param>
		private bool ShellTryInterceptCell(IntVec3 c, bool skipIfDestinationCell)
		{
			if (skipIfDestinationCell && destination.ToIntVec3() == c)
			{
				return false;
			}

			float num = ShellInterceptDistanceFactor(c);
			if (num <= 0f)
			{
				return false;
			}

			List<Thing> thingList = c.GetThingList(Map);
			for (int i = 0; i < thingList.Count; i++)
			{
				Thing thing = thingList[i];
				if (ShellCanHit(thing))
				{
					bool flag2 = false;
					if (thing.def.Fillage == FillCategory.Full)
					{
						Building_Door buildingDoor = thing as Building_Door;
						if (buildingDoor == null || !buildingDoor.Open)
						{
							Impact(thing, false);
							return true;
						}

						flag2 = true;
					}

					float num2 = 0f;
					Pawn pawn = thing as Pawn;
					if (pawn != null)
					{
						num2 = 0.4f * Mathf.Clamp(pawn.BodySize, 0.1f, 2f);
						if (pawn.GetPosture() != PawnPosture.Standing)
						{
							num2 *= 0.1f;
						}

						if (launcher != null && pawn.Faction != null && launcher.Faction != null && !pawn.Faction.HostileTo(launcher.Faction))
						{
							if (preventFriendlyFire)
							{
								num2 = 0f;
							}
							else
							{
								num2 *= Find.Storyteller.difficulty.friendlyFireChanceFactor;
							}
						}
					}
					else if (thing.def.fillPercent > 0.2f)
					{
						if (flag2)
						{
							num2 = 0.05f;
						}
						else if (DestinationCell.AdjacentTo8Way(c))
						{
							num2 = thing.def.fillPercent * 1f;
						}
						else
						{
							num2 = thing.def.fillPercent * 0.15f;
						}
					}

					num2 *= num;
					bool rollHit = didGroundBounce ? num2 > 1E-05f : num2 > 1E-05f && Rand.Chance(num2);
					if (rollHit)
					{
						Impact(thing, false);
						return true;
					}
				}
			}

			return false;
		}

		protected override void Impact(Thing hitThing, bool blockedByShield = false)
		{
			if (blockedByShield)
			{
				DetonateFromImpact();
				return;
			}

			if (hitThing == null)
			{
				if (!didGroundBounce)
				{
					didGroundBounce = true;
					PlayBounceImpactSound();
					DoGroundTouchExplosion();
					ContinueFlightHalfRemainingDistance();
					return;
				}

				DetonateFromImpact();
				return;
			}

			DetonateFromImpact();
		}

		private void DetonateFromImpact()
		{
			GenClamor.DoClamor(this, 12f, ClamorDefOf.Impact);
			if (def.projectile.landedEffecter != null)
			{
				def.projectile.landedEffecter.Spawn(Position, Map, 1f).Cleanup();
			}

			Explode();
		}

		private void PlayBounceImpactSound()
		{
			SoundDef snd = def.projectile.soundImpact;
			if (!snd.NullOrUndefined())
			{
				snd.PlayOneShot(SoundInfo.InMap(new TargetInfo(Position, Map), MaintenanceType.None));
			}
		}

		private int ResolveGroundTouchDamageAmount()
		{
			ProjectileProperties_RatkinCannonShell p = ShellProps;
			if (p != null && p.groundTouchDamageAmount >= 0)
			{
				return p.groundTouchDamageAmount;
			}

			return DamageAmount;
		}

		/// <summary>지면 도탄 순간: 본체는 유지. <see cref="Projectile_Explosive.Explode"/>와 동일 계열, 반경·피해는 Def 설정.</summary>
		private void DoGroundTouchExplosion()
		{
			Map map = Map;
			if (map == null)
			{
				return;
			}

			if (def.projectile.explosionEffect != null)
			{
				Effecter effecter = def.projectile.explosionEffect.Spawn();
				if (def.projectile.explosionEffectLifetimeTicks != 0)
				{
					map.effecterMaintainer.AddEffecterToMaintain(
						effecter,
						Position.ToVector3().ToIntVec3(),
						def.projectile.explosionEffectLifetimeTicks);
				}
				else
				{
					effecter.Trigger(new TargetInfo(Position, map), new TargetInfo(Position, map), -1);
					effecter.Cleanup();
				}
			}

			float explosionRadius = GroundTouchExplosionRadiusCells;
			DamageDef damageDef = DamageDef;
			Thing launcher = this.launcher;
			int damageAmount = ResolveGroundTouchDamageAmount();
			float armorPenetration = ArmorPenetration;
			SoundDef soundExplode = def.projectile.soundExplode;
			ThingDef equipmentDef = this.equipmentDef;
			ThingDef projectileDef = def;
			Thing intended = intendedTarget.Thing;
			ThingDef postExplosionSpawnThingDef = def.projectile.postExplosionSpawnThingDef
				?? (def.projectile.explosionSpawnsSingleFilth ? null : def.projectile.filth);
			ThingDef postExplosionSpawnThingDefWater = def.projectile.postExplosionSpawnThingDefWater;
			float postExplosionSpawnChance = def.projectile.postExplosionSpawnChance;
			int postExplosionSpawnThingCount = def.projectile.postExplosionSpawnThingCount;
			GasType? postExplosionGasType = def.projectile.postExplosionGasType;
			ThingDef preExplosionSpawnThingDef = def.projectile.preExplosionSpawnThingDef;
			float preExplosionSpawnChance = def.projectile.preExplosionSpawnChance;
			int preExplosionSpawnThingCount = def.projectile.preExplosionSpawnThingCount;
			bool applyDamageToExplosionCellsNeighbors = def.projectile.applyDamageToExplosionCellsNeighbors;
			float explosionChanceToStartFire = def.projectile.explosionChanceToStartFire;
			bool explosionDamageFalloff = def.projectile.explosionDamageFalloff;
			float? direction = new float?(origin.AngleToFlat(destination));
			float expolosionPropagationSpeed = damageDef.expolosionPropagationSpeed;
			float screenShakeFactor = def.projectile.screenShakeFactor;
			bool doExplosionVFX = def.projectile.doExplosionVFX;
			ThingDef preExplosionSpawnSingleThingDef = def.projectile.preExplosionSpawnSingleThingDef;
			ThingDef postExplosionSpawnSingleThingDef = def.projectile.postExplosionSpawnSingleThingDef;

			GenExplosion.DoExplosion(
				Position,
				map,
				explosionRadius,
				damageDef,
				launcher,
				damageAmount,
				armorPenetration,
				soundExplode,
				equipmentDef,
				projectileDef,
				intended,
				postExplosionSpawnThingDef,
				postExplosionSpawnChance,
				postExplosionSpawnThingCount,
				postExplosionGasType,
				null,
				255,
				applyDamageToExplosionCellsNeighbors,
				preExplosionSpawnThingDef,
				preExplosionSpawnChance,
				preExplosionSpawnThingCount,
				explosionChanceToStartFire,
				explosionDamageFalloff,
				direction,
				null,
				null,
				doExplosionVFX,
				expolosionPropagationSpeed,
				0f,
				true,
				postExplosionSpawnThingDefWater,
				screenShakeFactor,
				null,
				null,
				postExplosionSpawnSingleThingDef,
				preExplosionSpawnSingleThingDef);

			if (def.projectile.explosionSpawnsSingleFilth && def.projectile.filth != null && def.projectile.filthCount.TrueMax > 0
				&& Rand.Chance(def.projectile.filthChance) && !Position.Filled(map))
			{
				FilthMaker.TryMakeFilth(
					Position,
					map,
					def.projectile.filth,
					def.projectile.filthCount.RandomInRange,
					FilthSourceFlags.None,
					true);
			}
		}

		private void ContinueFlightHalfRemainingDistance()
		{
			Vector3 dir = (destination - origin).Yto0();
			if (dir.sqrMagnitude < 1E-6f)
			{
				dir = Vector3.forward;
			}
			else
			{
				dir.Normalize();
			}

			float fullLeg = Mathf.Max(MinBounceLegCells, (destination - origin).MagnitudeHorizontal());
			float leg = Mathf.Min(GroundBounceMaxDistanceCells, fullLeg * 0.5f);
			Vector3 pos = ExactPosition;
			origin = pos + dir * 0.06f;
			destination = origin + dir * Mathf.Max(0.25f, leg);
			ResetFlightAfterRedirect();
		}

		private void ResetFlightAfterRedirect()
		{
			ticksToImpact = Mathf.CeilToInt(StartingTicksToImpact);
			if (ticksToImpact < 1)
			{
				ticksToImpact = 1;
			}

			lifetime = ticksToImpact;
			landed = false;
		}
	}
}
