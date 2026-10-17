using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The idle life of a Hell model prefab (Resources/HellModels, made by HellModelsBuild): a hover bob on one child (the bot code
    /// owns the prefab root's own position and rotation, so the bob goes one level down) and a slow sway on cloth parts. Purely
    /// cosmetic: no hit zone depends on it, so it ignores the Rewind. Each copy gets its own phase from its instance id.
    /// </summary>
    [AddComponentMenu("JamKit/Hell/Model Motion")]
    public sealed class HellModelMotion : MonoBehaviour
    {
        public Transform hover;
        public float bobHeight = 0.06f;
        public float bobHz = 0.7f;
        public Transform[] sway = new Transform[0];
        public float swayDegrees = 3f;
        public float swayHz = 0.55f;

        Vector3 _hoverBase;
        Quaternion[] _swayBase;
        float _phase;

        void Awake()
        {
            if (hover != null) _hoverBase = hover.localPosition;
            _swayBase = new Quaternion[sway != null ? sway.Length : 0];
            for (int i = 0; i < _swayBase.Length; i++) _swayBase[i] = sway[i] != null ? sway[i].localRotation : Quaternion.identity;
            _phase = (GetInstanceID() & 1023) * 0.0061f;
        }

        void Update()
        {
            float t = Time.time + _phase * 10f;
            if (hover != null) hover.localPosition = _hoverBase + Vector3.up * (bobHeight * Mathf.Sin(t * bobHz * Mathf.PI * 2f));
            for (int i = 0; i < _swayBase.Length; i++)
            {
                var s = sway[i];
                if (s == null) continue;
                float a = swayDegrees * Mathf.Sin(t * swayHz * Mathf.PI * 2f + i * 1.7f);
                float b = swayDegrees * 0.6f * Mathf.Sin(t * swayHz * 1.37f * Mathf.PI * 2f + i * 0.9f);
                s.localRotation = _swayBase[i] * Quaternion.Euler(a, 0f, b);
            }
        }
    }
}
