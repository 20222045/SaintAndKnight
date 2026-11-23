using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SaintKnight.Day01;
using SaintKnight.Day02;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class Day03CombatTests
{
    private Keyboard keyboard;
    private SKDay01Player player;
    private SKPlayerCombat combat;
    private SKHealth health;
    private SKTrainingTarget target;
    private SKHealth targetHealth;
    private SKTrainingSession session;
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
        yield return SceneManager.LoadSceneAsync("Day02_CombatTraining");
        yield return null;
        player = Object.FindFirstObjectByType<SKDay01Player>();
        combat = player.GetComponent<SKPlayerCombat>();
        health = player.GetComponent<SKHealth>();
        target = Object.FindFirstObjectByType<SKTrainingTarget>();
        targetHealth = target.GetComponent<SKHealth>();
        session = Object.FindFirstObjectByType<SKTrainingSession>();
        target.AttacksEnabled = false;
        yield return new WaitForSeconds(.3f);
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

    private IEnumerator Press(params Key[] keys)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        yield return new WaitForSecondsRealtime(.06f);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
    }

    private void PositionForHit()
    {
        player.transform.position = target.transform.position + new Vector3(-.85f, 0f, 0f);
        player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }

    private static IEnumerator WaitForHitStop()
    {
        float deadline = Time.realtimeSinceStartup + 2f;
        while (!SKHitStop.IsActive && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.IsTrue(SKHitStop.IsActive, "A successful contact must start hit stop");
    }

    [UnityTest]
    public IEnumerator AttackRecoveryAndSwitchPreserveCooldown()
    {
        Assert.AreEqual(SKCombatState.Ready, combat.State);
        Assert.IsTrue(combat.TryAttack());
        Assert.AreEqual(SKCombatState.Attacking, combat.State);
        yield return new WaitForSeconds(.25f);
        Assert.AreEqual(SKCombatState.Recovery, combat.State);
        Assert.IsFalse(combat.TryAttack());
        player.SwitchCharacter();
        Assert.AreEqual(SKCombatState.Recovery, combat.State);
        Assert.IsFalse(combat.TryAttack());
        yield return new WaitForSeconds(.25f);
        Assert.AreEqual(SKCombatState.Ready, combat.State);
        Assert.IsTrue(combat.TryAttack());
    }

    [UnityTest]
    public IEnumerator ShortCooldownCannotRestartAnActiveSwing()
    {
        var profile = (SKPlayerCombat.AttackProfile)typeof(SKPlayerCombat)
            .GetField("cecilia", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(combat);
        profile.cooldown = 0f; // An Inspector tuning mistake must not bypass the one-hit rule.
        PositionForHit();
        Assert.IsTrue(combat.TryAttack());
        yield return new WaitForSeconds(.07f);
        Assert.IsFalse(combat.TryAttack());
        Assert.AreEqual(130f, targetHealth.CurrentHp);
        yield return new WaitForSeconds(.2f);
        Assert.AreEqual(SKCombatState.Ready, combat.State);
        Assert.AreEqual(130f, targetHealth.CurrentHp);
    }

    [UnityTest]
    public IEnumerator SimultaneousSwitchAndAttackDoesNotSpendCooldown()
    {
        PositionForHit();
        yield return Press(Key.Tab, Key.J);
        Assert.AreEqual(1, player.SelectedCharacter);
        Assert.IsFalse(combat.IsAttacking);
        Assert.AreEqual(150f, targetHealth.CurrentHp);
        Assert.AreEqual(SKCombatState.Ready, combat.State);
        yield return Press(Key.J);
        yield return new WaitForSeconds(.25f);
        Assert.AreEqual(125f, targetHealth.CurrentHp, "The next press uses the new character immediately");
    }

    [UnityTest]
    public IEnumerator DamageDeathAndDisableCancelActions()
    {
        Assert.IsTrue(combat.TryAttack());
        Assert.IsTrue(health.TryTakeDamage(25f, target.transform.position));
        Assert.AreEqual(SKCombatState.Hurt, combat.State);
        Assert.IsFalse(combat.IsAttacking);
        yield return Press(Key.D, Key.Space, Key.Tab, Key.J);
        Assert.AreEqual(0, player.SelectedCharacter);
        Assert.IsFalse(combat.IsAttacking);
        Assert.IsFalse(health.TryTakeDamage(25f, target.transform.position));
        yield return new WaitForSeconds(.6f);
        Assert.AreEqual(SKCombatState.Ready, combat.State);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
        yield return new WaitForSeconds(.06f);
        Assert.IsTrue(health.TryTakeDamage(1000f, target.transform.position));
        Assert.AreEqual(SKCombatState.Dead, combat.State);
        yield return new WaitForFixedUpdate();
        Assert.AreEqual(0f, player.GetComponent<Rigidbody2D>().linearVelocity.x, .001f,
            "Death must clear movement already sampled before the hit");
        yield return Press(Key.J, Key.Space, Key.Tab);
        Assert.AreEqual(0, player.SelectedCharacter);
        Assert.IsFalse(combat.IsAttacking);
        session.ResetTraining();
        yield return null;
        Assert.IsTrue(combat.TryAttack());
        combat.enabled = false;
        Assert.AreEqual(SKCombatState.Disabled, combat.State);
        Assert.IsFalse(combat.IsAttacking);
        Assert.IsFalse(combat.TryAttack());
        combat.enabled = true;
        Assert.AreEqual(SKCombatState.Recovery, combat.State);
    }

    [UnityTest]
    public IEnumerator ContactPausesPhysicsAndPreservesCombatTimers()
    {
        PositionForHit();
        Assert.IsTrue(combat.TryAttack());
        yield return WaitForHitStop();
        SKHitStop.Request(.1f, player.gameObject.scene);
        float pausedAt = Time.time;
        Vector3 position = player.transform.position;
        Assert.AreEqual(130f, targetHealth.CurrentHp);
        Assert.AreEqual(0f, Time.timeScale);
        Assert.IsTrue(targetHealth.IsStunned);
        Assert.IsFalse(combat.TryAttack());
        player.SwitchCharacter();
        Assert.AreEqual(0, player.SelectedCharacter);
        yield return new WaitForSecondsRealtime(.03f);
        Assert.AreEqual(pausedAt, Time.time, .001f);
        Assert.AreEqual(position, player.transform.position);
        Assert.IsTrue(targetHealth.IsStunned, "Hit stop must not consume hit reaction time");
        yield return new WaitForSecondsRealtime(.12f);
        Assert.IsFalse(SKHitStop.IsActive);
        Assert.AreEqual(1f, Time.timeScale);
        yield return new WaitForSeconds(.25f);
        Assert.AreEqual(130f, targetHealth.CurrentHp, "Resume must not hit twice");
    }

    [UnityTest]
    public IEnumerator WhiffAndInvulnerableContactDoNotPause()
    {
        Assert.IsTrue(combat.TryAttack());
        float until = Time.time + .5f;
        while (Time.time < until)
        {
            Assert.IsFalse(SKHitStop.IsActive, "A missed attack must not stop time");
            yield return null;
        }
        PositionForHit();
        typeof(SKHealth).GetField("invulnerabilitySeconds", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(targetHealth, 1f);
        Assert.IsTrue(targetHealth.TryTakeDamage(1f, player.transform.position));
        Assert.IsTrue(combat.TryAttack());
        until = Time.time + .3f;
        while (Time.time < until)
        {
            Assert.IsFalse(SKHitStop.IsActive, "Rejected damage must not trigger hit stop");
            yield return null;
        }
        Assert.AreEqual(149f, targetHealth.CurrentHp);
    }

    [UnityTest]
    public IEnumerator HitStopResetAndSceneUnloadRestorePreviousSpeed()
    {
        Time.timeScale = .5f;
        SKHitStop.Request(.1f, player.gameObject.scene);
        Assert.AreEqual(0f, Time.timeScale);
        yield return Press(Key.R);
        Assert.IsFalse(SKHitStop.IsActive, "Reset remains available while time is stopped");
        Assert.AreEqual(.5f, Time.timeScale);
        Assert.AreEqual(100f, health.CurrentHp);
        Assert.AreEqual(SKCombatState.Ready, combat.State);
        SKHitStop.Request(.1f, player.gameObject.scene);
        yield return SceneManager.LoadSceneAsync("Day01_Prototype");
        Assert.IsFalse(SKHitStop.IsActive);
        Assert.AreEqual(.5f, Time.timeScale, "Scene unload must never leave the next scene frozen");
        Time.timeScale = 0f;
        SKHitStop.Request(.1f, SceneManager.GetActiveScene());
        yield return null;
        Assert.IsFalse(SKHitStop.IsActive, "An existing external pause must not be taken over");
        Assert.AreEqual(0f, Time.timeScale);
    }

    [UnityTest]
    public IEnumerator AttackPressedDuringHitStopIsNotBuffered()
    {
        PositionForHit();
        SKHitStop.Request(.1f, player.gameObject.scene);
        yield return Press(Key.J, Key.Tab, Key.Space);
        yield return new WaitForSecondsRealtime(.15f);
        Assert.AreEqual(SKCombatState.Ready, combat.State);
        Assert.AreEqual(0, player.SelectedCharacter);
        Assert.AreEqual(150f, targetHealth.CurrentHp);
        Assert.IsFalse(combat.IsAttacking);
        yield return Press(Key.J);
        yield return new WaitForSeconds(.25f);
        Assert.AreEqual(130f, targetHealth.CurrentHp);
    }

    [UnityTest]
    public IEnumerator TargetWindupInterruptsAndCannotStrikeAfterDisable()
    {
        PositionForHit();
        target.AttacksEnabled = true;
        target.ResetCombat();
        yield return new WaitForSeconds(.85f);
        Assert.AreEqual(SKCombatState.Windup, target.State);
        Assert.IsTrue(targetHealth.TryTakeDamage(20f, player.transform.position));
        Assert.IsFalse(target.IsWindingUp);
        Assert.AreEqual(SKCombatState.Hurt, target.State);
        yield return new WaitForSeconds(.25f);
        Assert.AreEqual(SKCombatState.Recovery, target.State);
        Assert.AreEqual(100f, health.CurrentHp);
        target.ResetCombat();
        yield return new WaitForSeconds(.85f);
        Assert.AreEqual(SKCombatState.Windup, target.State);
        target.AttacksEnabled = false;
        Assert.IsFalse(target.IsWindingUp);
        Assert.IsFalse(target.GetComponentInChildren<SKHitbox>().IsActive);
        yield return new WaitForSeconds(.6f);
        Assert.AreEqual(100f, health.CurrentHp);
        target.AttacksEnabled = true;
        session.ResetTraining();
        PositionForHit(); // Reset returns the player to the safe spawn outside target range.
        yield return new WaitForSeconds(1.65f);
        Assert.AreEqual(85f, health.CurrentHp, "Reset restores the normal telegraphed attack");
    }
}
