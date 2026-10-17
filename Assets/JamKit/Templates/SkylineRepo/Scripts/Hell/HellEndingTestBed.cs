using UnityEngine;
using UnityEngine.InputSystem;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// A drop-in check of the ending (docs/HELL-ENDING.md, "Check it in Unity"): add it to a scene with a
    /// <see cref="SkylinePlayer"/> (Add Component, "JamKit/Hell/Ending Test Bed"; the Hell scene is best, it has the escape
    /// room) and press Play. <b>F9</b> begins the ending where you stand (Vesper kneels 6 m ahead: the fight's body when a fight
    /// ran, else a stand-in). <b>F10</b> chooses kill, <b>F11</b> chooses spare (both skip the walk). The ending's id is logged
    /// (<c>HELL_ENDING done: ...</c>); with the run director in the scene it rolls the credits too.
    /// </summary>
    [AddComponentMenu("JamKit/Hell/Ending Test Bed")]
    public sealed class HellEndingTestBed : MonoBehaviour
    {
        public bool debugKeys = true;

        void Start()
        {
            var p = Object.FindAnyObjectByType<SkylinePlayer>();
            if (p == null)
            {
                Debug.LogWarning("HELL_ENDING test bed: no SkylinePlayer in the scene");
                enabled = false;
                return;
            }
            if (p.GetComponent<HellRewind>() == null) p.gameObject.AddComponent<HellRewind>();
            HellRunEvents.EndingReached += id => Debug.Log("HELL_ENDING test bed: RaiseEnding(" + id + ")");
            Debug.Log("HELL_ENDING test bed: F9 begin, F10 kill, F11 spare");
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (!debugKeys || kb == null) return;
            if (kb.f9Key.wasPressedThisFrame) HellEndingDirector.Request();
            var d = HellEndingDirector.Instance;
            if (d == null || d.Stage != HellEndingDirector.Phase.Decision) return;
            if (kb.f10Key.wasPressedThisFrame) d.ForceChoice(true);
            if (kb.f11Key.wasPressedThisFrame) d.ForceChoice(false);
        }
    }
}
