using System.Collections.Generic;

namespace JamKit.SkylineRepo
{
    /// <summary>What the sound pass reads off a Hell enemy (HellSoundBinder, docs/HELL-SOUND-PASS.md): read-only views of the Hell
    /// state in SkylineBotHell.cs, so the sounds follow the same beat, stride and pools the visuals do (and the Rewind restores).</summary>
    public sealed partial class SkylineBot
    {
        /// <summary>The ooze's throb phase (0..1, one beat per unit), or -1 for an enemy that does not throb.</summary>
        public float HellPulsePhase => _pulse != null ? _pulse.Phase : -1f;

        /// <summary>The demon's stride phase (one cycle per two leg plants; it only advances while it strides).</summary>
        public float HellWalkPhase => _walkPhase;

        /// <summary>True while the demon strides in.</summary>
        public bool HellStriding => _striding;

        /// <summary>The ooze's lava pools (the newest last).</summary>
        public IReadOnlyList<HellLavaPool> HellPools => _pools;
    }
}
