using Verse;
using Verse.AI;

namespace NewRatkin
{
    public class JobGiver_PrayerService : ThinkNode_JobGiver
    {
        public int ticks = 50;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            JobGiver_PrayerService obj = (JobGiver_PrayerService)base.DeepCopy(resolve);
            obj.ticks = ticks;
            return obj;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            Job job = JobMaker.MakeJob(RatkinJobDefOf.RK_Job_PrayerService);
            job.expiryInterval = ticks;
            return job;
        }
    }
}
