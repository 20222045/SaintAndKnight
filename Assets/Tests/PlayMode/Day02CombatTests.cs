using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SaintKnight.Day01;
using SaintKnight.Day02;

public sealed class Day02CombatTests
{
    private Keyboard keyboard;
    private SKDay01Player player;
    private SKPlayerCombat combat;
    private SKHealth playerHealth;
    private SKHealth targetHealth;
    private SKTrainingTarget target;
    private SKDay01HUD hud;
    private InputSettings.BackgroundBehavior previousBackgroundBehavior;
#if UNITY_EDITOR
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInputBehavior;
#endif

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
        previousEditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        keyboard = InputSystem.AddDevice<Keyboard>();
        yield return SceneManager.LoadSceneAsync("Day02_CombatTraining");
        yield return null;
        player = Object.FindFirstObjectByType<SKDay01Player>();
        combat = player.GetComponent<SKPlayerCombat>();
        playerHealth = player.GetComponent<SKHealth>();
        target = Object.FindFirstObjectByType<SKTrainingTarget>();
        targetHealth = target.GetComponent<SKHealth>();
        target.AttacksEnabled = false;
        hud = Object.FindFirstObjectByType<SKDay01HUD>();
        yield return new WaitForSeconds(.3f);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
#if UNITY_EDITOR
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInputBehavior;
#endif
        yield return null;
    }

    private IEnumerator Press(params Key[] keys)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        // Headless rendering can advance several frames before a single physics step.
        // Hold the input across fixed ticks before asserting movement or facing.
        yield return new WaitForSeconds(.06f);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
    }

    private void PositionForHit(float side = -1f)
    {
        player.transform.position = target.transform.position + new Vector3(side * .85f, 0f, 0f);
        player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }

    [UnityTest]
    public IEnumerator MovementJumpSwitchAndAttackUseRealKeyboardInput()
    {
        float x = player.transform.position.x;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
        yield return new WaitForSeconds(.15f);
        Assert.Greater(player.transform.position.x, x + .15f, "D movement");
        // Compare with where D stopped: a long frame can stretch the D hold, so the start point is not a fair mark.
        float right = player.transform.position.x;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
        yield return new WaitForSeconds(.15f);
        Assert.Less(player.transform.position.x, right - .15f, "A movement");
        yield return Press(Key.D);
        float y = player.transform.position.y;
        yield return Press(Key.Space, Key.J);
        Assert.Greater(player.transform.position.y, y, "Jump while attacking");
        Assert.IsTrue(combat.IsAttacking);
        yield return Press(Key.Tab);
        Assert.AreEqual(1, player.SelectedCharacter);
        Assert.IsFalse(combat.IsAttacking, "Switch cancels the previous attack");
        Assert.AreEqual(playerHealth.CurrentHp, hud.DisplayedHp);
        yield return new WaitForSeconds(.6f);
        PositionForHit();
        yield return Press(Key.J);
        yield return new WaitForSeconds(.25f);
        Assert.AreEqual(125f, targetHealth.CurrentHp, "Adela attack input damages target");
    }

    [UnityTest]
    public IEnumerator EachCharacterHitsOncePerSwingAndCooldownSurvivesSwitch()
    {
        PositionForHit();
        Assert.IsTrue(combat.TryAttack());
        Assert.IsFalse(combat.TryAttack(), "No attack spam");
        yield return new WaitForSeconds(.25f);
        Assert.AreEqual(130f, targetHealth.CurrentHp, "Cecilia hits once across multiple physics ticks");
        player.SwitchCharacter();
        Assert.IsFalse(combat.TryAttack(), "Switch must not reset cooldown");
        yield return new WaitForSeconds(.3f);
        Assert.IsTrue(combat.TryAttack());
        yield return new WaitForSeconds(.25f);
        Assert.AreEqual(105f, targetHealth.CurrentHp, "Adela profile hits once");
        Assert.AreEqual(100f, playerHealth.CurrentHp, "Own hitbox cannot hurt its owner");
    }

    [UnityTest]
    public IEnumerator FacingAndRangeAreRespected()
    {
        PositionForHit(1f);
        Assert.IsTrue(combat.TryAttack());
        yield return new WaitForSeconds(.5f);
        Assert.AreEqual(150f, targetHealth.CurrentHp, "Facing away must miss");
        yield return Press(Key.A);
        Assert.AreEqual(-1f, player.Facing);
        Assert.IsTrue(combat.TryAttack());
        yield return new WaitForSeconds(.25f);
        Assert.AreEqual(130f, targetHealth.CurrentHp, "Facing left hits left target");
        yield return new WaitForSeconds(.25f);
        player.transform.position = new Vector3(-4f, -2.15f, 0f);
        Physics2D.SyncTransforms();
        Assert.IsTrue(combat.TryAttack());
        yield return new WaitForSeconds(.25f);
        Assert.AreEqual(130f, targetHealth.CurrentHp, "Out of range must miss");
    }

    [UnityTest]
    public IEnumerator EnemyStrikeDamagesPlayerAndSynchronizesHud()
    {
        PositionForHit();
        target.AttacksEnabled = true;
        target.ResetCombat();
        yield return new WaitForSeconds(1.65f);
        Assert.AreEqual(85f, playerHealth.CurrentHp, "One enemy strike, not repeated overlaps");
        Assert.AreEqual(85f, hud.DisplayedHp);
        Assert.AreEqual("HP 85 / 100", GameObject.Find("HP_TEXT").GetComponent<Text>().text);
        var bar = GameObject.Find("HP_FILL").GetComponent<Image>();
        Assert.AreEqual(.85f, bar.fillAmount, .001f);
        Assert.IsNotNull(bar.sprite, "uGUI cannot draw a partially filled bar without a sprite");
        Assert.AreEqual(Image.Type.Filled, bar.type);
        Assert.AreEqual(Image.FillMethod.Horizontal, bar.fillMethod);
        // Check generated UI geometry, not only the stored fillAmount value.
        Canvas.ForceUpdateCanvases();
        var mesh = new Mesh();
        using (var vertices = new VertexHelper())
        {
            typeof(Image).GetMethod("OnPopulateMesh", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic, null, new[] { typeof(VertexHelper) }, null)
                .Invoke(bar, new object[] { vertices });
            vertices.FillMesh(mesh);
        }
        mesh.RecalculateBounds();
        Assert.AreEqual(bar.rectTransform.rect.width * .85f, mesh.bounds.size.x, .1f);
        Object.Destroy(mesh);
        Assert.Greater(Mathf.Abs(player.transform.position.x - (target.transform.position.x - .85f)), .05f,
            "Enemy strike produces knockback");
        target.AttacksEnabled = false;
        yield return new WaitForSeconds(.2f);
        player.SwitchCharacter();
        Assert.AreEqual(85f, playerHealth.CurrentHp, "Switch must preserve health");
        Assert.AreEqual(85f, hud.DisplayedHp);
    }

    [UnityTest]
    public IEnumerator HitStunImmunityDeathAndReset()
    {
        PositionForHit();
        Assert.IsTrue(combat.TryAttack());
        Assert.IsTrue(playerHealth.TryTakeDamage(25f, target.transform.position));
        Assert.IsTrue(playerHealth.IsStunned);
        Assert.IsFalse(combat.IsAttacking, "Taking damage interrupts attack immediately");
        Assert.IsFalse(combat.TryAttack());
        Assert.IsFalse(playerHealth.TryTakeDamage(25f, target.transform.position));
        Assert.AreEqual(75f, hud.DisplayedHp);
        Assert.AreEqual(1, hud.DamageStage);
        yield return new WaitForSeconds(.6f);
        Assert.IsTrue(playerHealth.TryTakeDamage(1000f, target.transform.position));
        Assert.AreEqual(0f, playerHealth.CurrentHp);
        Assert.AreEqual(4, hud.DamageStage);
        Assert.IsFalse(combat.TryAttack());
        yield return Press(Key.R);
        Assert.AreEqual(100f, playerHealth.CurrentHp);
        Assert.AreEqual(150f, targetHealth.CurrentHp);
        Assert.AreEqual(100f, hud.DisplayedHp);
        Assert.IsFalse(playerHealth.IsInvulnerable);
        Assert.IsTrue(combat.TryAttack());
        yield return Press(Key.Q);
        Assert.AreEqual(75f, playerHealth.CurrentHp, "Legacy Q control affects real health");
        yield return Press(Key.E);
        Assert.AreEqual(100f, playerHealth.CurrentHp, "Legacy E control affects real health");
    }

    [UnityTest]
    public IEnumerator DefeatedTargetCannotAttackAndCanBeReset()
    {
        PositionForHit();
        target.AttacksEnabled = true;
        Assert.IsTrue(targetHealth.TryTakeDamage(999f, player.transform.position));
        yield return new WaitForSeconds(2f);
        Assert.IsTrue(targetHealth.IsDead);
        Assert.AreEqual(100f, playerHealth.CurrentHp);
        yield return Press(Key.R);
        Assert.IsFalse(targetHealth.IsDead);
        Assert.AreEqual(150f, targetHealth.CurrentHp);
    }

    [UnityTest]
    public IEnumerator OriginalDay01SceneStillSupportsMovementJumpSwitchAndDebugHud()
    {
        yield return SceneManager.LoadSceneAsync("Day01_Prototype");
        yield return new WaitForSeconds(.3f);
        player = Object.FindFirstObjectByType<SKDay01Player>();
        hud = Object.FindFirstObjectByType<SKDay01HUD>();
        Assert.IsNull(player.GetComponent<SKPlayerCombat>());
        float x = player.transform.position.x;
        yield return Press(Key.D);
        Assert.Greater(player.transform.position.x, x);
        float y = player.transform.position.y;
        yield return Press(Key.Space);
        Assert.Greater(player.transform.position.y, y);
        yield return Press(Key.Tab);
        Assert.AreEqual(1, player.SelectedCharacter);
        yield return Press(Key.Q);
        Assert.AreEqual(75f, hud.DisplayedHp);
        yield return Press(Key.E);
        Assert.AreEqual(100f, hud.DisplayedHp);
    }
}
