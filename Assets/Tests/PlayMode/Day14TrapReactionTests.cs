using System;
using System.Collections;
using System.Linq;
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

// Trap presentation follows the original poses and the current capture/escape rules.
public sealed class Day14TrapReactionTests
{
    private const float Ppu = 48f, Left = -6.6666665f;
    private static readonly float Floor = (180f - 310f) / Ppu;

    private Keyboard keyboard;
    private InputSettings.BackgroundBehavior previousBackground;
#if UNITY_EDITOR
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
#endif
    private SKDay01Player player;
    private SKHealth hp;
    private Rigidbody2D body;
    private BoxCollider2D box;
    private SpriteRenderer view;
    private SKFloorSpike spike;
    private float previousCaptureDelta;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        previousCaptureDelta = Time.captureDeltaTime;
        Time.captureDeltaTime = 1f / 60f; // Observe the authored .16-second impact window without frame skips.
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
        hp = player.GetComponent<SKHealth>();
        body = player.GetComponent<Rigidbody2D>();
        box = player.GetComponent<BoxCollider2D>();
        view = player.GetComponent<SpriteRenderer>();
        spike = Object.FindObjectsByType<SKFloorSpike>(FindObjectsSortMode.None).OrderBy(s => s.name).First();
        foreach (var e in Object.FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None)) e.gameObject.SetActive(false);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        SKHitStop.CancelActive();
        Time.timeScale = 1f;
        Time.captureDeltaTime = previousCaptureDelta;
        InputSystem.RemoveDevice(keyboard);
        InputSystem.settings.backgroundBehavior = previousBackground;
#if UNITY_EDITOR
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
#endif
        yield return null;
    }

    [UnityTest]
    public IEnumerator SlimeSwapsIdleAndRunForBothCharactersOnlyWhileStandingInIt()
    {
        Place(1210f);
        yield return Until(() => Shows("cecilia_slimeidle_f"), .5f, "Cecilia slime idle");
        Hold(Key.D);
        yield return Until(() => Shows("cecilia_slimewalk_f"), .5f, "Cecilia slime walk");
        Assert.AreEqual(.5f, player.SpeedMultiplier, .001f, "the slow itself is unchanged");
        Hold();

        Place(900f); // Leave the pool before changing character; the capture dwell must restart.
        yield return new WaitForFixedUpdate();
        player.SwitchCharacter();
        Place(1210f);
        yield return Until(() => Shows("adela_slimeidle_f"), .5f, "Adela slime idle");

        Hold(Key.Space);
        yield return Until(() => !player.IsGrounded, .5f, "jump");
        yield return null;
        Assert.IsTrue(Shows("adela_jump_f"), "airborne over the pool keeps the jump art: " + view.sprite.name);
        Hold();
        yield return Until(() => player.IsGrounded, 2f, "landing");

        Place(900f);
        yield return Until(() => Shows("adela_idle_f"), .5f, "plain idle outside the pool");
    }

    [UnityTest]
    public IEnumerator FloorTentacleHitShowsItsOwnPoseAndOtherHitsKeepTheOrdinaryOne()
    {
        yield return Until(() => spike.IsTelling && spike.StrikeAt - Time.time > .4f, 4f, "crack swells");
        Place((spike.transform.position.x - Left) * Ppu);
        float start = hp.CurrentHp;
        yield return Until(() => hp.CurrentHp < start, 1.5f, "tentacle erupts");
        var dedicated = SKTrapAnimationLibrary.Load(0, SKTrapMotionKind.Tentacle);
        var native = SaintKnight.SpriteCook.SKNativeDefeatLibrary.Load(0, "tentacle");
        yield return Until(() => Shows("cecilia_native_tentacle_f") || Shows(dedicated != null ? "cecilia_tentacle_swallow_f00" : "cecilia_spikehurt_f"),
            .1f, "tentacle entry pose, was " + view.sprite.name);
        if (native != null) Assert.AreSame(native.Atlas, view.sprite.texture, "Native entry uses the original-pixel composite atlas");
        else if (dedicated != null) Assert.IsTrue(dedicated.Owns(view.sprite), "Dedicated entry art uses the trap atlas");
        Assert.AreEqual(start - spike.Damage, hp.CurrentHp, .01f, "damage unchanged");

        var grab = player.GetComponent<SKPlayerCombat>().Grab;
        Assert.AreSame(spike.Capture, grab.Holder, "An accepted tentacle hit owns the restraint");
        yield return Until(() => !SKHitStop.IsActive, .5f, "accepted hit-stop finishes before struggle input");
        for (int i = 0; i < 14 && grab.IsHeld && !grab.EscapeReady; i++)
            yield return Tap(i % 2 == 0 ? Key.Space : Key.J);
        yield return Until(() => !grab.IsHeld, 2f, "mash free of the tentacle before walking away");
        Place(900f);                                              // far from both cracks
        yield return Until(() => !hp.IsInvulnerable && !hp.IsStunned, 2f, "recovered");
        Assert.IsTrue(hp.TryTakeDamage(5f, body.position + Vector2.right));
        yield return Until(() => Shows("cecilia_hurt_f"), .1f, "ordinary hurt pose, was " + view.sprite.name);
    }

    private bool Shows(string prefix) =>
        view.sprite != null && view.sprite.name.StartsWith(prefix, StringComparison.Ordinal);

    private void Place(float px)
    {
        var at = new Vector2(Left + px / Ppu, Floor - (box.bounds.min.y - body.position.y) + .02f);
        player.transform.position = new Vector3(at.x, at.y, player.transform.position.z);
        body.position = at;
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }

    private void Hold(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));

    private IEnumerator Tap(Key key)
    {
        Hold(key);
        yield return null;
        Hold();
        yield return null;
    }

    private static IEnumerator Until(Func<bool> condition, float seconds, string what)
    {
        float end = Time.time + seconds, realEnd = Time.realtimeSinceStartup + 30f;
        yield return null;
        while (!condition() && Time.time < end && Time.realtimeSinceStartup < realEnd) yield return null;
        Assert.That(condition(), Is.True, what + " not reached within " + seconds + "s");
    }
}
