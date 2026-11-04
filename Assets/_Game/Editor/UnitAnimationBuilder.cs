using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using ComponentUtility = UnityEditorInternal.ComponentUtility;

namespace Brawlkin.Editor
{
    // Builds a unit's animation clips + controller from its sprites.
    // Sprites: Art/Sprites/Character Sprites/<Name>/<Name>_<State>_<NN>.png
    // A state without its own frames reuses the Idle frames, so Walk/Attack/Hurt/Death
    // work right away as squash-and-stretch motion and pick up real frames once they exist.
    public static class UnitAnimationBuilder
    {
        const string SpriteRoot = "Assets/_Game/Art/Sprites/Character Sprites";
        const string AnimRoot = "Assets/_Game/Art/Animations";
        const string PrefabRoot = "Assets/_Game/Prefabs";
        const string BodyPath = "Body";
        const string CandyPath = "Candy";

        [MenuItem("Brawlkin/Build Unit Animations/Krakerman")]
        static void BuildKrakerman() => Build("Krakerman");

        public static void Build(string unit)
        {
            var frames = LoadFrames(unit);
            if (!frames.ContainsKey("Idle"))
            {
                Debug.LogError($"[{unit}] no {unit}_Idle_XX sprites in {SpriteRoot}/{unit}");
                return;
            }
            Directory.CreateDirectory($"{AnimRoot}/{unit}");

            var clips = new List<AnimationClip>
            {
                Idle(unit, frames),
                Walk(unit, frames),
                Attack(unit, frames),
                Hurt(unit, frames),
                Death(unit, frames),
            };
            var controller = BuildController(unit, clips);
            SetUpPrefab(unit, frames["Idle"][0], frames.TryGetValue("Projectile", out var candy) ? candy[0] : null, controller);

            AssetDatabase.SaveAssets();
            Debug.Log($"[{unit}] built {clips.Count} clips: {string.Join(", ", clips.Select(c => c.name))}");
        }

        // --- clips -------------------------------------------------------------

        static AnimationClip Idle(string unit, Dictionary<string, Sprite[]> frames)
        {
            var clip = NewClip(unit, "Idle", true);
            SetSprites(clip, frames["Idle"], 6);
            return Save(clip);
        }

        // two little hops per loop, rocking side to side
        static AnimationClip Walk(string unit, Dictionary<string, Sprite[]> frames)
        {
            var clip = NewClip(unit, "Walk", true);
            SetSprites(clip, FramesFor(frames, "Walk"), 8);
            Set(clip, "m_LocalPosition.y", Hop(0f, 0f, 0.125f, 0.05f, 0.25f, 0f, 0.375f, 0.05f, 0.5f, 0f));
            Set(clip, "localEulerAnglesRaw.z", Curve(0f, -4f, 0.25f, 4f, 0.5f, -4f));
            Set(clip, "m_LocalScale.x", Curve(0f, 1.06f, 0.125f, 0.97f, 0.25f, 1.06f, 0.375f, 0.97f, 0.5f, 1.06f));
            Set(clip, "m_LocalScale.y", Curve(0f, 0.94f, 0.125f, 1.04f, 0.25f, 0.94f, 0.375f, 1.04f, 0.5f, 0.94f));
            return Save(clip);
        }

        // wind up (lean back), throw (lunge forward) at 0.25s, settle.
        // With a <Name>_Projectile sprite the candy shows in hand and flies off in an arc.
        static AnimationClip Attack(string unit, Dictionary<string, Sprite[]> frames)
        {
            var clip = NewClip(unit, "Attack", false);
            SetSprites(clip, FramesFor(frames, "Attack"), 8);
            Set(clip, "localEulerAnglesRaw.z", Curve(0f, 0f, 0.15f, 8f, 0.25f, -10f, 0.4f, -3f, 0.5f, 0f));
            Set(clip, "m_LocalPosition.x", Curve(0f, 0f, 0.15f, -0.03f, 0.25f, 0.05f, 0.5f, 0f));
            Set(clip, "m_LocalScale.x", Curve(0f, 1f, 0.15f, 0.95f, 0.25f, 1.08f, 0.5f, 1f));
            Set(clip, "m_LocalScale.y", Curve(0f, 1f, 0.15f, 1.05f, 0.25f, 0.93f, 0.5f, 1f));

            if (frames.ContainsKey("Projectile"))
            {
                // grabbed at 0.1s, released at 0.25s, lands ~2.6 units ahead at 0.65s
                Bind(clip, CandyPath, typeof(SpriteRenderer), "m_Enabled", Steps(0f, 0f, 0.1f, 1f, 0.65f, 0f));
                Bind(clip, CandyPath, typeof(Transform), "m_LocalPosition.x", Lines(0.1f, 0.22f, 0.15f, 0.14f, 0.25f, 0.32f, 0.65f, 2.6f));
                Bind(clip, CandyPath, typeof(Transform), "m_LocalPosition.y", Curve(0.1f, 0.45f, 0.15f, 0.52f, 0.25f, 0.55f, 0.45f, 1.1f, 0.65f, 0.45f));
                Bind(clip, CandyPath, typeof(Transform), "localEulerAnglesRaw.z", Lines(0.25f, 0f, 0.65f, -540f));
            }
            return Save(clip);
        }

        // knocked back with a red flash
        static AnimationClip Hurt(string unit, Dictionary<string, Sprite[]> frames)
        {
            var clip = NewClip(unit, "Hurt", false);
            SetSprites(clip, FramesFor(frames, "Hurt"), 8);
            Set(clip, "m_LocalPosition.x", Curve(0f, 0f, 0.06f, -0.06f, 0.3f, 0f));
            Set(clip, "localEulerAnglesRaw.z", Curve(0f, 0f, 0.06f, 12f, 0.3f, 0f));
            Set(clip, "m_LocalScale.x", Curve(0f, 1f, 0.06f, 0.92f, 0.3f, 1f));
            Set(clip, "m_LocalScale.y", Curve(0f, 1f, 0.06f, 1.06f, 0.3f, 1f));
            SetColor(clip, "m_Color.g", Curve(0f, 1f, 0.06f, 0.4f, 0.3f, 1f));
            SetColor(clip, "m_Color.b", Curve(0f, 1f, 0.06f, 0.4f, 0.3f, 1f));
            return Save(clip);
        }

        // small hop, gets squashed flat, fades out (stays on last frame)
        static AnimationClip Death(string unit, Dictionary<string, Sprite[]> frames)
        {
            var clip = NewClip(unit, "Death", false);
            SetSprites(clip, FramesFor(frames, "Death"), 8);
            Set(clip, "m_LocalPosition.y", Curve(0f, 0f, 0.12f, 0.08f, 0.25f, 0f));
            Set(clip, "localEulerAnglesRaw.z", Curve(0f, 0f, 0.12f, 8f, 0.25f, 0f));
            Set(clip, "m_LocalScale.x", Curve(0f, 1f, 0.12f, 0.95f, 0.25f, 1.2f, 0.6f, 1.35f));
            Set(clip, "m_LocalScale.y", Curve(0f, 1f, 0.12f, 1.06f, 0.25f, 0.6f, 0.6f, 0.3f));
            SetColor(clip, "m_Color.a", Curve(0f, 1f, 0.3f, 1f, 0.6f, 0f));
            return Save(clip);
        }

        // --- helpers -----------------------------------------------------------

        static Dictionary<string, Sprite[]> LoadFrames(string unit)
        {
            return AssetDatabase.FindAssets("t:Sprite", new[] { $"{SpriteRoot}/{unit}" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(s => s != null && s.name.StartsWith(unit + "_"))
                .GroupBy(s => s.name.Split('_')[1])
                .ToDictionary(g => g.Key, g => g.OrderBy(s => s.name).ToArray());
        }

        static Sprite[] FramesFor(Dictionary<string, Sprite[]> frames, string state) =>
            frames.TryGetValue(state, out var own) ? own : frames["Idle"];

        // reuses the existing asset (keeps its GUID) so controllers and prefabs stay linked
        static AnimationClip NewClip(string unit, string state, bool loop)
        {
            string path = $"{AnimRoot}/{unit}/{unit}_{state}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { name = $"{unit}_{state}" };
                AssetDatabase.CreateAsset(clip, path);
            }
            foreach (var b in AnimationUtility.GetCurveBindings(clip)) AnimationUtility.SetEditorCurve(clip, b, null);
            foreach (var b in AnimationUtility.GetObjectReferenceCurveBindings(clip)) AnimationUtility.SetObjectReferenceCurve(clip, b, null);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }

        static AnimationClip Save(AnimationClip clip)
        {
            EditorUtility.SetDirty(clip);
            return clip;
        }

        static void SetSprites(AnimationClip clip, Sprite[] sprites, float fps)
        {
            clip.frameRate = fps;
            var keys = sprites.Select((s, i) => new ObjectReferenceKeyframe { time = i / fps, value = s }).ToList();
            // hold the last frame for its full duration
            keys.Add(new ObjectReferenceKeyframe { time = sprites.Length / fps, value = sprites[sprites.Length - 1] });
            var binding = EditorCurveBinding.PPtrCurve(BodyPath, typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys.ToArray());
        }

        static void Set(AnimationClip clip, string property, AnimationCurve curve) =>
            Bind(clip, BodyPath, typeof(Transform), property, curve);

        static void SetColor(AnimationClip clip, string property, AnimationCurve curve) =>
            Bind(clip, BodyPath, typeof(SpriteRenderer), property, curve);

        static void Bind(AnimationClip clip, string path, System.Type type, string property, AnimationCurve curve) =>
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, type, property), curve);

        static AnimationCurve Lines(params float[] tv) => WithTangents(Curve(tv), AnimationUtility.TangentMode.Linear);

        // on/off values that switch instantly
        static AnimationCurve Steps(params float[] tv) => WithTangents(Curve(tv), AnimationUtility.TangentMode.Constant);

        static AnimationCurve WithTangents(AnimationCurve curve, AnimationUtility.TangentMode mode)
        {
            for (int i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyBroken(curve, i, true);
                AnimationUtility.SetKeyLeftTangentMode(curve, i, mode);
                AnimationUtility.SetKeyRightTangentMode(curve, i, mode);
            }
            return curve;
        }

        // time/value pairs, smooth tangents
        static AnimationCurve Curve(params float[] tv)
        {
            var curve = new AnimationCurve();
            for (int i = 0; i < tv.Length; i += 2) curve.AddKey(tv[i], tv[i + 1]);
            for (int i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            }
            return curve;
        }

        // like Curve, but keys at value 0 get sharp (linear) tangents: a bounce that snaps off the ground
        static AnimationCurve Hop(params float[] tv)
        {
            var curve = Curve(tv);
            for (int i = 0; i < curve.length; i++)
            {
                if (curve[i].value != 0f) continue;
                AnimationUtility.SetKeyBroken(curve, i, true);
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            }
            return curve;
        }

        static AnimatorController BuildController(string unit, List<AnimationClip> clips)
        {
            string path = $"{AnimRoot}/{unit}/{unit}.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var machine = controller.layers[0].stateMachine;

            foreach (var clip in clips)
            {
                string stateName = clip.name.Substring(unit.Length + 1);
                var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == stateName);
                if (state == null) state = machine.AddState(stateName);
                state.motion = clip;
                state.writeDefaultValues = true;
                if (stateName == "Idle") machine.defaultState = state;
            }
            EditorUtility.SetDirty(controller);
            return controller;
        }

        // root keeps the Animator (game code moves/flips the root), the sprite lives on a "Body" child
        // so squash/rotate/hop never touches the unit's real position
        static void SetUpPrefab(string unit, Sprite firstFrame, Sprite projectile, AnimatorController controller)
        {
            string path = $"{PrefabRoot}/{unit}.prefab";
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            var root = exists ? PrefabUtility.LoadPrefabContents(path) : new GameObject(unit);
            try
            {
                var body = root.transform.Find(BodyPath);
                if (body == null)
                {
                    body = new GameObject(BodyPath).transform;
                    body.SetParent(root.transform, false);
                }
                // GetComponent returns a "fake null" in the editor, so no ?? here
                if (!body.TryGetComponent(out SpriteRenderer bodyRenderer))
                    bodyRenderer = body.gameObject.AddComponent<SpriteRenderer>();
                if (root.TryGetComponent(out SpriteRenderer rootRenderer))
                {
                    ComponentUtility.CopyComponent(rootRenderer);
                    ComponentUtility.PasteComponentValues(bodyRenderer);
                    Object.DestroyImmediate(rootRenderer);
                }
                if (bodyRenderer.sprite == null) bodyRenderer.sprite = firstFrame;
                bodyRenderer.spriteSortPoint = SpriteSortPoint.Pivot;

                // hidden candy next to the body; only the Attack clip shows and moves it
                if (projectile != null)
                {
                    var candy = root.transform.Find(CandyPath);
                    if (candy == null)
                    {
                        candy = new GameObject(CandyPath).transform;
                        candy.SetParent(root.transform, false);
                        candy.localPosition = new Vector3(0.22f, 0.45f, 0f);
                    }
                    if (!candy.TryGetComponent(out SpriteRenderer candyRenderer))
                        candyRenderer = candy.gameObject.AddComponent<SpriteRenderer>();
                    candyRenderer.sprite = projectile;
                    candyRenderer.sharedMaterial = bodyRenderer.sharedMaterial;
                    candyRenderer.sortingOrder = bodyRenderer.sortingOrder + 1;
                    candyRenderer.enabled = false;
                }

                if (!root.TryGetComponent(out Animator animator)) animator = root.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                if (exists) PrefabUtility.UnloadPrefabContents(root);
                else Object.DestroyImmediate(root);
            }
        }
    }
}
