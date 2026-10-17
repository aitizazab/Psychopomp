using JamKit.Skyline;
using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>A chain anchor of Break the Chains: shootable (a <see cref="SkylineHitZone"/> on its collider points here). A shot
    /// bites only from within the ward (12 m); from farther it is deflected (no hit marker). The room keeps the state.</summary>
    public sealed class HellAnchor : MonoBehaviour, ISkylineHittable
    {
        public HellRoom room;
        public int index;

        public SkylineHitResult TakeHit(in SkylineHitInfo hit)
        {
            if (room == null) return default(SkylineHitResult);
            var r = room.HitAnchor(index, hit.damage);
            return new SkylineHitResult
            {
                hit = r == ChainAnchors.HitResult.Damaged || r == ChainAnchors.HitResult.Broke,
                killed = r == ChainAnchors.HitResult.Broke,
            };
        }
    }
}
