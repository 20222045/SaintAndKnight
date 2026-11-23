using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SaintKnight.Day01;
using SaintKnight.Day02;
using SaintKnight.Day04;
using SaintKnight.Day06;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>
/// 2026-11-10 pending design decisions (Day 5 scene): enemies counter after a hit so point-blank
/// mashing meets their tell, a ground attack commits (no jump), and crouch ducks under B's high swing.
/// </summary>
public sealed class CombatDecisionTests
{
    private Keyboard keyboard;
    private SKDay01Player player;
    private SKPlayerCombat combat;
    private SKHealth health;
    private SKEnemyAgent a;
    private SKEnemyAgent b;
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
        a.AttacksEnabled = b.AttacksEnabled = false;
        player = Object.FindFirstObjectByType<SKDay01Player>();
        combat = player.GetComponent<SKPlayerCombat>();
        health = player.GetComponent<SKHealth>();
        Place(new Vector2(-5.5f, -2.2f));
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

    private void Place(Vector2 at)
    {
        player.transform.position = at;
        player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }

    private void Hold(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));

    private static IEnumerator Until(Func<bool> condition, float timeout, string message)
    {
        float end = Time.realtimeSinceStartup + timeout;
        while (!condition() && Time.realtimeSinceStartup < end) yield return null;
        Assert.IsTrue(condition(), message);
    }

    [UnityTest]
    public IEnumerator PointBlankMashingMeetsTheEnemyTellBeforeTheKill()
    {
        foreach (bool heavy in new[] { false, true })
        foreach (int character in new[] { 0, 1 })
        {
            var enemy = heavy ? b : a;
            (heavy ? a : b).gameObject.SetActive(false);
            enemy.gameObject.SetActive(true);
            enemy.ResetAgent();
            enemy.Health.ResetHealth();
            health.ResetHealth();
            combat.ResetCombat();
            if (player.SelectedCharacter != character) player.SwitchCharacter();
            yield return null;
            enemy.AttacksEnabled = true;
            // We arrive while it is still recovering, so the first hit is ours (worst case for the enemy).
            Set(enemy, "readyAt", Time.time + 10f);
            // Inside both characters' reach, outside A's grab range.
            Place((Vector2)enemy.transform.position + new Vector2(-.75f, -.05f));
            yield return new WaitForFixedUpdate();

            string label = (heavy ? "B" : "A") + (character == 0 ? " vs Cecilia" : " vs Adela");
            float start = health.CurrentHp;
            bool punished = false;
            float end = Time.time + 4f;
            while (Time.time < end && !enemy.Health.IsDead && !punished)
            {
                Hold(combat.State == SKCombatState.Ready && !keyboard.jKey.isPressed ? new[] { Key.J } : new Key[0]);
                yield return null;
                punished = health.CurrentHp < start || combat.Grab.IsHeld;
            }
            Hold();
            Assert.Less(enemy.Health.CurrentHp, enemy.Health.MaximumHp, label + ": mashing must land first");
            Assert.IsTrue(punished, label + ": the counter strike must resolve before the enemy dies");
            yield return null;
        }
    }

    // ---- Day 7 character traits: Adela's sword cuts armor, Cecilia's chain delays a cancelled tell. ----

    private IEnumerator Engage(SKEnemyAgent enemy, int character, float gap)
    {
        (enemy == a ? b : a).gameObject.SetActive(false);
        enemy.gameObject.SetActive(true);
        enemy.ResetAgent();
        enemy.Health.ResetHealth();
        health.ResetHealth();
        combat.ResetCombat();
        if (player.SelectedCharacter != character) player.SwitchCharacter();
        yield return null;
        Place((Vector2)enemy.transform.position + new Vector2(-gap, -.05f));
        enemy.AttacksEnabled = true;
        Set(enemy, "readyAt", 0f);
        yield return new WaitForFixedUpdate();
    }

    private IEnumerator StrikeOnce(SKEnemyAgent enemy)
    {
        float hp = enemy.Health.CurrentHp;
        Hold(Key.J);
        yield return null;
        Hold();
        yield return Until(() => enemy.Health.CurrentHp < hp, 1f, "Our strike must land during the tell");
    }

    [UnityTest]
    public IEnumerator OnlyAdelaCutsTheHeavySwingArmor()
    {
        foreach (int character in new[] { 0, 1 })
        {
            yield return Engage(b, character, .75f);
            yield return Until(() => b.State == SKEnemyState.Windup, 2f, "B must start its armored tell");
            yield return StrikeOnce(b);
            yield return Until(() => b.State != SKEnemyState.Hurt, 1f, "Hit flash ends");
            if (character == 1)
                Assert.AreEqual(SKEnemyState.Recovery, b.State, "Adela's sword cancels B's armored swing");
            else
                Assert.That(b.State, Is.EqualTo(SKEnemyState.Windup).Or.EqualTo(SKEnemyState.Attacking),
                    "Cecilia's chain does not stop B's armored swing");
        }
    }

    [UnityTest]
    public IEnumerator OnlyAdelaCutsTheGrabTell()
    {
        foreach (int character in new[] { 0, 1 })
        {
            yield return Engage(a, character, .45f);
            yield return Until(() => a.State == SKEnemyState.GrabWindup, 2f, "A must start its grab tell");
            yield return StrikeOnce(a);
            // The 0.45 s tell was already running; a fresh one after a cut cannot finish this soon.
            yield return new WaitForSeconds(.5f);
            if (character == 1) Assert.IsFalse(combat.Grab.IsHeld, "Adela's sword cancels the grab");
            else Assert.IsTrue(combat.Grab.IsHeld, "Cecilia is still grabbed through the armored tell");
            a.ResetAgent();
            yield return null;
        }
    }

    // Cutting the first (unarmored or sword-broken) tell and mashing must still meet the counter.
    [UnityTest]
    public IEnumerator CuttingTheFirstTellThenMashingStillMeetsTheCounter()
    {
        foreach (bool heavy in new[] { false, true })
        foreach (int character in new[] { 0, 1 })
        {
            var enemy = heavy ? b : a;
            string label = (heavy ? "B" : "A") + (character == 0 ? " vs Cecilia" : " vs Adela");
            yield return Engage(enemy, character, .75f);
            yield return Until(() => enemy.State == SKEnemyState.Windup, 2f, label + ": tell starts");
            float start = health.CurrentHp;
            bool punished = false;
            float end = Time.time + 4f;
            while (Time.time < end && !enemy.Health.IsDead && !punished)
            {
                Hold(combat.State == SKCombatState.Ready && !keyboard.jKey.isPressed ? new[] { Key.J } : new Key[0]);
                yield return null;
                punished = health.CurrentHp < start || combat.Grab.IsHeld;
            }
            Hold();
            Assert.Less(enemy.Health.CurrentHp, enemy.Health.MaximumHp, label + ": mashing must land");
            Assert.IsTrue(punished, label + ": the counter must resolve before the enemy dies");
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator CeciliaCancellingATellDelaysTheNextAttack()
    {
        yield return Engage(a, 0, .9f);
        yield return Until(() => a.State == SKEnemyState.Windup, 2f, "A must start its strike tell");
        yield return StrikeOnce(a);
        float hitAt = Time.time;
        yield return Until(() => a.State == SKEnemyState.Recovery, 1f, "The tell is cancelled");
        yield return Until(() => a.State == SKEnemyState.Windup, 2f, "A attacks again");
        // Hurt recovery 0.2 s + chain delay 0.2 s (vs 0.2 s without the chain trait).
        Assert.AreEqual(.4f, Time.time - hitAt, .08f);
    }

    // Day 5, Day 8, Day 9 and the lobby each serialize their own copy of the player attack profiles.
    [UnityTest]
    public IEnumerator EveryPlayableSceneUsesTheDay5AttackProfiles()
    {
        string Profiles()
        {
            var c = Object.FindFirstObjectByType<SKPlayerCombat>();
            var f = BindingFlags.Instance | BindingFlags.NonPublic;
            return JsonUtility.ToJson(typeof(SKPlayerCombat).GetField("cecilia", f).GetValue(c)) +
                JsonUtility.ToJson(typeof(SKPlayerCombat).GetField("adela", f).GetValue(c));
        }
        string day5 = Profiles();
        StringAssert.Contains("\"interruptDelay\":0.2", day5);
        StringAssert.Contains("\"breaksArmor\":true", day5);
        foreach (var scene in new[] { "Day08_Scrolling", "Day09_Stage1", "Day09_BossChapel", "Lobby_Sanctuary" })
        {
            yield return SceneManager.LoadSceneAsync(scene);
            yield return null;
            Assert.AreEqual(day5, Profiles(), scene + " must match the Day 5 attack profiles");
        }
    }

    [UnityTest]
    public IEnumerator GroundAttackCommitsSoJumpWaitsForTheStrikeToEnd()
    {
        var rb = player.GetComponent<Rigidbody2D>();
        yield return Until(() => player.IsGrounded && Mathf.Abs(rb.linearVelocity.y) < .01f, 2f, "Grounded first");
        float ground = player.transform.position.y;
        Hold(Key.J);
        yield return null;
        Hold(Key.J, Key.Space);
        yield return null;
        Assert.IsTrue(combat.LocksFacing, "Attack must still be committed");
        yield return new WaitForSeconds(.05f);
        Assert.AreEqual(ground, player.transform.position.y, .02f, "No jump during windup/strike in Day 5");
        Hold();
        yield return Until(() => !combat.LocksFacing, 1f, "Strike ends");
        Hold(Key.Space);
        yield return new WaitForSeconds(.1f);
        Hold();
        Assert.Greater(player.transform.position.y, ground + .1f, "Jump works again after the strike");
    }

    [UnityTest]
    public IEnumerator CrouchLowersTheBodyAndDucksUnderTheHeavySwing()
    {
        var body = player.GetComponent<BoxCollider2D>();
        var hurt = player.GetComponentInChildren<SKHurtbox>().GetComponent<BoxCollider2D>();
        var view = player.GetComponent<SKCharacterSpriteView>();
        yield return Until(() => player.IsGrounded, 2f, "Grounded first");
        float feet = body.bounds.min.y, hurtFeet = hurt.bounds.min.y, x = player.transform.position.x;

        Hold(Key.S, Key.D);
        yield return new WaitForSeconds(.3f);
        Assert.IsTrue(player.IsCrouching);
        Assert.AreEqual(.72f, body.size.y, .001f);
        Assert.AreEqual(.72f, hurt.size.y, .001f);
        Assert.AreEqual(feet, body.bounds.min.y, .01f, "Feet stay on the floor");
        Assert.AreEqual(hurtFeet, hurt.bounds.min.y, .01f);
        Assert.AreEqual(x, player.transform.position.x, .01f, "No walking while crouched");
        StringAssert.Contains("_crouch_", view.Current.name);

        // B's high swing passes over a crouch...
        b.gameObject.SetActive(true);
        a.gameObject.SetActive(false);
        yield return null;
        Place((Vector2)b.transform.position + new Vector2(-.9f, -.05f));
        Hold(Key.S);
        b.AttacksEnabled = true;
        Set(b, "readyAt", 0f);
        float start = health.CurrentHp;
        yield return Until(() => b.State == SKEnemyState.Attacking, 3f, "B must swing");
        yield return Until(() => b.State == SKEnemyState.Recovery, 1f, "B swing ends");
        Assert.AreEqual(start, health.CurrentHp, "Crouch ducks under B's high swing");

        // ...but hits the player standing.
        Hold();
        yield return null;
        Assert.IsFalse(player.IsCrouching);
        Assert.AreEqual(.94f, hurt.size.y, .001f, "Standing restores the hurtbox");
        Set(b, "readyAt", 0f);
        yield return Until(() => health.CurrentHp < start, 3f, "B's swing must hit a standing player");
    }
}
