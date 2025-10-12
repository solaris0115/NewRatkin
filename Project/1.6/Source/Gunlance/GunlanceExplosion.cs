using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NewRatkin
{
	public class GunlanceExplosion : Explosion
	{
		private int startTick;

		private List<IntVec3> cellsToAffect;

		private List<Thing> damagedThings;

		private List<Thing> ignoredThings;

		private HashSet<IntVec3> addedCellsAffectedOnlyByDamage;

		private const float DamageFactorAtEdge = 0.2f;

		private static HashSet<IntVec3> temporaryCells = new HashSet<IntVec3>();

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			bool flag = !respawningAfterLoad;
			if (flag)
			{
				this.cellsToAffect = SimplePool<List<IntVec3>>.Get();
				this.cellsToAffect.Clear();
				this.damagedThings = SimplePool<List<Thing>>.Get();
				this.damagedThings.Clear();
				this.addedCellsAffectedOnlyByDamage = SimplePool<HashSet<IntVec3>>.Get();
				this.addedCellsAffectedOnlyByDamage.Clear();
			}
		}

		public override void DeSpawn(DestroyMode mode = 0)
		{
			base.DeSpawn(mode);
			this.cellsToAffect.Clear();
			SimplePool<List<IntVec3>>.Return(this.cellsToAffect);
			this.cellsToAffect = null;
			this.damagedThings.Clear();
			SimplePool<List<Thing>>.Return(this.damagedThings);
			this.damagedThings = null;
			this.addedCellsAffectedOnlyByDamage.Clear();
			SimplePool<HashSet<IntVec3>>.Return(this.addedCellsAffectedOnlyByDamage);
			this.addedCellsAffectedOnlyByDamage = null;
		}

		public void PreStartExplosion(List<IntVec3> explosionCells = null)
		{
			cellsToAffect.Clear();
			cellsToAffect.AddRange(explosionCells);
		}

		public override void StartExplosion(SoundDef explosionSound, List<Thing> ignoredThings)
		{
			bool flag = !base.Spawned;
			if (flag)
			{
				Log.Error("Called StartExplosion() on unspawned thing.");
			}
			else
			{
				this.startTick = Find.TickManager.TicksGame;
				this.ignoredThings = ignoredThings;
				this.damagedThings.Clear();
				this.addedCellsAffectedOnlyByDamage.Clear();
				bool applyDamageToExplosionCellsNeighbors = this.applyDamageToExplosionCellsNeighbors;
				if (applyDamageToExplosionCellsNeighbors)
				{
					this.AddCellsNeighbors(this.cellsToAffect);
				}
				this.damType.Worker.ExplosionStart(this, this.cellsToAffect);
				this.PlayExplosionSound(explosionSound);
				FleckMaker.WaterSplash(base.Position.ToVector3Shifted(), base.Map, this.radius * 6f, 20f);
				this.cellsToAffect.Sort((IntVec3 a, IntVec3 b) => this.GetCellAffectTick(b).CompareTo(this.GetCellAffectTick(a)));
				RegionTraverser.BreadthFirstTraverse(base.Position, base.Map, (Region from, Region to) => true, delegate (Region x)
				{
					List<Thing> allThings = x.ListerThings.AllThings;
					for (int i = allThings.Count - 1; i >= 0; i--)
					{
						bool spawned = allThings[i].Spawned;
						if (spawned)
						{
							allThings[i].Notify_Explosion(this);
						}
					}
					return false;
				}, 25, RegionType.Set_Passable);
			}
		}

		public override void Tick()
		{
			int ticksGame = Find.TickManager.TicksGame;
			int num = this.cellsToAffect.Count - 1;
			while (num >= 0 && ticksGame >= this.GetCellAffectTick(this.cellsToAffect[num]))
			{
				try
				{
					this.AffectCell(this.cellsToAffect[num]);
				}
				catch (Exception ex)
				{
					Log.Error(string.Concat(new object[]
					{
						"Explosion could not affect cell ",
						this.cellsToAffect[num],
						": ",
						ex
					}));
				}
				this.cellsToAffect.RemoveAt(num);
				num--;
			}
			bool flag = !GenCollection.Any<IntVec3>(this.cellsToAffect);
			if (flag)
			{
				this.Destroy(0);
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look<float>(ref this.radius, "radius", 0f, false);
			Scribe_Defs.Look<DamageDef>(ref this.damType, "damType");
			Scribe_Values.Look<int>(ref this.damAmount, "damAmount", 0, false);
			Scribe_Values.Look<float>(ref this.armorPenetration, "armorPenetration", 0f, false);
			Scribe_References.Look<Thing>(ref this.instigator, "instigator", false);
			Scribe_Defs.Look<ThingDef>(ref this.weapon, "weapon");
			Scribe_Defs.Look<ThingDef>(ref this.projectile, "projectile");
			Scribe_References.Look<Thing>(ref this.intendedTarget, "intendedTarget", false);
			Scribe_Values.Look<bool>(ref this.applyDamageToExplosionCellsNeighbors, "applyDamageToExplosionCellsNeighbors", false, false);
			Scribe_Defs.Look<ThingDef>(ref this.preExplosionSpawnThingDef, "preExplosionSpawnThingDef");
			Scribe_Values.Look<float>(ref this.preExplosionSpawnChance, "preExplosionSpawnChance", 0f, false);
			Scribe_Values.Look<int>(ref this.preExplosionSpawnThingCount, "preExplosionSpawnThingCount", 1, false);
			Scribe_Defs.Look<ThingDef>(ref this.postExplosionSpawnThingDef, "postExplosionSpawnThingDef");
			Scribe_Values.Look<float>(ref this.postExplosionSpawnChance, "postExplosionSpawnChance", 0f, false);
			Scribe_Values.Look<int>(ref this.postExplosionSpawnThingCount, "postExplosionSpawnThingCount", 1, false);
			Scribe_Values.Look<float>(ref this.chanceToStartFire, "chanceToStartFire", 0f, false);
			Scribe_Values.Look<bool>(ref this.damageFalloff, "dealMoreDamageAtCenter", false, false);
			Scribe_Values.Look<IntVec3?>(ref this.needLOSToCell1, "needLOSToCell1", null, false);
			Scribe_Values.Look<IntVec3?>(ref this.needLOSToCell2, "needLOSToCell2", null, false);
			Scribe_Values.Look<int>(ref this.startTick, "startTick", 0, false);
			Scribe_Collections.Look<IntVec3>(ref this.cellsToAffect, "cellsToAffect", LookMode.Value, Array.Empty<object>());
			Scribe_Collections.Look<Thing>(ref this.damagedThings, "damagedThings", LookMode.Reference, Array.Empty<object>());
			Scribe_Collections.Look<Thing>(ref this.ignoredThings, "ignoredThings", LookMode.Reference, Array.Empty<object>());
			Scribe_Collections.Look<IntVec3>(ref this.addedCellsAffectedOnlyByDamage, "addedCellsAffectedOnlyByDamage", LookMode.Value);
			bool flag = Scribe.mode == LoadSaveMode.PostLoadInit;
			if (flag)
			{
				List<Thing> list = this.damagedThings;
				if (list != null)
				{
					list.RemoveAll((Thing x) => x == null);
				}
				List<Thing> list2 = this.ignoredThings;
				if (list2 != null)
				{
					list2.RemoveAll((Thing x) => x == null);
				}
			}
		}

		private int GetCellAffectTick(IntVec3 cell)
		{
			return this.startTick + (int)((cell - base.Position).LengthHorizontal * 1.5f);
		}

		private void AffectCell(IntVec3 c)
		{
			bool flag2 = !GenGrid.InBounds(c, base.Map);
			if (!flag2)
			{
				bool flag = this.ShouldCellBeAffectedOnlyByDamage(c);
				bool flag3 = !flag && Rand.Chance(this.preExplosionSpawnChance) && GenGrid.Walkable(c, base.Map);
				if (flag3)
				{
					this.TrySpawnExplosionThing(this.preExplosionSpawnThingDef, c, this.preExplosionSpawnThingCount);
				}
				this.damType.Worker.ExplosionAffectCell(this, c, this.damagedThings, this.ignoredThings, !flag);
				bool flag4 = !flag && Rand.Chance(this.postExplosionSpawnChance) && GenGrid.Walkable(c, base.Map);
				if (flag4)
				{
					this.TrySpawnExplosionThing(this.postExplosionSpawnThingDef, c, this.postExplosionSpawnThingCount);
				}
				float num = this.chanceToStartFire;
				bool damageFalloff = this.damageFalloff;
				if (damageFalloff)
				{
					num *= Mathf.Lerp(1f, 0.2f, IntVec3Utility.DistanceTo(c, base.Position) / this.radius);
				}
				bool flag5 = Rand.Chance(num);
				if (flag5)
				{
					FireUtility.TryStartFireIn(c, base.Map, Rand.Range(0.1f, 0.925f), instigator);
				}
			}
		}

		private void TrySpawnExplosionThing(ThingDef thingDef, IntVec3 c, int count)
		{
			bool flag = thingDef == null;
			if (!flag)
			{
				bool isFilth = thingDef.IsFilth;
				if (isFilth)
				{
					FilthMaker.TryMakeFilth(c, base.Map, thingDef, count, 0);
				}
				else
				{
					Thing thing = ThingMaker.MakeThing(thingDef, null);
					thing.stackCount = count;
					GenSpawn.Spawn(thing, c, base.Map, 0);
				}
			}
		}

		private void PlayExplosionSound(SoundDef explosionSound)
		{
			bool devMode = Prefs.DevMode;
			bool flag;
			if (devMode)
			{
				flag = (explosionSound != null);
			}
			else
			{
				flag = !SoundDefHelper.NullOrUndefined(explosionSound);
			}
			bool flag2 = flag;
			if (flag2)
			{
				SoundStarter.PlayOneShot(explosionSound, new TargetInfo(base.Position, base.Map, false));
			}
			else
			{
				SoundStarter.PlayOneShot(this.damType.soundExplosion, new TargetInfo(base.Position, base.Map, false));
			}
		}

		private void AddCellsNeighbors(List<IntVec3> cells)
		{
			GunlanceExplosion.temporaryCells.Clear();
			this.addedCellsAffectedOnlyByDamage.Clear();
			for (int i = 0; i < cells.Count; i++)
			{
				GunlanceExplosion.temporaryCells.Add(cells[i]);
			}
			for (int j = 0; j < cells.Count; j++)
			{
				bool flag = GenGrid.Walkable(cells[j], base.Map);
				if (flag)
				{
					for (int k = 0; k < GenAdj.AdjacentCells.Length; k++)
					{
						IntVec3 intVec = cells[j] + GenAdj.AdjacentCells[k];
						bool flag2 = GenGrid.InBounds(intVec, base.Map) && GunlanceExplosion.temporaryCells.Add(intVec);
						if (flag2)
						{
							this.addedCellsAffectedOnlyByDamage.Add(intVec);
						}
					}
				}
			}
			cells.Clear();
			foreach (IntVec3 item in GunlanceExplosion.temporaryCells)
			{
				cells.Add(item);
			}
			GunlanceExplosion.temporaryCells.Clear();
		}

		private bool ShouldCellBeAffectedOnlyByDamage(IntVec3 c)
		{
			return this.applyDamageToExplosionCellsNeighbors && this.addedCellsAffectedOnlyByDamage.Contains(c);
		}
	}
}
