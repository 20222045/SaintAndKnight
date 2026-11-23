using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SaintKnight.Day01;
using SaintKnight.Day02;
using SaintKnight.Day04;
using SaintKnight.Day06;
using SaintKnight.Day09;
using SaintKnight.Day10;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>HP appearance policy independently of a HUD, sprite or combat state.</summary>
public sealed class Day13DamageStageRulesTests
{
    [TestCase(100f)]
    [TestCase(200f)]
    [TestCase(360f)]
    public void BoundariesUseMaximumHpAndNeverRoundIntoAnotherStage(float maximum)
    {
        float[] fractions = { 1f, .7501f, .75f, .7499f, .5001f, .5f, .4999f,
            .2501f, .25f, .2499f, .0001f, 0f };
        int[] stages = { 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4 };
        for (int i = 0; i < fractions.Length; i++)
        {
            Assert.AreEqual(stages[i], SKHealth.DamageStageFor(maximum * fractions[i], maximum),
                "HP fraction " + fractions[i] + " with maximum " + maximum);
            Assert.AreEqual(stages[i], SKDay01HUD.StageFor(100f * fractions[i]),
                "The standalone Day 1 percent API preserves the same boundaries");
        }
        Assert.AreEqual(3, SKHealth.DamageStageFor(float.Epsilon, maximum),
            "Even the smallest positive HP is alive; division must not underflow into stage 4");
        Assert.AreEqual(4, SKHealth.DamageStageFor(-1f, maximum));
        Assert.AreEqual(0, SKHealth.DamageStageFor(maximum + 1f, maximum));
    }

    [Test]
    public void AcceptedDamageAndDrainPublishTheirStageButRejectedDamageDoesNot()
    {
        var host = new GameObject("Day13 isolated health");
        try
        {
            var health = host.AddComponent<SKHealth>();
            int changes = 0, damageEvents = 0, publishedStage = -1;
            health.Changed += () => { changes++; publishedStage = health.DamageStage; };
            health.Damaged += () => damageEvents++;
            Assert.IsTrue(health.TryTakeDamage(25f, Vector2.zero));
            Assert.AreEqual(75f, health.CurrentHp);
            Assert.AreEqual(1, publishedStage, "Subscribers read the new stage in the HP event");
            Assert.IsTrue(health.IsInvulnerable);
            Assert.IsFalse(health.TryTakeDamage(75f, Vector2.zero));
            Assert.AreEqual(1, changes);
            Assert.AreEqual(1, damageEvents);
            Assert.AreEqual(25f, health.Drain(25f), "Restraint drain still works during hit immunity");
            Assert.AreEqual(2, publishedStage);
            Assert.AreEqual(1, damageEvents, "Drain does not replay the hit reaction event");

            health.ResetHealth();
            Assert.AreEqual(75f, health.Drain(75f));
            Assert.AreEqual(3, publishedStage);
            Assert.IsFalse(health.IsStunned, "Drain alone does not create hit stun");
            Assert.IsFalse(health.IsInvulnerable, "Drain alone does not create hit immunity");
            int beforeRejected = changes;
            foreach (float invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
            {
                Assert.IsFalse(health.TryTakeDamage(invalid, Vector2.zero));
                Assert.AreEqual(0f, health.Drain(invalid));
            }
            health.enabled = false;
            Assert.IsFalse(health.TryTakeDamage(25f, Vector2.zero));
            Assert.AreEqual(0f, health.Drain(25f));
            Assert.AreEqual(beforeRejected, changes);
            Assert.AreEqual(25f, health.CurrentHp);
            Assert.AreEqual(3, health.DamageStage);
        }
        finally { Object.DestroyImmediate(host); }
    }

    [Test]
    public void HealingAndResetReverseDamageWithoutASeparateLatchedStage()
    {
        var host = new GameObject("Day13 healing health");
        var hudHost = new GameObject("Day13 bound HUD");
        try
        {
            var health = host.AddComponent<SKHealth>();
            typeof(SKHealth).GetField("maximumHp", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(health, 200f);
            health.ResetHealth();
            var hud = hudHost.AddComponent<SKDay01HUD>();
            hud.BindHealth(health);
            health.Drain(200f);
            Assert.AreEqual(4, health.DamageStage);
            float[] targets = { .125f, 50f, 50.125f, 100f, 100.125f, 150f, 150.125f, 200f };
            int[] stages = { 3, 3, 2, 2, 1, 1, 0, 0 };
            for (int i = 0; i < targets.Length; i++)
            {
                health.AdjustForTraining(targets[i] - health.CurrentHp);
                Assert.AreEqual(stages[i], health.DamageStage);
                Assert.AreEqual(stages[i], hud.DamageStage);
                Assert.AreEqual(targets[i], hud.DisplayedHp);
            }
            health.Drain(180f);
            health.ResetHealth();
            Assert.AreEqual(200f, hud.DisplayedHp);
            Assert.AreEqual(0, hud.DamageStage);
            hud.BindHealth(null);
        }
        finally { Object.DestroyImmediate(hudHost); Object.DestroyImmediate(host); }
    }
}

/// <summary>Existing sprites and HUD agree with HP stages in the current playable rooms.</summary>
public sealed class Day13DamageStageTests
{
    private static readonly string[] Scenes = { "Day09_Stage1", "Day09_BossChapel" };
    private Keyboard keyboard;
    private InputSettings.BackgroundBehavior previousBackground;
#if UNITY_EDITOR
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
#endif
    private SKDay01Player player;
    private SKPlayerCombat combat;
    private SKHealth health;
    private SKDay01HUD hud;
    private SKCharacterSpriteView view;
    private Rigidbody2D body;

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
        yield return Load(Scenes[0]);
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

    private IEnumerator Load(string scene)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return SceneManager.LoadSceneAsync(scene);
        yield return null;
        player = Object.FindFirstObjectByType<SKDay01Player>();
        combat = player.GetComponent<SKPlayerCombat>();
        health = player.GetComponent<SKHealth>();
        hud = Object.FindFirstObjectByType<SKDay01HUD>();
        view = player.GetComponent<SKCharacterSpriteView>();
        body = player.GetComponent<Rigidbody2D>();
        foreach (var enemy in Object.FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None))
            enemy.gameObject.SetActive(false);
        var boss = Object.FindFirstObjectByType<SKBossAgent>();
        if (boss != null) boss.enabled = false;
        yield return Until(() => player.IsGrounded && body.linearVelocity.magnitude < .05f, 2f, "Player settled");
    }

    private static T Field<T>(object owner, string field) => (T)owner.GetType()
        .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);

    private static IEnumerator Until(Func<bool> condition, float timeout, string message)
    {
        float end = Time.time + timeout, realEnd = Time.realtimeSinceStartup + 30f;
        while (!condition() && Time.time < end && Time.realtimeSinceStartup < realEnd) yield return null;
        Assert.IsTrue(condition(), message);
    }

    private IEnumerator Tap(Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
    }

    private IEnumerator Select(int character)
    {
        if (player.SelectedCharacter != character) yield return Tap(Key.Tab);
        Assert.AreEqual(character, player.SelectedCharacter);
    }

    private void AssertHud(int stage)
    {
        Assert.AreEqual(stage, health.DamageStage);
        Assert.AreEqual(stage, hud.DamageStage);
        Assert.AreEqual(health.CurrentHp, hud.DisplayedHp);
        Assert.AreEqual(health.CurrentHp / health.MaximumHp, Field<Image>(hud, "hpFill").fillAmount, .0001f);
        Assert.AreEqual("DAMAGE STAGE " + stage + " / 4" + (stage == 4 ? "  |  INCAPACITATED" : ""),
            Field<Text>(hud, "stageText").text);
        string character = player.SelectedCharacter == 0 ? "cecilia" : "adela";
        var portraits = Field<Sprite[]>(hud, character + "StagePortraits");
        Sprite expected = SKDay01HUD.StagePortrait(player.SelectedCharacter, stage) ?? (portraits != null && stage < portraits.Length && portraits[stage] != null
            ? portraits[stage] : Field<Sprite>(hud, character + "Portrait"));
        Assert.IsNotNull(expected);
        Assert.AreSame(expected, Field<Image>(hud, "portrait").sprite);
    }

    private void AssertIdleOrCollapse(int stage)
    {
        AssertHud(stage);
        string character = player.SelectedCharacter == 0 ? "cecilia" : "adela";
        string sprite = view.Refresh().name;
        if (stage == 0) StringAssert.StartsWith(character + "_idle_f", sprite);
        else if (stage == 4) StringAssert.StartsWith(character + "_collapse_f", sprite);
        else Assert.AreEqual(character + "_stage" + stage, sprite);
    }

    [UnityTest]
    public IEnumerator BothCharactersUseAllFiveStagesInTheStageAndBossChapel()
    {
        foreach (string scene in Scenes)
        {
            yield return Load(scene);
            foreach (int character in new[] { 0, 1 })
            {
                yield return Select(character);
                AssertIdleOrCollapse(0);
                for (int stage = 1; stage <= 4; stage++)
                {
                    health.Drain(health.MaximumHp * .25f);
                    AssertIdleOrCollapse(stage);
                    Assert.AreEqual(stage == 4, health.IsDead);
                    if (stage < 4) Assert.IsTrue(combat.CanAct, "Appearance damage does not disable a living character");
                }
                Assert.IsFalse(combat.TryAttack());
                player.SwitchCharacter();
                Assert.AreEqual(character, player.SelectedCharacter, "Stage 4 keeps the existing incapacitation rules");
                yield return Tap(Key.R);
                yield return Until(() => player.IsGrounded && body.linearVelocity.magnitude < .05f, 2f, "Reset settled");
                Assert.AreEqual(character, player.SelectedCharacter);
                AssertIdleOrCollapse(0);
            }
        }
    }

    [UnityTest]
    public IEnumerator HealingAcrossEveryBoundaryRestoresTheExistingDisplayWithNon100MaximumHp()
    {
        foreach (string scene in Scenes)
        {
            yield return Load(scene);
            typeof(SKHealth).GetField("maximumHp", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(health, 200f);
            foreach (int character in new[] { 0, 1 })
            {
                health.ResetHealth();
                yield return Select(character);
                health.Drain(200f);
                AssertIdleOrCollapse(4);
                float[] targets = { .125f, 50f, 50.125f, 100f, 100.125f, 150f, 150.125f, 200f };
                int[] stages = { 3, 3, 2, 2, 1, 1, 0, 0 };
                for (int i = 0; i < targets.Length; i++)
                {
                    health.AdjustForTraining(targets[i] - health.CurrentHp);
                    AssertIdleOrCollapse(stages[i]);
                }
                Assert.IsTrue(combat.CanAct);
                Assert.AreEqual("HP 200 / 200", Field<Text>(hud, "healthText").text);
            }
        }
    }

    [UnityTest]
    public IEnumerator RealFleshWallDrainCrossesAStageWhileHeldAndRRestoresIt()
    {
        var wall = Object.FindFirstObjectByType<SKFleshWall>();
        Assert.IsNotNull(wall);
        foreach (int character in new[] { 0, 1 })
        {
            yield return Select(character);
            health.Drain(24f); // 76 HP: the real unresisted drain soon crosses 75%.
            AssertHud(0);
            var box = player.GetComponent<BoxCollider2D>();
            Vector2 at = new Vector2(wall.transform.position.x,
                wall.transform.position.y - (box.bounds.min.y - body.position.y) + .02f);
            player.transform.position = new Vector3(at.x, at.y, player.transform.position.z);
            body.position = at;
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            yield return Until(() => combat.Grab.IsHeld, 2f, "Wall begins its real hold");
            yield return Until(() => health.CurrentHp <= 75f, 2f, "Wall drains HP");
            AssertHud(1);
            Assert.AreSame(wall, combat.Grab.Holder);
            Assert.AreEqual(SKCombatState.Held, combat.State, "Damage stage does not replace restraint state");
            Assert.IsFalse(health.IsStunned);
            Assert.IsFalse(health.IsInvulnerable);
            Assert.IsFalse(combat.TryAttack());
            Assert.IsTrue(player.GetComponent<SpriteRenderer>().forceRenderingOff,
                "The wall composite keeps presentation priority over damaged idle");
            yield return Tap(Key.R);
            yield return Until(() => player.IsGrounded && body.linearVelocity.magnitude < .05f, 2f, "Reset settled");
            Assert.IsFalse(combat.Grab.IsHeld);
            Assert.IsFalse(player.GetComponent<SpriteRenderer>().forceRenderingOff);
            AssertIdleOrCollapse(0);
        }
    }
}
