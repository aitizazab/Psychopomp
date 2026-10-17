using System.Collections.Generic;
namespace JamKit.SkylineRepo
{
    public static class HellRewindRegistry
    {
        public static readonly List<IHellRewindable> All = new List<IHellRewindable>();
        public static void Register(IHellRewindable r) { if (!All.Contains(r)) All.Add(r); }
        public static void Unregister(IHellRewindable r) { All.Remove(r); }
    }
}
