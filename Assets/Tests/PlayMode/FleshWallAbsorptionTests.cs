using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SaintKnight.Day01;
using SaintKnight.Day02;
using SaintKnight.Day04;
using SaintKnight.Day10;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>Inactivity absorption uses the real wall, player input, HP and room reset.</summary>
public sealed class FleshWallAbsorptionTests
{
    private Keyboard keyboard;
    private InputSettings.BackgroundBehavior previousBackground;
#if UNITY_EDITOR
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
#endif
    private float previousCaptureDelta;
    private SKDay01Player player;
    private SKPlayerCombat combat;
    private SKHealth health;
    private SKGrabState grab;
    private SKFleshWall wall;
    private SKEnemyTrainingSession session;
    private Rigidbody2D body;
    private BoxCollider2D box;
    private SpriteRenderer solo;

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
        combat = player.GetComponent<SKPlayerCombat>();
        health = player.GetComponent<SKHealth>();
        grab = combat.Grab;
        body = player.GetComponent<Rigidbody2D>();
        box = player.GetComponent<BoxCollider2D>();
        solo = player.GetComponent<SpriteRenderer>();
        wall = Object.FindFirstObjectByType<SKFleshWall>();
        session = Object.FindFirstObjectByType<SKEnemyTrainingSession>();
        foreach (var enemy in Object.FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None))
            enemy.gameObject.SetActive(false);
        foreach (var spike in Object.FindObjectsByType<SKFloorSpike>(FindObjectsSortMode.None))
        {
            spike.Cancel();
            spike.enabled = false;
        }
        SetAutoRetry(0f);
        yield return Until(() => player.IsGrounded && body.linearVelocity.magnitude < .05f, 2f, "Player settled");
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

    private void SetAutoRetry(float seconds) => typeof(SKEnemyTrainingSession)
        .GetField("autoRetrySeconds", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(session, seconds);

    private void Hold(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));

    private IEnumerator Tap(Key key)
    {
        Hold(key);
        yield return null;
        Hold();
        yield return null;
    }

    private static IEnumerator Until(Func<bool> condition, float seconds, string message)
    {
        float end = Time.time + seconds, realEnd = Time.realtimeSinceStartup + 30f;
        while (!condition() && Time.time < end && Time.realtimeSinceStartup < realEnd) yield return null;
        Assert.IsTrue(condition(), message);
    }

    private IEnumerator BeginHold(int character)
    {
        SKHitStop.CancelActive();
        Hold();
        wall.enabled = true;
        SetAutoRetry(0f);
        session.ResetTraining();
        yield return null;
        if (player.SelectedCharacter != character) yield return Tap(Key.Tab);
        Assert.AreEqual(character, player.SelectedCharacter);
        var at = new Vector2(wall.transform.position.x,
            wall.transform.position.y - (box.bounds.min.y - body.position.y) + .02f);
        player.transform.position = new Vector3(at.x, at.y, player.transform.position.z);
        body.position = at;
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        yield return Until(() => grab.IsHeld && grab.Holder == wall, 2f, "Wall catches the selected character");
        Assert.AreEqual(SKFleshWallState.Holding, wall.State);
        Assert.Less(wall.AbsorptionProgress, .05f);
    }

    private void AssertConsumed()
    {
        Assert.AreEqual(SKFleshWallState.Consumed, wall.State);
        Assert.IsTrue(wall.IsPlayerConsumed);
        Assert.AreEqual(1f, wall.AbsorptionProgress);
        Assert.AreEqual(0f, health.CurrentHp);
        Assert.AreEqual(SKCombatState.Dead, combat.State);
        Assert.IsFalse(grab.IsHeld, "Absorption releases the restraint state");
        Assert.IsFalse(wall.IsHoldingPlayer);
        Assert.IsTrue(solo.forceRenderingOff, "The closed wall keeps the solo collapse sprite hidden");
        var library = SKTrapAnimationLibrary.Load(player.SelectedCharacter, SKTrapMotionKind.Wall);
        var native = SaintKnight.SpriteCook.SKNativeDefeatLibrary.Load(player.SelectedCharacter, "fleshwall");
        if (native != null)
        {
            Assert.AreSame(native.Atlas, wall.GetComponent<SpriteRenderer>().sprite.texture);
            StringAssert.Contains("_native_fleshwall_", wall.GetComponent<SpriteRenderer>().sprite.name);
        }
        else if (library != null)
        {
            Assert.IsTrue(library.Owns(wall.GetComponent<SpriteRenderer>().sprite));
            StringAssert.StartsWith((player.SelectedCharacter == 0 ? "cecilia" : "adela") + "_wall_consumed_",
                wall.GetComponent<SpriteRenderer>().sprite.name);
        }
        else StringAssert.StartsWith("flesh_wall_idle_", wall.GetComponent<SpriteRenderer>().sprite.name);
    }

    private void AssertReleased()
    {
        Assert.AreEqual(SKFleshWallState.Idle, wall.State);
        Assert.IsFalse(wall.IsPlayerConsumed);
        Assert.AreEqual(0f, wall.AbsorptionProgress);
        Assert.IsFalse(grab.IsHeld);
        Assert.IsFalse(solo.forceRenderingOff);
    }

    [UnityTest]
    public IEnumerator BothCharactersAreAbsorbedWithoutResistanceAndTheExistingAutoRetryRestoresThem()
    {
        foreach (int character in new[] { 0, 1 })
        {
            yield return BeginHold(character);
            float started = grab.HeldSince;
            yield return Until(() => wall.IsPlayerConsumed, 4.3f, "Four seconds without resistance fully absorbs");
            Assert.GreaterOrEqual(Time.time - started, 4f - .03f);
            AssertConsumed();
            yield return null;
            AssertConsumed(); // The dead-player guard must not reopen the wall next frame.
            SetAutoRetry(1.6f);
            if (SaintKnight.SpriteCook.SKNativeDefeatLibrary.Load(character, "fleshwall") != null)
            {
                yield return new WaitForSeconds(1.7f);
                Assert.IsTrue(health.IsDead, "The animated native scene waits for the existing retry key");
                Assert.IsTrue(session.HoldRetry);
                yield return Tap(Key.R);
            }
            yield return Until(() => !health.IsDead && wall.State == SKFleshWallState.Idle, 2f, "Existing auto retry");
            AssertReleased();
            Assert.AreEqual(health.MaximumHp, health.CurrentHp);
            Assert.AreEqual(character, player.SelectedCharacter);
        }
    }

    [UnityTest]
    public IEnumerator AStrugglePressPausesTheHpLossWithoutRestoringIt()
    {
        // Day 15: the quiet hold drains HP (full bar in 4 s); a press pauses the loss for a moment.
        yield return BeginHold(0);
        float started = grab.HeldSince;
        yield return Until(() => wall.AbsorptionProgress > .55f, 2.5f, "Absorption advances without input");
        yield return Tap(Key.J);
        float resistanceAt = grab.LastStruggleAt, hp = health.CurrentHp;
        Assert.GreaterOrEqual(resistanceAt, started + 2f);
        Assert.Greater(wall.AbsorptionProgress, .5f, "A press gives no HP back");
        Assert.IsFalse(grab.EscapeReady);
        yield return Until(() => Time.time >= resistanceAt + .25f, .5f, "Inside the resistance window");
        Assert.AreEqual(hp, health.CurrentHp, .01f, "Resisting pauses the loss");
        Assert.IsTrue(grab.IsHeld);
        yield return Until(() => wall.IsPlayerConsumed, 2.5f, "The loss resumes once resistance stops");
        AssertConsumed();
    }

    [UnityTest]
    public IEnumerator ExistingMashEscapeWorksForBothCharactersAndRestoresTheSoloSprite()
    {
        foreach (int character in new[] { 0, 1 })
        {
            yield return BeginHold(character);
            for (int i = 0; i < 16 && grab.IsHeld && !grab.EscapeReady; i++)
                yield return Tap(i % 2 == 0 ? Key.Space : Key.J);
            yield return Until(() => !grab.IsHeld, 1f, "Sufficient discrete presses escape");
            Assert.AreEqual(SKFleshWallState.Rest, wall.State);
            Assert.IsFalse(wall.IsPlayerConsumed);
            Assert.AreEqual(0f, wall.AbsorptionProgress);
            Assert.IsTrue(health.IsInvulnerable);
            Assert.IsFalse(health.IsDead);
            Assert.Greater(grab.EscapedAt, 0f);
            yield return Until(() => !solo.forceRenderingOff, .4f, "Existing escape composite finishes");
        }
    }

    [UnityTest]
    public IEnumerator RAndDirectHealthRecoveryRestoreThePlayerBeforeAndAfterFullAbsorption()
    {
        foreach (bool consumed in new[] { false, true })
        {
            yield return BeginHold(consumed ? 1 : 0);
            yield return Until(() => consumed ? wall.IsPlayerConsumed : wall.AbsorptionProgress > .1f,
                4.3f, "Requested absorption phase");
            SKHitStop.Request(.1f, player.gameObject.scene);
            yield return Tap(Key.R);
            Assert.IsFalse(SKHitStop.IsActive);
            AssertReleased();
            Assert.AreEqual(health.MaximumHp, health.CurrentHp);
        }
        foreach (bool useDebugHeal in new[] { false, true })
        {
            yield return BeginHold(useDebugHeal ? 1 : 0);
            yield return Until(() => wall.IsPlayerConsumed, 4.3f, "Full absorption before direct HP restoration");
            AssertConsumed();
            if (useDebugHeal) yield return Tap(Key.E);
            else health.ResetHealth();
            yield return Until(() => !wall.IsPlayerConsumed && !solo.forceRenderingOff && !grab.IsHeld,
                .3f, "HP restoration without TrainingReset reopens the wall");
            // Direct healing leaves the player beside the wall, so a fresh tell may already begin.
            Assert.IsTrue(wall.State == SKFleshWallState.Idle || wall.State == SKFleshWallState.Tell);
            Assert.IsFalse(wall.IsPlayerConsumed);
            Assert.AreEqual(0f, wall.AbsorptionProgress);
            Assert.IsFalse(grab.IsHeld);
            Assert.IsFalse(wall.IsHoldingPlayer);
            Assert.IsFalse(solo.forceRenderingOff);
            Assert.AreEqual(useDebugHeal ? 25f : health.MaximumHp, health.CurrentHp);
        }
    }

    [UnityTest]
    public IEnumerator DisablingTheWallRestoresItsHiddenPlayerInEitherPhase()
    {
        foreach (bool consumed in new[] { false, true })
        {
            yield return BeginHold(consumed ? 1 : 0);
            yield return Until(() => consumed ? wall.IsPlayerConsumed : wall.AbsorptionProgress > .1f,
                4.3f, "Requested absorption phase");
            wall.enabled = false;
            AssertReleased();
            yield return null;
            AssertReleased();
        }
    }

    [UnityTest]
    public IEnumerator HitStopPausesAbsorptionAndDrainTogether()
    {
        yield return BeginHold(0);
        yield return Until(() => wall.AbsorptionProgress > .1f, .7f, "Some scaled absorption time elapses");
        float progress = wall.AbsorptionProgress, hp = health.CurrentHp;
        // Re-request each frame so even a slow batch frame cannot leave a later frame unpaused.
        for (int i = 0; i < 5; i++)
        {
            SKHitStop.Request(.1f, player.gameObject.scene);
            Assert.IsTrue(SKHitStop.IsActive);
            yield return null;
            Assert.AreEqual(progress, wall.AbsorptionProgress, .00001f);
            Assert.AreEqual(hp, health.CurrentHp);
        }
        SKHitStop.CancelActive();
        yield return Until(() => wall.AbsorptionProgress > progress + .1f, .7f, "Absorption resumes with scaled time");
        Assert.Less(health.CurrentHp, hp, "Drain resumes on the same game clock");
    }
}
