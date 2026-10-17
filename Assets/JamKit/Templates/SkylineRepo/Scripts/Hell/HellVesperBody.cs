using JamKit.Skyline;
using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// Vesper's body (docs/HELL-BOSS.md, "Her body"): a stand-in from primitives, about 2.4 m to the crown's tips: a tall
    /// slim figure in a long black-violet gown, pale arms, a veil falling from a thin ember crown, and her heart, an ember
    /// core in a violet cage at her chest: the weak point. It is the hittable (shots go to <see cref="HellVesperFight"/>) and an
    /// aim-assist target. The fight sets the pose (<see cref="SetPose"/>) and the heart's look (<see cref="SetHeart"/>).
    /// <para>To swap in final art: a prefab at <c>Resources/HellModels/vesper</c> with a child named <c>Heart</c> (the weak
    /// point), optionally <c>ArmR</c> (raised for the volley and the cut; where her bolts leave) and an Animator with the int
    /// <c>Pose</c> (this enum) and the trigger <c>Blink</c> (then its clips carry the poses); its origin at her feet.</para>
    /// </summary>
    public sealed class HellVesperBody : MonoBehaviour, ISkylineHittable, ISkylineAimable
    {
        public enum Pose { Stand, Cast, Rewind, Reel, Strike, Kneel }
        public enum HeartLook { Ember, Cold, Blaze, Cracked, Dim }

        public static readonly Color Gown = new Color(0.05f, 0.025f, 0.07f);
        public static readonly Color Skin = new Color(0.72f, 0.66f, 0.7f);
        public static readonly Color VeilColour = new Color(0.12f, 0.06f, 0.16f);
        public static readonly Color CrownHot = new Color(3.2f, 1.2f, 0.25f);
        public static readonly Color HeartEmber = new Color(4.5f, 1.6f, 0.4f);
        public static readonly Color HeartViolet = new Color(1.6f, 0.6f, 3.6f);
        public static readonly Color HeartCold = new Color(0.4f, 2.6f, 3.2f);
        public static readonly Color HeartBlaze = new Color(6f, 3.6f, 1.2f);

        public HellVesperFight fight;
        Transform _rig, _armR, _heart, _cage, _ring;
        Animator _anim;   // the model's (Resources/HellModels/vesper): its clips play the poses; the stand-in tilts instead
        static readonly int PoseId = Animator.StringToHash("Pose");
        static readonly int BlinkId = Animator.StringToHash("Blink");
        Renderer _heartR, _cageR, _ringR;
        Pose _pose = Pose.Stand;
        HeartLook _look = HeartLook.Ember;
        float _poseT;

        public string Id => "vesper";
        public Vector3 Center => _heart != null ? _heart.position : transform.position + Vector3.up * 1.5f;
        public float Radius => 0.6f;
        public bool Alive => fight != null && fight.Fight != null && fight.Fight.Vulnerable && gameObject.activeInHierarchy;
        public Transform Hand => _armR != null ? _armR : transform;

        public static HellVesperBody Create(Transform parent, HellVesperFight fight)
        {
            var go = new GameObject("Vesper");
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<HellVesperBody>();
            b.fight = fight;
            b.Build();
            return b;
        }

        void OnEnable() { if (!SkylineTargets.Aimables.Contains(this)) SkylineTargets.Aimables.Add(this); }
        void OnDisable() { SkylineTargets.Aimables.Remove(this); }

        void Build()
        {
            var model = Resources.Load<GameObject>(HellBodies.ModelFolder + "vesper");
            if (model != null)
            {
                _rig = Instantiate(model, transform, false).transform;
                _rig.name = "Rig";
                _heart = SkylineBotBodies.Find(_rig, "Heart");
                _armR = SkylineBotBodies.Find(_rig, "ArmR");
                _anim = _rig.GetComponentInChildren<Animator>();
                foreach (var r in _rig.GetComponentsInChildren<Renderer>(true))
                    if (r.GetComponent<Collider>() == null && r is MeshRenderer) r.gameObject.AddComponent<BoxCollider>();
            }
            else
            {
                _rig = SkylineBotLook.Empty("Rig", transform, Vector3.zero);
                var gownMat = HellLook.HostileLit(Gown);
                // the gown: a long narrow cone of stacked cylinders, flaring to the floor
                Lit(PrimitiveType.Cylinder, "Gown Hem", new Vector3(0f, 0.35f, 0f), new Vector3(0.95f, 0.35f, 0.95f), gownMat, true);
                Lit(PrimitiveType.Cylinder, "Gown Skirt", new Vector3(0f, 0.95f, 0f), new Vector3(0.62f, 0.3f, 0.62f), gownMat, true);
                Lit(PrimitiveType.Capsule, "Bodice", new Vector3(0f, 1.5f, 0f), new Vector3(0.42f, 0.42f, 0.3f), gownMat, true);
                Lit(PrimitiveType.Sphere, "Shoulders", new Vector3(0f, 1.78f, 0f), new Vector3(0.55f, 0.16f, 0.3f), gownMat, false);
                var skin = HellLook.HostileLit(Skin);
                Lit(PrimitiveType.Capsule, "Neck", new Vector3(0f, 1.92f, 0f), new Vector3(0.1f, 0.1f, 0.1f), skin, false);
                Lit(PrimitiveType.Sphere, "Head", new Vector3(0f, 2.08f, 0f), new Vector3(0.22f, 0.27f, 0.23f), skin, true);
                // the veil: a dark sheet from the crown down her back and sides
                var veil = Lit(PrimitiveType.Cube, "Veil", new Vector3(0f, 1.75f, -0.12f), new Vector3(0.5f, 0.75f, 0.04f), HellLook.HostileLit(VeilColour), false);
                veil.transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);
                // the crown: a thin ember ring with points
                var crownMat = HellLook.HostileGlow(CrownHot);
                Lit(PrimitiveType.Cylinder, "Crown", new Vector3(0f, 2.24f, 0f), new Vector3(0.24f, 0.012f, 0.24f), crownMat, false);
                for (int i = 0; i < 5; i++)
                {
                    float a = i * 72f * Mathf.Deg2Rad;
                    var spike = Lit(PrimitiveType.Cube, "Crown Point", new Vector3(Mathf.Sin(a) * 0.11f, 2.31f, Mathf.Cos(a) * 0.11f), new Vector3(0.025f, 0.14f, 0.025f), crownMat, false);
                    spike.transform.localRotation = Quaternion.Euler(Mathf.Cos(a) * 12f, 0f, -Mathf.Sin(a) * 12f);
                }
                // arms: the left at her side, the right on a pivot at the shoulder (raised to cast and to cut)
                var armL = Lit(PrimitiveType.Capsule, "ArmL", new Vector3(-0.28f, 1.45f, 0f), new Vector3(0.09f, 0.33f, 0.09f), skin, false);
                armL.transform.localRotation = Quaternion.Euler(0f, 0f, -6f);
                _armR = SkylineBotLook.Empty("ArmR", _rig, new Vector3(0.28f, 1.76f, 0f));
                var arm = SkylineBotLook.Visual(PrimitiveType.Capsule, "Arm", _armR, skin);
                arm.transform.localPosition = new Vector3(0f, -0.32f, 0f);
                arm.transform.localScale = new Vector3(0.09f, 0.33f, 0.09f);
                var hand = SkylineBotLook.Visual(PrimitiveType.Sphere, "Hand Flame", _armR, HellLook.HostileGlow(HeartViolet), false);
                hand.transform.localPosition = new Vector3(0f, -0.66f, 0f);
                hand.transform.localScale = Vector3.one * 0.12f;
                // her heart: an ember core in a violet cage at the chest, a clock ring round it (shown during her Rewind)
                _heart = SkylineBotLook.Empty("Heart", _rig, new Vector3(0f, 1.55f, 0.17f));
            }
            if (_heart == null) _heart = SkylineBotLook.Empty("Heart", _rig, new Vector3(0f, 1.55f, 0.17f));
            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "Heart Core";
            core.transform.SetParent(_heart, false);
            core.transform.localScale = Vector3.one * 0.26f;
            _heartR = core.GetComponent<Renderer>();
            _heartR.sharedMaterial = HellLook.Glow(HeartEmber);
            var cage = SkylineBotLook.Visual(PrimitiveType.Sphere, "Heart Cage", _heart, HellLook.Glow(HeartViolet), false);
            cage.transform.localScale = new Vector3(0.36f, 0.36f, 0.12f);
            _cage = cage.transform;
            _cageR = cage.GetComponent<Renderer>();
            var ring = SkylineBotLook.Visual(PrimitiveType.Cylinder, "Clock Ring", _heart, HellLook.Glow(HeartCold), false);
            ring.transform.localScale = new Vector3(0.9f, 0.01f, 0.9f);
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _ring = ring.transform;
            _ringR = ring.GetComponent<Renderer>();
            ring.SetActive(false);

            // hit zones: every collider on her is the body, the heart's core the weak point
            foreach (var c in GetComponentsInChildren<Collider>(true))
            {
                var z = c.gameObject.AddComponent<SkylineHitZone>();
                z.owner = this;
                z.zone = c.gameObject == core ? HitZone.WeakPoint : HitZone.Body;
            }
            SkylineInkRoles.Apply(_rig.gameObject, SkylineInkRoles.Hostile);
            SkylineInkRoles.Apply(_heart.gameObject, SkylineInkRoles.BotWeakPoint);
        }

        GameObject Lit(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material m, bool collider)
        {
            GameObject go;
            if (collider)
            {
                go = GameObject.CreatePrimitive(type);
                go.name = name;
                go.transform.SetParent(_rig, false);
                go.GetComponent<Renderer>().sharedMaterial = m;
            }
            else go = SkylineBotLook.Visual(type, name, _rig, m);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            return go;
        }

        public SkylineHitResult TakeHit(in SkylineHitInfo hit)
        {
            if (fight == null) return default(SkylineHitResult);
            bool heart = hit.zone == HitZone.WeakPoint;
            float mult = hit.gun != null ? hit.gun.weakPointMultiplier : 0f;
            float done = fight.TakeShot(hit.damage, heart, mult, hit.point);
            return new SkylineHitResult
            {
                hit = done > 0f,
                killed = fight.Fight != null && fight.Fight.Defeated,
                damage = new BotDamage { toChassis = done, plateShed = -1, wrecked = fight.Fight != null && fight.Fight.Defeated },
            };
        }

        public void SetVisible(bool on)
        {
            if (_rig != null && _rig.gameObject.activeSelf != on) _rig.gameObject.SetActive(on);
        }

        public void SetPose(Pose p)
        {
            if (_pose == p) return;
            _pose = p;
            _poseT = 0f;
            if (_anim != null && _anim.runtimeAnimatorController != null) _anim.SetInteger(PoseId, (int)p);
        }

        /// <summary>The model's blink clip (a lunge, arms swept back), when it has one; the fight calls it as she dissolves.</summary>
        public void PlayBlink()
        {
            if (_anim != null && _anim.runtimeAnimatorController != null) _anim.SetTrigger(BlinkId);
        }

        public void SetHeart(HeartLook look)
        {
            if (_look == look) return;
            _look = look;
            if (_heartR == null) return;
            Color c = look == HeartLook.Cold ? HeartCold : look == HeartLook.Blaze ? HeartBlaze : look == HeartLook.Dim ? HeartEmber * 0.25f
                : look == HeartLook.Cracked ? new Color(4f, 0.5f, 0.3f) : HeartEmber;
            _heartR.sharedMaterial = HellLook.Glow(c);
            _cageR.sharedMaterial = HellLook.Glow(look == HeartLook.Cold ? HeartCold * 0.6f : HeartViolet);
            _ring.gameObject.SetActive(look == HeartLook.Cold);
        }

        public void Face(Vector3 at)
        {
            var d = at - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(d);
        }

        void Update()
        {
            _poseT += Time.deltaTime;
            float t = Time.time;
            if (_ring != null && _ring.gameObject.activeSelf) _ring.Rotate(0f, -540f * Time.deltaTime, 0f, Space.Self);
            if (_cage != null) _cage.localScale = new Vector3(0.36f, 0.36f, 0.12f) * (1f + 0.06f * Mathf.Sin(t * (_look == HeartLook.Cold ? 18f : 5f)));
            if (_rig == null) return;
            if (_anim != null)
            {
                // the model's clips carry the pose (kneel, reel, cast); the rig only hovers a hand's breadth while she stands
                float hover = _pose == Pose.Stand ? Mathf.Sin(t * 1.3f) * 0.04f : 0f;
                _rig.localPosition = Vector3.Lerp(_rig.localPosition, new Vector3(0f, (_pose == Pose.Stand ? 0.08f : 0f) + hover, 0f), 8f * Time.deltaTime);
                return;
            }
            // the pose: the rig sinks and leans, the right arm swings
            float lean = 0f, sink = 0f, arm = 0f;
            switch (_pose)
            {
                case Pose.Cast: arm = -100f; break;
                case Pose.Rewind: arm = -160f; lean = -6f; sink = Mathf.Sin(t * 3f) * 0.05f; break;
                case Pose.Reel: lean = 14f; sink = 0.15f; arm = 20f; break;
                case Pose.Strike: arm = -150f; lean = -10f; break;   // drawn back for the cut (the fight swings it on the hit)
                case Pose.Kneel: sink = 0.75f; lean = 18f; arm = 15f; break;
                default: sink = Mathf.Sin(t * 1.3f) * 0.04f; break;   // she hovers a hand's breadth
            }
            _rig.localPosition = Vector3.Lerp(_rig.localPosition, new Vector3(0f, 0.08f - sink, 0f), 12f * Time.deltaTime);
            _rig.localRotation = Quaternion.Slerp(_rig.localRotation, Quaternion.Euler(lean, 0f, 0f), 10f * Time.deltaTime);
            if (_armR != null) _armR.localRotation = Quaternion.Slerp(_armR.localRotation, Quaternion.Euler(arm, 0f, 0f), 14f * Time.deltaTime);
        }
    }
}
