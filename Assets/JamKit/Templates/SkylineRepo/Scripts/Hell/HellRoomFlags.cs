namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The rooms' flags for the abilities (the shim the main session wires at the merge). <see cref="CarryingLantern"/> is
    /// true while the Soul Lantern is carried: the player is slower and cannot Shadow Step. <see cref="Carry"/> also sets
    /// <c>HellAbilities.ShadowStepBlocked</c> (the stub until the abilities branch is merged, then the real one).
    /// </summary>
    public static class HellRoomFlags
    {
        public static bool CarryingLantern { get; private set; }

        public static void Carry(bool carrying)
        {
            CarryingLantern = carrying;
            HellAbilities.ShadowStepBlocked = carrying;
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { CarryingLantern = false; }
    }
}
