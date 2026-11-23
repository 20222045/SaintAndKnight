using System;
using System.Collections;
using System.Linq;
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

/// <summary>Environmental restraint, quiet-time absorption and recovery through the real stage hazards.</summary>
public sealed class EnvironmentalCaptureTests
{
    private Keyboard keyboard;
    private InputSettings.BackgroundBehavior previousBackground;
#if UNITY_EDITOR
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
#endif
    private float previousCaptureDelta;
    private SKDay01Player player;
    private SKHealth health;
    private SKPlayerCombat combat;
    private SKGrabState grab;
    private SKEnemyTrainingSession session;
    private SKSlimePool pool;
    private SKFloorSpike[] spikes;
    private Rigidbody2D body;
    private BoxCollider2D box;
    private SpriteRenderer playerView;
    private Vector2 spawn;

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
        combat = player.GetComponent<SKPlayerCombat>();
        grab = combat.Grab;
        body = player.GetComponent<Rigidbody2D>();
        box = player.GetComponent<BoxCollider2D>();
        playerView = player.GetComponent<SpriteRenderer>();
        session = Object.FindFirstObjectByType<SKEnemyTrainingSession>();
        pool = GameObject.Find("Trap_Slime_Pool").GetComponent<SKSlimePool>();
        spikes = Object.FindObjectsByType<SKFloorSpike>(FindObjectsSortMode.None).OrderBy(s => s.name).ToArray();
        foreach (var enemy in Object.FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None))
            enemy.gameObject.SetActive(false);
        foreach (var wall in Object.FindObjectsByType<SKFleshWall>(FindObjectsSortMode.None)) wall.enabled = false;
        yield return Until(() => player.IsGrounded && body.linearVelocity.magnitude < .05f, 2f, "Spawn settles");
        spawn = body.position;
        Assert.IsNotNull(pool.Capture);
        Assert.AreEqual(SKEnvironmentalCaptureKind.Slime, pool.Capture.Kind);
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

    private void SlimeOnly()
    {
        foreach (var spike in spikes) spike.enabled = false;
    }

    private static void SetField(object owner, string name, object value) => owner.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);

    private void Hold(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));

    private IEnumerator Tap(Key key)
    {
        Hold(key);
        yield return null;
        Hold();
        yield return null;
    }

    private IEnumerator Select(int character)
    {
        if (player.SelectedCharacter != character) yield return Tap(Key.Tab);
        Assert.AreEqual(character, player.SelectedCharacter);
    }

    private void PlaceOnFloor(Vector2 floor)
    {
        Vector2 at = new Vector2(floor.x, floor.y - (box.bounds.min.y - body.position.y) + .02f);
        player.transform.position = new Vector3(at.x, at.y, player.transform.position.z);
        body.position = at;
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
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

    private IEnumerator BeginSlimeCapture()
    {
        PlaceOnFloor(pool.transform.position);
        yield return Until(() => player.IsGrounded, .5f, "Grounded over slime");
        Assert.IsTrue(pool.Capture.TryCapture(player));
        Assert.AreSame(pool.Capture, grab.Holder);
        Assert.AreSame(pool.Capture, SKEnvironmentalCapture.ForPlayer(player));
    }

    [UnityTest]
    public IEnumerator SlimeWaitsForContinuousGroundDwellAndJumpingRestartsIt()
    {
        SlimeOnly();
        PlaceOnFloor(pool.transform.position);
        yield return Until(() => pool.Covers(player), .5f, "Standing in the real pool");
        Assert.AreEqual(1.25f, pool.DwellSeconds, .001f);
        Assert.IsFalse(grab.IsHeld, "Entering slime is not an immediate capture");
        yield return WaitGame(.8f);
        Assert.IsFalse(grab.IsHeld);
        Hold(Key.Space);
        yield return Until(() => !player.IsGrounded && body.linearVelocity.y > 0f, .5f, "Jump rising");
        Hold();
        yield return new WaitForFixedUpdate();
        Assert.IsFalse(pool.Covers(player));
        Assert.AreEqual(1f, player.SpeedMultiplier, .001f);
        yield return Until(() => player.IsGrounded && body.linearVelocity.y <= .1f, 2f, "Landed back in slime");
        float landedAt = Time.time;
        yield return WaitGame(.75f);
        Assert.IsFalse(grab.IsHeld, "Pre-jump dwell must not carry into a new grounded stay");
        yield return Until(() => pool.Capture.IsHoldingPlayer, 1f, "Continuous grounded stay eventually captures");
        Assert.GreaterOrEqual(Time.time - landedAt, pool.DwellSeconds - .06f);
        Assert.AreSame(pool.Capture, grab.Holder);
        Assert.AreEqual(SKCombatState.Held, combat.State);
        Assert.Greater(health.CurrentHp, 95f, "Capture itself deals no damage; only the quiet hold drains");
    }

    [UnityTest]
    public IEnumerator BothCharactersAreAbsorbedAfterFourQuietSecondsAndRRestoresThem()
    {
        SlimeOnly();
        foreach (int character in new[] { 0, 1 })
        {
            yield return Select(character);
            yield return BeginSlimeCapture();
            float heldSince = grab.HeldSince;
            Assert.IsFalse(combat.TryAttack());
            player.SwitchCharacter();
            Assert.AreEqual(character, player.SelectedCharacter);
            yield return WaitGame(3.75f);
            Assert.IsFalse(pool.Capture.IsPlayerConsumed);
            Assert.IsTrue(pool.Capture.IsHoldingPlayer);
            Assert.That(pool.Capture.AbsorptionProgress, Is.InRange(.9f, .99f));
            yield return Until(() => pool.Capture.IsPlayerConsumed, .5f, "Quiet-time absorption completes");
            Assert.GreaterOrEqual(Time.time - heldSince, 4f - .04f);
            Assert.AreEqual(0f, health.CurrentHp);
            Assert.IsFalse(grab.IsHeld, "A consumed player has no stale restraint owner");
            Assert.IsTrue(playerView.forceRenderingOff, "The absorbed body disappears");
            Assert.AreEqual(1f, pool.Capture.AbsorptionProgress);
            Assert.AreSame(pool.Capture, SKEnvironmentalCapture.ForPlayer(player));
            yield return Tap(Key.R);
            yield return Until(() => player.IsGrounded && body.linearVelocity.magnitude < .05f, 2f, "R restores the spawn");
            Assert.AreEqual(health.MaximumHp, health.CurrentHp);
            Assert.AreEqual(character, player.SelectedCharacter);
            Assert.IsFalse(pool.Capture.IsPlayerConsumed);
            Assert.IsFalse(playerView.forceRenderingOff);
            Assert.IsNull(SKEnvironmentalCapture.ForPlayer(player));
            Assert.AreEqual(SKCombatState.Ready, combat.State);
        }
    }

    [UnityTest]
    public IEnumerator OneRealStrugglePressPausesTheHpLossWithoutRestoringIt()
    {
        // Day 15: the quiet hold drains HP (full bar in 4 s); a press pauses the loss for a moment.
        SlimeOnly();
        yield return BeginSlimeCapture();
        yield return WaitGame(2f);
        yield return Tap(Key.J);
        float resistedAt = grab.LastStruggleAt, hp = health.CurrentHp;
        Assert.IsFalse(float.IsNegativeInfinity(resistedAt));
        Assert.That(hp, Is.InRange(40f, 60f), "Two quiet seconds cost about half the bar and a press gives none back");
        Assert.AreEqual(1f - hp / health.MaximumHp, pool.Capture.AbsorptionProgress, .001f);
        Assert.IsTrue(grab.IsHeld, "A single press is resistance, not a full escape");
        yield return Until(() => Time.time >= resistedAt + .25f, .5f, "Inside the resistance window");
        Assert.AreEqual(hp, health.CurrentHp, .01f, "Resisting pauses the loss");
        yield return Until(() => pool.Capture.IsPlayerConsumed, 2.8f, "The loss resumes once resistance stops");
        Assert.AreEqual(0f, health.CurrentHp);
    }

    [UnityTest]
    public IEnumerator MashingEscapesWithImmunityAndTheSameCaptureWaitsBeforeRecatching()
    {
        SlimeOnly();
        yield return BeginSlimeCapture();
        for (int i = 0; i < 14 && grab.IsHeld && !grab.EscapeReady; i++)
            yield return Tap(i % 2 == 0 ? Key.Space : Key.J);
        yield return Until(() => !grab.IsHeld, 1f, "Discrete mash input releases the restraint");
        float releasedAt = Time.time;
        Assert.IsFalse(pool.Capture.IsPlayerConsumed);
        Assert.Greater(health.CurrentHp, 0f);
        Assert.IsTrue(health.IsInvulnerable);
        Assert.IsFalse(playerView.forceRenderingOff);
        Assert.IsNull(SKEnvironmentalCapture.ForPlayer(player));
        Assert.IsFalse(pool.Capture.TryCapture(player), "Immediate recapture is rejected");
        PlaceOnFloor(new Vector2(-6.6666665f + 900f / 48f, pool.transform.position.y));
        yield return Until(() => !health.IsInvulnerable, .9f, "Escape immunity expires");
        Assert.Less(Time.time - releasedAt, 1.5f);
        Assert.IsFalse(pool.Capture.TryCapture(player), "The trap's cooldown outlasts damage immunity");
        yield return Until(() => Time.time >= releasedAt + 1.55f, 1.2f, "Trap rest expires");
        Assert.IsTrue(pool.Capture.TryCapture(player));
        Assert.AreSame(pool.Capture, grab.Holder);
    }

    [UnityTest]
    public IEnumerator OnlyAnAcceptedFloorTentacleHitCapturesAndItsCallbackRunsOnce()
    {
        pool.gameObject.SetActive(false);
        var spike = spikes[0];
        foreach (var other in spikes) if (other != spike) other.enabled = false;
        SetField(spike, "period", 0f);
        spike.ResetCycle();
        var hitbox = spike.GetComponentInChildren<SKHitbox>();
        int accepted = 0;
        SKHealth victim = null;
        Action<SKHealth> onAccepted = who => { accepted++; victim = who; };
        hitbox.HitAccepted += onAccepted;
        try
        {
            PlaceOnFloor(spike.transform.position);
            health.GrantInvulnerability(3f);
            spike.Arm(spike.transform.position, 0f);
            float rejectedStrike = spike.StrikeAt;
            yield return Until(() => Time.time >= rejectedStrike + spike.ActiveSeconds + .1f, 2f, "Immune eruption ends");
            Assert.AreEqual(0, accepted, "A rejected hit emits no accepted-source callback");
            Assert.AreEqual(100f, health.CurrentHp);
            Assert.IsFalse(spike.Capture.IsHoldingPlayer);
            Assert.IsNull(SKEnvironmentalCapture.ForPlayer(player));

            health.ResetHealth();
            spike.ResetCycle();
            PlaceOnFloor(spike.transform.position);
            spike.Arm(spike.transform.position, 0f);
            yield return Until(() => spike.Capture.IsHoldingPlayer, 1.5f, "The actual accepted tentacle hit captures");
            Assert.AreEqual(1, accepted);
            Assert.AreSame(health, victim);
            Assert.AreEqual(100f - spike.Damage, health.CurrentHp, 1.5f);
            Assert.AreEqual(15f, spike.Damage, .001f);
            Assert.AreEqual(SKEnvironmentalCaptureKind.Tentacle, spike.Capture.Kind);
            Assert.AreSame(spike.Capture, grab.Holder);
            Assert.IsTrue(health.IsInvulnerable, "Capture bypasses only immunity granted by this accepted hit");
            Assert.IsFalse(hitbox.IsActive, "Capturing cancels the remaining active hitbox");
            yield return WaitGame(.4f);
            Assert.AreEqual(1, accepted, "The same eruption never repeats the accepted callback");
            Assert.That(health.CurrentHp, Is.InRange(70f, 85f), "No duplicate strike; only the quiet-hold loss");
        }
        finally { hitbox.HitAccepted -= onAccepted; }
    }

    [UnityTest]
    public IEnumerator TimedPoolKeepsItsHoldThenClearRespawnDisableAndDestroyCleanUpOwnership()
    {
        SlimeOnly();
        SetField(pool, "permanent", false);
        Vector2 floor = pool.transform.position;
        pool.Spawn(floor, .15f);
        yield return BeginSlimeCapture();
        yield return WaitGame(.3f);
        Assert.IsTrue(pool.Active, "Timed pool remains present while it owns a restraint");
        Assert.IsTrue(pool.Capture.IsHoldingPlayer);
        pool.Clear();
        Assert.IsFalse(pool.Active);
        Assert.IsFalse(grab.IsHeld);
        Assert.IsNull(SKEnvironmentalCapture.ForPlayer(player));

        pool.Spawn(floor, 10f);
        Assert.IsTrue(pool.Capture.TryCapture(player));
        yield return Until(() => pool.Capture.IsPlayerConsumed, 4.3f, "Consumed before respawning the pool");
        Assert.IsTrue(playerView.forceRenderingOff);
        pool.Spawn(floor, 10f);
        Assert.IsFalse(pool.Capture.IsPlayerConsumed);
        Assert.IsFalse(playerView.forceRenderingOff, "Reusing a pool restores an absorbed player's renderer");
        Assert.IsNull(SKEnvironmentalCapture.ForPlayer(player));
        health.ResetHealth();
        Assert.IsTrue(pool.Capture.TryCapture(player));
        pool.gameObject.SetActive(false);
        Assert.IsFalse(grab.IsHeld);
        Assert.IsFalse(playerView.forceRenderingOff);
        Assert.IsNull(SKEnvironmentalCapture.ForPlayer(player));
        pool.gameObject.SetActive(true);
        pool.Spawn(floor, 10f);

        GameObject duplicate = Object.Instantiate(pool.gameObject);
        duplicate.name = "EnvironmentalCaptureTest second pool";
        try
        {
            var other = duplicate.GetComponent<SKSlimePool>();
            other.Spawn(floor, 10f);
            yield return null;
            Assert.IsTrue(pool.Capture.TryCapture(player));
            Assert.IsFalse(other.Capture.TryCapture(player), "Two overlapping pools cannot own the same grab");
            Assert.AreSame(pool.Capture, grab.Holder);
            Assert.AreSame(pool.Capture, SKEnvironmentalCapture.ForPlayer(player));
            pool.Clear();
            Assert.IsTrue(other.Capture.TryCapture(player));
            Assert.AreSame(other.Capture, grab.Holder);
            Object.Destroy(duplicate);
            yield return null;
            Assert.IsFalse(grab.IsHeld);
            Assert.IsNull(SKEnvironmentalCapture.ForPlayer(player));
            Assert.IsFalse(playerView.forceRenderingOff);
        }
        finally { if (duplicate != null) Object.Destroy(duplicate); }
    }

    [UnityTest]
    public IEnumerator AutomaticRetryRestoresTheAbsorbedBodyAndSelectedCharacterWithoutInput()
    {
        SlimeOnly();
        yield return Select(1);
        Assert.AreEqual(1.6f, session.AutoRetrySeconds, .001f);
        int resets = 0;
        Action onReset = () => resets++;
        session.TrainingReset += onReset;
        try
        {
            yield return BeginSlimeCapture();
            yield return Until(() => pool.Capture.IsPlayerConsumed, 4.3f, "Absorbed before retry");
            float consumedAt = Time.time;
            Assert.IsTrue(playerView.forceRenderingOff);
            Assert.IsTrue(health.IsDead);
            yield return Until(() => resets == 1 && !health.IsDead, session.AutoRetrySeconds + .5f, "Automatic retry restores HP");
            Assert.GreaterOrEqual(Time.time - consumedAt, session.AutoRetrySeconds - .05f);
            Assert.AreEqual(health.MaximumHp, health.CurrentHp);
            Assert.AreEqual(1, player.SelectedCharacter);
            Assert.IsFalse(pool.Capture.IsPlayerConsumed);
            Assert.IsFalse(grab.IsHeld);
            Assert.IsFalse(playerView.forceRenderingOff);
            Assert.IsNull(SKEnvironmentalCapture.ForPlayer(player));
            Assert.Less(Vector2.Distance(spawn, body.position), .1f);
            Assert.AreEqual(SKCombatState.Ready, combat.State);
        }
        finally { session.TrainingReset -= onReset; }
    }
}
