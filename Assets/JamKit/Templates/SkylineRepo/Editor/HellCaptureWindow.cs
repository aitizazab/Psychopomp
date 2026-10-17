using System.IO;
using JamKit.Skyline;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JamKit.SkylineRepo.EditorTools
{
    /// <summary>
    /// JamKit/Hell/Capture (docs/HELL-CAPTURE.md): record a play of the Hell scene in the editor, or preview a replay (from a
    /// time, through a shot, with the clean picture) without the Recorder. Each button hands the choice to play mode through
    /// <see cref="HellCaptureBoot.TakePath"/> (used once) and enters play mode in the Hell scene.
    /// </summary>
    public sealed class HellCaptureWindow : EditorWindow
    {
        const string Prefix = "HellCaptureWindow.";

        string _record = "run1", _replay = "run1", _shot = "";
        int _fps = HellCapture.DefaultFps;
        float _from, _to;
        int _voices;
        bool _clean = true;

        static readonly string[] VoiceNames = { "all", "no-voices", "voices-only" };

        [MenuItem("JamKit/Hell/Capture (record, replay, shots)")]
        static void Open() => GetWindow<HellCaptureWindow>("Hell capture");

        void OnEnable()
        {
            _record = EditorPrefs.GetString(Prefix + "record", _record);
            _replay = EditorPrefs.GetString(Prefix + "replay", _replay);
            _shot = EditorPrefs.GetString(Prefix + "shot", _shot);
            _fps = EditorPrefs.GetInt(Prefix + "fps", _fps);
            _from = EditorPrefs.GetFloat(Prefix + "from", _from);
            _to = EditorPrefs.GetFloat(Prefix + "to", _to);
            _voices = EditorPrefs.GetInt(Prefix + "voices", _voices);
            _clean = EditorPrefs.GetBool(Prefix + "clean", _clean);
        }

        void Save()
        {
            EditorPrefs.SetString(Prefix + "record", _record);
            EditorPrefs.SetString(Prefix + "replay", _replay);
            EditorPrefs.SetString(Prefix + "shot", _shot);
            EditorPrefs.SetInt(Prefix + "fps", _fps);
            EditorPrefs.SetFloat(Prefix + "from", _from);
            EditorPrefs.SetFloat(Prefix + "to", _to);
            EditorPrefs.SetInt(Prefix + "voices", _voices);
            EditorPrefs.SetBool(Prefix + "clean", _clean);
        }

        void OnGUI()
        {
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                EditorGUILayout.LabelField("Record a play", EditorStyles.boldLabel);
                _record = EditorGUILayout.TextField("Name or path", _record);
                _fps = EditorGUILayout.IntPopup("Step (fps)", _fps, new[] { "60", "90", "120" }, HellCapture.Rates);
                EditorGUILayout.HelpBox("Saved to " + HellCapture.ReplayPath(_record) + ". The game runs one fixed step a frame: if the editor draws " +
                                        "slower than the step, play is slightly slowed. Record at the rate you will capture at (120).", MessageType.None);
                if (GUILayout.Button("Play and record")) Go(new HellCurrentTake { record = _record, recordFps = _fps, preview = true, hud = "full" });

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Preview a replay", EditorStyles.boldLabel);
                _replay = EditorGUILayout.TextField("Name or path", _replay);
                _from = EditorGUILayout.FloatField("From (s)", _from);
                _to = EditorGUILayout.FloatField("To (s, 0 = end)", _to);
                _shot = EditorGUILayout.TextField("Shot (empty = player)", _shot);
                _voices = EditorGUILayout.Popup("Sound", _voices, VoiceNames);
                _clean = EditorGUILayout.Toggle("Clean picture", _clean);
                string path = HellCapture.ReplayPath(_replay);
                if (!File.Exists(path)) EditorGUILayout.HelpBox("No file " + path, MessageType.Warning);
                if (GUILayout.Button("Play the replay"))
                    Go(new HellCurrentTake { replay = _replay, from = _from, to = _to, shot = _shot, voices = VoiceNames[_voices], hud = _clean ? "clean" : "full", preview = true });
                EditorGUILayout.HelpBox("The jump to the start time runs undrawn and silent. At the end the controls come back to you.", MessageType.None);
            }
        }

        void Go(HellCurrentTake take)
        {
            Save();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(SkylineArenaBuilder.HellScenePath)) SkylineArenaBuilder.BuildHell(SkylineArenaBuilder.HellScenePath);
            Directory.CreateDirectory(Path.GetDirectoryName(HellCaptureBoot.TakePath));
            File.WriteAllText(HellCaptureBoot.TakePath, JsonUtility.ToJson(take));
            if (EditorSceneManager.GetActiveScene().path != SkylineArenaBuilder.HellScenePath)
                EditorSceneManager.OpenScene(SkylineArenaBuilder.HellScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }
    }
}
