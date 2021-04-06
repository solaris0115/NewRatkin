using RimWorld;
using Verse;

namespace NewRatkin
{
    public class InteractionWorker_PriestPray : InteractionWorker
	{
		public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
		{
			if (initiator.story.adulthood != null && initiator.story.adulthood == BackstoryCache.Ratkin_Sister)
			{
				return 0.5f;
			}

			return 0f;
		}
	}
}
