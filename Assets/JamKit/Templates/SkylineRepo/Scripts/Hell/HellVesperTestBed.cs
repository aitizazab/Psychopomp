using UnityEngine;
using UnityEngine.InputSystem;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// A drop-in check of Vesper's fight (docs/HELL-BOSS.md, "See it in Unity"): add it to any scene with a
    /// <see cref="SkylinePlayer"/> (Add Component, "JamKit/Hell/Vesper Test Bed") and press Play. It builds her arena at its
    /// world slot, puts the player at the entry and begins the fight. Keys: <b>F6</b> deals 250 damage to her (to see the
    /// phases quickly), <b>F7</b> the real Rewind for free, <b>F8</b> begins the fight again. Logs <c>HELL_VESPER ...</c>.
    /// </summary>
    [AddComponentMenu("JamKit/Hell/Vesper Test Bed")]
    public sealed class HellVesperTestBed : MonoBehaviour
    {
        public bool debugKeys = true;
        HellVesperFight _fight;
        SkylinePlayer _player;

        void Start()
        {
            _player = Object.FindAnyObjectByType<SkylinePlayer>();
            if (_player == null)
            {
                Debug.LogWarning("HELL_VESPER test bed: no SkylinePlayer in the scene");
                enabled = false;
                return;
            }
            if (_player.GetComponent<HellRewind>() == null) _player.gameObject.AddComponent<HellRewind>();
            _fight = new GameObject("Vesper Fight (test bed)").AddComponent<HellVesperFight>();
            _fight.player = _player;
            Begin();
        }

        void Begin() => _fight.Begin(() => Debug.Log("HELL_VESPER test bed: onDefeated called (the ending would start here)"));

        void Update()
        {
            var kb = Keyboard.current;
            if (!debugKeys || kb == null || _fight == null) return;
            if (kb.f6Key.wasPressedThisFrame && _fight.Fight != null)
            {
                float done = _fight.TakeShot(250f, false, 1f, _fight.VesperPosition);
                Debug.Log("HELL_VESPER test bed: F6 dealt " + done + "; health " + _fight.Fight.S.health + ", phase " + _fight.Fight.S.phase + ", " + _fight.Fight.S.action);
            }
            if (kb.f7Key.wasPressedThisFrame)
            {
                var rw = _player.GetComponent<HellRewind>();
                Debug.Log("HELL_VESPER test bed: rewind " + (rw != null && rw.TryRewind(free: true) ? "used" : "not ready"));
            }
            if (kb.f8Key.wasPressedThisFrame) Begin();
        }
    }
}
