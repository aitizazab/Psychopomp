using System.Collections.Generic;
using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>The Hell descent's Rewind on the kit's bolts (docs/HELL-ABILITIES.md): one rewindable for the whole pool
    /// (<see cref="Rewindable"/>, registered by HellRewind). A snapshot is every bolt in flight; a restore switches every bolt
    /// off and flies the snapshot's ones again from where they were (an imp's ember again as an ember). A bolt fired after the
    /// snapshot is gone with the rest. The bolts themselves never register: this is the only bolt rewind path
    /// (docs/HELL-INTEGRATION.md).</summary>
    public sealed partial class SkylineBolt
    {
        struct HellBoltSnap
        {
            public Vector3 pos, vel, attackerPos, from;
            public Quaternion rot;
            public float radius, damage, age;
            public bool ember;
            public string attackerId;
            public ISkylineTarget target;
            public Transform owner;
        }

        sealed class PoolRewind : IHellRewindable
        {
            public object Snapshot()
            {
                Purge();
                List<HellBoltSnap> list = null;
                for (int i = 0; i < _pool.Count; i++)
                {
                    var b = _pool[i];
                    if (!b.gameObject.activeSelf) continue;
                    (list ?? (list = new List<HellBoltSnap>())).Add(new HellBoltSnap
                    {
                        pos = b.transform.position, rot = b.transform.rotation, vel = b._vel, attackerPos = b._attackerPos, from = b._from,
                        radius = b._radius, damage = b._damage, age = b._age, attackerId = b._attackerId, target = b._target, owner = b._owner,
                        ember = b._ember,
                    });
                }
                return list;   // null: none in flight
            }

            public void Restore(object snapshot)
            {
                ClearAll();
                if (!(snapshot is List<HellBoltSnap> list)) return;
                foreach (var s in list)
                {
                    var b = Fire(s.pos, s.vel, s.radius, s.damage, s.attackerId, s.target, s.owner);
                    b._attackerPos = s.attackerPos;
                    b._from = s.from;
                    b._age = s.age;
                    b.transform.rotation = s.rot;
                    if (s.ember) b.MakeEmber();
                }
            }

            public override string ToString() => "SkylineBolt pool";
        }

        /// <summary>The pool's one rewindable.</summary>
        public static readonly IHellRewindable Rewindable = new PoolRewind();
    }
}
