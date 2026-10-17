#if !HELL_ABILITIES
using System;
using UnityEngine;

namespace JamKit.SkylineRepo
{
    // COMPILE STUBS for the abilities branch's types (hell-abilities 969ff59e owns HellAbilities, HellDarkness and HellBurn),
    // with the same signatures the rooms call. They let the rooms branch build on its own. AT THE MERGE: delete this file
    // (or define HELL_ABILITIES): the real types replace these. The abilities branch's own HellRunEventsStub.cs (under
    // #if !HELL_ROOMS) goes too: this branch's HellRunEvents is the real one.

    /// <summary>STUB of the abilities branch's player abilities (the static members the rooms use).</summary>
    public sealed class HellAbilities : MonoBehaviour
    {
        /// <summary>True while the Shadow Step may not be used (the rooms set it through <see cref="HellRoomFlags"/>).</summary>
        public static bool ShadowStepBlocked;
        /// <summary>Lava does no damage while true (Ashwalker).</summary>
        public static bool LavaImmune => false;
        /// <summary>Is this point lava? The rooms set it (Ashwalker steps onto lava with it).</summary>
        public static Func<Vector3, bool> LavaQuery;
        /// <summary>Call every frame the feet touch lava: true means it bounced you (Lava Hop), so deal no damage.</summary>
        public static bool TryLavaBounce() => false;
    }

    /// <summary>STUB of the abilities branch's darkness registry: the Shadow Step lands only in darkness.</summary>
    public static class HellDarkness
    {
        public static int RegisterZone(Bounds b) => 0;
        public static void UnregisterZone(Bounds b) { }
        public static int RegisterLight(Vector3 pos, float radius) => 0;
    }

    /// <summary>STUB of the abilities branch's burn status on bots.</summary>
    public sealed class HellBurn : MonoBehaviour
    {
        public static void Apply(SkylineBot bot, float seconds = 3f, float dps = 8f) { }
    }
}
#endif
