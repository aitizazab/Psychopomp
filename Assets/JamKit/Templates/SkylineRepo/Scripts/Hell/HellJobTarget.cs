using JamKit.Skyline;
using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>A shootable piece of a job (the target, the keeper, a chain anchor, the ledger): every collider under it gets a
    /// <see cref="SkylineHitZone"/> pointing here; hits go to the <see cref="HellJobSite"/>. A child named "WeakPoint" takes the
    /// gun's weak-point multiplier.</summary>
    public sealed class HellJobTarget : MonoBehaviour, ISkylineHittable
    {
        public enum Part { Target, Keeper, Chain }

        public HellJobSite site;
        public Part part;
        public int index;

        /// <summary>Hooks every collider under this object to it (call after the body is built).</summary>
        public void HookColliders()
        {
            foreach (var c in GetComponentsInChildren<Collider>(true))
            {
                var z = c.GetComponent<SkylineHitZone>();
                if (z == null) z = c.gameObject.AddComponent<SkylineHitZone>();
                z.owner = this;
                z.zone = c.name == "WeakPoint" ? HitZone.WeakPoint : HitZone.Body;
            }
        }

        public SkylineHitResult TakeHit(in SkylineHitInfo hit)
        {
            if (site == null || !isActiveAndEnabled) return default(SkylineHitResult);
            float dmg = hit.damage * (hit.zone == HitZone.WeakPoint && hit.gun != null ? hit.gun.weakPointMultiplier : 1f);
            return site.Hit(this, dmg);
        }
    }
}
