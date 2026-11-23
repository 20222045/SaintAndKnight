using System;
using System.Collections;
using NUnit.Framework;
using SaintKnight.Day01;
using SaintKnight.Day02;
using SaintKnight.Day04;
using SaintKnight.Day10;
using SaintKnight.Day15;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>Day 15: HP regeneration, restraint as HP loss, and the per-cause game over.</summary>
public sealed class Day15HealthTests
{
    private Keyboard keyboard;
    private InputSettings.BackgroundBehavior previousBackground;
#if UNITY_EDITOR
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
#endif
    private float previousCaptureDelta;
    private SKDay01Player player;
    private SKHealth health;
    private SKGrabState grab;
    private SKEnemyTrainingSession session;
    private SKGameOverScreen gameOver;
    private SKSlimePool pool;
    private SKFloorSpike spike;
    private SKFleshWall wall;
    private SKEnemyAgent[] enemies;
    private Rigidbody2D body;
    private BoxCollider2D box;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        SKHitStop.CancelActive();
        Time.timeScale = 1f;
        previousCaptureDelta = Time.captureDeltaTime;
        Time.captureDeltaTime = 1f / 60f;
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
        health = player.GetComponent<SKHealth>();
        grab = player.GetComponent<SKPlayerCombat>().Grab;
        body = player.GetComponent<Rigidbody2D>();
        box = player.GetComponent<BoxCollider2D>();
        session = Object.FindFirstObjectByType<SKEnemyTrainingSession>();
        gameOver = session.GetComponent<SKGameOverScreen>();
        pool = GameObject.Find("Trap_Slime_Pool").GetComponent<SKSlimePool>();
        spike = Object.FindFirstObjectByType<SKFloorSpike>();
        wall = Object.FindFirstObjectByType<SKFleshWall>();
        enemies = Object.FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None);
        // Disabled, not deactivated: the cause lookup below still finds them.
        foreach (var enemy in enemies) enemy.enabled = false;
        foreach (var s in Object.FindObjectsByType<SKFloorSpike>(FindObjectsSortMode.None)) s.enabled = false;
        wall.enabled = false;
        yield return Until(() => player.IsGrounded && body.linearVelocity.magnitude < .05f, 2f, "Spawn settles");
        Assert.IsNotNull(gameOver, "The session adds the game-over screen by itself");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        SKHitStop.CancelActive();
        Time.timeScale = 1f;
        Time.captureDeltaTime = previousCaptureDelta;
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        InputSystem.settings.backgroundBehavior = previousBackground;
#if UNITY_EDITOR
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
#endif
        yield return null;
    }

    private IEnumerator Tap(Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
    }

    private static IEnumerator Until(Func<bool> condition, float timeout, string message)
    {
        float end = Time.time + timeout, realEnd = Time.realtimeSinceStartup + 30f;
        while (!condition() && Time.time < end && Time.realtimeSinceStartup < realEnd) yield return null;
        Assert.IsTrue(condition(), message);
    }

    private static IEnumerator WaitGame(float seconds)
    {
        float at = Time.time + seconds;
        yield return Until(() => Time.time >= at, seconds + .25f, "Scaled game time advances");
    }

    private IEnumerator CaptureInSlime()
    {
        Vector2 floor = pool.transform.position;
        Vector2 at = new Vector2(floor.x, floor.y - (box.bounds.min.y - body.position.y) + .02f);
        player.transform.position = new Vector3(at.x, at.y, player.transform.position.z);
        body.position = at;
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        yield return Until(() => player.IsGrounded, .5f, "Grounded over slime");
        Assert.IsTrue(pool.Capture.TryCapture(player));
    }

    [UnityTest]
    public IEnumerator HpRegeneratesAfterThreeUnhurtSecondsAndNewDamageRestartsTheWait()
    {
        Assert.IsTrue(health.TryTakeDamage(40f, body.position));
        float hurtAt = Time.time;
        yield return Until(() => Time.time >= hurtAt + 2.8f, 3.2f, "Just before the regeneration delay");
        Assert.AreEqual(60f, health.CurrentHp, .001f, "No regeneration inside the delay");
        yield return Until(() => Time.time >= hurtAt + 5f, 2.5f, "Two seconds into regeneration");
        Assert.That(health.CurrentHp, Is.InRange(68f, 71f), "5 HP per second after 3 seconds");
        Assert.AreEqual(1, health.DamageStage, "Appearance stage follows the regenerated HP");

        Assert.IsTrue(health.TryTakeDamage(10f, body.position));
        float hp = health.CurrentHp;
        yield return WaitGame(2.5f);
        Assert.AreEqual(hp, health.CurrentHp, .001f, "New damage restarts the wait");
        yield return Until(() => health.CurrentHp >= health.MaximumHp, 12f, "Regenerates to full");
        yield return WaitGame(.2f);
        Assert.AreEqual(health.MaximumHp, health.CurrentHp, "Never above the maximum");
        foreach (var enemy in enemies)
        {
            enemy.Health.TryTakeDamage(1f, body.position);
            float enemyHp = enemy.Health.CurrentHp;
            Assert.Less(enemyHp, enemy.Health.MaximumHp);
        }
        yield return WaitGame(3.5f);
        foreach (var enemy in enemies)
            Assert.AreEqual(enemy.Health.MaximumHp - 1f, enemy.Health.CurrentHp, .001f, "Enemies do not regenerate");
    }

    [UnityTest]
    public IEnumerator AResistedHoldNeitherDrainsNorRegeneratesAndGivingUpLosesHpUntilSwallowed()
    {
        Assert.IsTrue(health.TryTakeDamage(40f, body.position));
        yield return WaitGame(3.1f);   // regeneration is running when the trap closes
        yield return CaptureInSlime();
        yield return Tap(Key.J);
        float held = health.CurrentHp;
        for (int i = 0; i < 4 && grab.IsHeld; i++)
        {
            yield return WaitGame(.2f);
            yield return Tap(Key.J);
        }
        Assert.IsTrue(grab.IsHeld, "Slow presses resist without escaping");
        Assert.AreEqual(held, health.CurrentHp, .01f, "Resisting: no loss, and no regeneration while restrained");

        float quietFrom = Time.time;
        yield return Until(() => pool.Capture.IsPlayerConsumed, 4f, "Giving up drains the remaining HP");
        Assert.AreEqual(0f, health.CurrentHp);
        Assert.Less(Time.time - quietFrom, 4f * held / health.MaximumHp + .6f, "Lower HP is swallowed sooner");
        yield return null;
        Assert.IsTrue(gameOver.IsShowing);
        Assert.AreEqual("SLIME", gameOver.CauseKey);
        Assert.IsNull(gameOver.Cutscene, "No cutscene art yet");
        Assert.IsFalse(session.HoldRetry);
        var dim = GameObject.Find("GameOver_Dim").GetComponent<SpriteRenderer>();
        Assert.AreSame(Camera.main.transform, dim.transform.parent, "The dark layer rides the world camera, under the overlay HUD");
        yield return Until(() => gameOver.DimAlpha >= SKGameOverScreen.DimTarget, 1f, "The play field darkens instead of showing text");
        Assert.AreEqual(.5f, dim.color.a, .001f, "Only slightly dark");
        Assert.IsTrue(dim.enabled);
        yield return Until(() => !health.IsDead, session.AutoRetrySeconds + .5f, "Without a cutscene the auto retry still runs");
        yield return null;
        Assert.IsFalse(gameOver.IsShowing);
        Assert.AreEqual(0f, gameOver.DimAlpha, "A retry lights the room again");
        Assert.IsFalse(dim.enabled);
        Assert.AreEqual(health.MaximumHp, health.CurrentHp);
    }

    [UnityTest]
    public IEnumerator EachCharacterHasItsOwnOrbThatTurnsBlueYellowRedWithHp()
    {
        var hud = Object.FindFirstObjectByType<SKDay01HUD>();
        Assert.IsNotNull(hud.Orb, "The HUD builds the orb by itself");
        Sprite cecilia = hud.Orb.sprite;
        Assert.AreEqual("hp_orb_cecilia", cecilia.name);
        Assert.AreSame(hud.OrbFrame.transform, hud.Orb.transform.parent, "The orb sits inside its ornament");
        Assert.AreEqual("hp_orb_ornaments_f01", hud.OrbFrame.sprite.name, "Drawn halo-and-chain ornament");
        Assert.AreEqual("hp_orb_ornaments_f00", hud.OrbFrame.transform.Find("HP_ORB_GLASS").GetComponent<UnityEngine.UI.Image>().sprite.name);
        Assert.AreEqual(Color.white, hud.OrbFrame.color, "Only the orb changes colour");
        var expected = new (float hp, int state)[] { (100f, 0), (76f, 0), (75f, 1), (40f, 1), (25f, 2), (1f, 2), (0f, 3) };
        foreach (var (hp, state) in expected)
        {
            health.ResetHealth();
            if (hp < 100f) health.Drain(100f - hp);
            Assert.AreEqual(state, hud.OrbState, "HP " + hp);
            Assert.AreEqual(hud.OrbColor, hud.Orb.color, "HP " + hp);
        }
        health.ResetHealth();
        Color blue = hud.Orb.color;
        Assert.Greater(blue.b, blue.r);
        health.Drain(50f);
        Assert.Less(hud.Orb.color.b, hud.Orb.color.r, "Yellow");
        Assert.Greater(hud.Orb.color.g, .6f);
        health.Drain(30f);
        Assert.Less(hud.Orb.color.g, .5f, "Red");
        yield return Tap(Key.Tab);
        Assert.AreEqual(1, player.SelectedCharacter);
        Assert.AreEqual("hp_orb_adela", hud.Orb.sprite.name);
        Assert.AreEqual("hp_orb_ornaments_f02", hud.OrbFrame.sprite.name, "Drawn sword-and-shield ornament");
        Assert.AreEqual(Color.white, hud.OrbFrame.color);
        Assert.AreNotSame(cecilia, hud.Orb.sprite);
        Assert.AreEqual(2, hud.OrbState, "Shared HP keeps its colour across the switch");
        health.ResetHealth();
        Assert.AreEqual(0, hud.OrbState);
    }

    [UnityTest]
    public IEnumerator EachKillerHasItsOwnCutsceneKeyAndACutsceneWaitsForR()
    {
        Assert.AreEqual("UNKNOWN", SKGameOverScreen.KeyFor(null));
        Assert.AreEqual("FLESHWALL", SKGameOverScreen.KeyFor(wall));
        Assert.AreEqual("SLIME", SKGameOverScreen.KeyFor(pool.Capture));
        Assert.AreEqual("TENTACLE", SKGameOverScreen.KeyFor(spike.GetComponentInChildren<SKHitbox>(true)));
        foreach (var enemy in enemies)
            Assert.AreEqual(enemy.DisplayName.EndsWith("MONK") ? "MONK" : "EXECUTIONER",
                SKGameOverScreen.KeyFor(enemy.Health), enemy.DisplayName);

        health.Drain(health.CurrentHp, wall);
        yield return null;
        Assert.IsTrue(gameOver.IsShowing);
        Assert.AreEqual("FLESHWALL", gameOver.CauseKey);
        session.HoldRetry = true;   // what a present cutscene sets
        yield return WaitGame(session.AutoRetrySeconds + .6f);
        Assert.IsTrue(health.IsDead, "A cutscene on screen suspends the auto retry");
        yield return Tap(Key.R);
        Assert.IsFalse(health.IsDead);
        yield return null;
        Assert.IsFalse(gameOver.IsShowing);
        Assert.IsFalse(session.HoldRetry);
        Assert.IsNull(health.LastCause);
    }
}
