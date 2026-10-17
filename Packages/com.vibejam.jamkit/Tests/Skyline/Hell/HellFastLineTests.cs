using System.Collections.Generic;
using System.Numerics;
using JamKit.Skyline;
using NUnit.Framework;

namespace JamKit.Tests.Skyline
{
    /// <summary>Every room's fast line, leg by leg, driven through the real movement engine (docs/HELL-ROOMS.md, "The traversal
    /// simulator"): each leg that needs a jump, a wall-run, a slide or a Shadow Step must succeed on all three floors, at full
    /// and at 90% speed, with every press 0.1 s early or late.</summary>
    public class HellFastLineTests
    {
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        public static IEnumerable<HellLeg> Legs => HellFastLines.All();

        [TestCaseSource(nameof(Legs))]
        public void TheLegHoldsOnEveryFloor(HellLeg leg)
        {
            var fails = new List<string>();
            for (int floor = 0; floor < HellRoomVariants.FloorCount; floor++) fails.AddRange(HellTraversal.RunWithMargins(leg, floor));
            if (fails.Count > 0) Assert.Fail(fails.Count + " runs failed; the first:\n" + string.Join("\n", fails.GetRange(0, System.Math.Min(2, fails.Count))));
        }
    }
}
