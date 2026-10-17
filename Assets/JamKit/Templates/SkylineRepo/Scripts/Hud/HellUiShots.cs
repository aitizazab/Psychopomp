using System.Collections;
using System.IO;
using JamKit.Skyline;
using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The UI stills for checking the Hell HUD by eye (docs/HELL-UI.md, "Checking it"): <c>-hell-ui-shots &lt;dir&gt;</c> puts
    /// every UI state on screen in turn, saves a PNG of each into the folder, logs <c>HELL_UI_SHOT name path</c> and quits.
    /// <list type="bullet">
    /// <item>With <c>-hellSkipTitle</c> (the HUD run): play (objective line and room clock, the hourglass, the abilities and
    /// three owned skills, a hint card, a subtitle, a banner), job (+ the briefing card), low health (the cracked vessel, no
    /// Ward), the ability wheel, the boss (Vesper's bar, her hint, a banner), the decision (prompt, ring, the escape line,
    /// with the hint card under it), a beat hold (letterbox and subtitle), the clean picture, the skill pick.</item>
    /// <item>Without it (the screens run): the title, an opening card, a floor card, the credits, the stats, the pause menu.</item>
    /// </list>
    /// Sample words are layout fillers, not the game's lines. Nothing here runs unless the switch is given.
    /// </summary>
    public sealed class HellUiShots : MonoBehaviour
    {
        public const string Switch = "-hell-ui-shots";
        const float Settle = 0.7f;

        string _dir;
        int _count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var dir = SkylineRunArgs.Value(Switch);
            if (string.IsNullOrEmpty(dir) || FindFirstObjectByType<HellUiShots>() != null) return;
            var go = new GameObject("HellUiShots");
            DontDestroyOnLoad(go);
            go.AddComponent<HellUiShots>()._dir = dir;
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(_dir);
            HellCleanPicture.Hud = HellHudLayout.Full;
            HellRunDirector run = null;
            float waited = 0f;
            while ((run == null || SkylineHud.Instance == null || SkylineHud.Instance.Safe == null) && waited < 30f)
            {
                run = FindFirstObjectByType<HellRunDirector>();
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return new WaitForSecondsRealtime(2f);
            if (run == null) { Debug.LogWarning("HELL_UI_SHOT no run director; nothing to shoot"); Quit(); yield break; }

            if (run.Screens != null && run.Screens.Visible) yield return Screens(run);
            else yield return Hud(run);
            Debug.Log("HELL_UI_SHOT done (" + _count + " stills in " + _dir + ")");
            Quit();
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForSecondsRealtime(Settle);
            string path = Path.GetFullPath(Path.Combine(_dir, name + ".png"));
            ScreenCapture.CaptureScreenshot(path);
            _count++;
            Debug.Log("HELL_UI_SHOT " + name + " " + path);
            yield return null;
            yield return null;
        }

        IEnumerator Screens(HellRunDirector run)
        {
            var s = run.Screens;
            yield return Shot("title");
            s.ShowOpening(HellFlowText.Opening);
            yield return Shot("opening-card");
            s.ShowFloorCard(0);
            yield return Shot("floor-card");
            s.ShowCredits();
            yield return new WaitForSecondsRealtime(2.5f);
            yield return Shot("credits");
            s.ShowStats(new HellRunStats(), HellEndings.Kill);
            yield return new WaitForSecondsRealtime(2.5f);
            yield return Shot("stats");
            SkylinePauseMenu.OpenMenu();
            yield return Shot("pause-menu");
        }

        IEnumerator Hud(HellRunDirector run)
        {
            // the tutorial would put its own hints up; switched off for this run only (its setting is saved, the switch is not used)
            foreach (var t in FindObjectsByType<HellTutorial>(FindObjectsSortMode.None)) t.enabled = false;
            var hud = SkylineHud.Instance;
            var player = run.player != null ? run.player : FindFirstObjectByType<SkylinePlayer>();
            var abilities = player != null ? player.GetComponent<HellAbilities>() : null;
            if (abilities != null)
            {
                foreach (var id in new[] { HellSkills.Nightfall, HellSkills.Ambush, HellSkills.DeeperRewind })
                    if (!abilities.Has(id)) abilities.Grant(id);
            }
            HellHud.SetHourglass(0.62f, 37 * 60 + 14);
            var banner = new HellHudLine();
            banner.Build(hud.Safe);
            banner.Banner(HellFlowText.Floor(0).name, 4f);
            HellLinePlayer.Instance.ShowHint("HOLD [E] TO AIM, RELEASE TO STEP");
            hud.Subtitles.Say("BAJA", "A sample subtitle, long enough to show how the line wraps under the layout.", false, 30f);
            yield return Shot("play");

            var jobs = run.GetComponent<HellJobDirector>();
            if (jobs != null) { jobs.ShowBriefingForShot(0); yield return Shot("job-card"); }

            if (player != null && player.Armor != null)
            {
                var a = player.Armor;
                float plating = a.Plating, chassis = a.Chassis;
                a.Restore(0f, a.ChassisMax * 0.25f);
                yield return new WaitForSecondsRealtime(0.4f);
                yield return Shot("low-health");
                a.Restore(plating, chassis);
            }

            var wheel = player != null ? player.GetComponent<HellAbilityWheel>() : null;
            if (wheel != null)
            {
                wheel.ShowForShot(true, 3);
                yield return Shot("wheel");
                wheel.ShowForShot(false);
                yield return new WaitForSecondsRealtime(0.3f);
            }

            var boss = new HellVesperHud();
            boss.Build(hud.Safe, 0.66f, 0.33f);
            boss.Show(true);
            boss.SetHealth(0.7f, false);
            boss.Hint("Her heart is winding time back: SHOOT THE HEART", 6f);
            banner.Banner("VESPER", 4f);
            yield return Shot("boss");
            boss.Show(false);

            var ending = new HellEndingHud();
            ending.Build();
            ending.SetPrompt("HOLD [F] OFFER YOUR HAND     [LEFT MOUSE] END IT", true, 0.45f);
            ending.SetLine("CARRY HER SOUL OUT");
            HellLinePlayer.Instance.ShowHint("LAVA BURNS: STEP OVER IT");
            yield return Shot("decision");
            ending.Clear();
            HellLinePlayer.Instance.HideHint();

            HellLinePlayer.Instance.Letterbox(true);
            hud.Subtitles.Say("VESPER", "A sample line during a held beat, over the letterbox.", false, 6f);
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("beat-hold");
            HellLinePlayer.Instance.Letterbox(false);
            yield return new WaitForSecondsRealtime(0.5f);

            HellCleanPicture.Hud = HellHudLayout.Clean;
            yield return Shot("clean-picture");
            HellCleanPicture.Hud = HellHudLayout.Full;

            var pick = player != null ? player.GetComponent<HellSkillPick>() : null;
            if (pick != null) { pick.Show(1); yield return Shot("skill-pick"); }
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
