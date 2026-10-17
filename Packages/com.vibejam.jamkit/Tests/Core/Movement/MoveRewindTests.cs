using System.Numerics;
using JamKit.Movement;
using NUnit.Framework;
using static JamKit.Tests.Movement.MoveTestKit;

namespace JamKit.Tests.Movement
{
    /// <summary>The Hell descent's Rewind (docs/HELL-ABILITIES.md): SaveState / RestoreState put the body back exactly, so
    /// stepping on from a restore follows the same path as stepping on from the save, mid-slide and mid-jump included.</summary>
    public class MoveRewindTests
    {
        static void Steps(MoveSim sim, MoveInput input, int n)
        {
            for (int i = 0; i < n; i++) { sim.Step(input, H); input.jumpPressed = false; input.crouchPressed = false; }
        }

        [Test]
        public void RestoreReplaysTheSamePath()
        {
            var w = Flat();
            var sim = Standing(w, new Vector3(0, 0, 0));
            Steps(sim, Forward(), 120);                                   // run up to speed
            Steps(sim, Forward().With(crouchPressed: true), 10);          // into a slide
            var save = sim.SaveState();
            var mode = sim.Mode;
            Steps(sim, Forward().With(jump: true), 1);
            Steps(sim, Forward(), 60);
            var first = sim.Position;
            var firstVel = sim.Velocity;

            sim.Step(Forward(), H);                                      // wander off a bit more
            sim.Teleport(new Vector3(5, 0, -50), 90f, Vector3.Zero);
            sim.RestoreState(save);
            Assert.That(sim.Mode, Is.EqualTo(mode));
            Steps(sim, Forward().With(jump: true), 1);
            Steps(sim, Forward(), 60);
            Assert.That(Vector3.Distance(sim.Position, first), Is.LessThan(1e-5f));
            Assert.That(Vector3.Distance(sim.Velocity, firstVel), Is.LessThan(1e-5f));
        }

        [Test]
        public void SaveIsACopyNotALiveView()
        {
            var sim = Standing(Flat(), Vector3.Zero);
            var save = sim.SaveState();
            Steps(sim, Forward(), 60);
            Assert.That(sim.Position.Z, Is.GreaterThan(1f));
            sim.RestoreState(save);
            Assert.That(sim.Position.Z, Is.EqualTo(0f).Within(1e-6f));
            Assert.That(sim.Grounded, Is.True);
        }

        [Test]
        public void DriverResyncDropsInterpolation()
        {
            var sim = Standing(Flat(), Vector3.Zero);
            var d = new MoveDriver(sim);
            var save = sim.SaveState();
            d.Frame(0.5f, Forward());
            sim.RestoreState(save);
            d.Resync();
            Assert.That(Vector3.Distance(d.InterpolatedPosition, Vector3.Zero), Is.LessThan(1e-6f));
        }
    }
}
