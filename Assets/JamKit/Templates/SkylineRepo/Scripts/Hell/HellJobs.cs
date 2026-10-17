using System;
using JamKit.Skyline;
using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The run's jobs state and events (docs/HELL-JOBS.md): the ledger (offered, done, failed), whether the God of Death will
    /// spare you for sparing Vesper (<see cref="Favoured"/>), and the rewards. Lane 1's stats screen and lane 4's ending read
    /// this; the job director (<see cref="HellJobDirector"/>) writes it.
    /// </summary>
    public static class HellJobs
    {
        public static readonly HellJobLedger Ledger = new HellJobLedger();

        /// <summary>A job was offered (its room entered), done, or failed (the job, and for done what it paid).</summary>
        public static event Action<HellJobDef> Offered;
        public static event Action<HellJobDef, string> Done;
        public static event Action<HellJobDef, string> Failed;

        public static int DoneCount => Ledger.DoneCount;
        public static int OfferedCount => Ledger.OfferedCount;
        /// <summary>For the ending: did most of what she asked (<see cref="HellJobLedger.Favoured"/>).</summary>
        public static bool Favoured => Ledger.Favoured;

        public static void ResetRun() => Ledger.Clear();

        /// <summary>The Vesper chapter's answer to "did you run her errands on the way down?" (no jobs are offered there): the
        /// ending's pick reads it instead of the counts (<see cref="HellEndingRules.Pick(bool, int, int, bool?)"/>). Null
        /// (the default) in every other run. Not cleared by <see cref="ResetRun"/> (the job director resets the ledger when
        /// the chapter's run starts, after the answer); the run director sets or clears it on every choice from the title.</summary>
        public static bool? AssumedErrands { get; private set; }

        public static void AssumeErrands(bool ranThem) => AssumedErrands = ranThem;
        public static void ClearAssumedErrands() => AssumedErrands = null;

        internal static void RaiseOffered(HellJobDef j)
        {
            Ledger.Offer(j.id);
            HellRunEvents.RaiseJobOffered(j.id);     // counts into the run's stats and fires HellRunEvents.JobOffered
            Offered?.Invoke(j);
        }

        /// <summary>Marks the job done (once), pays its reward and returns the reward's text (null when it was already settled).</summary>
        internal static string RaiseDone(HellJobDef j)
        {
            if (!Ledger.MarkDone(j.id)) return null;
            string paid = Pay(j);
            Debug.Log("HELL_JOB done " + j.id + " (" + paid + "), " + Ledger.DoneCount + "/" + Ledger.OfferedCount);
            HellRunEvents.RaiseJobDone(j.id);        // counts into the run's stats and fires HellRunEvents.JobDone
            Done?.Invoke(j, paid);
            return paid;
        }

        internal static void RaiseFailed(HellJobDef j, string why)
        {
            if (!Ledger.MarkFailed(j.id)) return;
            Debug.Log("HELL_JOB failed " + j.id + " (" + why + "), " + Ledger.DoneCount + "/" + Ledger.OfferedCount);
            HellRunEvents.RaiseJobFailed(j.id);
            Failed?.Invoke(j, why);
        }

        static readonly System.Random Rng = new System.Random();

        /// <summary>Grants the job's reward to the player: a gift through <see cref="HellAbilities.Grant"/> (the same pool as the
        /// floor pick, which then never offers it), or a boon on the abilities and the armour.</summary>
        static string Pay(HellJobDef j)
        {
            var a = HellAbilities.Instance;
            if (a == null) return j.RewardText(null);
            var payout = HellJobRewards.Resolve(j, HellSkills.All, new System.Collections.Generic.List<string>(a.Owned), Rng.Next);
            if (payout.skill != null)
            {
                a.Grant(payout.skill);
                var def = HellSkills.Find(payout.skill);
                return j.RewardText(def != null ? def.name : payout.skill);
            }
            ApplyBoon(a, payout.boon);
            return HellJobBoons.Describe(payout.boon);
        }

        public static void ApplyBoon(HellAbilities a, string boon)
        {
            switch (boon)
            {
                case HellJobBoons.Mend:
                    if (a.Player != null && a.Player.Armor != null) a.Player.Armor.Restore(a.Player.Armor.PlatingMax, a.Player.Armor.ChassisMax);
                    break;
                case HellJobBoons.QuickStep:
                    if (a.StepCharges != null) a.StepCharges.Recharge = HellJobBoons.StepRecharge(a.StepCharges.Recharge);
                    break;
                case HellJobBoons.ShortMemory:
                    if (a.Rewind != null) a.Rewind.cooldown = HellJobBoons.RewindCooldown(a.Rewind.cooldown);   // the field: HellRewind copies it into Cooldown.Duration each frame
                    break;
                case HellJobBoons.LongBreath:
                    a.dilationFuelSeconds += HellJobBoons.LongBreathSeconds;   // a bigger tank (it refills at the same share)
                    break;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Ledger.Clear();
            AssumedErrands = null;
            Offered = null; Done = null; Failed = null;
        }
    }
}
