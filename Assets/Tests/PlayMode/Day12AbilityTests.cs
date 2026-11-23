using System;
using System.Collections;
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

// Day 12: traits must follow the selected character through real Tab input in the current stage.
public sealed class Day12AbilityTests
{
    private Keyboard keyboard;
    private SKDay01Player player;
    private SKPlayerCombat combat;
    private SKHealth health;
    private SKEnemyAgent monk, executioner;
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
        yield return SceneManager.LoadSceneAsync("Day09_Stage1");
        yield return null;
        player = Object.FindFirstObjectByType<SKDay01Player>();
        combat = player.GetComponent<SKPlayerCombat>();
        health = player.GetComponent<SKHealth>();
        monk = GameObject.Find("Enemy_A1_Monk").GetComponent<SKEnemyAgent>();
        executioner = GameObject.Find("Enemy_B1_Executioner").GetComponent<SKEnemyAgent>();
        foreach (var enemy in Object.FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None))
            enemy.gameObject.SetActive(false);
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

    private static FieldInfo Field(object value, string name) => value.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);

    private static IEnumerator Until(Func<bool> condition, float seconds, string message)
    {
        float end = Time.time + seconds, watchdog = Time.realtimeSinceStartup + 20f;
        while (!condition() && Time.time < end && Time.realtimeSinceStartup < watchdog) yield return null;
        Assert.IsTrue(condition(), message);
    }

    private IEnumerator Tap(Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
    }

    private IEnumerator SwitchTo(int character)
    {
        yield return Until(() => combat.CanAct, 2f, "Switch waits for hit stop / hit reaction");
        if (player.SelectedCharacter != character) yield return Tap(Key.Tab);
        Assert.AreEqual(character, player.SelectedCharacter, "Tab must select the requested character");
    }

    private IEnumerator Engage(SKEnemyAgent enemy)
    {
        SKHitStop.CancelActive();
        monk.gameObject.SetActive(false);
        executioner.gameObject.SetActive(false);
        enemy.gameObject.SetActive(true);
        enemy.AttacksEnabled = false;
        enemy.ResetAgent();
        enemy.Health.ResetHealth();
        health.ResetHealth();
        combat.ResetCombat();
        player.transform.position = enemy.transform.position + new Vector3(-.75f, -.05f, 0f);
        player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        // Face the enemy through the same input owner used during play.
        yield return Tap(Key.D);
        enemy.AttacksEnabled = true;
        Field(enemy, "readyAt").SetValue(enemy, 0f);
        yield return Until(() => enemy.State == SKEnemyState.Windup, 2f, "Enemy starts its normal tell");
    }

    private IEnumerator Strike(SKEnemyAgent enemy, float damage, bool breaksArmor, float delay)
    {
        float hp = enemy.Health.CurrentHp;
        yield return Tap(Key.J);
        yield return Until(() => enemy.Health.CurrentHp < hp, 2f, "The selected character's strike lands");
        Assert.AreEqual(hp - damage, enemy.Health.CurrentHp, .001f);
        Assert.AreEqual(breaksArmor, enemy.Health.LastHitBreaksArmor, "Armor trait follows this hit's character");
        Assert.AreEqual(delay, enemy.Health.LastHitInterruptDelay, .001f, "Chain trait follows this hit's character");
    }

    [UnityTest]
    public IEnumerator TabChangesHeavyArmorInteractionInBothDirections()
    {
        foreach (int character in new[] { 0, 1, 0 })
        {
            yield return SwitchTo(character);
            yield return Engage(executioner);
            yield return Strike(executioner, character == 0 ? 20f : 35f, character == 1,
                character == 0 ? .2f : 0f);
            yield return Until(() => !executioner.Health.IsStunned, 1f, "Hit flash ends");
            if (character == 1)
            {
                Assert.AreEqual(SKEnemyState.Recovery, executioner.State, "Adela cuts the built-in heavy armor");
                Assert.IsFalse(float.IsNegativeInfinity(executioner.ArmorBrokenAt));
            }
            else
            {
                Assert.That(executioner.State, Is.EqualTo(SKEnemyState.Windup).Or.EqualTo(SKEnemyState.Attacking),
                    "Switching back to Cecilia must remove the sword's armor break");
                Assert.IsTrue(float.IsNegativeInfinity(executioner.ArmorBrokenAt));
            }
        }
    }

    [UnityTest]
    public IEnumerator TabFromAdelaToCeciliaAddsChainDelayToTheCancelledTell()
    {
        foreach (int character in new[] { 1, 0 })
        {
            yield return SwitchTo(character);
            yield return Engage(monk);
            float recoveryRemaining = -1f;
            bool tellCancelled = false;
            // Observe the actual accepted hit rather than inferring the delay from render frame counts.
            Action capture = () =>
            {
                recoveryRemaining = (float)Field(monk, "readyAt").GetValue(monk) - Time.time;
                tellCancelled = (SKEnemyState)Field(monk, "phase").GetValue(monk) == SKEnemyState.Recovery;
            };
            monk.Health.Damaged += capture;
            try
            {
                yield return Strike(monk, character == 0 ? 20f : 35f, character == 1,
                    character == 0 ? .2f : 0f);
            }
            finally { monk.Health.Damaged -= capture; }
            Assert.AreEqual(character == 0 ? .4f : .2f, recoveryRemaining, .001f,
                "Only the chain adds 0.2 s to the monk's ordinary 0.2 s hit recovery");
            Assert.IsTrue(tellCancelled, "Both weapons cancel an ordinary tell at the accepted hit");
        }
    }

    [UnityTest]
    public IEnumerator SwitchingToAdelaPreservesTheHeavyEnemyCounterArmor()
    {
        yield return Engage(executioner);
        // Give Cecilia the first hit while the enemy waits, producing its real post-hit counter.
        executioner.AttacksEnabled = false;
        yield return Strike(executioner, 20f, false, .2f);
        executioner.AttacksEnabled = true;
        yield return SwitchTo(1);
        yield return Until(() => executioner.State == SKEnemyState.Windup, 2f, "The post-hit counter begins");
        yield return Until(() => combat.State == SKCombatState.Ready, 1f, "Original chain cooldown remains");
        yield return Strike(executioner, 35f, true, 0f);
        yield return Until(() => !executioner.Health.IsStunned, 1f, "Sword hit flash ends");
        Assert.That(executioner.State, Is.EqualTo(SKEnemyState.Windup).Or.EqualTo(SKEnemyState.Attacking),
            "The newly selected sword must not cut the post-hit counter armor");
        Assert.IsTrue(float.IsNegativeInfinity(executioner.ArmorBrokenAt), "A counter is not a broken built-in armor");
        yield return Until(() => executioner.State == SKEnemyState.Attacking, 2f, "The surviving counter resolves");
    }
}
