using System;

namespace JamKit.Skyline
{
    /// <summary>What one step of a job saw (the Unity side measures it).</summary>
    public struct HellJobInput
    {
        /// <summary>The player is within the job's reach of its spot (Reap: the soul; Hold: the seal; Retrieve: the thing).</summary>
        public bool inReach;
        /// <summary>The player's feet are inside the pocket.</summary>
        public bool inPocket;
        /// <summary>The target could notice the player now: within its alert radius, in its sight, and not hidden by darkness
        /// when the job says darkness hides (the Unity side decides; the machine adds the glance rule).</summary>
        public bool seen;
        /// <summary>The keeper (Destroy's clerk, Free's smith) is alive.</summary>
        public bool keeperAlive;
    }

    /// <summary>What happened in a step or a hit (flags).</summary>
    [Flags]
    public enum HellJobEvent
    {
        None = 0,
        Alerted = 1,
        Done = 2,
        Failed = 4,
        /// <summary>Burst: the wound was undone (the target heals and hops back).</summary>
        Undone = 8,
        /// <summary>Free: a chain was re-forged.</summary>
        Reforged = 16,
        /// <summary>Hold: an enemy is due.</summary>
        SpawnDue = 32,
        /// <summary>Burst: the target hops to its next perch.</summary>
        Hopped = 64,
        PickedUp = 128,
        Dropped = 256,
        Damaged = 512,
        /// <summary>A shot from outside the ward: no damage.</summary>
        Deflected = 1024,
    }

    /// <summary>A job's whole changing state: plain fields only, so a snapshot is a copy (the Rewind).</summary>
    public sealed class HellJobState
    {
        public HellJobStatus status = HellJobStatus.Idle;
        public string failReason = "";
        /// <summary>Seconds since the job began; seconds since the alert (Escape, Destroy).</summary>
        public float clock, alertClock;
        /// <summary>Reap / Hold progress in seconds.</summary>
        public float progress;
        /// <summary>The target's (Escape, Burst) or the object's (Destroy) hit points.</summary>
        public float health;
        /// <summary>Burst: seconds since the first unfinished hit (negative: none open).</summary>
        public float window = -1f;
        /// <summary>Burst: the current perch.</summary>
        public int perch;
        /// <summary>Free: each chain's hit points (up to three chains).</summary>
        public float chain0, chain1, chain2;
        /// <summary>Free: seconds since a chain was last broken or re-forged while any is broken.</summary>
        public float reforgeClock;
        /// <summary>Retrieve: carried.</summary>
        public bool carried;
        /// <summary>Hold: seconds since the last spawn; spawns made.</summary>
        public float spawnClock;
        public int spawned;

        public HellJobState Copy() => (HellJobState)MemberwiseClone();
    }

    /// <summary>
    /// One job's rules, pure: <see cref="Begin"/> when its room starts, <see cref="Tick"/> every fixed step, <see cref="Hit"/>
    /// for shots, <see cref="Leave"/> when the room is left (unfinished = failed), <see cref="Restart"/> after a death (the job
    /// starts over with the room). Rewind: <see cref="Snapshot"/> / <see cref="Restore"/>.
    /// </summary>
    public sealed class HellJob
    {
        public readonly HellJobDef def;
        HellJobState _s = new HellJobState();

        public HellJob(HellJobDef def)
        {
            this.def = def ?? throw new ArgumentNullException(nameof(def));
            Reset(HellJobStatus.Idle);
        }

        public HellJobState State => _s;
        public HellJobStatus Status => _s.status;
        public bool Settled => _s.status == HellJobStatus.Done || _s.status == HellJobStatus.Failed;
        public bool Alerted => _s.status == HellJobStatus.Alerted;
        public float Health => _s.health;
        public bool Carried => _s.carried;
        public int Perch => _s.perch;

        /// <summary>0..1: Escape, how far along its flee the target is; Reap / Hold, the progress; Destroy, the keeper's filing.</summary>
        public float Progress01
        {
            get
            {
                switch (def.verb)
                {
                    case HellJobVerb.Escape: return Alerted ? Clamp01(_s.alertClock / Math.Max(0.01f, def.escapeSeconds)) : 0f;
                    case HellJobVerb.Reap:
                    case HellJobVerb.Hold: return Clamp01(_s.progress / Math.Max(0.01f, def.seconds));
                    case HellJobVerb.Destroy: return Alerted ? Clamp01(_s.alertClock / Math.Max(0.01f, def.timerSeconds)) : 0f;
                    default: return 0f;
                }
            }
        }

        /// <summary>Free: is chain <paramref name="i"/> broken.</summary>
        public bool ChainBroken(int i) => i >= 0 && i < Chains && ChainHealth(i) <= 0f;
        public int Chains => Math.Max(1, Math.Min(3, def.chains));
        public float ChainHealth(int i) => i == 0 ? _s.chain0 : i == 1 ? _s.chain1 : _s.chain2;

        void SetChain(int i, float v)
        {
            if (i == 0) _s.chain0 = v;
            else if (i == 1) _s.chain1 = v;
            else _s.chain2 = v;
        }

        void Reset(HellJobStatus status)
        {
            _s = new HellJobState { status = status, health = def.health };
            for (int i = 0; i < 3; i++) SetChain(i, i < Chains ? def.health : 0f);
        }

        /// <summary>The room began: the job is on.</summary>
        public void Begin()
        {
            if (_s.status == HellJobStatus.Idle) _s.status = HellJobStatus.Active;
        }

        /// <summary>A death restarted the room: the job starts over (a settled job stays settled).</summary>
        public void Restart()
        {
            if (Settled) return;
            Reset(HellJobStatus.Active);
        }

        /// <summary>The room was left: an unfinished job fails. Retrieve is done instead when the thing is carried out through the
        /// room's drop (<paramref name="throughDrop"/>).</summary>
        public HellJobEvent Leave(bool throughDrop)
        {
            if (Settled || _s.status == HellJobStatus.Idle) return HellJobEvent.None;
            if (def.verb == HellJobVerb.Retrieve && _s.carried && throughDrop) return Finish();
            return Fail("left");
        }

        /// <summary>The target noticed the player (or was shot at).</summary>
        public HellJobEvent Alert()
        {
            if (_s.status != HellJobStatus.Active) return HellJobEvent.None;
            if (def.verb != HellJobVerb.Escape && def.verb != HellJobVerb.Destroy) return HellJobEvent.None;
            _s.status = HellJobStatus.Alerted;
            _s.alertClock = 0f;
            return HellJobEvent.Alerted;
        }

        /// <summary>Is the target looking round now (the Poacher's glance)? Always, without a glance rule.</summary>
        public bool Glancing => def.glanceEvery <= 0f || (_s.clock % def.glanceEvery) >= def.glanceEvery - def.glanceSeconds;

        public HellJobEvent Tick(float dt, HellJobInput input)
        {
            if (_s.status != HellJobStatus.Active && _s.status != HellJobStatus.Alerted) return HellJobEvent.None;
            _s.clock += dt;
            var ev = HellJobEvent.None;
            switch (def.verb)
            {
                case HellJobVerb.Escape:
                    if (!Alerted && input.seen && Glancing) ev |= Alert();
                    if (Alerted)
                    {
                        _s.alertClock += dt;
                        if (_s.alertClock >= def.escapeSeconds) ev |= Fail("escaped");
                    }
                    break;
                case HellJobVerb.Destroy:
                    if (!Alerted && (input.inPocket || input.seen)) ev |= Alert();
                    if (Alerted && input.keeperAlive)
                    {
                        _s.alertClock += dt;
                        if (_s.alertClock >= def.timerSeconds) ev |= Fail("filed");
                    }
                    break;
                case HellJobVerb.Reap:
                    if (input.inReach) _s.progress += dt;
                    else _s.progress = Math.Max(0f, _s.progress - def.decay * dt);
                    if (_s.progress >= def.seconds) ev |= Finish();
                    break;
                case HellJobVerb.Hold:
                    if (input.inReach) _s.progress += dt;
                    if (_s.progress > 0f)
                    {
                        _s.spawnClock += dt;
                        if (_s.spawnClock >= def.spawnEvery && _s.spawned < def.spawnCap)
                        {
                            _s.spawnClock = 0f;
                            _s.spawned++;
                            ev |= HellJobEvent.SpawnDue;
                        }
                    }
                    if (_s.progress >= def.seconds) ev |= Finish();
                    break;
                case HellJobVerb.Retrieve:
                    if (!_s.carried && input.inReach)
                    {
                        _s.carried = true;
                        ev |= HellJobEvent.PickedUp;
                    }
                    break;
                case HellJobVerb.Free:
                    bool anyBroken = false;
                    for (int i = 0; i < Chains; i++) if (ChainBroken(i)) anyBroken = true;
                    if (anyBroken && input.keeperAlive)
                    {
                        _s.reforgeClock += dt;
                        if (_s.reforgeClock >= def.reforgeSeconds)
                        {
                            _s.reforgeClock = 0f;
                            for (int i = 0; i < Chains; i++)
                                if (ChainBroken(i)) { SetChain(i, def.health); ev |= HellJobEvent.Reforged; break; }
                        }
                    }
                    else if (!anyBroken) _s.reforgeClock = 0f;
                    break;
                case HellJobVerb.Burst:
                    int perch = def.hopSeconds > 0f ? (int)(_s.clock / def.hopSeconds) : 0;
                    if (_s.window >= 0f)
                    {
                        _s.window += dt;
                        if (_s.window > def.burstWindow)
                        {
                            _s.window = -1f;
                            _s.health = def.health;
                            ev |= HellJobEvent.Undone;
                        }
                    }
                    else if (perch != _s.perch)
                    {
                        // the target only hops while no wound is open (a hop never saves it from a burst in progress)
                        _s.perch = perch;
                        ev |= HellJobEvent.Hopped;
                    }
                    break;
            }
            return ev;
        }

        /// <summary>A shot on the target (Escape, Burst) or the object (Destroy) for <paramref name="damage"/>, from
        /// <paramref name="distance"/> metres (the ward: Destroy).</summary>
        public HellJobEvent Hit(float damage, float distance)
        {
            if (_s.status != HellJobStatus.Active && _s.status != HellJobStatus.Alerted) return HellJobEvent.None;
            if (!(damage > 0f)) return HellJobEvent.None;
            var ev = HellJobEvent.None;
            switch (def.verb)
            {
                case HellJobVerb.Escape:
                    if (!Alerted) damage *= def.sneakMultiplier;
                    _s.health -= damage;
                    ev |= HellJobEvent.Damaged;
                    if (_s.health <= 0f) return ev | Finish();
                    return ev | Alert();
                case HellJobVerb.Destroy:
                    if (distance > def.wardRadius) return HellJobEvent.Deflected | Alert();
                    _s.health -= damage;
                    ev |= HellJobEvent.Damaged | Alert();
                    if (_s.health <= 0f) ev |= Finish();
                    return ev;
                case HellJobVerb.Burst:
                    _s.health -= damage;
                    ev |= HellJobEvent.Damaged;
                    if (_s.health <= 0f) return ev | Finish();
                    if (_s.window < 0f) _s.window = 0f;
                    return ev;
                default:
                    return HellJobEvent.None;
            }
        }

        /// <summary>Free: a shot on chain <paramref name="i"/> from <paramref name="distance"/> metres.</summary>
        public HellJobEvent HitChain(int i, float damage, float distance)
        {
            if (def.verb != HellJobVerb.Free || Settled || _s.status == HellJobStatus.Idle) return HellJobEvent.None;
            if (i < 0 || i >= Chains || ChainBroken(i) || !(damage > 0f)) return HellJobEvent.None;
            if (distance > def.wardRadius) return HellJobEvent.Deflected;
            float h = ChainHealth(i) - damage;
            SetChain(i, Math.Max(0f, h));
            var ev = HellJobEvent.Damaged;
            if (h <= 0f)
            {
                _s.reforgeClock = 0f;
                bool all = true;
                for (int k = 0; k < Chains; k++) if (!ChainBroken(k)) all = false;
                if (all) ev |= Finish();
            }
            return ev;
        }

        /// <summary>Retrieve: the carrier sank or fell; the thing goes back to its shelf.</summary>
        public HellJobEvent Drop()
        {
            if (def.verb != HellJobVerb.Retrieve || !_s.carried || Settled) return HellJobEvent.None;
            _s.carried = false;
            return HellJobEvent.Dropped;
        }

        HellJobEvent Finish()
        {
            _s.status = HellJobStatus.Done;
            _s.window = -1f;
            return HellJobEvent.Done;
        }

        HellJobEvent Fail(string why)
        {
            _s.status = HellJobStatus.Failed;
            _s.failReason = why;
            return HellJobEvent.Failed;
        }

        public object Snapshot() => _s.Copy();

        /// <summary>A Rewind can undo a failure (the Viscount reaching his door, the ledger filed) but never a success: a done job
        /// stays done (its reward is already paid).</summary>
        public void Restore(object snapshot)
        {
            if (_s.status == HellJobStatus.Done) return;
            if (snapshot is HellJobState s) _s = s.Copy();
        }

        static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
