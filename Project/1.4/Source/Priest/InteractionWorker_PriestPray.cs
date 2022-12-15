using RimWorld;
using Verse;

namespace NewRatkin
{
    public class InteractionWorker_PriestPray : InteractionWorker
	{
		public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
		{
			if (initiator.story.Adulthood != null && initiator.story.Adulthood == RatkinBackstoryDefOf.Ratkin_Sister)
			{
				return ConstPriest.InteractionPrayWeight;
			}

			return 0f;
		}
	}
}
