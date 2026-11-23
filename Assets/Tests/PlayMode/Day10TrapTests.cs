using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SaintKnight.Day01;
using SaintKnight.Day02;
using SaintKnight.Day04;
using SaintKnight.Day09;
using SaintKnight.Day10;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

// Day 10 traps in the Day 9 cloister strip (stage1_layout.json trapDay10 = 1120..1300 px, floor y310).
public sealed class Day10TrapTests
{
    private const float Ppu = 48f, Left = -6.6666665f;
    private static float X(float px) => Left + px / Ppu;
    private static readonly float Floor = (180f - 310f) / Ppu;

    private Keyboard keyboard;
    private InputSettings.BackgroundBehavior previousBackground;
#if UNITY_EDITOR
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
#endif
    private SKDay01Player player;
    private SKHealth hp;
    private Rigidbody2D body;
    private BoxCollider2D box;
    private SKSlimePool pool;
    private SKFloorSpike[] spikes;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        previousBackground = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
        previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        keyboard = InputSystem.AddDevice<Keyboard>();
        yield return SceneManager.LoadSceneAsync("Day09_Stage1");
        yield return null;
        player = Object.FindFirstObjectByType<SKDay01Player>();
        hp = player.GetComponent<SKHealth>();
        body = player.GetComponent<Rigidbody2D>();
        box = player.GetComponent<BoxCollider2D>();
        pool = GameObject.Find("Trap_Slime_Pool").GetComponent<SKSlimePool>();
        spikes = Object.FindObjectsByType<SKFloorSpike>(FindObjectsSortMode.None).OrderBy(s => s.name).ToArray();
        foreach (var e in Object.FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None)) e.gameObject.SetActive(false);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        InputSystem.RemoveDevice(keyboard);
        InputSystem.settings.backgroundBehavior = previousBackground;
#if UNITY_EDITOR
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
#endif
        yield return null;
    }

    [UnityTest]
    public IEnumerator TrapsSitInTheCloisterStrip()
    {
        yield return null;
        Assert.AreEqual(2, spikes.Length, "two floor-tentacle cracks");
        Assert.AreEqual(X(1210f), pool.transform.position.x, .01f, "slime centred on the strip");
        Assert.AreEqual(Floor, pool.transform.position.y, .01f, "slime on the floor line");
        Assert.That(pool.transform.position.x - pool.HalfWidth, Is.GreaterThanOrEqualTo(X(1120f) - .01f));
        Assert.That(pool.transform.position.x + pool.HalfWidth, Is.LessThanOrEqualTo(X(1300f) + .01f));
        foreach (var s in spikes)
        {
            Assert.That(s.transform.position.x, Is.InRange(X(1120f), X(1300f)), s.name);
            Assert.AreEqual(Floor, s.transform.position.y, .01f, s.name);
        }
        Assert.AreEqual(1.2f, Mathf.Abs(spikes[0].StrikeAt - spikes[1].StrikeAt), .05f, "cracks erupt in turn");
    }

    [UnityTest]
    public IEnumerator SlimeHalvesWalkingSpeedOnlyOnTheFloorOverIt()
    {
        Place(1210f + 30f);                                       // between the cracks, inside the pool
        yield return new WaitForSeconds(.15f);
        Assert.AreEqual(.5f, player.SpeedMultiplier, .001f, "walking in slime");

        Hold(Key.Space);                                          // airborne: no slime under the feet
        yield return Until(() => !player.IsGrounded, .5f, "jump");
        yield return new WaitForFixedUpdate();
        Assert.AreEqual(1f, player.SpeedMultiplier, .001f, "jumping is not slowed");
        Hold();
        yield return Until(() => player.IsGrounded, 2f, "landing");

        Place(900f);
        yield return new WaitForSeconds(.15f);
        Assert.AreEqual(1f, player.SpeedMultiplier, .001f, "outside the pool");
    }

    [UnityTest]
    public IEnumerator CrackSwellsWithoutHarmThenTheTentacleHitsOnce()
    {
        var spike = spikes[0];
        yield return Until(() => spike.IsTelling && spike.StrikeAt - Time.time > .4f, 4f, "crack swells");
        Place((spike.transform.position.x - Left) * Ppu);
        float start = hp.CurrentHp;
        yield return Until(() => Time.time >= spike.StrikeAt - .05f, 1f, "just before eruption");
        Assert.AreEqual(start, hp.CurrentHp, "the swelling crack never hurts");
        Assert.IsTrue(spike.GetComponent<SpriteRenderer>().enabled, "the crack is drawn while it swells");
        yield return Until(() => hp.CurrentHp < start, .5f, "tentacle erupts");
        Assert.AreEqual(0f, spike.GetComponentInChildren<SKHitbox>().GetComponent<SpriteRenderer>().color.a, .001f,
            "no red training box over the tentacle art");
        // Day 15: the capture that follows the hit drains HP while unresisted (25 per second).
        Assert.AreEqual(start - spike.Damage, hp.CurrentHp, 1.5f);
        yield return new WaitForSeconds(spike.ActiveSeconds + .1f);
        Assert.Greater(hp.CurrentHp, start - spike.Damage * 2f, "one hit per eruption");
    }

    [UnityTest]
    public IEnumerator SteppingOutOfTheSwellingCrackAvoidsItEvenInSlime()
    {
        // A full jump peaks ~1.13 units, under the 1.4-unit tentacle: the answer is to leave the crack.
        var spike = spikes[0];
        Place((spike.transform.position.x - Left) * Ppu);
        yield return Until(() => spike.IsTelling && spike.StrikeAt - Time.time > .5f, 4f, "crack swells");
        float start = hp.CurrentHp;
        Hold(Key.D);                                              // slowed walk toward the other crack
        yield return new WaitForSeconds(.4f);
        Hold();
        Assert.AreEqual(.5f, player.SpeedMultiplier, .001f, "still in the slime");
        yield return Until(() => Time.time >= spike.StrikeAt + spike.ActiveSeconds, 1f, "eruption over");
        Assert.AreEqual(start, hp.CurrentHp, "stepped clear of the crack in time");
    }

    [UnityTest]
    public IEnumerator ResetRestartsTheTrapRhythmWithNothingLeftActive()
    {
        var spike = spikes[0];
        yield return Until(() => Time.time >= spike.StrikeAt + .05f, 3f, "an eruption");
        Assert.IsTrue(spike.GetComponentInChildren<SKHitbox>().IsActive);
        Object.FindFirstObjectByType<SKEnemyTrainingSession>().ResetTraining();
        yield return null;
        Assert.IsFalse(spike.GetComponentInChildren<SKHitbox>().IsActive, "reset cancels the live tentacle");
        Assert.AreEqual(Time.time + spike.TellSeconds, spike.StrikeAt, .05f, "first crack restarts its cycle");
        Assert.AreEqual(1f, player.SpeedMultiplier, .001f, "reset puts the player back at the start, out of the slime");
    }

    [UnityTest]
    public IEnumerator StageAndBossUseTheAnimatedTentacleAndSlime()
    {
        yield return CheckTrapArt(spikes[0], pool);

        yield return SceneManager.LoadSceneAsync("Day09_BossChapel");
        yield return null;
        Object.FindFirstObjectByType<SKBossAgent>().enabled = false;
        var bossPool = Object.FindFirstObjectByType<SKSlimePool>();
        bossPool.Spawn(bossPool.transform.position, 2f);
        yield return CheckTrapArt(Object.FindFirstObjectByType<SKFloorSpike>(), bossPool);
    }

    private static IEnumerator CheckTrapArt(SKFloorSpike spike, SKSlimePool slime)
    {
        var spikeView = spike.GetComponent<SpriteRenderer>();
        var slimeView = slime.GetComponent<SpriteRenderer>();
        spike.Arm(spike.transform.position, 0f);
        yield return Until(() => Shows(spikeView, "floor_tentacle_tell_"), 1f, "tentacle tell art");
        Assert.That(Shows(slimeView, "slime_pool_f"), Is.True, "new slime art");
        Sprite firstSlime = slimeView.sprite;
        yield return Until(() => Shows(spikeView, "floor_tentacle_rise_"), 1.5f, "tentacle rise art");
        yield return Until(() => Shows(spikeView, "floor_tentacle_retract_"), 1f, "tentacle retract art");
        yield return Until(() => Shows(slimeView, "slime_pool_f") && slimeView.sprite != firstSlime, .7f, "slime advances frames");
    }

    private static bool Shows(SpriteRenderer view, string prefix) =>
        view.enabled && view.sprite != null && view.sprite.name.StartsWith(prefix, StringComparison.Ordinal);

    [UnityTest]
    public IEnumerator FleshWallGrabsAPlayerWhoWalksStraightThroughAndMashingFreesThem()
    {
        var wall = Object.FindFirstObjectByType<SKFleshWall>();
        var grab = player.GetComponent<SKPlayerCombat>().Grab;
        var wallView = wall.GetComponent<SpriteRenderer>();
        var playerView = player.GetComponent<SpriteRenderer>();
        float wallPx = (wall.transform.position.x - Left) * Ppu;
        Assert.AreEqual(Floor, wall.transform.position.y, .01f, "the wall stands on the floor line");
        Place(wallPx - wall.Reach * Ppu - 15f);
        yield return new WaitForSeconds(.2f);
        Hold(Key.D);
        yield return Until(() => wall.State == SKFleshWallState.Tell, 1f, "purple tell");
        yield return null;                                        // the warning is drawn in LateUpdate
        Assert.IsTrue(GameObject.Find("Flesh_Wall_Warning").GetComponent<SpriteRenderer>().enabled, "purple warning drawn");
        Assert.IsFalse(grab.IsHeld, "the tell itself never grabs");
        yield return Until(() => grab.IsHeld, 1f, "walking straight through is caught");
        Hold();
        Assert.AreEqual(SKFleshWallState.Holding, wall.State);
        Assert.IsTrue(wall.IsHoldingPlayer);
        bool dedicatedWall = SKTrapAnimationLibrary.Load(0, SKTrapMotionKind.Wall) != null;
        yield return Until(() => Shows(wallView, "cecilia_native_fleshwall_") || Shows(wallView, dedicatedWall ? "cecilia_wall_swallow_" : "flesh_wall_cecilia_struggle_"),
            .8f, "Cecilia wall composite");
        Assert.IsTrue(playerView.forceRenderingOff, "the composite replaces the solo character");
        float held = hp.CurrentHp;
        for (int i = 0; i < 14 && grab.IsHeld && !grab.EscapeReady; i++) yield return Tap(i % 2 == 0 ? Key.Space : Key.J);
        yield return Until(() => !grab.IsHeld, 1f, "mashed free");
        Assert.AreEqual(SKFleshWallState.Rest, wall.State, "the wall rests after letting go");
        Assert.IsTrue(hp.IsInvulnerable, "escape grants a short immunity");
        Assert.Greater(hp.CurrentHp, held - 9f, "a quick escape costs at most a drain tick or two");
        yield return Until(() => Shows(wallView, dedicatedWall ? "cecilia_wall_escape_" : "flesh_wall_cecilia_escape_"),
            .5f, "Cecilia escape composite");
        yield return Until(() => !playerView.forceRenderingOff, .5f, "solo sprite restored after escape");
    }

    [UnityTest]
    public IEnumerator BaitingTheTellThenPassingWhileItRestsIsSafe()
    {
        var wall = Object.FindFirstObjectByType<SKFleshWall>();
        var grab = player.GetComponent<SKPlayerCombat>().Grab;
        float wallPx = (wall.transform.position.x - Left) * Ppu;
        Place(wallPx - wall.Reach * Ppu + 10f);                  // just inside the reach
        yield return Until(() => wall.State == SKFleshWallState.Tell, 1f, "tell");
        Place(wallPx - wall.Reach * Ppu - 40f);                  // step back out
        yield return Until(() => wall.State == SKFleshWallState.Rest, 1f, "whiff");
        Assert.IsFalse(grab.IsHeld);
        Hold(Key.D);
        yield return Until(() => (player.transform.position.x - Left) * Ppu > wallPx + wall.Reach * Ppu + 10f, 2f,
            "walked past");
        Hold();
        Assert.IsFalse(grab.IsHeld, "passing while the wall rests is safe");
    }

    [UnityTest]
    public IEnumerator ResetLetsGoOfAHeldPlayer()
    {
        var wall = Object.FindFirstObjectByType<SKFleshWall>();
        var grab = player.GetComponent<SKPlayerCombat>().Grab;
        var wallView = wall.GetComponent<SpriteRenderer>();
        var playerView = player.GetComponent<SpriteRenderer>();
        player.SwitchCharacter();
        Place((wall.transform.position.x - Left) * Ppu + 20f);
        yield return Until(() => grab.IsHeld, 1.5f, "held");
        bool dedicatedWall = SKTrapAnimationLibrary.Load(1, SKTrapMotionKind.Wall) != null;
        yield return Until(() => Shows(wallView, "adela_native_fleshwall_") || (dedicatedWall
                ? Shows(wallView, "adela_wall_swallow_") || Shows(wallView, "adela_wall_resist_")
                : Shows(wallView, "flesh_wall_adela_swallow_") || Shows(wallView, "flesh_wall_adela_struggle_")), .8f,
            "Adela wall composite");
        Assert.IsTrue(wallView.flipX, "composite mirrors to match the right-side approach");
        Assert.IsTrue(playerView.forceRenderingOff);
        Object.FindFirstObjectByType<SKEnemyTrainingSession>().ResetTraining();
        yield return null;
        Assert.IsFalse(grab.IsHeld);
        Assert.AreEqual(SKFleshWallState.Idle, wall.State);
        Assert.IsFalse(playerView.forceRenderingOff, "reset restores the solo sprite");
        Assert.IsFalse(wallView.flipX);
        Assert.IsTrue(Shows(wallView, "flesh_wall_idle_"));
    }

    /// <summary>Visual check only: writes Logs/Day10Capture_*.png. Needs graphics (no -nographics) and -testFilter.</summary>
    [UnityTest, Explicit("Needs a graphics device; writes screenshots")]
    public IEnumerator CaptureTrapScenes()
    {
        var grab = player.GetComponent<SKPlayerCombat>().Grab;
        var spike = spikes[0];
        Place(1210f);                                             // in the slime, between the two cracks
        yield return Until(() => spike.IsTelling && spike.StrikeAt - Time.time < .35f, 4f, "crack swelling");
        Shoot("0_slime_crack_tell");
        yield return Until(() => Time.time >= spike.StrikeAt + .1f, 1f, "eruption");
        Shoot("1_tentacle_rise");
        yield return Until(() => Time.time >= spike.StrikeAt + spike.ActiveSeconds + .1f, 1f, "retract");
        Shoot("2_tentacle_retract");

        var wall = Object.FindFirstObjectByType<SKFleshWall>();
        float wallPx = (wall.transform.position.x - Left) * Ppu;
        hp.ResetHealth();
        Place(wallPx - wall.Reach * Ppu - 40f);
        yield return new WaitForSeconds(.4f);
        Shoot("3_wall_idle");
        Hold(Key.D);
        yield return Until(() => wall.State == SKFleshWallState.Tell, 1f, "wall tell");
        yield return null;
        Shoot("4_wall_tell");
        yield return Until(() => grab.IsHeld, 1f, "Cecilia held");
        Hold();
        yield return new WaitForSeconds(.1f);
        Shoot("5_wall_swallow_cecilia");
        yield return new WaitForSeconds(.6f);
        Shoot("6_wall_struggle_cecilia");
        for (int i = 0; i < 14 && grab.IsHeld && !grab.EscapeReady; i++) yield return Tap(i % 2 == 0 ? Key.Space : Key.J);
        yield return Until(() => !grab.IsHeld, 1f, "escape");
        yield return null;
        Shoot("7_wall_escape_cecilia");

        Object.FindFirstObjectByType<SKEnemyTrainingSession>().ResetTraining();
        yield return new WaitForSeconds(.3f);
        yield return Tap(Key.Tab);
        Place(wallPx + wall.Reach * Ppu + 40f);                   // Adela from the right (mirrored composite)
        yield return new WaitForSeconds(.3f);
        Hold(Key.A);
        yield return Until(() => grab.IsHeld, 2f, "Adela held");
        Hold();
        yield return new WaitForSeconds(.7f);
        Shoot("8_wall_struggle_adela");

        yield return SceneManager.LoadSceneAsync("Day09_BossChapel");
        yield return null;
        var boss = Object.FindFirstObjectByType<SKBossAgent>();
        boss.enabled = false;
        var bossSpikes = Object.FindObjectsByType<SKFloorSpike>(FindObjectsSortMode.None).OrderBy(s => s.name).ToArray();
        var bossPool = Object.FindFirstObjectByType<SKSlimePool>();
        float x = Object.FindFirstObjectByType<SKDay01Player>().transform.position.x;
        bossPool.Spawn(new Vector2(x + 1.2f, boss.transform.position.y - .575f), 3f);
        for (int i = 0; i < bossSpikes.Length; i++)
            bossSpikes[i].Arm(new Vector2(x + 2.4f + i * .6f, boss.transform.position.y - .575f), i * .3f);
        yield return new WaitForSeconds(.5f);
        Shoot("9_boss_tells_and_slime");
        yield return new WaitForSeconds(.6f);
        Shoot("10_boss_rise_and_slime");
    }

    private static void Shoot(string name)
    {
        var cam = Camera.main;
        var rt = new RenderTexture(1280, 720, 24);
        var rect = cam.rect;
        cam.rect = new Rect(0f, 0f, 1f, 1f);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        tex.Apply();
        cam.targetTexture = null;
        cam.rect = rect;
        RenderTexture.active = null;
        System.IO.File.WriteAllBytes("Logs/Day10Capture_" + name + ".png", tex.EncodeToPNG());
    }

    private IEnumerator Tap(Key key)
    {
        Hold(key);
        yield return null;
        Hold();
        yield return null;
    }

    private void Place(float px)
    {
        var at = new Vector2(X(px), Floor - (box.bounds.min.y - body.position.y) + .02f);
        player.transform.position = new Vector3(at.x, at.y, player.transform.position.z);
        body.position = at;
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }

    private void Hold(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));

    private static IEnumerator Until(Func<bool> condition, float seconds, string what)
    {
        float end = Time.realtimeSinceStartup + seconds;
        yield return null;
        while (!condition() && Time.realtimeSinceStartup < end) yield return null;
        Assert.That(condition(), Is.True, what + " not reached within " + seconds + "s");
    }
}
