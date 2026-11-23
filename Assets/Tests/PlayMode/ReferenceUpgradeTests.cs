using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SaintKnight.Day01;
using SaintKnight.Day02;
using SaintKnight.Day04;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>
/// 2026-11-08 reference upgrade: variable jump, air control, close-range grab with
/// mash escape, automatic retry and facing the damage source (Day 5 scene only).
/// </summary>
public sealed class ReferenceUpgradeTests
{
    private Keyboard keyboard;
    private SKDay01Player player;
    private SKPlayerCombat combat;
    private SKHealth health;
    private SKGrabState grab;
    private SKEnemyAgent a;
    private SKEnemyAgent b;
    private SKEnemyTrainingSession session;
    private InputSettings.BackgroundBehavior previousBackground;
#if UNITY_EDITOR
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
#endif

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        SKHitStop.CancelActive();
        Time.timeScale = 1f;
        previousBackground = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
        previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        keyboard = InputSystem.AddDevice<Keyboard>();
        yield return SceneManager.LoadSceneAsync("Day05_EnemyPatterns");
        yield return null;
        var enemies = Object.FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None).OrderBy(e => e.DisplayName).ToArray();
        a = enemies[0]; b = enemies[1];
        b.gameObject.SetActive(false);
        a.AttacksEnabled = false;
        player = Object.FindFirstObjectByType<SKDay01Player>();
        combat = player.GetComponent<SKPlayerCombat>();
        health = player.GetComponent<SKHealth>();
        grab = combat.Grab;
        session = Object.FindFirstObjectByType<SKEnemyTrainingSession>();
        PlacePlayer(new Vector2(-5.5f, -2.2f));
        yield return new WaitForSeconds(.25f);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        SKHitStop.CancelActive();
        Time.timeScale = 1f;
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        InputSystem.settings.backgroundBehavior = previousBackground;
#if UNITY_EDITOR
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
#endif
        yield return null;
    }

    private static void Set(object obj, string name, object value) => obj.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, value);
    private static T Get<T>(object obj, string name) => (T)obj.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);

    // Timeouts count game time: an editor stall must not use up a window the game never simulated.
    // The real-time cap only stops a frozen (timeScale 0) game from hanging the run.
    private static IEnumerator Until(Func<bool> condition, float timeout = 3f, string message = "Expected transition")
    {
        float end = Time.time + timeout, realEnd = Time.realtimeSinceStartup + timeout * 5f + 5f;
        while (!condition() && Time.time < end && Time.realtimeSinceStartup < realEnd) yield return null;
        Assert.IsTrue(condition(), message);
    }

    private void PlacePlayer(Vector2 at)
    {
        player.transform.position = at;
        player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }

    private IEnumerator Tap(Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
    }

    private IEnumerator MeasureJumpPeak(float holdSeconds, Action<float> rise)
    {
        yield return Until(() => player.IsGrounded && Mathf.Abs(player.GetComponent<Rigidbody2D>().linearVelocity.y) < .01f,
            3f, "Player must be grounded before a jump");
        float start = player.transform.position.y;
        float peak = start;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
        float releaseAt = Time.time + holdSeconds;
        bool released = false;
        float end = Time.time + 1.5f;
        while (Time.time < end)
        {
            yield return null;
            if (!released && Time.time >= releaseAt)
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                released = true;
            }
            peak = Mathf.Max(peak, player.transform.position.y);
            if (released && Time.time > releaseAt + .1f && player.IsGrounded &&
                player.GetComponent<Rigidbody2D>().linearVelocity.y <= 0f) break;
        }
        if (!released) InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
        rise(peak - start);
    }

    private IEnumerator StartHold()
    {
        a.AttacksEnabled = true;
        PlacePlayer((Vector2)a.transform.position + new Vector2(-.35f, -.05f));
        Set(a, "readyAt", 0f);
        Set(a, "phase", SKEnemyState.Patrol);
        yield return Until(() => a.State == SKEnemyState.GrabWindup, 2f, "Hugging the enemy must start a grab tell");
        yield return Until(() => a.State == SKEnemyState.Holding || a.State == SKEnemyState.Recovery, 1f,
            "The grab tell must resolve");
        Vector2 gap = player.transform.position - a.transform.position;
        Assert.AreEqual(SKEnemyState.Holding, a.State, $"The grab must connect when the player stays close " +
            $"(gap {gap.x:F2},{gap.y:F2}, immune {health.IsInvulnerable}, hit-stop {SKHitStop.IsActive})");
    }

    [UnityTest]
    public IEnumerator LatestSceneTapJumpIsLowerThanHeldJump()
    {
        float tap = 0f, held = 0f;
        yield return MeasureJumpPeak(.02f, h => tap = h);
        yield return MeasureJumpPeak(.45f, h => held = h);
        Assert.Greater(held, .9f, "Holding Space keeps the full Day 5 jump");
        Assert.Less(tap, held - .3f, "Releasing early must give a clearly lower hop");
        Assert.Greater(tap, .05f, "A tap still leaves the ground");
    }

    [UnityTest]
    public IEnumerator LatestSceneAirControlIsReducedButNotRemoved()
    {
        Assert.AreEqual(.85f, player.AirSpeedMultiplier, .001f);
        yield return Until(() => player.IsGrounded, 2f);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
        yield return new WaitForSeconds(.1f);
        float ground = player.GetComponent<Rigidbody2D>().linearVelocity.x;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.Space));
        yield return new WaitForSeconds(.15f);
        Assert.IsFalse(player.IsGrounded);
        float air = player.GetComponent<Rigidbody2D>().linearVelocity.x;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
        // Ground contact friction trims the sampled ground speed slightly; air has no contact.
        Assert.Greater(ground, 3.3f);
        Assert.AreEqual(3.6f * .85f, air, .02f);
        Assert.Less(air, ground);
    }

    [UnityTest]
    public IEnumerator OlderScenesKeepFixedJumpAndFullAirControl()
    {
        yield return SceneManager.LoadSceneAsync("Day02_CombatTraining");
        yield return null;
        var old = Object.FindFirstObjectByType<SKDay01Player>();
        Assert.AreEqual(1f, old.JumpReleaseVelocityScale);
        Assert.AreEqual(1f, old.AirSpeedMultiplier);
        Assert.IsNull(Object.FindFirstObjectByType<SKEnemyAgent>());
    }

    [UnityTest]
    public IEnumerator HuggingEnemyAGrabsAndMashingBreaksFree()
    {
        yield return StartHold();
        Assert.IsTrue(grab.IsHeld);
        Assert.AreEqual(SKCombatState.Held, combat.State);
        Assert.IsFalse(combat.CanAct);
        Assert.IsFalse(combat.TryAttack(), "No attacks while restrained");
        int before = player.SelectedCharacter;
        yield return Tap(Key.Tab);
        Assert.AreEqual(before, player.SelectedCharacter, "No character switch while restrained");
        for (int i = 0; i < 12 && grab.IsHeld && !grab.EscapeReady; i++)
        {
            yield return Tap(i % 2 == 0 ? Key.J : Key.Space);
        }
        yield return Until(() => !grab.IsHeld, 1f, "Enough presses must break the hold");
        Assert.AreEqual(SKEnemyState.Recovery, a.State);
        Assert.IsTrue(health.IsInvulnerable, "Escape grants a short immunity so the player is not re-grabbed");
        Assert.Greater(Get<float>(a, "readyAt") - Time.time, 1f, "Escaping opens a long punish window");
        Assert.Greater(health.CurrentHp, 80f);
        yield return Until(() => combat.State == SKCombatState.Ready, 1f);
        Assert.IsTrue(combat.CanAct);
    }

    [UnityTest]
    public IEnumerator UnansweredHoldDrainsInStepsThenThrows()
    {
        yield return StartHold();
        yield return new WaitForSeconds(.6f);
        Assert.AreEqual(96f, health.CurrentHp, .01f, "First drain tick after 0.5 s");
        Assert.IsFalse(health.IsStunned, "Drain is not a hit reaction");
        yield return Until(() => !grab.IsHeld, 5f, "The hold must end on its own");
        Assert.IsTrue(health.IsStunned, "The throw is a normal accepted hit");
        Assert.LessOrEqual(health.CurrentHp, 100f - 12f - 24f);
        Assert.Greater(health.CurrentHp, 0f);
        Assert.AreEqual(SKEnemyState.Recovery, a.State);
    }

    [UnityTest]
    public IEnumerator SteppingOutDuringGrabTellMakesItWhiff()
    {
        a.AttacksEnabled = true;
        PlacePlayer((Vector2)a.transform.position + new Vector2(-.35f, -.05f));
        Set(a, "readyAt", 0f);
        Set(a, "phase", SKEnemyState.Patrol);
        yield return Until(() => a.State == SKEnemyState.GrabWindup, 2f);
        Assert.IsTrue(a.transform.Find("Attack_Warning").GetComponent<SpriteRenderer>().enabled);
        PlacePlayer((Vector2)a.transform.position + new Vector2(-1.4f, -.05f));
        yield return Until(() => a.State != SKEnemyState.GrabWindup, 1f);
        Assert.AreEqual(SKEnemyState.Recovery, a.State);
        Assert.IsFalse(grab.IsHeld);
        Assert.AreEqual(100f, health.CurrentHp);
    }

    [UnityTest]
    public IEnumerator ResetReleasesHeldPlayerAndRestoresControl()
    {
        yield return StartHold();
        session.ResetTraining();
        Assert.IsFalse(grab.IsHeld);
        Assert.AreEqual(SKCombatState.Ready, combat.State);
        yield return new WaitForFixedUpdate();
        Assert.AreEqual(SKEnemyState.Recovery, a.State);
        Assert.IsFalse(a.IsHoldingPlayer);
        Assert.AreEqual(100f, health.CurrentHp);
    }

    [UnityTest]
    public IEnumerator EnemyStrikeRangeStillUsesTheNormalStrike()
    {
        a.AttacksEnabled = true;
        PlacePlayer((Vector2)a.transform.position + new Vector2(-.85f, -.05f));
        Set(a, "readyAt", 0f);
        Set(a, "phase", SKEnemyState.Patrol);
        yield return Until(() => a.State == SKEnemyState.Windup, 2f);
        yield return Until(() => health.CurrentHp < 100f, 1f);
        Assert.AreEqual(85f, health.CurrentHp);
        Assert.IsFalse(grab.IsHeld);
    }

    [UnityTest]
    public IEnumerator HittingTheGrabTellDoesNotCancelIt()
    {
        a.AttacksEnabled = true;
        PlacePlayer((Vector2)a.transform.position + new Vector2(-.35f, -.05f));
        Set(a, "readyAt", 0f);
        Set(a, "phase", SKEnemyState.Patrol);
        yield return Until(() => a.State == SKEnemyState.GrabWindup, 2f);
        Assert.IsTrue(a.Health.TryTakeDamage(20f, player.transform.position));
        yield return Until(() => a.State == SKEnemyState.Holding, 1f, "Armored grab tell continues through a hit");
        Assert.AreEqual(40f, a.Health.CurrentHp);
        Assert.IsTrue(grab.IsHeld);
    }

    [UnityTest]
    public IEnumerator HitEnemyACanAttackAgainSoonInsteadOfBeingStunLocked()
    {
        PlacePlayer((Vector2)a.transform.position + new Vector2(-.85f, -.05f));
        Assert.IsTrue(a.Health.TryTakeDamage(5f, player.transform.position));
        float wait = Get<float>(a, "readyAt") - Time.time;
        // 2026-11-10: 0.3 -> 0.2 with an armored counter windup (CombatDecisionTests).
        Assert.AreEqual(.2f, wait, .05f, "Short post-hit recovery, not the full 1.0 s");
        yield return null;
    }

    [UnityTest]
    public IEnumerator HeavyBStrikeCannotBeCancelledByAHit()
    {
        a.gameObject.SetActive(false);
        b.gameObject.SetActive(true);
        yield return null;
        PlacePlayer((Vector2)b.transform.position + new Vector2(-1.45f, -.05f));
        Set(b, "readyAt", 0f);
        Set(b, "phase", SKEnemyState.Patrol);
        yield return Until(() => b.State == SKEnemyState.Windup, 2f);
        Assert.IsTrue(b.Health.TryTakeDamage(20f, player.transform.position));
        Assert.IsTrue(b.transform.Find("Attack_Warning").GetComponent<SpriteRenderer>().enabled,
            "B's tell stays up after being hit");
        yield return Until(() => health.CurrentHp < 100f, 1.5f, "B's committed strike still lands");
        Assert.AreEqual(75f, health.CurrentHp);
    }

    [UnityTest]
    public IEnumerator IncapacitationRestartsTheRoomAutomatically()
    {
        Assert.AreEqual(1.6f, session.AutoRetrySeconds, .001f);
        a.Health.TryTakeDamage(30f, player.transform.position);
        Assert.IsTrue(health.TryTakeDamage(1000f, a.transform.position));
        yield return new WaitForSeconds(.9f);
        Assert.IsTrue(health.IsDead, "The down state is visible before the retry");
        yield return Until(() => !health.IsDead, 2f, "The room must restart without pressing R");
        Assert.AreEqual(100f, health.CurrentHp);
        Assert.AreEqual(60f, a.Health.CurrentHp);
        Assert.AreEqual(-3f, player.transform.position.x, .01f);
    }

    [UnityTest]
    public IEnumerator AcceptedDamageTurnsPlayerTowardTheSource()
    {
        Assert.AreEqual(1f, player.Facing);
        Assert.IsTrue(health.TryTakeDamage(5f, (Vector2)player.transform.position + Vector2.left * 2f));
        Assert.AreEqual(-1f, player.Facing);
        Assert.IsTrue(player.GetComponent<SpriteRenderer>().flipX);
        yield return null;
        health.AdjustForTraining(-5f); // Debug damage from our own position keeps facing.
        Assert.AreEqual(-1f, player.Facing);
    }
}
