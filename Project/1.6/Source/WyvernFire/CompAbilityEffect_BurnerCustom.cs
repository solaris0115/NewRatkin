using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NewRatkin
{
	public class CompAbilityEffect_BurnerCustom : CompAbilityEffect
	{
		public new CompProperties_AbilityBurnerCustom Props
		{
			get
			{
				return (CompProperties_AbilityBurnerCustom)this.props;
			}
		}

		public override IEnumerable<PreCastAction> GetPreCastActions()
		{
			if (!ModsConfig.AnomalyActive || ThingDefOf.IncineratorSpray == null)
			{
				yield break;
			}
			yield return new PreCastAction
			{
				action = delegate (LocalTargetInfo targetInfo, LocalTargetInfo _)
				{
					Pawn pawn = this.parent.pawn;
					IntVec3 pawnCell = pawn.DrawPos.Yto0().ToIntVec3();
					Map map = pawn.Map;
					// 초기에 pawn 위치를 셀 중심으로 변환 (0.5씩 더함)
					Vector3 pawnDrawPos = pawnCell.ToVector3Shifted();
					IncineratorSpray incineratorSpray = GenSpawn.Spawn(ThingDefOf.IncineratorSpray, pawnCell, map, WipeMode.Vanish) as IncineratorSpray;
					int numStreams = this.Props.numStreams;
					int addedStreamsCount = 0;

					// 타겟 방향 계산 - 타겟 위치도 셀 중심으로 변환 (0.5씩 더함)
					Vector3 baseDirection;
					if (targetInfo.IsValid)
					{
						Vector3 targetPos = targetInfo.Cell.ToVector3Shifted();
						Vector3 targetDir = (targetPos - pawnDrawPos).Yto0();
						baseDirection = targetDir.normalized;
					}
					else
					{
						baseDirection = pawn.Rotation.FacingCell.ToVector3().normalized;
					}
					// spawnOffsetDistance를 적용한 시작 위치 계산 (변환된 pawnDrawPos 사용)
					Vector3 baseSpawnPos = pawnDrawPos + baseDirection * this.Props.spawnOffsetDistance;
					// 각도 균등 분산 계산 (랜덤 제거)
					float angleStepPerStream = numStreams > 1 ? (2f * this.Props.coneSizeDegrees) / (numStreams - 1) : 0f;
					for (int streamIndex = 0; streamIndex < numStreams; streamIndex++)
					{
						// -coneSizeDegrees부터 +coneSizeDegrees까지 균등 분산
						float streamAngle = -this.Props.coneSizeDegrees + (streamIndex * angleStepPerStream);
						Vector3 streamDirection = baseDirection.RotatedBy(streamAngle);
						// 무조건 range만큼만 렌더링 (시야 체크 없음)
						float streamRange = this.Props.range + Rand.Value * this.Props.rangeNoise;
						Vector3 streamEndPos = baseSpawnPos + streamDirection * streamRange;
						IntVec3 finalEndCell = streamEndPos.ToIntVec3();
						// 계산된 위치를 그대로 사용 (셀 중심 변환 하지 않음 - 이미 초기에 변환했으므로)
						Vector3 calculatedWorldTarget = streamEndPos;
						float streamDistance = Vector3.Distance(calculatedWorldTarget, baseSpawnPos);
						float sizeMultiplier = Mathf.Clamp01(streamDistance / this.Props.sizeReductionDistanceThreshold);
						// dotProduct 계산: streamDirection과 실제 도달 방향의 일치도 확인
						Vector3 actualDirection = (calculatedWorldTarget - baseSpawnPos).normalized;
						float dotProduct = Vector3.Dot(actualDirection, streamDirection);
						// dotProduct 임계값을 0.3으로 낮춰서 더 많은 스트림이 통과하도록 함
						if (dotProduct > 0.3f)
						{
							// Use Props.moteDef if set, otherwise fallback to Mote_IncineratorBurst
							ThingDef moteDefToUse = this.Props.moteDef ?? ThingDefOf.Mote_IncineratorBurst;
							MoteDualAttached mote = MoteMaker.MakeInteractionOverlay(moteDefToUse, new TargetInfo(pawnCell, map, false), new TargetInfo(finalEndCell, map, false));

							// 계산된 값들
							Vector3 calculatedWorldSource = baseSpawnPos + streamDirection * this.Props.barrelOffsetDistance;
							float calculatedStartScale = Rand.Range(0.8f, 1.2f) * sizeMultiplier;
							float calculatedEndScale = (1f + Rand.Range(0.1f, 0.4f)) * sizeMultiplier;
							// lifespan 계산: lifespanTicks가 지정되면 고정값 사용, 아니면 거리 기반 계산
							int calculatedLifespanTicks;
							if (this.Props.lifespanTicks.HasValue)
							{
								calculatedLifespanTicks = this.Props.lifespanTicks.Value + Rand.Range(-this.Props.lifespanNoise, this.Props.lifespanNoise);
							}
							else
							{
								calculatedLifespanTicks = Mathf.FloorToInt(streamDistance * this.Props.lifespanMultiplier) + Rand.Range(-this.Props.lifespanNoise, this.Props.lifespanNoise);
							}

							if (incineratorSpray != null)
							{
								incineratorSpray.Add(new IncineratorProjectileMotion
								{
									mote = mote,
									targetDest = targetInfo.Cell,
									worldSource = calculatedWorldSource,
									worldTarget = calculatedWorldTarget,
									moveVector = streamDirection,
									startScale = calculatedStartScale / 2,
									endScale = calculatedEndScale / 2,
									lifespanTicks = calculatedLifespanTicks
								});
								addedStreamsCount++;
							}
							if (this.Props.effecterDef != null)
							{
								map.effecterMaintainer.AddEffecterToMaintain(this.Props.effecterDef.Spawn(finalEndCell, map, 1f), finalEndCell, 100);
							}
						}
					}
				},
				ticksAwayFromCast = 5
			};
			yield break;
		}
	}
}
