using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace JamKit.Skyline
{
    /// <summary>Which of a room's two routes the autopilot plays (docs/HELL-AUTOPILOT.md).</summary>
    public enum HellAutoRoute { Safe, Fast }

    /// <summary>How the autopilot gets to a plan point from the one before it.</summary>
    public enum HellAutoMove
    {
        /// <summary>Run on ground (sprint when nothing is being fought).</summary>
        Run,
        /// <summary>A rise of more than a step within a few metres: jump, double jump at the apex, hold forward (the mantle).</summary>
        Climb,
        /// <summary>A gap: sprint to its edge and jump (double jump at the apex if short).</summary>
        Jump,
        /// <summary>A beam lane crosses the leg: slide under it.</summary>
        Slide,
        /// <summary>A gap to a dark landing: stop at the edge, aim at the landing (a turning wrist, not a snap), hold the Shadow
        /// Step button, release once the aim has settled.</summary>
        ShadowStep,
        /// <summary>A gap beside a runnable wall: jump at the wall and run along it; kick off at its end.</summary>
        WallRun,
        /// <summary>Walk off an edge (a drop into a pit, the exit hole).</summary>
        Drop,
    }

    /// <summary>What the autopilot does on arriving at a plan point.</summary>
    public enum HellAutoTask
    {
        None,
        /// <summary>Touch an ordered stop (a sigil, the lantern, the altar): wait until the objective counts it.</summary>
        Stop,
        /// <summary>Shoot chain anchor <see cref="HellAutoPoint.stop"/> from here (inside its ward) until it breaks.</summary>
        Anchor,
        /// <summary>Stand by well <see cref="HellAutoPoint.stop"/> until it is sealed (strafing, shooting oozes).</summary>
        Seal,
        /// <summary>Hold the Brand: stay here (behind the stone) until it is set.</summary>
        Hold,
        /// <summary>Hold the Brand the fast way: run the racing line (<see cref="HellAutoPlan.lap"/>) until it is set.</summary>
        Lap,
        /// <summary>The drop hole: walk into it.</summary>
        Exit,
    }

    public struct HellAutoPoint
    {
        public Vector3 point;
        public HellAutoMove move;
        public HellAutoTask task;
        /// <summary>The stop's index in <see cref="HellRoomDef.stops"/> for Stop, Anchor and Seal; -1 otherwise.</summary>
        public int stop;
        /// <summary>For Jump, ShadowStep and WallRun: the leg's gap (horizontal metres without ground under it).</summary>
        public float gap;
    }

    /// <summary>One room's plan: the points in order, the racing line for a lap, and what the planner could not plan.</summary>
    public sealed class HellAutoPlan
    {
        public HellRoomKind kind;
        public HellAutoRoute route;
        public readonly List<HellAutoPoint> points = new List<HellAutoPoint>();
        public readonly List<Vector3> lap = new List<Vector3>();
        /// <summary>The Collapse: never stop to fight, never leave the line.</summary>
        public bool urgent;
        /// <summary>Things the planner found it can't do (a gap too wide to jump with no dark landing and no wall).</summary>
        public readonly List<string> notes = new List<string>();

        public int IndexOfTask(HellAutoTask task, int stop)
        {
            for (int i = 0; i < points.Count; i++) if (points[i].task == task && points[i].stop == stop) return i;
            return -1;
        }

        public int Count(HellAutoMove move)
        {
            int n = 0;
            foreach (var p in points) if (p.move == move) n++;
            return n;
        }
    }

    /// <summary>
    /// The pure half of the Hell autopilot (docs/HELL-AUTOPILOT.md): turns a room's data (<see cref="HellRoomDef"/>: its safe
    /// or fast path, its stops, its ground, hazards and dark zones) into a plan of points, each with how to get there (run,
    /// climb, jump a gap, slide under a beam, Shadow Step to a dark landing, wall-run) and what to do there (touch, shoot an
    /// anchor, seal a well, hold or lap the brand, drop out). The Unity half (<c>HellAutopilot</c>) plays the plan with the
    /// real player's inputs. Also: where to pick the plan up after a sink, a death or a switch of route.
    /// </summary>
    public static class HellAutopilotPlan
    {
        /// <summary>Path samples (m).</summary>
        public const float Sample = 0.5f;
        /// <summary>A sample has ground when a top lies from this far below it ...</summary>
        public const float GroundBelow = 1.0f;
        /// <summary>... to this far above it (the lint's tolerance).</summary>
        public const float GroundAbove = HellRoomLint.GroundTolerance;
        /// <summary>The widest gap a sprint jump with the double jump is planned over (m: 9.5 m/s for about 1.2 s in the air, less a
        /// margin). Wider needs a Shadow Step or a wall.</summary>
        public const float JumpGap = 9f;
        /// <summary>The Shadow Step's reach (HellAbilities: 18 m), less a margin.</summary>
        public const float StepReach = 17f;
        /// <summary>A runnable wall this close to a gap's middle (m, horizontal) makes it a wall-run.</summary>
        public const float WallReach = 2.5f;
        /// <summary>A rise above this within <see cref="HellRoomLint.ClimbHorizontal"/> is a climb.</summary>
        public const float ClimbRise = 0.6f;
        /// <summary>A fall of more than this is a drop.</summary>
        public const float DropFall = 1.5f;
        /// <summary>Eye height over the feet (MoveDef.eyeHeight), for the anchors' ward.</summary>
        public const float EyeHeight = 1.6f;
        /// <summary>Inside the ward by this margin (m), so a strafe doesn't step out of it.</summary>
        public const float WardMargin = 1.0f;

        public static HellAutoPlan Build(HellRoomDef r, HellAutoRoute route)
        {
            var plan = new HellAutoPlan { kind = r.kind, route = route, urgent = r.kind == HellRoomKind.Collapse };
            var src = route == HellAutoRoute.Fast && r.fastPath.Count >= 2 ? r.fastPath : r.safePath;
            if (src.Count == 0) { plan.notes.Add("no path"); return plan; }

            // 1. the polyline, densely sampled (remembering which samples are the authored corners)
            var samples = new List<Vector3>();
            var corner = new List<bool>();
            var step = new List<bool>();
            samples.Add(src[0]);
            corner.Add(true);
            step.Add(false);
            for (int i = 1; i < src.Count; i++)
            {
                int n = Math.Max(1, (int)Math.Ceiling(Vector3.Distance(src[i - 1], src[i]) / Sample));
                // a climb or a drop of a few metres (the lint allows climbs of 3 m within 4 m): its samples are in the air by
                // design, so they count as ground (the move is decided at the corner)
                float rise = src[i].Y - src[i - 1].Y;
                bool climbOrDrop = Math.Abs(rise) > GroundAbove && Horizontal(src[i - 1], src[i]) <= HellRoomLint.ClimbHorizontal
                    && HasGround(r, src[i - 1]) && HasGround(r, src[i]);
                for (int k = 1; k <= n; k++)
                {
                    samples.Add(Vector3.Lerp(src[i - 1], src[i], k / (float)n));
                    corner.Add(k == n);
                    step.Add(climbOrDrop && k < n);
                }
            }

            // 2. the tasks, each at its best sample
            var tasks = new HellAutoTask[samples.Count];
            var taskStop = new int[samples.Count];
            for (int i = 0; i < taskStop.Length; i++) taskStop[i] = -1;
            AssignTasks(r, route, samples, tasks, taskStop, plan);

            // 3. ground under each sample
            var ground = new bool[samples.Count];
            for (int i = 0; i < samples.Count; i++) ground[i] = step[i] || HasGround(r, samples[i]);
            ground[0] = true;

            // 4. walk the samples: emit the corners and task points; a run of samples without ground becomes one gap leg
            plan.points.Add(new HellAutoPoint { point = samples[0], move = HellAutoMove.Run, task = tasks[0], stop = taskStop[0] });
            int last = 0;
            for (int i = 1; i < samples.Count; i++)
            {
                if (!ground[i])
                {
                    int j = i;
                    while (j < samples.Count && !ground[j]) j++;
                    int land = Math.Min(j, samples.Count - 1);
                    int take = i - 1;
                    // the takeoff point (the last sample with ground) is a point of its own, reached by the usual moves
                    if (take > last)
                    {
                        Emit(r, plan, samples, last, take, tasks[take], taskStop[take]);
                        last = take;
                    }
                    float gap = Horizontal(samples[take], samples[land]);
                    // the gap, leg by leg (its authored corners, and any task in the air: an anchor shot from a wall): each
                    // leg is a wall-run along a runnable wall, a Shadow Step to a dark landing, or a jump
                    int from = take;
                    for (int k = take + 1; k <= land; k++)
                    {
                        if (k < land && !corner[k] && tasks[k] == HellAutoTask.None) continue;
                        var move = GapMove(r, samples, from, k, k == land, gap);
                        // a jump off or onto a wall (a kick between wall-runs) is planned as such; a jump down carries farther
                        float reachable = JumpGap + 2f * Math.Max(0f, samples[from].Y - samples[k].Y);
                        if (move == HellAutoMove.Jump && Horizontal(samples[from], samples[k]) > reachable
                            && !BesideWall(r, samples[from]) && !BesideWall(r, samples[k]))
                            plan.notes.Add("gap " + F(Horizontal(samples[from], samples[k])) + " m at " + V(samples[from]) + " to " + V(samples[k]) + ": no dark landing, no wall; tried as a jump");
                        plan.points.Add(new HellAutoPoint { point = samples[k], move = move, task = tasks[k], stop = taskStop[k], gap = gap });
                        from = k;
                    }
                    last = land;
                    i = land;
                    continue;
                }
                if (corner[i] || tasks[i] != HellAutoTask.None || i == samples.Count - 1)
                {
                    Emit(r, plan, samples, last, i, tasks[i], taskStop[i]);
                    last = i;
                }
            }

            // 5. the racing line for a lap
            if (r.hold != null) plan.lap.AddRange(r.hold.loop);

            // 6. out: the drop hole (its middle, at the exit's floor height)
            var c = (r.dropZone.min + r.dropZone.max) * 0.5f;
            plan.points.Add(new HellAutoPoint { point = new Vector3(c.X, r.exit.Y, c.Z), move = HellAutoMove.Drop, task = HellAutoTask.Exit, stop = -1 });
            return plan;
        }

        /// <summary>One point reached over ground from sample <paramref name="from"/> to <paramref name="to"/>.</summary>
        static void Emit(HellRoomDef r, HellAutoPlan plan, List<Vector3> s, int from, int to, HellAutoTask task, int stop)
        {
            var a = s[from];
            var b = s[to];
            var move = HellAutoMove.Run;
            float rise = b.Y - a.Y, h = Horizontal(a, b);
            if (CrossesBeam(r, a, b)) move = HellAutoMove.Slide;
            else if (rise > ClimbRise && h <= HellRoomLint.ClimbHorizontal && !OnRamp(r, a, b)) move = HellAutoMove.Climb;
            else if (-rise > DropFall && !OnRamp(r, a, b)) move = HellAutoMove.Drop;
            plan.points.Add(new HellAutoPoint { point = b, move = move, task = task, stop = stop });
        }

        /// <summary>One leg of a gap, samples <paramref name="a"/> to <paramref name="b"/>: a Shadow Step when it ends on the
        /// gap's dark landing in reach; a wall-run when most of it runs beside a runnable wall; else a jump.</summary>
        static HellAutoMove GapMove(HellRoomDef r, List<Vector3> s, int a, int b, bool landing, float gap)
        {
            float reach = Vector3.Distance(s[a] + Vector3.UnitY * EyeHeight, s[b]);
            if (landing && InDark(r, s[b]) && reach <= StepReach && gap > 2.5f) return HellAutoMove.ShadowStep;
            int near = 0, n = 0;
            for (int k = a + 1; k <= b; k++)
            {
                n++;
                if (BesideWall(r, s[k])) near++;
            }
            if (n > 0 && near * 2 >= n && Horizontal(s[a], s[b]) > 1.5f) return HellAutoMove.WallRun;
            return HellAutoMove.Jump;
        }

        /// <summary>A runnable wall stands within <see cref="WallReach"/> of the point, reaching above it.</summary>
        public static bool BesideWall(HellRoomDef r, Vector3 p)
        {
            foreach (var o in r.solids)
            {
                if (!o.runnable || o.box.max.Y < p.Y + 1f || o.box.min.Y > p.Y + 1f) continue;
                var q = new Vector3(p.X, Math.Min(Math.Max(p.Y + 1f, o.box.min.Y), o.box.max.Y), p.Z);
                if (HellRoomLint.DistanceToBox(q, o.box) <= WallReach) return true;
            }
            return false;
        }

        static void AssignTasks(HellRoomDef r, HellAutoRoute route, List<Vector3> s, HellAutoTask[] tasks, int[] stopOf, HellAutoPlan plan)
        {
            switch (r.kind)
            {
                case HellRoomKind.SixSigils:
                case HellRoomKind.SoulLantern:
                {
                    // in order: each stop at the first sample in reach after the one before
                    int from = 0;
                    for (int i = 0; i < r.stops.Count; i++)
                    {
                        var st = r.stops[i];
                        int best = -1;
                        float bestD = float.MaxValue;
                        for (int k = from; k < s.Count; k++)
                        {
                            float d = Vector3.Distance(s[k], st.position);
                            if (d < bestD) { bestD = d; best = k; }
                            // the first approach: stop searching once we've been in reach and are walking away
                            if (bestD <= st.reach * 0.6f && d > bestD + 2f) break;
                        }
                        if (best < 0 || bestD > st.reach + 0.5f) { plan.notes.Add("stop " + st.id + " not reached by the path (" + F(bestD) + " m)"); continue; }
                        Put(tasks, stopOf, best, HellAutoTask.Stop, i, plan);
                        from = best + 1;
                    }
                    break;
                }
                case HellRoomKind.BreakChains:
                    for (int i = 0; i < r.stops.Count; i++)
                    {
                        float ward = r.tuning.wardRadius;
                        var st = r.stops[i];
                        int best = -1;
                        float bestD = float.MaxValue;
                        for (int k = 0; k < s.Count; k++)
                        {
                            float d = Vector3.Distance(s[k] + Vector3.UnitY * EyeHeight, st.position);
                            if (d < bestD) { bestD = d; best = k; }
                        }
                        if (best < 0 || bestD > ward) { plan.notes.Add("anchor " + st.id + " out of ward from the path (" + F(bestD) + " m)"); continue; }
                        // the first sample comfortably inside the ward on the way to the best one (a player shoots once in range)
                        int at = best;
                        for (int k = 0; k <= best; k++)
                            if (Vector3.Distance(s[k] + Vector3.UnitY * EyeHeight, st.position) <= ward - WardMargin && HasGround(r, s[k])) { at = k; break; }
                        if (route == HellAutoRoute.Safe && !HasGround(r, s[at])) at = best;
                        Put(tasks, stopOf, at, HellAutoTask.Anchor, i, plan);
                    }
                    break;
                case HellRoomKind.OozeWells:
                    for (int i = 0; i < r.stops.Count; i++)
                    {
                        var st = r.stops[i];
                        float reach = Math.Max(1f, st.reach - 0.8f);
                        int best = -1;
                        float bestD = float.MaxValue;
                        for (int k = 0; k < s.Count; k++)
                        {
                            float d = Horizontal(s[k], st.position);
                            if (d < bestD && HasGround(r, s[k])) { bestD = d; best = k; }
                            if (bestD <= reach && d > bestD + 2f) break;
                        }
                        if (best < 0 || bestD > st.reach) { plan.notes.Add("well " + st.id + " not in reach of the path (" + F(bestD) + " m)"); continue; }
                        Put(tasks, stopOf, best, HellAutoTask.Seal, i, plan);
                    }
                    break;
                case HellRoomKind.HoldBrand:
                    if (r.hold == null) break;
                    if (route == HellAutoRoute.Fast)
                    {
                        for (int k = 0; k < s.Count; k++)
                            if (InRing(r.hold, s[k]) && HasGround(r, s[k])) { Put(tasks, stopOf, k, HellAutoTask.Lap, -1, plan); break; }
                    }
                    else
                    {
                        // the sample inside the ring nearest a stone at least 1 m tall
                        int best = -1;
                        float bestD = float.MaxValue;
                        for (int k = 0; k < s.Count; k++)
                        {
                            if (!InRing(r.hold, s[k])) continue;
                            foreach (var o in r.solids)
                            {
                                if (o.role != "cover" || o.box.max.Y - s[k].Y < HellRoomLint.CoverMinHeight) continue;
                                float d = HellRoomLint.DistanceToBox(new Vector3(s[k].X, o.box.min.Y, s[k].Z), o.box);
                                if (d < bestD) { bestD = d; best = k; }
                            }
                        }
                        if (best < 0) for (int k = 0; k < s.Count; k++) if (InRing(r.hold, s[k])) { best = k; break; }
                        if (best >= 0) Put(tasks, stopOf, best, HellAutoTask.Hold, -1, plan);
                        else plan.notes.Add("the safe path never enters the ring");
                    }
                    break;
            }
        }

        static void Put(HellAutoTask[] tasks, int[] stopOf, int at, HellAutoTask task, int stop, HellAutoPlan plan)
        {
            // two tasks on one sample: move the second to the next free sample
            while (at < tasks.Length && tasks[at] != HellAutoTask.None) at++;
            if (at >= tasks.Length) { plan.notes.Add("no room for task " + task + " " + stop); return; }
            tasks[at] = task;
            stopOf[at] = stop;
        }

        /// <summary>Would the objective count task <paramref name="task"/> on stop <paramref name="stop"/> with the feet here?</summary>
        public static bool TaskCountsAt(HellRoomDef r, HellAutoTask task, int stop, Vector3 feet)
        {
            switch (task)
            {
                case HellAutoTask.Stop: return stop >= 0 && stop < r.stops.Count && Vector3.Distance(feet, r.stops[stop].position) <= r.stops[stop].reach;
                case HellAutoTask.Anchor: return stop >= 0 && stop < r.stops.Count && Vector3.Distance(feet + Vector3.UnitY * EyeHeight, r.stops[stop].position) <= r.tuning.wardRadius;
                case HellAutoTask.Seal: return stop >= 0 && stop < r.stops.Count && Horizontal(feet, r.stops[stop].position) <= r.stops[stop].reach;
                case HellAutoTask.Hold:
                case HellAutoTask.Lap: return r.hold != null && InRing(r.hold, feet);
                default: return true;
            }
        }

        // ------------------------------------------------------------------ geometry

        /// <summary>A top (a solid, a collapsing platform, a timeline tile) lies under the sample, from <see cref="GroundBelow"/>
        /// below it to <see cref="GroundAbove"/> above it. Lava and beams are not ground.</summary>
        public static bool HasGround(HellRoomDef r, Vector3 s)
        {
            foreach (var o in r.solids)
            {
                if (o.role == "gate" || !OnXZ(o.box, s)) continue;
                float top = o.TopAt(s.X, s.Z);
                if (top <= s.Y + GroundAbove && top >= s.Y - GroundBelow) return true;
            }
            foreach (var h in r.hazards)
            {
                if (h.kind != HellHazardKind.Collapse || !OnXZ(h.box, s)) continue;
                float top = h.box.max.Y;
                if (top <= s.Y + GroundAbove && top >= s.Y - GroundBelow) return true;
            }
            return false;
        }

        /// <summary>The landing is in an authored dark zone (the Shadow Step's rule, checked half a metre over the feet).</summary>
        public static bool InDark(HellRoomDef r, Vector3 feet)
        {
            var q = feet + Vector3.UnitY * 0.5f;
            foreach (var d in r.darkZones) if (HellRoomLint.Contains(d, q)) return true;
            return false;
        }

        public static bool InRing(HellHoldRing ring, Vector3 feet) =>
            Horizontal(feet, ring.center) <= ring.radius && Math.Abs(feet.Y - ring.center.Y) <= ring.halfHeight;

        static bool CrossesBeam(HellRoomDef r, Vector3 a, Vector3 b)
        {
            foreach (var h in r.hazards)
                if (h.kind == HellHazardKind.Beam && HellRoomLint.SegmentCrossesXZ(a, b, h.box) && Math.Max(a.Y, b.Y) + 1.8f > h.box.min.Y && Math.Min(a.Y, b.Y) < h.box.max.Y)
                    return true;
            return false;
        }

        /// <summary>The leg's middle stands on a ramp (so a rise or fall along it is walked, not climbed).</summary>
        static bool OnRamp(HellRoomDef r, Vector3 a, Vector3 b)
        {
            var m = (a + b) * 0.5f;
            foreach (var o in r.solids)
                if (o.ramp && OnXZ(o.box, m) && Math.Abs(o.TopAt(m.X, m.Z) - m.Y) <= 0.8f) return true;
            return false;
        }

        static bool OnXZ(CoverBox b, Vector3 s) => s.X >= b.min.X - 0.01f && s.X <= b.max.X + 0.01f && s.Z >= b.min.Z - 0.01f && s.Z <= b.max.Z + 0.01f;

        public static float Horizontal(Vector3 a, Vector3 b)
        {
            float dx = b.X - a.X, dz = b.Z - a.Z;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        // ------------------------------------------------------------------ picking the plan up again

        /// <summary>
        /// Where to carry on after a sink (back to the last safe spot), a switch from the fast plan to the safe one, or any
        /// other jump of the player: the plan point nearest <paramref name="feet"/> between the last task already done and the
        /// first one still to do (<paramref name="taskDone"/> answers per plan point; points without a task count as done).
        /// After the last task, anywhere up to the end.
        /// </summary>
        public static int ResumeIndex(HellAutoPlan plan, Vector3 feet, Func<HellAutoPoint, bool> taskDone)
        {
            int n = plan.points.Count;
            if (n == 0) return 0;
            int firstPending = -1;
            for (int i = 0; i < n; i++)
            {
                var p = plan.points[i];
                if (p.task == HellAutoTask.None || p.task == HellAutoTask.Exit) continue;
                if (!taskDone(p)) { firstPending = i; break; }
            }
            int hi = firstPending >= 0 ? firstPending : n - 1;
            int lo = 0;
            for (int i = hi - 1; i >= 0; i--)
            {
                var p = plan.points[i];
                if (p.task != HellAutoTask.None && p.task != HellAutoTask.Exit) { lo = i; break; }
            }
            int best = hi;
            float bestD = float.MaxValue;
            for (int i = lo; i <= hi; i++)
            {
                // don't resume onto the far side of a gap: its takeoff must be walked to
                var p = plan.points[i];
                if (i > lo && (p.move == HellAutoMove.ShadowStep || p.move == HellAutoMove.WallRun || p.move == HellAutoMove.Jump)) continue;
                float d = Vector3.Distance(p.point, feet);
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        static string F(float v) => v.ToString("0.#", CultureInfo.InvariantCulture);
        static string V(Vector3 v) => "(" + F(v.X) + ", " + F(v.Y) + ", " + F(v.Z) + ")";
    }

    /// <summary>
    /// The autopilot's stuck watch: progress is a move of more than <see cref="radius"/> from where it was last seen moving, or
    /// a change in the progress number (the plan index, the objective's progress). No progress for <see cref="seconds"/> is
    /// stuck (once, until progress resumes). Pure.
    /// </summary>
    public sealed class HellProgressWatch
    {
        public float radius = 1.5f, seconds = 8f;
        Vector3 _at;
        double _progress = double.NaN;
        float _since;
        bool _fired, _started;

        public float StillFor(float now) => _started ? now - _since : 0f;

        public void Reset(float now, Vector3 at, double progress)
        {
            _at = at;
            _progress = progress;
            _since = now;
            _fired = false;
            _started = true;
        }

        /// <summary>True on the call that first finds no progress for <see cref="seconds"/>.</summary>
        public bool Tick(float now, Vector3 at, double progress)
        {
            if (!_started) { Reset(now, at, progress); return false; }
            if (Vector3.Distance(at, _at) > radius || Math.Abs(progress - _progress) > 1e-6)
            {
                Reset(now, at, progress);
                return false;
            }
            if (_fired || now - _since < seconds) return false;
            _fired = true;
            return true;
        }
    }

    /// <summary>Frame times in 0.1 ms bins up to 250 ms (no allocation per frame): mean, percentiles, the 1% low frame rate.</summary>
    public sealed class HellFrameStats
    {
        public const float BinMs = 0.1f, MaxMs = 250f;
        readonly int[] _bins = new int[(int)(MaxMs / BinMs) + 1];
        double _sum;
        public int Count { get; private set; }
        public float MaxFrameMs { get; private set; }

        public void Add(float ms)
        {
            if (!(ms >= 0f)) return;
            int b = (int)(Math.Min(ms, MaxMs) / BinMs);
            _bins[b]++;
            _sum += ms;
            Count++;
            if (ms > MaxFrameMs) MaxFrameMs = ms;
        }

        public void Clear()
        {
            Array.Clear(_bins, 0, _bins.Length);
            _sum = 0;
            Count = 0;
            MaxFrameMs = 0f;
        }

        public float MeanMs => Count == 0 ? 0f : (float)(_sum / Count);

        /// <summary>The frame time (ms, the bin's top) that <paramref name="share"/> of frames are at or under.</summary>
        public float PercentileMs(float share)
        {
            if (Count == 0) return 0f;
            int need = (int)Math.Ceiling(share * Count);
            int seen = 0;
            for (int i = 0; i < _bins.Length; i++)
            {
                seen += _bins[i];
                if (seen >= need) return (i + 1) * BinMs;
            }
            return MaxMs;
        }

        public float MeanFps => MeanMs > 0f ? 1000f / MeanMs : 0f;
        /// <summary>The 1% low: the frame rate of the 99th percentile frame time.</summary>
        public float OnePercentLowFps { get { float p = PercentileMs(0.99f); return p > 0f ? 1000f / p : 0f; } }
    }

    /// <summary>What the Rewind edge-case script saw around one Rewind (docs/HELL-AUTOPILOT.md, "Rewind edge cases").</summary>
    public struct HellRewindObservation
    {
        public string caseId;
        /// <summary>Did the Rewind go off (false: refused, on cooldown or blocked)?</summary>
        public bool rewound;
        /// <summary>Should it have gone off (false for the cases where the game must refuse it: during the pick).</summary>
        public bool expectRewind;
        public float clockBefore, clockAfter, rewindSeconds;
        public float progressBefore, progressAfter;
        /// <summary>The lantern room: the objective says carried / the player is flagged carrying (HellRoomFlags).</summary>
        public bool objectiveCarrying, flagCarrying;
        public bool playerInsideSolid;
        public bool playerOutsideRoom;
        /// <summary>Live bots standing within 0.3 m of another live bot of the same kind.</summary>
        public int doubledBots;
        public int liveBotsBefore, liveBotsAfter, maxLiveBotsSeen;
        public bool aimingAfter;
        public float timeScaleAfter;
        public bool pickOpenAfter;
    }

    /// <summary>The Rewind edge-case checks, pure: what is inconsistent in an observation.</summary>
    public static class HellRewindChecks
    {
        public static List<string> Problems(HellRewindObservation o)
        {
            var p = new List<string>();
            if (o.rewound != o.expectRewind) p.Add(o.expectRewind ? "the Rewind was refused" : "the Rewind went off when it should not have");
            if (o.rewound)
            {
                float back = o.clockBefore - o.clockAfter;
                // the room clock goes back by the Rewind's seconds, or to the room's start (no older history)
                if (back < -0.05f) p.Add("the room clock went forward (" + F(o.clockBefore) + " to " + F(o.clockAfter) + ")");
                else if (back > o.rewindSeconds + 0.25f) p.Add("the room clock went back " + F(back) + " s, more than the Rewind's " + F(o.rewindSeconds));
                else if (back < o.rewindSeconds - 0.25f && o.clockAfter > 0.25f) p.Add("the room clock went back only " + F(back) + " s");
                if (o.progressAfter > o.progressBefore + 1e-4f) p.Add("the objective moved forward on a Rewind (" + F(o.progressBefore) + " to " + F(o.progressAfter) + ")");
                if (o.aimingAfter) p.Add("still aiming a Shadow Step after the Rewind");
            }
            if (o.objectiveCarrying != o.flagCarrying) p.Add("lantern mismatch: objective carrying=" + o.objectiveCarrying + ", player flag carrying=" + o.flagCarrying);
            if (o.playerInsideSolid) p.Add("the player is inside a solid");
            if (o.playerOutsideRoom) p.Add("the player is outside the room");
            if (o.doubledBots > 0) p.Add(o.doubledBots + " doubled bot(s) (two of a kind on one spot)");
            if (o.maxLiveBotsSeen >= 0 && o.liveBotsAfter > o.maxLiveBotsSeen) p.Add("more live bots after the Rewind (" + o.liveBotsAfter + ") than in the last seconds (" + o.maxLiveBotsSeen + ")");
            if (!o.pickOpenAfter && o.timeScaleAfter < 0.05f) p.Add("time is stopped after the Rewind with no screen open");
            return p;
        }

        static string F(float v) => v.ToString("0.##", CultureInfo.InvariantCulture);
    }

    /// <summary>The autopilot's run report: a record per room, the summary line, and the JSON the log file holds (written by
    /// hand: no allocation-free requirement here, it is written once a room).</summary>
    public sealed class HellAutoReport
    {
        public sealed class Room
        {
            public int floor, index;
            public string kind = "", route = "";
            public float entered, objectiveAt = -1f, left = -1f;
            public int deaths, sinks, rewinds, steps, routeSwitches;
            public bool objectiveDone, skipped;
            public float meanFps, lowFps, maxFrameMs;
            public readonly List<string> stuck = new List<string>();
            public readonly List<string> errors = new List<string>();
            public readonly List<string> notes = new List<string>();
            public float Seconds => left >= 0f ? left - entered : float.NaN;
        }

        public string mode = "safe", seed = "", result = "running";
        public readonly List<Room> rooms = new List<Room>();
        public readonly List<string> rewindCases = new List<string>();
        public readonly List<string> rewindProblems = new List<string>();
        public readonly List<string> errors = new List<string>();
        public int errorCount;

        public Room Current => rooms.Count > 0 ? rooms[rooms.Count - 1] : null;

        public int RoomsCleared { get { int n = 0; foreach (var r in rooms) if (r.left >= 0f && !r.skipped) n++; return n; } }
        public int StuckCount { get { int n = 0; foreach (var r in rooms) n += r.stuck.Count; return n; } }
        public int Skipped { get { int n = 0; foreach (var r in rooms) if (r.skipped) n++; return n; } }
        public int Deaths { get { int n = 0; foreach (var r in rooms) n += r.deaths; return n; } }

        public string SummaryLine() =>
            "HELL_AUTOPILOT done rooms=" + RoomsCleared + "/" + rooms.Count + " stuck=" + StuckCount + " skipped=" + Skipped +
            " deaths=" + Deaths + " errors=" + errorCount + " rewindProblems=" + rewindProblems.Count + " result=" + result +
            " mode=" + mode + " seed=" + seed;

        public string ToJson()
        {
            var sb = new StringBuilder(4096);
            sb.Append("{\n  \"mode\": ").Append(Q(mode)).Append(",\n  \"seed\": ").Append(Q(seed)).Append(",\n  \"result\": ").Append(Q(result));
            sb.Append(",\n  \"roomsCleared\": ").Append(RoomsCleared).Append(",\n  \"stuck\": ").Append(StuckCount);
            sb.Append(",\n  \"skipped\": ").Append(Skipped).Append(",\n  \"deaths\": ").Append(Deaths).Append(",\n  \"errorCount\": ").Append(errorCount);
            sb.Append(",\n  \"rooms\": [");
            for (int i = 0; i < rooms.Count; i++)
            {
                var r = rooms[i];
                sb.Append(i == 0 ? "\n" : ",\n").Append("    {\"floor\": ").Append(r.floor + 1).Append(", \"room\": ").Append(r.index + 1);
                sb.Append(", \"kind\": ").Append(Q(r.kind)).Append(", \"route\": ").Append(Q(r.route));
                sb.Append(", \"entered\": ").Append(N(r.entered)).Append(", \"objectiveAt\": ").Append(N(r.objectiveAt)).Append(", \"seconds\": ").Append(N(r.Seconds));
                sb.Append(", \"objectiveDone\": ").Append(r.objectiveDone ? "true" : "false").Append(", \"skipped\": ").Append(r.skipped ? "true" : "false");
                sb.Append(", \"deaths\": ").Append(r.deaths).Append(", \"sinks\": ").Append(r.sinks).Append(", \"rewinds\": ").Append(r.rewinds);
                sb.Append(", \"shadowSteps\": ").Append(r.steps).Append(", \"routeSwitches\": ").Append(r.routeSwitches);
                sb.Append(", \"meanFps\": ").Append(N(r.meanFps)).Append(", \"onePercentLowFps\": ").Append(N(r.lowFps)).Append(", \"maxFrameMs\": ").Append(N(r.maxFrameMs));
                sb.Append(", \"stuck\": ").Append(List(r.stuck)).Append(", \"errors\": ").Append(List(r.errors)).Append(", \"notes\": ").Append(List(r.notes)).Append('}');
            }
            sb.Append("\n  ],\n  \"rewindCases\": ").Append(List(rewindCases));
            sb.Append(",\n  \"rewindProblems\": ").Append(List(rewindProblems));
            sb.Append(",\n  \"errors\": ").Append(List(errors));
            sb.Append(",\n  \"summary\": ").Append(Q(SummaryLine())).Append("\n}\n");
            return sb.ToString();
        }

        static string N(float v) => float.IsNaN(v) || float.IsInfinity(v) ? "null" : v.ToString("0.###", CultureInfo.InvariantCulture);

        static string List(List<string> l)
        {
            var sb = new StringBuilder("[");
            for (int i = 0; i < l.Count; i++) sb.Append(i == 0 ? "" : ", ").Append(Q(l[i]));
            return sb.Append(']').ToString();
        }

        public static string Q(string s)
        {
            if (s == null) return "null";
            var sb = new StringBuilder(s.Length + 2);
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.Append('"').ToString();
        }
    }
}
