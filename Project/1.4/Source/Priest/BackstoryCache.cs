using RimWorld;
using Verse;

namespace NewRatkin
{
    public static class BackstoryCache
    {
        public static Backstory Ratkin_Sister
        {
            get
            {
                if (backstorySisterCached == null)
                {
                    CacheBackstorys();
                }

                return backstorySisterCached;
            }
        }

        static void CacheBackstorys()
        {
            
            backstorySisterCached = BackstoryDatabase.allBackstories.TryGetValue("Ratkin_Sister", null);
        }

        static Backstory backstorySisterCached = null;
    }
}
