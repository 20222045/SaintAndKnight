using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SaintKnight.Day01;
using SaintKnight.Day02;
using SaintKnight.Day04;
using SaintKnight.Day09;
using SaintKnight.Day10;
using SaintKnight.SpriteCook;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>
/// The applied drawn sheets appear in the real scenes for the state that owns them.
/// With a graphics device each state is also written to Logs/SpriteCookApply/Screens (HUD included).
/// </summary>
public sealed class SpriteCookAppliedTests
{
    private const string Folder = "Logs/SpriteCookApply/Screens/";
    private static readonly string[] Names = { "cecilia", "adela" };
    private Keyboard keyboard;
    private InputSettings.BackgroundBehavior previousBackground;
#if UNITY_EDITOR
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
#endif
    private SKDay01Player player;
    private SKHealth hp;
    private Rigidbody2D body;
    private BoxCollider2D box;
    private SpriteRenderer view;
    private SKGrabState grab;
    private SKEnemyTrainingSession session;

    private IEnumerator Load(string scene)
    {
        previousBackground = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
        previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        keyboard = InputSystem.AddDevice<Keyboard>();
        yield return SceneManager.LoadSceneAsync(scene);
        yield return null;
        player = Object.FindFirstObjectByType<SKDay01Player>();
        hp = player.GetComponent<SKHealth>();
        body = player.GetComponent<Rigidbody2D>();
        box = player.GetComponent<BoxCollider2D>();
        view = player.GetComponent<SpriteRenderer>();
        grab = player.GetComponent<SKPlayerCombat>().Grab;
        session = Object.FindFirstObjectByType<SKEnemyTrainingSession>();
        // Deaths here are staged; keep the fallen pose on screen instead of retrying.
        typeof(SKEnemyTrainingSession).GetField("autoRetrySeconds", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(session, 0f);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        InputSystem.settings.backgroundBehavior = previousBackground;
#if UNITY_EDITOR
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
#endif
        yield return null;
    }

    [UnityTest]
    public IEnumerator StageShowsDrawnSlimeReactionsAndEnemyDeaths()
    {
        yield return Load("Day09_Stage1");
        SKEnemyAgent monk = null, executioner = null;
        foreach (var enemy in Object.FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None))
        {
            enemy.enabled = false;   // still a valid death cause, but it no longer acts
            if (enemy.DisplayName.EndsWith("MONK", StringComparison.Ordinal)) monk = enemy;
            else executioner = enemy;
        }
        foreach (var trap in Object.FindObjectsByType<SKFloorSpike>(FindObjectsSortMode.None)) trap.enabled = false;
        var pool = GameObject.Find("Trap_Slime_Pool").GetComponent<SKSlimePool>();
        yield return Until(() => player.IsGrounded, 2f, "settled");
        // Same floor, clear of the pool, and far enough in that the camera centres on the player.
        Vector2 home = new Vector2(pool.transform.position.x - 2.5f,
            pool.transform.position.y - (box.bounds.min.y - body.position.y) + .02f);

        for (int character = 0; character < 2; character++)
        {
            string name = Names[character];
            if (player.SelectedCharacter != character) player.SwitchCharacter();
            MoveTo(new Vector2(pool.transform.position.x, pool.transform.position.y - (box.bounds.min.y - body.position.y) + .02f));
            // Slime idle/walk stay on the original atlas pixels (the redrawn sheet made the character pop).
            yield return Until(() => Shows(name + "_slimeidle_f"), .5f, name + " slime idle");
            yield return Shoot(name + "_1_slime_idle");
            Hold(Key.D);
            yield return Until(() => Shows(name + "_slimewalk_f"), .5f, name + " slime walk");
            yield return Shoot(name + "_2_slime_walk");
            Hold();

            // Standing in the pool for its real dwell time gets the player restrained.
            MoveTo(new Vector2(pool.transform.position.x, body.position.y));
            yield return Until(() => grab.IsHeld && grab.Holder == pool.Capture, pool.DwellSeconds + 1f, name + " is caught by the pool");
            yield return null;
            Assert.IsTrue(Shows(name + "_slime_"), "corridor slime sheet while held, was " + view.sprite.name);
            yield return Shoot(name + "_2b_slime_caught");
            yield return new WaitForSeconds(1.5f);
            yield return Shoot(name + "_2c_slime_deeper");
            session.ResetTraining();
            yield return null;

            MoveTo(home);
            yield return Until(() => player.IsGrounded, 2f, "back home");
            hp.Drain(hp.CurrentHp, monk);
            if (SKNativeDefeatLibrary.Load(character, "monk") != null)
            {
                var monkDisplay = monk.GetComponent<SpriteRenderer>();
                yield return Until(() => Shows(name + "_native_monk_f00"), .2f, name + " native monk starts");
                Assert.IsFalse(monkDisplay.enabled);
                yield return Until(() => Shows(name + "_native_monk_f08"), 1.5f, name + " native monk enters its loop");
                yield return Until(() => Shows(name + "_native_monk_f17"), 1.5f, name + " native monk loop moves");
                session.ResetTraining(); yield return null; yield return null;
                Assert.IsTrue(monkDisplay.enabled, "Original monk returns after reset");
                hp.Drain(hp.CurrentHp, monk);
            }
            else if (Resources.Load<Texture2D>("SaintKnightSpriteCook/" + name + "_death_monk") != null)
            {
                // Looping defeat scene with the monk drawn in: the real monk sprite is hidden while it plays.
                var monkDisplay = monk.GetComponent<SpriteRenderer>();
                yield return Until(() => Shows(name + "_death_monk_f00"), .2f, name + " monk scene starts");
                Assert.IsFalse(monkDisplay.enabled, "monk sprite hidden during the scene");
                yield return Until(() => Shows(name + "_death_monk_f03"), 1f, name + " monk scene advances");
                yield return Until(() => Shows(name + "_death_monk_f02"), 1f, name + " monk scene keeps its last two cells moving");
                yield return Shoot(name + "_3_monk_death");
                session.ResetTraining();
                yield return null;
                yield return null;
                Assert.IsTrue(monkDisplay.enabled, "monk sprite back after the reset");
                hp.Drain(hp.CurrentHp, monk);
            }
            else
            {
                yield return Until(() => Shows(name + "_enemy_death_f", 0, 3), .2f, name + " falls to the monk");
                yield return Until(() => Shows(name + "_enemy_death_f", 3, 3), 1.5f, name + " ends slumped");
            }
            // HP 0 HUD: the per-cause illustration when drawn, otherwise the stage 4 picture stays.
            string defeatPath = SKDay01HUD.DefeatPortraitPath(character, "MONK");
            Assert.AreEqual("SaintKnightDay15/DefeatPortraits/" + name + "_hud_monk", defeatPath);
            var hudPortrait = (UnityEngine.UI.Image)typeof(SKDay01HUD).GetField("portrait", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(Object.FindFirstObjectByType<SKDay01HUD>());
            var drawnDefeat = Resources.Load<Texture2D>(defeatPath);
            if (drawnDefeat != null) Assert.AreEqual(drawnDefeat.name, hudPortrait.sprite.name, "drawn defeat illustration is shown");
            else StringAssert.DoesNotContain("_hud_", hudPortrait.sprite.name, "no art yet: the existing picture stays");
            yield return Shoot(name + "_3_monk_death");
            session.ResetTraining();
            yield return null;

            MoveTo(home);
            yield return Until(() => player.IsGrounded, 2f, "back home again");
            hp.Drain(hp.CurrentHp, executioner);
            if (SKNativeDefeatLibrary.Load(character, "executioner") != null)
            {
                yield return Until(() => Shows(name + "_native_executioner_f00"), .2f, name + " native executioner starts");
                Assert.IsFalse(executioner.GetComponent<SpriteRenderer>().enabled);
                yield return Until(() => Shows(name + "_native_executioner_f17"), 2.5f, name + " native executioner settles");
                yield return Until(() => Shows(name + "_native_executioner_f08"), .5f, name + " native executioner keeps moving");
            }
            else if (Resources.Load<Texture2D>("SaintKnightSpriteCook/" + name + "_death_executioner") != null)
            {
                // Lift -> drop -> pinned: plays once, then the last two cells (07, 08) alternate.
                var executionerDisplay = executioner.GetComponent<SpriteRenderer>();
                yield return Until(() => Shows(name + "_death_executioner_f00"), .2f, name + " executioner scene starts");
                Assert.IsFalse(executionerDisplay.enabled, "executioner sprite hidden during the scene");
                yield return Shoot(name + "_4_executioner_blow");
                yield return Until(() => Shows(name + "_death_executioner_f08"), 2.5f, name + " ends pinned");
                yield return Until(() => Shows(name + "_death_executioner_f07"), 1f, name + " pinned cells alternate");
                yield return Until(() => Shows(name + "_death_executioner_f08"), 1f, name + " never back to the start");
            }
            else
            {
                yield return Until(() => Shows(name + "_enemy_death_f", 4, 7), .2f, name + " falls to the executioner");
                yield return new WaitForSeconds(.2f);
                yield return Shoot(name + "_4_executioner_blow");
                yield return Until(() => Shows(name + "_enemy_death_f", 7, 7), 1.5f, name + " ends sprawled");
            }
            yield return Shoot(name + "_5_executioner_death");
            session.ResetTraining();
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator BossRoomShowsLiftKnockdownRoseSlimeAndBossDeath()
    {
        yield return Load("Day09_BossChapel");
        var boss = Object.FindFirstObjectByType<SKBossAgent>();
        var ready = typeof(SKBossAgent).GetField("readyAt", BindingFlags.Instance | BindingFlags.NonPublic);
        var puddle = GameObject.Find("Slime_Puddle").GetComponent<SKSlimePool>();
        ready.SetValue(boss, float.MaxValue);
        yield return new WaitForSeconds(.3f);

        for (int character = 0; character < 2; character++)
        {
            string name = Names[character];
            if (player.SelectedCharacter != character) player.SwitchCharacter();

            // Grab: lifted, struggling, landing.
            PlaceFromBoss(boss, -2f);
            yield return Until(() => player.IsGrounded, 2f, "grounded");
            boss.ForceNext(SKBossAttack.Grab);
            ready.SetValue(boss, 0f);
            yield return Until(() => grab.IsHeld, 4f, name + " held by the boss");
            ready.SetValue(boss, float.MaxValue);
            yield return Until(() => Shows(name + "_native_boss_grab_f") || Shows(name + "_boss_pattern_f", 4, 4), .5f, name + " hangs lifted");
            yield return Shoot(name + "_1_boss_lift");
            yield return Tap(Key.Space);
            yield return Until(() => Shows(name + "_native_boss_grab_f") || Shows(name + "_boss_pattern_f", 5, 7), .3f, name + " struggles in the air");
            yield return Shoot(name + "_2_boss_struggle");
            for (int i = 0; i < 30 && grab.IsHeld && !grab.EscapeReady; i++) yield return Tap(i % 2 == 0 ? Key.J : Key.Space);
            yield return Until(() => !grab.IsHeld, 1f, name + " mashes free");
            yield return Until(() => Shows(name + "_boss_pattern_f", 8, 8), .1f, name + " landing pose, was " + view.sprite.name);
            yield return Shoot(name + "_3_boss_escape");
            yield return new WaitForSeconds(1.2f);
            hp.ResetHealth();

            // Sweep: knocked off the feet.
            PlaceFromBoss(boss, -1.2f);
            yield return Until(() => player.IsGrounded && !hp.IsInvulnerable, 3f, "ready for the sweep");
            float before = hp.CurrentHp;
            boss.ForceNext(SKBossAttack.Sweep);
            ready.SetValue(boss, 0f);
            yield return Until(() => hp.CurrentHp < before, 4f, name + " is hit by the sweep");
            ready.SetValue(boss, float.MaxValue);
            yield return Until(() => Shows(name + "_boss_pattern_f", 0, 3), .1f, name + " knockdown, was " + view.sprite.name);
            yield return new WaitForSeconds(.1f);
            yield return Shoot(name + "_4_boss_knockdown");
            yield return new WaitForSeconds(1.5f);
            hp.ResetHealth();

            // Spit pool: rose slime from above.
            yield return Until(() => player.IsGrounded && !grab.IsHeld, 3f, "ready for the pool");
            puddle.Spawn(new Vector2(body.position.x, box.bounds.min.y), 8f);
            yield return null;
            Assert.IsTrue(puddle.Capture.TryCapture(player), "the timed pool restrains the player");
            yield return null;
            yield return Until(() => Shows(name + "_native_boss_spit_f") || Shows(name + "_bossslime_"), .2f, name + " rose slime sheet, was " + view.sprite.name);
            yield return Shoot(name + "_5_rose_slime_start");
            yield return new WaitForSeconds(2f);
            Assert.IsTrue(Shows(name + "_native_boss_spit_f") || Shows(name + "_bossslime_"), "still the rose sheet while it deepens: " + view.sprite.name);
            yield return Shoot(name + "_6_rose_slime_deep");
            session.ResetTraining();
            ready.SetValue(boss, float.MaxValue);
            yield return null;

            // Boss death.
            PlaceFromBoss(boss, -2f);
            yield return Until(() => player.IsGrounded, 2f, "grounded before the last blow");
            hp.Drain(hp.CurrentHp, boss);
            yield return Until(() => Shows(name + "_native_boss") || Shows(name + "_boss_pattern_f", 9, 10), .2f, name + " falls to the boss");
            if (Shows(name + "_native_boss"))
            {
                var first = view.sprite;
                yield return Until(() => view.sprite != first, .3f, name + " boss defeat keeps moving");
            }
            else yield return Until(() => Shows(name + "_boss_pattern_f", 10, 10), 1f, name + " lies still");
            yield return Shoot(name + "_7_boss_death");
            session.ResetTraining();
            ready.SetValue(boss, float.MaxValue);
            yield return null;
        }
    }

    private bool Shows(string prefix, int from = -1, int to = -1)
    {
        string shown = view.sprite != null ? view.sprite.name : "";
        if (from < 0) return shown.StartsWith(prefix, StringComparison.Ordinal);
        for (int cell = from; cell <= to; cell++)
            if (shown == prefix + cell.ToString("D2")) return true;
        return false;
    }

    private void MoveTo(Vector2 at)
    {
        player.transform.position = new Vector3(at.x, at.y, player.transform.position.z);
        body.position = at;
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }

    private void PlaceFromBoss(SKBossAgent boss, float dx) => MoveTo(new Vector2(boss.transform.position.x + dx,
        boss.transform.position.y - .575f + (body.position.y - box.bounds.min.y) + .02f));

    private void Hold(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
    private IEnumerator Tap(Key key) { Hold(key); yield return null; Hold(); yield return null; }

    private static IEnumerator Until(Func<bool> condition, float seconds, string what)
    {
        float end = Time.time + seconds, realEnd = Time.realtimeSinceStartup + 30f;
        while (!condition() && Time.time < end && Time.realtimeSinceStartup < realEnd) yield return null;
        Assert.That(condition(), Is.True, what + " not reached within " + seconds + "s");
    }

    // Whole screen with the overlay HUD; skipped without a graphics device.
    private static IEnumerator Shoot(string name)
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) yield break;
        // Batch mode never reaches end-of-frame, so the overlay HUD is drawn through the camera for one render.
        var cam = Camera.main;
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        var modes = new RenderMode[canvases.Length];
        for (int i = 0; i < canvases.Length; i++)
        {
            modes[i] = canvases[i].renderMode;
            if (modes[i] != RenderMode.ScreenSpaceOverlay) continue;
            canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
            canvases[i].worldCamera = cam;
            canvases[i].planeDistance = cam.nearClipPlane + .1f;
        }
        Canvas.ForceUpdateCanvases();
        var rt = new RenderTexture(1280, 720, 24);
        var rect = cam.rect;
        cam.rect = new Rect(0f, 0f, 1f, 1f);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var shot = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        shot.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        shot.Apply();
        cam.targetTexture = null;
        cam.rect = rect;
        RenderTexture.active = null;
        for (int i = 0; i < canvases.Length; i++) canvases[i].renderMode = modes[i];
        Directory.CreateDirectory(Folder);
        File.WriteAllBytes(Folder + SceneManager.GetActiveScene().name + "_" + name + ".png", shot.EncodeToPNG());
        Object.Destroy(shot);
        Object.Destroy(rt);
        yield break;
    }
}
