using RimWorld;
using Verse;
using Verse.Sound;

namespace NewRatkin
{
	/// <summary>
	/// Stance_Cooldown을 상속하여 쿨다운 종료 시 사운드를 재생하는 클래스
	/// </summary>
	public class Stance_Cooldown_WithSound : Verse.Stance_Cooldown
	{
		private SoundDef endSound;

		public Stance_Cooldown_WithSound()
		{
		}

		/// <summary>
		/// 생성자
		/// </summary>
		/// <param name="ticks">쿨다운 틱 수</param>
		/// <param name="focusTarg">타겟 정보</param>
		/// <param name="verb">Verb (능력의 경우 null)</param>
		/// <param name="endSound">쿨다운 종료 시 재생할 사운드</param>
		public Stance_Cooldown_WithSound(int ticks, LocalTargetInfo focusTarg, Verb verb, SoundDef endSound) 
			: base(ticks, focusTarg, verb)
		{
			this.endSound = endSound;
		}

		/// <summary>
		/// 쿨다운 종료 시 호출되는 메서드 오버라이드
		/// 사운드를 재생한 후 기본 Expire 동작 수행
		/// </summary>
		protected override void Expire()
		{
			// 사운드 재생
			if (this.endSound != null && this.stanceTracker != null && this.stanceTracker.pawn != null)
			{
				Pawn pawn = this.stanceTracker.pawn;
				if (pawn.Spawned && pawn.Map != null)
				{
					this.endSound.PlayOneShot(SoundInfo.InMap(new TargetInfo(pawn.Position, pawn.Map, false), MaintenanceType.None));
				}
			}

			// 기본 Expire 동작 수행
			base.Expire();
		}

		/// <summary>
		/// Save/Load 시 SoundDef 참조 저장
		/// </summary>
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Defs.Look<SoundDef>(ref this.endSound, "endSound");
		}
	}
}
