using UnityEngine;

namespace StackIsland
{
    /// <summary>
    /// Drives the Kenney mini-character clips (legacy Animation): walk / idle on the base layer and a right-arm
    /// melee swing layered on top, so you can swing while walking. If a model has no clips it does nothing.
    /// </summary>
    public class CharAnim : MonoBehaviour
    {
        const string Walk = "walk", Idle = "idle", Sprint = "sprint", Swing = "attack-melee-right";
        Animation _a;
        string _cur;
        public bool HasClips => _a != null && _a[Walk] != null;

        public static CharAnim Attach(GameObject model)
        {
            var c = model.AddComponent<CharAnim>();
            c.Setup();
            return c;
        }

        void Setup()
        {
            _a = GetComponentInChildren<Animation>();
            if (_a == null) return;
            _a.playAutomatically = false;
            _a.cullingType = AnimationCullingType.AlwaysAnimate;
            foreach (var n in new[] { Walk, Idle, Sprint })
                if (_a[n] != null) _a[n].wrapMode = WrapMode.Loop;
            var swing = _a[Swing];
            if (swing != null)
            {
                swing.wrapMode = WrapMode.Once;
                swing.layer = 1;
                var arm = Bone("arm-right");
                if (arm != null) swing.AddMixingTransform(arm, true);
                var torso = Bone("torso");
                if (torso != null) swing.AddMixingTransform(torso, false);
            }
            if (_a[Idle] != null) { _a.Play(Idle); _cur = Idle; }
        }

        public Transform Bone(string name)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        /// <summary>Walk while moving, idle when still. <paramref name="speed"/> scales the walk cycle.</summary>
        public void SetMoving(bool moving, float speed = 1f)
        {
            if (!HasClips) return;
            string want = moving ? Walk : Idle;
            if (want != _cur)
            {
                _a.CrossFade(want, 0.12f);
                _cur = want;
            }
            if (moving) _a[Walk].speed = speed;
        }

        /// <summary>One quick swing of the right arm (layered over walking).</summary>
        public void Attack(float speed = 1.8f)
        {
            if (_a == null) return;
            var s = _a[Swing];
            if (s == null) return;
            s.speed = speed;
            s.time = 0;
            s.weight = 1;
            s.enabled = true;
        }
    }
}
