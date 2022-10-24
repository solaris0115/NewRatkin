using RimWorld;
using Verse;

namespace NewRatkin
{
    public static class BackstoryCache
    {
        static BackstoryDef backstorySisterCached = null;
        public static BackstoryDef Ratkin_Sister
        {
            get
            {
                return backstorySisterCached;
            }
        }

        public static void CacheBackstorys(Pawn pawn)
        {
            if (backstorySisterCached != null) return;
            backstorySisterCached = pawn.story?.AllBackstories.Find(x => x.defName == "Ratkin_Sister");
        }

    }
}
