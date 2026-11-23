using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SaintKnight.Day01;
using SaintKnight.Day02;
using SaintKnight.Day04;
using SaintKnight.Day09;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

// Rose Window Madonna (Docs/Design/Boss/BOSS_ROSE_MADONNA.md) in Day09_BossChapel.
public sealed class Day09BossTests
{
    private Keyboard keyboard;
    private InputSettings.BackgroundBehavior previousBackground;
#if UNITY_EDITOR
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
#endif
    private SKBossAgent boss;
    private SKDay01Player player;
    private SKHealth hp;
    private Rigidbody2D body;
    private BoxCollider2D box;
    private SKGrabState grab;

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
        yield return SceneManager.LoadSceneAsync("Day09_BossChapel");
        yield return null;
        boss = Object.FindFirstObjectByType<SKBossAgent>();
        player = Object.FindFirstObjectByType<SKDay01Player>();
        hp = player.GetComponent<SKHealth>();
        body = player.GetComponent<Rigidbody2D>();
        box = player.GetComponent<BoxCollider2D>();
        grab = player.GetComponent<SKPlayerCombat>().Grab;
        Hold(int.MaxValue); // nothing happens until a test asks for an attack
        yield return new WaitForSeconds(.3f);
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
    public IEnumerator ClosedShellCutsDamageUntilTwoSwordHitsOpenTheHeart()
    {
        var health = boss.Health;
        float shell = health.DamageScale;                          // closed-shell scale (0.3 for the first boss)
        Assert.Less(shell, 1f);
        Place(-.8f);
        yield return Settle();
        float start = health.CurrentHp;
        yield return Tap(Key.J);                                   // Cecilia 20 x 0.2
        yield return new WaitForSeconds(.5f);
        Assert.AreEqual(start - 20f * shell, health.CurrentHp, .01f, "Closed shell: Cecilia's hit is scaled down");
        Assert.AreEqual(0, boss.ShellBreaks, "The chain does not crack the shell");

        yield return Tap(Key.Tab);
        yield return new WaitForSeconds(.5f);
        yield return Tap(Key.J);                                   // Adela 35 x 0.2, crack 1
        yield return new WaitForSeconds(.8f);
        Assert.AreEqual(1, boss.ShellBreaks);
        Assert.IsFalse(boss.CoreOpen);
        yield return Tap(Key.J);                                   // crack 2 -> open
        yield return Until(() => boss.CoreOpen, 1f, "heart opens");
        Assert.AreEqual(SKBossState.Exposed, boss.State, "Opening staggers the boss");
        Assert.AreEqual(start - 20f * shell - 70f * shell, health.CurrentHp, .01f);
        Hold(int.MaxValue);
        float open = health.CurrentHp;
        yield return new WaitForSeconds(.6f);
        yield return Tap(Key.J);                                   // full 35 while open
        yield return new WaitForSeconds(.5f);
        Assert.AreEqual(open - 35f, health.CurrentHp, .01f, "Open heart takes full damage");
        yield return Until(() => !boss.CoreOpen, 4.5f, "heart closes after 4 s");
        Assert.AreEqual(shell, health.DamageScale, .001f);
    }

    [UnityTest]
    public IEnumerator HighSweepPassesOverACrouchAndHitsAStandingPlayer()
    {
        Place(-2f);
        yield return Settle();
        float start = hp.CurrentHp;
        HoldKeys(Key.S);
        yield return Force(SKBossAttack.Sweep);
        yield return Until(() => boss.State == SKBossState.Recovery, 2f, "sweep ends");
        Assert.AreEqual(start, hp.CurrentHp, "Crouching under the high sweep");
        HoldKeys();
        Hold(int.MaxValue);
        yield return new WaitForSeconds(.3f);
        yield return Force(SKBossAttack.Sweep);
        yield return Until(() => hp.CurrentHp < start, 2f, "standing player is hit");
        Assert.AreEqual(start - 20f, hp.CurrentHp, .01f);
    }

    [UnityTest]
    public IEnumerator ChainCutsTheSweepTellAndSwordCutsTheGrabTell()
    {
        Place(-.9f);
        yield return Settle();
        float start = hp.CurrentHp;
        yield return Force(SKBossAttack.Sweep, waitForStrike: false);
        Assert.AreEqual(SKBossState.Tell, boss.State);
        yield return Tap(Key.J);
        yield return Until(() => boss.Attack == SKBossAttack.None, .6f, "Cecilia's chain cuts the sweep tell");
        Assert.AreEqual(SKBossState.Recovery, boss.State);
        yield return new WaitForSeconds(1f);
        Assert.AreEqual(start, hp.CurrentHp, "A cut sweep never fires later");

        yield return Tap(Key.Tab);
        Hold(int.MaxValue);
        yield return new WaitForSeconds(.8f);
        yield return Force(SKBossAttack.Grab, waitForStrike: false);
        yield return Tap(Key.J);
        yield return Until(() => boss.Attack == SKBossAttack.None, .6f, "Adela's sword cuts the grab tell");
        yield return new WaitForSeconds(.8f);
        Assert.IsFalse(grab.IsHeld, "A cut grab never catches later");
    }

    [UnityTest]
    public IEnumerator TentacleGrabHoldsUntilMashedOutAndAJumpAvoidsIt()
    {
        float previousCaptureDelta = Time.captureDeltaTime;
        Time.captureDeltaTime = 1f / 60f; // Do not let a long frame consume the tell before input is read.
        try
        {
            Place(-2f);
            yield return Settle();
            yield return Force(SKBossAttack.Grab);
            yield return Until(() => grab.IsHeld, 1.5f, "caught by the tentacle");
            Assert.IsTrue(boss.IsHoldingPlayer);
            Assert.AreEqual(SKBossState.Holding, boss.State);
            for (int i = 0; i < 14 && grab.IsHeld && !grab.EscapeReady; i++) yield return Tap(i % 2 == 0 ? Key.Space : Key.J);
            yield return Until(() => !grab.IsHeld, 2f, "mashed free");
            Assert.AreEqual(SKBossState.Recovery, boss.State);
            Assert.IsTrue(hp.IsInvulnerable, "Escape grants a short immunity");

            Hold(int.MaxValue);
            yield return new WaitForSeconds(1.2f);
            Place(-2f);
            yield return Settle();
            Assert.IsFalse(hp.IsInvulnerable, "The second grab is avoided by height, not escape immunity");
            float floorFeet = box.bounds.min.y;
            yield return Force(SKBossAttack.Grab, waitForStrike: false);
            yield return new WaitForSeconds(.3f);
            HoldKeys(Key.Space);
            yield return Until(() => !player.IsGrounded && body.linearVelocity.y > 0f, .2f, "jump starts during the tell");
            Assert.AreEqual(SKBossState.Tell, boss.State);
            yield return Until(() => boss.State != SKBossState.Tell, 1f, "tentacle attempts the grab");
            Assert.AreEqual(SKBossState.Recovery, boss.State);
            Assert.Greater(box.bounds.min.y, floorFeet + .3f, "The feet clear the crawling tentacle at its strike");
            Assert.IsFalse(grab.IsHeld, "Jumping over the crawling tentacle avoids the grab");
            HoldKeys();
            Hold(int.MaxValue);
            yield return Settle();
            Assert.IsFalse(grab.IsHeld, "A missed grab cannot catch the player on landing");
        }
        finally { Time.captureDeltaTime = previousCaptureDelta; }
    }

    [UnityTest]
    public IEnumerator FloorTentacleEruptsUnderAStandingPlayer()
    {
        Place(-3.5f);
        yield return Settle();
        float start = hp.CurrentHp;
        yield return Force(SKBossAttack.Spikes);
        yield return Until(() => hp.CurrentHp < start, 2f, "first floor tentacle hits");
        // Day 15: the capture that follows the 15-damage hit keeps draining HP while unresisted.
        Assert.LessOrEqual(hp.CurrentHp, start - 15f);
        // The boss room tentacles belong to the boss, so they lead to her cutscene.
        Assert.AreEqual("BOSS", SaintKnight.Day15.SKGameOverScreen.KeyFor(hp.LastCause));
    }

    [UnityTest]
    public IEnumerator HalfHealthStartsPhaseTwoOnceSummonsOneMonkAndSlimeSlows()
    {
        var monk = (SKEnemyAgent)typeof(SKBossAgent).GetField("summon", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(boss);
        Assert.IsFalse(monk.gameObject.activeSelf, "No monk before phase 2");
        Place(-5f);
        yield return Settle();
        // Through the closed shell, down to 45% of the boss's health (crosses the 50% phase line).
        boss.Health.TryTakeDamage(boss.Health.MaximumHp * .55f / boss.Health.DamageScale, Vector2.zero);
        yield return Until(() => boss.Phase == 2, .5f, "phase 2");
        Assert.AreEqual(SKBossState.Transition, boss.State);
        Assert.IsTrue(boss.Health.IsInvulnerable, "Transition is invulnerable");
        yield return Until(() => boss.State != SKBossState.Transition, 2.5f, "transition ends");
        Assert.IsTrue(monk.gameObject.activeSelf, "One monk comes in");
        monk.AttacksEnabled = false; // keep the summon from grabbing the player during the slime check

        Hold(int.MaxValue);
        yield return Force(SKBossAttack.Spit);
        yield return Until(() => boss.PuddleActive, 2f, "slime lands");
        Place((boss.PuddleX - boss.transform.position.x));
        yield return new WaitForSeconds(.1f);
        Assert.AreEqual(.5f, player.SpeedMultiplier, .001f, "Standing in slime halves walking speed");
        Place((boss.PuddleX - boss.transform.position.x) + 1.5f);
        yield return new WaitForSeconds(.1f);
        Assert.AreEqual(1f, player.SpeedMultiplier, .001f);

        boss.Health.TryTakeDamage(1f, Vector2.zero);
        yield return new WaitForSeconds(.2f);
        Assert.AreEqual(2, boss.Phase);
        Assert.AreEqual(1, Object.FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None).Length, "Still one monk");
    }

    [UnityTest]
    public IEnumerator FirstBossIsSizedAndTunedDown()
    {
        yield return null;
        Assert.AreEqual(360f, boss.Health.MaximumHp, "First boss HP (was 600)");
        Assert.AreEqual(.3f, boss.Health.DamageScale, .001f, "Closed shell lets 30% through (was 20%)");
        // The imported texture is not CPU-readable; read the same PNG from disk.
        var tex = new Texture2D(2, 2);
        tex.LoadImage(System.IO.File.ReadAllBytes("Assets/SaintKnightDay09/Art/Boss/boss_body_p1_f00_NOT_FINAL.png"));
        var px = tex.GetPixels32();
        int top = -1;
        for (int y = tex.height - 1; y >= 0 && top < 0; y--)
            for (int x = 0; x < tex.width; x++)
                if (px[y * tex.width + x].a > 128) { top = y; break; }
        Assert.That(top + 1, Is.InRange(124, 140), "Standing body about 130 px (~2.2x the heroines, was 240)");
    }

    [UnityTest]
    public IEnumerator DefeatStopsEverythingAndResetRestoresTheFight()
    {
        boss.Health.DamageScale = 1f;
        boss.Health.TryTakeDamage(5000f, Vector2.zero);
        yield return new WaitForSeconds(.3f);
        Assert.AreEqual(SKBossState.Dead, boss.State);
        Assert.AreEqual(SKBossAttack.None, boss.Attack);
        foreach (var hit in boss.GetComponentsInChildren<SKHitbox>()) Assert.IsFalse(hit.IsActive);
        StringAssert.Contains("DEFEATED", GameObject.Find("Boss_Name").GetComponent<TextMesh>().text);

        Object.FindFirstObjectByType<SKEnemyTrainingSession>().ResetTraining();
        yield return null;
        Assert.AreEqual(boss.Health.MaximumHp, boss.Health.CurrentHp);
        Assert.AreEqual(1, boss.Phase);
        Assert.AreEqual(SKBossState.Intro, boss.State);
        Assert.Less(boss.Health.DamageScale, 1f, "Shell closed again");
    }

    /// <summary>Visual check only: writes Logs/Day09Boss_*.png. Run with graphics and -testFilter.</summary>
    [UnityTest, Explicit]
    public IEnumerator CaptureBossFight()
    {
        Place(-3f);
        yield return Settle();
        Shoot("0_idle");
        yield return Force(SKBossAttack.Sweep, waitForStrike: false);
        yield return new WaitForSeconds(.5f);
        Shoot("1_sweep_tell");
        yield return Until(() => boss.State == SKBossState.Strike, 1f, "strike");
        yield return null;
        Shoot("2_sweep_strike");
        Hold(int.MaxValue);
        yield return new WaitForSeconds(1.2f);
        yield return Force(SKBossAttack.Spikes, waitForStrike: false);
        yield return new WaitForSeconds(.75f);
        Shoot("3_floor_tentacles");
        Hold(int.MaxValue);
        yield return new WaitForSeconds(1.5f);
        hp.ResetHealth();
        Place(-2f);
        yield return Settle();
        yield return Force(SKBossAttack.Grab);
        yield return Until(() => grab.IsHeld, 1.5f, "held");
        yield return new WaitForSeconds(.3f);
        Shoot("4_grab");
        for (int i = 0; i < 14 && grab.IsHeld && !grab.EscapeReady; i++) yield return Tap(i % 2 == 0 ? Key.Space : Key.J);
        Hold(int.MaxValue);
        yield return new WaitForSeconds(1f);
        boss.Health.DamageScale = 1f;
        typeof(SKBossAgent).GetField("coreOpenUntil", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(boss, Time.time + 2f);
        yield return new WaitForSeconds(.2f);
        Shoot("5_heart_open");
        boss.Health.TryTakeDamage(boss.Health.MaximumHp * .55f, Vector2.zero);
        yield return new WaitForSeconds(2.3f);
        Hold(int.MaxValue);
        Shoot("6_phase2");
        boss.Health.DamageScale = 1f;
        boss.Health.TryTakeDamage(5000f, Vector2.zero);
        yield return new WaitForSeconds(.5f);
        Shoot("7_defeated");
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
        System.IO.File.WriteAllBytes("Logs/Day09Boss_" + name + ".png", tex.EncodeToPNG());
    }

    // ---------------------------------------------------------------- helpers
    private IEnumerator Force(SKBossAttack attack, bool waitForStrike = true)
    {
        boss.ForceNext(attack);
        Hold(0f);
        yield return Until(() => boss.State == SKBossState.Tell && boss.Attack == attack, 3f, attack + " tell");
        if (waitForStrike)
            yield return Until(() => boss.State != SKBossState.Tell, 3f, attack + " strike");
    }

    // Sets when the boss may next choose an attack (0 = now, MaxValue = never).
    private void Hold(float readyAt) => typeof(SKBossAgent)
        .GetField("readyAt", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(boss, readyAt);

    private IEnumerator Settle()
    {
        yield return Until(() => player.IsGrounded && Mathf.Abs(body.linearVelocity.y) < .01f, 2f, "grounded");
    }

    // Places the player's centre dx units from the boss, feet on the floor.
    private void Place(float dx)
    {
        var at = new Vector2(boss.transform.position.x + dx,
            boss.transform.position.y - .575f + (body.position.y - box.bounds.min.y) + .02f);
        player.transform.position = new Vector3(at.x, at.y, player.transform.position.z);
        body.position = at;
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }

    private IEnumerator Tap(Key key)
    {
        HoldKeys(key);
        yield return null;
        HoldKeys();
        yield return null;
    }

    private void HoldKeys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));

    private static IEnumerator Until(Func<bool> condition, float seconds, string what)
    {
        float end = Time.time + seconds, realEnd = Time.realtimeSinceStartup + 30f;
        yield return null;
        while (!condition() && Time.time < end && Time.realtimeSinceStartup < realEnd) yield return null;
        Assert.That(condition(), Is.True, what + " not reached within " + seconds + "s");
    }
}
