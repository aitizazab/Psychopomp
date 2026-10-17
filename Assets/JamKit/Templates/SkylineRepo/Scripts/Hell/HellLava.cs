using System.Collections.Generic;
using JamKit.Skyline;
using UnityEngine;
using NVec3 = System.Numerics.Vector3;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// A room's lava (all its basins; the editor builds the glowing boxes under it). While the player's feet touch it: damage
    /// over time; staying in it for the room's sink time sinks you (a heavy hit, then <see cref="HellRoom.Sank"/> puts you back
    /// at the last safe spot). The abilities: no damage while <c>HellAbilities.LavaImmune</c>; <c>HellAbilities.TryLavaBounce()</c>
    /// is asked every step the feet touch it (true: bounced, no damage). <c>HellAbilities.LavaQuery</c> answers "is this
    /// point lava?" from every live room. Bots in the lava burn (<c>HellBurn.Apply</c>). Rewindable (the sink clock).
    /// </summary>
    public sealed class HellLava : MonoBehaviour, IHellRewindable
    {
        public const float BurnEvery = 0.5f, BurnSeconds = 1.5f;

        static readonly List<HellLava> Live = new List<HellLava>();

        HellRoom _room;
        LavaContact _contact;
        readonly List<CoverBox> _boxes = new List<CoverBox>();
        readonly Dictionary<SkylineBot, float> _lastBurn = new Dictionary<SkylineBot, float>();

        public bool Touching { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Live.Clear(); }

        public void Bind(HellRoom room)
        {
            _room = room;
            _boxes.Clear();
            foreach (var h in room.Def.hazards) if (h.kind == HellHazardKind.Lava) _boxes.Add(h.box);
            _contact = new LavaContact(room.Def.lavaDps, room.Def.lavaSinkDamage, room.Def.lavaSinkSeconds);
        }

        void OnEnable()
        {
            if (!Live.Contains(this)) Live.Add(this);
            HellRewindRegistry.Register(this);
            HellAbilities.LavaQuery = IsLava;
        }

        void OnDisable()
        {
            Live.Remove(this);
            HellRewindRegistry.Unregister(this);
        }

        /// <summary>Is the point in (or a hand above) any live room's lava?</summary>
        public static bool IsLava(Vector3 p)
        {
            var n = new NVec3(p.x, p.y, p.z);
            foreach (var l in Live)
                foreach (var b in l._boxes)
                    if (LavaContact.Touching(b, n, 0.3f)) return true;
            return false;
        }

        public bool Contains(Vector3 feet)
        {
            var n = new NVec3(feet.x, feet.y, feet.z);
            foreach (var b in _boxes) if (LavaContact.Touching(b, n)) return true;
            return false;
        }

        public void ResetLava()
        {
            if (_contact != null) _contact.S = default(LavaContact.State);
            Touching = false;
        }

        void FixedUpdate()
        {
            if (_room == null || !_room.Running || _room.Player == null || _contact == null) return;
            var player = _room.Player;
            var feet = player.transform.position;
            bool touching = Contains(feet);
            Touching = touching;
            float dt = Time.fixedDeltaTime;
            if (touching && HellAbilities.TryLavaBounce())
            {
                _contact.S = default(LavaContact.State);
            }
            else
            {
                float dmg = _contact.Tick(dt, touching, HellAbilities.LavaImmune, out bool sank);
                if (dmg > 0f) player.TakeAttack(dmg, "lava", feet + Vector3.down);
                if (sank && player.Armor != null && !player.Armor.Wrecked) _room.Sank("lava");
            }
            BurnBots();
        }

        void BurnBots()
        {
            float now = Time.time;
            foreach (var bot in SkylineBot.Live)
            {
                if (bot == null || !bot.Alive) continue;
                if (!Contains(bot.transform.position)) continue;
                if (_lastBurn.TryGetValue(bot, out float t) && now - t < BurnEvery) continue;
                _lastBurn[bot] = now;
                HellBurn.Apply(bot, BurnSeconds, _room.Def.lavaDps);
            }
        }

        readonly HellSnapPool<HellSnapBox<LavaContact.State>> _snaps = new HellSnapPool<HellSnapBox<LavaContact.State>>();

        public object Snapshot()
        {
            var b = _snaps.Next();
            b.value = _contact != null ? _contact.S : default(LavaContact.State);
            return b;
        }

        public void Restore(object snapshot)
        {
            if (_contact != null) _contact.S = HellSnapBox<LavaContact.State>.From(snapshot);
        }
    }
}
