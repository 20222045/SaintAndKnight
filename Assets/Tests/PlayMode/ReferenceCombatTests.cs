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

public sealed class ReferenceCombatTests
{
    private Keyboard keyboard;
    private SKDay01Player player;
    private SKPlayerCombat combat;
    private SKHealth health;
    private SKHitbox hitbox;
    private SKHealth target;
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
        BindPlayer();
        var dummy = Object.FindFirstObjectByType<SKTrainingTarget>();
        dummy.AttacksEnabled = false;
        target = dummy.GetComponent<SKHealth>();
        yield return new WaitForSeconds(.25f);
    }

    private void BindPlayer()
    {
        player = Object.FindFirstObjectByType<SKDay01Player>();
        combat = player.GetComponent<SKPlayerCombat>();
        health = player.GetComponent<SKHealth>();
        hitbox = player.GetComponentInChildren<SKHitbox>();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        SKHitStop.CancelActive();
        Time.timeScale = 1f;
        InputSystem.RemoveDevice(keyboard);
        InputSystem.settings.backgroundBehavior = previousBackground;
#if UNITY_EDITOR
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
#endif
        yield return null;
    }

    private SKPlayerCombat.AttackProfile Profile() =>
        (SKPlayerCombat.AttackProfile)typeof(SKPlayerCombat)
            .GetField("cecilia", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(combat);

    private void PlaceForHit(float distance = .85f)
    {
        player.transform.position = target.transform.position + Vector3.left * distance;
        player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }

    [UnityTest]
    public IEnumerator DirectionAndAttackInSameFrameUseNewFacing()
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A, Key.J));
        yield return null;
        yield return null;
        Assert.IsTrue(combat.IsAttacking);
        Assert.AreEqual(-1f, player.Facing);
        Assert.Less(hitbox.transform.localPosition.x, 0f);
        Assert.IsTrue(player.GetComponent<SpriteRenderer>().flipX);
    }

    [UnityTest]
    public IEnumerator WindupCannotDealDamageAndActiveStrikeHitsOnlyOnce()
    {
        Profile().windup = .25f;
        PlaceForHit();
        Assert.IsTrue(combat.TryAttack());
        Assert.AreEqual(SKCombatState.Windup, combat.State);
        Assert.IsFalse(hitbox.GetComponent<Collider2D>().enabled);
        Assert.IsTrue(hitbox.GetComponent<SpriteRenderer>().enabled);
        yield return new WaitForSeconds(.12f);
        Assert.AreEqual(150f, target.CurrentHp);
        yield return new WaitForSeconds(.4f);
        Assert.AreEqual(130f, target.CurrentHp);
        Assert.IsFalse(hitbox.GetComponent<SpriteRenderer>().enabled);
    }

    [UnityTest]
    public IEnumerator ReverseMovementKeepsCommittedFacingUntilStrikeEnds()
    {
        Profile().windup = .2f;
        Assert.IsTrue(combat.TryAttack());
        float before = player.transform.position.x;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
        yield return new WaitForSeconds(.1f);
        Assert.Less(player.transform.position.x, before, "Retreat remains available");
        Assert.AreEqual(1f, player.Facing);
        Assert.Greater(hitbox.transform.localPosition.x, 0f);
        yield return new WaitForSeconds(.15f);
        Assert.IsTrue(combat.IsAttacking);
        Assert.AreEqual(1f, player.Facing);
        Assert.IsFalse(player.GetComponent<SpriteRenderer>().flipX);
        yield return new WaitForSeconds(.2f);
        Assert.AreEqual(-1f, player.Facing, "Facing unlocks in recovery");
    }

    [UnityTest]
    public IEnumerator DamageCancelsPendingStrikeAndPreview()
    {
        Profile().windup = .25f;
        PlaceForHit();
        Assert.IsTrue(combat.TryAttack());
        Assert.IsTrue(health.TryTakeDamage(5f, target.transform.position));
        Assert.IsFalse(combat.LocksFacing);
        Assert.IsFalse(hitbox.GetComponent<SpriteRenderer>().enabled);
        yield return new WaitForSeconds(.55f);
        Assert.AreEqual(150f, target.CurrentHp, "Canceled windup must not fire later");
        Assert.AreEqual(SKCombatState.Ready, combat.State);
    }

    [UnityTest]
    public IEnumerator SwitchCancelsWindupWithoutErasingCooldown()
    {
        Profile().windup = .25f;
        PlaceForHit();
        Assert.IsTrue(combat.TryAttack());
        player.SwitchCharacter();
        Assert.AreEqual(1, player.SelectedCharacter);
        Assert.AreEqual(SKCombatState.Recovery, combat.State);
        Assert.IsFalse(combat.TryAttack());
        yield return new WaitForSeconds(.3f);
        Assert.AreEqual(150f, target.CurrentHp);
        Assert.IsFalse(combat.TryAttack());
        yield return new WaitForSeconds(.2f);
        Assert.IsTrue(combat.TryAttack());
    }

    [UnityTest]
    public IEnumerator PlayerDisableAndTrainingResetDoNotLeaveDelayedAttacks()
    {
        Profile().windup = .25f;
        PlaceForHit();
        Assert.IsTrue(combat.TryAttack());
        player.enabled = false;
        Assert.AreEqual(SKCombatState.Disabled, combat.State);
        Assert.IsFalse(hitbox.GetComponent<SpriteRenderer>().enabled);
        yield return new WaitForSeconds(.5f);
        Assert.AreEqual(150f, target.CurrentHp);
        player.enabled = true;
        yield return null;
        Assert.IsTrue(combat.TryAttack());
        Object.FindFirstObjectByType<SKTrainingSession>().ResetTraining();
        PlaceForHit();
        yield return new WaitForSeconds(.5f);
        Assert.AreEqual(150f, target.CurrentHp);
        Assert.AreEqual(SKCombatState.Ready, combat.State);
    }

    [UnityTest]
    public IEnumerator HitStopPreservesWindupAndShortCooldownCannotOverlapStrike()
    {
        Profile().windup = .2f;
        Profile().cooldown = 0f;
        Assert.IsTrue(combat.TryAttack());
        SKHitStop.Request(.1f, player.gameObject.scene);
        yield return new WaitForSecondsRealtime(.04f);
        Assert.AreEqual(SKCombatState.Windup, combat.State);
        Assert.IsFalse(combat.IsAttacking);
        yield return new WaitForSeconds(.24f);
        Assert.IsTrue(combat.IsAttacking);
        Assert.IsFalse(combat.TryAttack());
        yield return new WaitForSeconds(.2f);
        Assert.AreEqual(SKCombatState.Ready, combat.State);
    }

    private IEnumerator LoadLatestTraining()
    {
        yield return SceneManager.LoadSceneAsync("Day05_EnemyPatterns");
        yield return null;
        BindPlayer();
        foreach (var enemy in Object.FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None))
        {
            enemy.enabled = false;
            var body = enemy.GetComponent<Rigidbody2D>();
            body.linearVelocity = Vector2.zero;
            body.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        target = Object.FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None)
            .OrderBy(e => e.DisplayName).First().Health;
        yield return new WaitForSeconds(.25f);
    }

    [UnityTest]
    public IEnumerator LatestSceneCeciliaReachesAnEnemyOutsideAdelaRange()
    {
        yield return LoadLatestTraining();
        // Between the two strikes' far edges (Day 6: hitboxes end at the drawn weapon tips), plus the target's half width.
        float FarEdge(string field)
        {
            var p = (SKPlayerCombat.AttackProfile)typeof(SKPlayerCombat)
                .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(combat);
            return p.offset + p.size.x * .5f;
        }
        float ceciliaFar = FarEdge("cecilia"), adelaFar = FarEdge("adela");
        Assert.Greater(ceciliaFar, adelaFar + .1f, "Cecilia must out-range Adela");
        float halfWidth = target.GetComponentInChildren<SKHurtbox>().GetComponent<BoxCollider2D>().bounds.extents.x;
        float distance = (ceciliaFar + adelaFar) * .5f + halfWidth;
        PlaceForHit(distance);
        Assert.IsTrue(combat.TryAttack());
        yield return new WaitForSeconds(.55f);
        Assert.AreEqual(40f, target.CurrentHp);
        target.ResetHealth();
        player.SwitchCharacter();
        yield return null;
        PlaceForHit(distance);
        Assert.IsTrue(combat.TryAttack());
        yield return new WaitForSeconds(.5f);
        Assert.AreEqual(60f, target.CurrentHp, "The short strike must miss at the same distance");
    }

    [UnityTest]
    public IEnumerator LatestSceneAdelaDefeatsAInTwoCloseStrikes()
    {
        yield return LoadLatestTraining();
        player.SwitchCharacter();
        yield return null;
        PlaceForHit();
        Assert.IsTrue(combat.TryAttack());
        yield return new WaitForSeconds(.4f);
        Assert.AreEqual(25f, target.CurrentHp);
        Assert.IsFalse(combat.TryAttack(), "The heavier strike has a longer recovery");
        yield return new WaitForSeconds(.4f);
        PlaceForHit();
        Assert.IsTrue(combat.TryAttack());
        yield return new WaitForSeconds(.4f);
        Assert.IsTrue(target.IsDead);
    }
}
