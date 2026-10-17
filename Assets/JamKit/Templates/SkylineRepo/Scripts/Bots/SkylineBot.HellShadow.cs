using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// Shadow hides you (docs/HELL-SHADOW.md): the bot's side. <see cref="HellTargeting"/> sets <see cref="targetHidden"/> every
    /// frame before the bots think. A bot whose target is hidden doesn't see it (no wake, no telegraph, no shot), and hunts the
    /// spot where it last saw it: the imp's search flies there (to <see cref="HiddenSearchStandOff"/> m of it, so a player who
    /// stayed put is found), the demon walks there, the ooze drives there. Recomputed every frame, so nothing here rewinds.
    /// </summary>
    public sealed partial class SkylineBot
    {
        /// <summary>The target is hidden from this bot this frame (set by HellTargeting; false for every non-Hell use).</summary>
        [System.NonSerialized] public bool targetHidden;

        /// <summary>A searching imp comes this close (m) to where it lost you: inside the 4 m any dark gives way at.</summary>
        public const float HiddenSearchStandOff = 2.5f;

        Vector3 _hellLastSeenFeet, _hellLastSeenChest;
        bool _hellHasLastSeen;

        /// <summary>Where it last saw the target's feet (its own spot until it has seen it).</summary>
        public Vector3 LastSeenFeet => _hellHasLastSeen ? _hellLastSeenFeet : _pos;
        /// <summary>Searching: awake, the target hidden from it, a spot to hunt.</summary>
        public bool HuntingInShadow => targetHidden && IsAwake && _hellHasLastSeen;

        void HellNoteSeen()
        {
            if (!TargetLive(target) || target.Root == null) return;
            _hellLastSeenFeet = target.Root.position;
            _hellLastSeenChest = target.Chest;
            _hellHasLastSeen = true;
        }

        /// <summary>Where a moving bot heads for its target: the target's feet, or the last-seen spot while it is hidden.</summary>
        Vector3 HuntFeet() => targetHidden && _hellHasLastSeen ? _hellLastSeenFeet : target.Root.position;

        /// <summary>The chest to strafe across: the target's, or where it was last seen while hidden.</summary>
        Vector3 HuntChest() => targetHidden && _hellHasLastSeen ? _hellLastSeenChest : target.Chest;

        /// <summary>The imp lost a hidden target: its search heads straight for the spot (keeping its height), stopping
        /// <see cref="HiddenSearchStandOff"/> short, instead of holding where it has a clear line.</summary>
        bool HiddenSearchGoal(out Vector3 goal)
        {
            goal = _pos;
            if (!targetHidden || !_hellHasLastSeen) return false;
            Vector3 spot = _hellLastSeenChest;
            Vector3 flat = new Vector3(_pos.x - spot.x, 0f, _pos.z - spot.z);
            float d = flat.magnitude;
            if (d <= HiddenSearchStandOff) return false;
            Vector3 g = spot + flat / d * HiddenSearchStandOff;
            g.y = Mathf.Max(_pos.y, spot.y);
            g.x = Mathf.Clamp(g.x, SearchArea.xMin, SearchArea.xMax);
            g.z = Mathf.Clamp(g.z, SearchArea.yMin, SearchArea.yMax);
            if (BlockedThick(_pos, g, Def.bodyRadius)) return false;
            goal = g;
            return true;
        }
    }
}
