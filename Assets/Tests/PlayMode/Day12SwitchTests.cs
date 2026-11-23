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

/// <summary>Day 12: existing switch rules in the current stage, chapel and environmental hazards.</summary>
public sealed class Day12SwitchTests
{
    private static readonly string[] Scenes = { "Day09_Stage1", "Day09_BossChapel" };
    private Keyboard keyboard;
    private InputSettings.BackgroundBehavior previousBackground;
#if UNITY_EDITOR
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
#endif
    private SKDay01Player player;
    private SKPlayerCombat combat;
    private SKHealth hp;
    private SKDay01HUD hud;
    private SKCharacterSpriteView view;
    private Rigidbody2D body;
    private BoxCollider2D box;
    private SKHitbox hitbox;

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
        Hold();
        SKHitStop.CancelActive();
        yield return SceneManager.LoadSceneAsync(scene);
        yield return null;
        player = Object.FindFirstObjectByType<SKDay01Player>();
        combat = player.GetComponent<SKPlayerCombat>();
        hp = player.GetComponent<SKHealth>();
        body = player.GetComponent<Rigidbody2D>();
        box = player.GetComponent<BoxCollider2D>();
        hitbox = player.GetComponentInChildren<SKHitbox>();
        hud = Object.FindFirstObjectByType<SKDay01HUD>();
        view = player.GetComponent<SKCharacterSpriteView>();
        foreach (var enemy in Object.FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None))
            enemy.gameObject.SetActive(false);
        var boss = Object.FindFirstObjectByType<SKBossAgent>();
        if (boss != null) boss.enabled = false;
        yield return Until(() => player.IsGrounded, 2f, scene + ": settled on the floor");
    }

    private void Hold(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));

    private IEnumerator Tap(params Key[] keys)
    {
        Hold(keys);
        yield return null;
        Hold();
        yield return null;
    }

    // Game time tolerates editor stalls; the real-time limit also bounds an accidentally paused game.
    private static IEnumerator Until(Func<bool> condition, float seconds, string what)
    {
        float end = Time.time + seconds, realEnd = Time.realtimeSinceStartup + 30f;
        while (!condition() && Time.time < end && Time.realtimeSinceStartup < realEnd) yield return null;
        Assert.IsTrue(condition(), what);
    }

    private static T Field<T>(object target, string name) => (T)target.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

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

    [UnityTest]
    public IEnumerator SimultaneousTabAndAttackUsesNeitherProfileInStageAndChapel()
    {
        foreach (string scene in Scenes)
        {
            yield return Load(scene);
            for (int i = 0; i < 2; i++)
            {
                int next = 1 - player.SelectedCharacter;
                yield return Tap(Key.Tab, Key.J);
                Assert.AreEqual(next, player.SelectedCharacter, scene);
                Assert.AreEqual(SKCombatState.Ready, combat.State, "Tab + J must not spend a cooldown");
                Assert.IsFalse(hitbox.IsActive);
                Assert.IsFalse(hitbox.GetComponent<BoxCollider2D>().enabled);
                StringAssert.StartsWith(next == 0 ? "cecilia_" : "adela_", view.Refresh().name);
                Hold(Key.J);
                yield return Until(() => combat.LocksFacing, .3f, "A new J press attacks with the selected character");
                Hold();
                yield return Until(() => combat.State == SKCombatState.Ready, 1f, "Normal recovery");
                yield return null;
            }
        }
    }

    [UnityTest]
    public IEnumerator SwitchingInEitherAttackPhaseCancelsItWithoutShorteningRecovery()
    {
        foreach (string scene in Scenes)
        {
            yield return Load(scene);
            foreach (int character in new[] { 0, 1 })
            foreach (bool duringStrike in new[] { false, true })
            {
                combat.ResetCombat();
                yield return Select(character);
                yield return null; // Reset/selection intentionally forbids an attack in that same frame.
                float started = Time.time;
                Assert.IsTrue(combat.TryAttack());
                if (duringStrike) yield return Until(() => combat.IsAttacking, .4f, "Strike starts");
                else Assert.IsTrue(combat.IsWindingUp);

                // Call at the exact observed phase, without advancing out of Cecilia's 0.06 s tell.
                player.SwitchCharacter();
                player.SwitchCharacter();
                player.SwitchCharacter();
                Assert.AreEqual(1 - character, player.SelectedCharacter);
                Assert.AreEqual(SKCombatState.Recovery, combat.State);
                Assert.IsFalse(hitbox.IsActive);
                Assert.IsFalse(hitbox.GetComponent<BoxCollider2D>().enabled);
                StringAssert.DoesNotContain("_attack_", view.Refresh().name);
                StringAssert.DoesNotContain("_windup_", view.Current.name);

                float cooldown = character == 0 ? .45f : .70f;
                while (Time.time < started + cooldown - .02f)
                {
                    Assert.IsFalse(combat.TryAttack(), "Repeated switching cannot erase the original recovery");
                    Assert.IsFalse(combat.LocksFacing, "Cancelled preparation never resumes later");
                    yield return null;
                }
                yield return Until(() => combat.State == SKCombatState.Ready, .3f, "Original recovery finishes");
                Assert.GreaterOrEqual(Time.time + .02f, started + cooldown);
                Assert.IsFalse(hitbox.IsActive, "No ghost hitbox after the cancelled attack's full lifetime");
                Assert.IsTrue(combat.TryAttack(), "Selected character can attack after the original cooldown");
                combat.ResetCombat();
                yield return null;
            }
        }
    }

    [UnityTest]
    public IEnumerator SwitchingKeepsSharedHealthDamageStageAndMatchingHudInBothScenes()
    {
        foreach (string scene in Scenes)
        {
            yield return Load(scene);
            hp.Drain(40f);
            for (int i = 0; i < 4; i++)
            {
                yield return Tap(Key.Tab);
                int selected = player.SelectedCharacter;
                Assert.AreEqual(60f, hp.CurrentHp);
                Assert.AreEqual(60f, hud.DisplayedHp);
                Assert.AreEqual(1, hud.DamageStage);
                Assert.AreEqual(.6f, Field<Image>(hud, "hpFill").fillAmount, .001f);
                Assert.AreEqual(selected == 0 ? "CECILIA NOEL" : "ADELA CROW", Field<Text>(hud, "characterName").text);
                var stages = Field<Sprite[]>(hud, selected == 0 ? "ceciliaStagePortraits" : "adelaStagePortraits");
                Sprite expected = SKDay01HUD.StagePortrait(selected, 1) ?? (stages != null && stages.Length > 1 && stages[1] != null
                    ? stages[1] : Field<Sprite>(hud, selected == 0 ? "ceciliaPortrait" : "adelaPortrait"));
                Assert.IsNotNull(expected);
                Assert.AreSame(expected, Field<Image>(hud, "portrait").sprite, "Selected character and HP stage agree");
                StringAssert.StartsWith(selected == 0 ? "cecilia_" : "adela_", view.Refresh().name);
            }
        }
    }

    [UnityTest]
    public IEnumerator SwitchingWhileCrouchedOrAirbornePreservesTheBodyAndMotion()
    {
        var hurt = player.GetComponentInChildren<SKHurtbox>().GetComponent<BoxCollider2D>();
        foreach (int character in new[] { 0, 1 })
        {
            yield return Select(character);
            Hold(Key.S);
            yield return Until(() => player.IsCrouching, .4f, "Crouched");
            float feet = box.bounds.min.y;
            Vector2 crouchSize = box.size, hurtSize = hurt.size;
            Hold(Key.S, Key.Tab);
            yield return null;
            Hold(Key.S);
            yield return null;
            Assert.AreEqual(1 - character, player.SelectedCharacter);
            Assert.IsTrue(player.IsCrouching);
            Assert.AreEqual(crouchSize, box.size);
            Assert.AreEqual(hurtSize, hurt.size);
            Assert.AreEqual(feet, box.bounds.min.y, .01f);
            StringAssert.Contains("_crouch_", view.Refresh().name);
            Hold();
            yield return Until(() => !player.IsCrouching, .3f, "Standing again");

            Hold(Key.Space, Key.D);
            yield return Until(() => !player.IsGrounded && body.linearVelocity.y > 0f, .5f, "Jump rising");
            Vector2 position = body.position, velocity = body.linearVelocity, size = box.size;
            player.SwitchCharacter();
            Assert.AreEqual(character, player.SelectedCharacter);
            Assert.AreEqual(position, body.position, "Switch does not teleport an airborne player");
            Assert.AreEqual(velocity, body.linearVelocity, "Switch does not grant another jump or cancel momentum");
            Assert.AreEqual(size, box.size);
            StringAssert.StartsWith(character == 0 ? "cecilia_" : "adela_", view.Refresh().name);
            Hold();
            yield return Until(() => player.IsGrounded && Mathf.Abs(body.linearVelocity.y) < .1f, 2f, "Landed");
        }
    }

    [UnityTest]
    public IEnumerator SwitchingCannotRemoveSlimeSlowdownAndJumpStillLeavesIt()
    {
        foreach (var spike in Object.FindObjectsByType<SKFloorSpike>(FindObjectsSortMode.None)) spike.enabled = false;
        var pool = Object.FindFirstObjectByType<SKSlimePool>();
        PlaceOnFloor(pool.transform.position);
        yield return Until(() => player.IsGrounded && player.SpeedMultiplier == pool.Slow, 1f, "Standing in slime");
        foreach (int character in new[] { 0, 1 })
        {
            yield return Select(character);
            Hold(Key.D);
            yield return new WaitForSeconds(.1f);
            yield return new WaitForFixedUpdate();
            // Physics applies ground-contact friction after the controller sets its desired speed.
            // Compare actual speed across Tab rather than assuming a frictionless Rigidbody2D.
            float slowedSpeed = body.linearVelocity.x;
            Assert.That(slowedSpeed, Is.GreaterThan(0f).And.LessThanOrEqualTo(1.8f));
            Hold(Key.D, Key.Tab);
            yield return null;
            Hold(Key.D);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(1 - character, player.SelectedCharacter);
            Assert.AreEqual(pool.Slow, player.SpeedMultiplier);
            Assert.AreEqual(slowedSpeed, body.linearVelocity.x, .03f, "Switch preserves actual slowed walking speed");
            Hold();
            yield return null;
        }
        Hold(Key.Space);
        yield return Until(() => !player.IsGrounded, .5f, "Jump out of slime");
        yield return new WaitForFixedUpdate();
        Assert.AreEqual(1f, player.SpeedMultiplier);
        yield return Tap(Key.Tab);
        Assert.AreEqual(1f, player.SpeedMultiplier, "Switching in the air keeps the air exemption");
    }

    [UnityTest]
    public IEnumerator HurtHitStopDeathAndDisabledStatesRejectSwitchInStageAndChapel()
    {
        foreach (string scene in Scenes)
        {
            yield return Load(scene);
            // Keep the explicit death check separate from the automatic retry timer.
            var session = Object.FindFirstObjectByType<SKEnemyTrainingSession>();
            typeof(SKEnemyTrainingSession).GetField("autoRetrySeconds", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(session, 0f);
            foreach (int character in new[] { 0, 1 })
            {
                hp.ResetHealth();
                yield return Select(character);
                Assert.IsTrue(hp.TryTakeDamage(10f, player.transform.position));
                Assert.AreEqual(SKCombatState.Hurt, combat.State);
                player.SwitchCharacter();
                Assert.AreEqual(character, player.SelectedCharacter);
                yield return Tap(Key.Tab, Key.J);
                Assert.AreEqual(character, player.SelectedCharacter);
                Assert.IsFalse(combat.LocksFacing);
                yield return Until(() => !hp.IsStunned, .5f, "Hurt ends");

                SKHitStop.Request(.25f, player.gameObject.scene);
                yield return Tap(Key.Tab, Key.J);
                Assert.IsTrue(SKHitStop.IsActive);
                player.SwitchCharacter();
                Assert.AreEqual(character, player.SelectedCharacter);
                Assert.IsFalse(combat.LocksFacing);
                SKHitStop.CancelActive();

                combat.enabled = false;
                yield return Tap(Key.Tab, Key.J);
                Assert.AreEqual(character, player.SelectedCharacter);
                combat.enabled = true;
                player.enabled = false;
                player.SwitchCharacter();
                Assert.AreEqual(character, player.SelectedCharacter);
                player.enabled = true;
                hp.enabled = false;
                yield return Tap(Key.Tab, Key.J);
                Assert.AreEqual(character, player.SelectedCharacter);
                hp.enabled = true;

                hp.Drain(hp.MaximumHp);
                yield return Tap(Key.Tab, Key.J);
                player.SwitchCharacter();
                Assert.AreEqual(character, player.SelectedCharacter);
                Assert.AreEqual(SKCombatState.Dead, combat.State);
                Assert.IsFalse(hitbox.IsActive);
                hp.ResetHealth();
                yield return Tap(Key.Tab);
                Assert.AreEqual(1 - character, player.SelectedCharacter, "Control returns once the state is restored");
            }
        }
    }

    [UnityTest]
    public IEnumerator FleshWallHoldBlocksSwitchAndResetDuringHitStopRestoresBothCharacters()
    {
        var wall = Object.FindFirstObjectByType<SKFleshWall>();
        var session = Object.FindFirstObjectByType<SKEnemyTrainingSession>();
        foreach (int character in new[] { 0, 1 })
        {
            session.ResetTraining();
            yield return null;
            yield return Select(character);
            PlaceOnFloor(wall.transform.position);
            yield return Until(() => combat.Grab.IsHeld, 2f, "Wall catches selected character");
            float progress = combat.Grab.StruggleProgress;
            yield return Tap(Key.Tab);
            player.SwitchCharacter();
            Assert.AreEqual(character, player.SelectedCharacter);
            Assert.AreSame(wall, combat.Grab.Holder);
            Assert.LessOrEqual(combat.Grab.StruggleProgress, progress, "Tab is not an escape input");
            Assert.IsFalse(combat.TryAttack());
            Assert.IsTrue(player.GetComponent<SpriteRenderer>().forceRenderingOff, "Wall composite owns the held image");

            SKHitStop.Request(.3f, player.gameObject.scene);
            yield return Tap(Key.R);
            Assert.IsFalse(SKHitStop.IsActive);
            Assert.IsFalse(combat.Grab.IsHeld);
            Assert.IsFalse(player.GetComponent<SpriteRenderer>().forceRenderingOff);
            Assert.AreEqual(character, player.SelectedCharacter, "R preserves the selected character");
            Assert.AreEqual(hp.MaximumHp, hp.CurrentHp);
            Assert.AreEqual(hp.CurrentHp, hud.DisplayedHp);
            Assert.AreEqual(SKCombatState.Ready, combat.State);
            yield return Tap(Key.Tab);
            Assert.AreEqual(1 - character, player.SelectedCharacter, "Switch works after release/reset");
        }
    }
}
