using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SaintKnight.Day01;
using SaintKnight.Day02;
using SaintKnight.Day04;
using SaintKnight.Day06;
using SaintKnight.Day10;
using SaintKnight.SpriteCook;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class TrapDedicatedAnimationRulesTests
{
    [Test]
    public void SixAtlasesSupplyAllTwentyThreeDistinctNativeFramesWithTheOriginalScale()
    {
        for (int character = 0; character < 2; character++)
        foreach (SKTrapMotionKind kind in Enum.GetValues(typeof(SKTrapMotionKind)))
        {
            var library = SKTrapAnimationLibrary.Load(character, kind);
            Assert.IsNotNull(library, character + " / " + kind + ": dedicated art and manifest exist");
            var frames = new HashSet<Sprite>();
            for (int stage = 0; stage < 6; stage++)
            {
                float progress = (stage + .25f) / 6f;
                Assert.AreEqual(stage, SKTrapAnimationLibrary.StageFor(progress));
                frames.Add(library.Sample(SKTrapMotionPhase.Swallow, progress, 0f));
                frames.Add(library.Sample(SKTrapMotionPhase.Resist, progress, .01f));
                frames.Add(library.Sample(SKTrapMotionPhase.Resist, progress, .10f));
            }
            for (int frame = 0; frame < 3; frame++) frames.Add(library.Sample(SKTrapMotionPhase.Escape, 0f, frame * .1f + .01f));
            frames.Add(library.Sample(SKTrapMotionPhase.Consumed, 1f, .01f));
            frames.Add(library.Sample(SKTrapMotionPhase.Consumed, 1f, .19f));
            Assert.AreEqual(23, frames.Count);
            foreach (var frame in frames)
            {
                Assert.IsTrue(library.Owns(frame));
                Assert.AreEqual(48f, frame.pixelsPerUnit);
                Assert.AreEqual(kind == SKTrapMotionKind.Wall ? 160 : 128, frame.rect.width);
                Assert.AreEqual(kind == SKTrapMotionKind.Wall ? 112 : 96, frame.rect.height);
                Assert.AreEqual(kind == SKTrapMotionKind.Wall ? 0f : 25.56f, frame.pivot.y, .001f);
            }
            // Compare PNG pixels as well as Sprite references, even when the imported atlas is not CPU-readable.
            string characterName = character == 0 ? "cecilia" : "adela";
            string path = Application.dataPath + "/SaintKnightDay10/Resources/SaintKnightDay10/TrapMotion/" +
                characterName + "_" + kind.ToString().ToLowerInvariant() + ".png";
            var readable = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.IsTrue(readable.LoadImage(System.IO.File.ReadAllBytes(path)));
                var pixels = readable.GetPixels32();
                var hashes = new HashSet<ulong>();
                foreach (var frame in frames)
                {
                    Rect rect = frame.rect;
                    ulong hash = 1469598103934665603UL;
                    unchecked
                    {
                        for (int y = (int)rect.y; y < rect.yMax; y++)
                        for (int x = (int)rect.x; x < rect.xMax; x++)
                        {
                            var color = pixels[y * readable.width + x];
                            hash = (hash ^ color.r) * 1099511628211UL;
                            hash = (hash ^ color.g) * 1099511628211UL;
                            hash = (hash ^ color.b) * 1099511628211UL;
                            hash = (hash ^ color.a) * 1099511628211UL;
                        }
                    }
                    hashes.Add(hash);
                }
                Assert.AreEqual(23, hashes.Count, characterName + " / " + kind + ": every supplied frame changes pixels");
            }
            finally { Object.DestroyImmediate(readable); }
        }
        Assert.IsNull(SKTrapAnimationLibrary.Load(2, SKTrapMotionKind.Slime), "Unsupported/missing art has a null fallback");
    }

    [Test]
    public void ResistanceRetainsTheCurrentDepthThenEasesBackWithoutChangingTheGameProgress()
    {
        var library = SKTrapAnimationLibrary.Load(0, SKTrapMotionKind.Slime);
        Assert.IsNotNull(library);
        var playback = new SKTrapAnimationPlayback();
        Sprite deep = playback.Held(library, .8f, float.NegativeInfinity, 10f);
        Assert.AreSame(library.Sample(SKTrapMotionPhase.Swallow, .8f, 10f), deep);
        Sprite resist0 = playback.Held(library, 0f, 10.01f, 10.01f);
        Sprite resist1 = playback.Held(library, .025f, 10.01f, 10.11f);
        Assert.AreSame(library.Sample(SKTrapMotionPhase.Resist, .8f, 0f), resist0);
        Assert.AreSame(library.Sample(SKTrapMotionPhase.Resist, .8f, .1f), resist1);
        Assert.AreNotSame(resist0, resist1);
        Assert.AreEqual(.8f, playback.VisualProgress, .0001f, "Resisting does not abruptly expose the whole body");
        playback.Held(library, .1f, 10.01f, 10.4f);
        Assert.That(playback.VisualProgress, Is.GreaterThan(.5f).And.LessThan(.8f));
        Sprite frozen = playback.Held(library, .1f, 10.01f, 10.4f);
        Assert.AreSame(frozen, playback.Held(library, .1f, 10.01f, 10.4f), "The same scaled time is the same frame");
        playback.Reset();
        Assert.AreSame(library.Sample(SKTrapMotionPhase.Swallow, 0f, 0f),
            playback.Held(library, 0f, float.NegativeInfinity, 20f));
    }
}

/// <summary>Dedicated art follows the real capture state and existing player input in the current stage.</summary>
public sealed class TrapDedicatedAnimationTests
{
    private Keyboard keyboard;
    private InputSettings.BackgroundBehavior previousBackground;
#if UNITY_EDITOR
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
#endif
    private float previousCaptureDelta;
    private SKDay01Player player;
    private SKPlayerCombat combat;
    private SKHealth health;
    private SKGrabState grab;
    private SKCharacterSpriteView view;
    private SKTrapEnvelopeView envelope;
    private SpriteRenderer bodyView;
    private Rigidbody2D body;
    private BoxCollider2D box;
    private SKEnemyTrainingSession session;
    private SKFleshWall wall;
    private SKSlimePool pool;
    private SKFloorSpike spike;
    private SKTrapMotionKind activeKind;
    private SKEnvironmentalCapture activeCapture;

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
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        keyboard = InputSystem.AddDevice<Keyboard>();
        yield return SceneManager.LoadSceneAsync("Day09_Stage1");
        yield return null;
        player = Object.FindFirstObjectByType<SKDay01Player>();
        combat = player.GetComponent<SKPlayerCombat>();
        grab = combat.Grab;
        health = player.GetComponent<SKHealth>();
        view = player.GetComponent<SKCharacterSpriteView>();
        envelope = player.GetComponent<SKTrapEnvelopeView>();
        bodyView = player.GetComponent<SpriteRenderer>();
        body = player.GetComponent<Rigidbody2D>();
        box = player.GetComponent<BoxCollider2D>();
        session = Object.FindFirstObjectByType<SKEnemyTrainingSession>();
        wall = Object.FindFirstObjectByType<SKFleshWall>();
        pool = Object.FindFirstObjectByType<SKSlimePool>();
        spike = Object.FindFirstObjectByType<SKFloorSpike>();
        foreach (var enemy in Object.FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None)) enemy.gameObject.SetActive(false);
        foreach (var trap in Object.FindObjectsByType<SKFloorSpike>(FindObjectsSortMode.None)) trap.enabled = false;
        pool.enabled = false;
        typeof(SKEnemyTrainingSession).GetField("autoRetrySeconds", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(session, 0f);
        yield return Until(() => player.IsGrounded, 2f, "Settled");
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

    private static IEnumerator Until(Func<bool> condition, float seconds, string message)
    {
        float end = Time.time + seconds, realEnd = Time.realtimeSinceStartup + 30f;
        while (!condition() && Time.time < end && Time.realtimeSinceStartup < realEnd) yield return null;
        Assert.IsTrue(condition(), message);
    }

    private void Hold(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
    private IEnumerator Tap(Key key) { Hold(key); yield return null; Hold(); yield return null; }
    private float Progress => activeKind == SKTrapMotionKind.Wall ? wall.AbsorptionProgress : activeCapture.AbsorptionProgress;
    private bool Consumed => activeKind == SKTrapMotionKind.Wall ? wall.IsPlayerConsumed : activeCapture.IsPlayerConsumed;
    private Sprite Shown()
    {
        view.Refresh();
        if (activeKind == SKTrapMotionKind.Wall) return wall.GetComponent<SpriteRenderer>().sprite;
        if (view.UsesNativeMotion) return Consumed ? envelope.Current : view.Current;
        return Consumed ? envelope.Current : view.Current;
    }
    private SKTrapAnimationLibrary Library => SKTrapAnimationLibrary.Load(player.SelectedCharacter, activeKind);
    private SKNativeDefeatLibrary Native => SKNativeDefeatLibrary.Load(player.SelectedCharacter,
        activeKind == SKTrapMotionKind.Wall ? "fleshwall" : activeKind == SKTrapMotionKind.Tentacle ? "tentacle" :
        activeKind == SKTrapMotionKind.BossSlime ? "boss_spit" : null, defeat: health.IsDead);
    private bool OwnsDrawn(Sprite sprite) => Library.Owns(sprite) || Native != null && sprite != null && sprite.texture == Native.Atlas;

    private IEnumerator Begin(int character, SKTrapMotionKind kind)
    {
        Hold();
        SKHitStop.CancelActive();
        session.ResetTraining();
        pool.enabled = false;
        spike.enabled = false;
        pool.Capture.enabled = true;
        spike.Capture.enabled = true;
        wall.enabled = true;
        view.enabled = true;
        yield return null;
        if (player.SelectedCharacter != character) yield return Tap(Key.Tab);
        activeKind = kind;
        // The boss's spit is the same pool component, only timed instead of permanent.
        typeof(SKSlimePool).GetField("permanent", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(pool, kind != SKTrapMotionKind.BossSlime);
        Transform target = kind == SKTrapMotionKind.Wall ? wall.transform : pool.transform;
        var at = new Vector2(target.position.x, target.position.y - (box.bounds.min.y - body.position.y) + .02f);
        player.transform.position = new Vector3(at.x, at.y, player.transform.position.z);
        body.position = at;
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        yield return Until(() => player.IsGrounded, .5f, "On the trap floor");
        if (kind == SKTrapMotionKind.Wall)
        {
            activeCapture = null;
            yield return Until(() => wall.IsHoldingPlayer, 2f, "Wall captures");
        }
        else
        {
            activeCapture = kind == SKTrapMotionKind.Tentacle ? spike.Capture : pool.Capture;
            Assert.IsTrue(activeCapture.TryCapture(player), "Use the real trap-side hold without changing its clock");
        }
        yield return null;
        Assert.IsNotNull(Library);
        Assert.IsTrue(OwnsDrawn(Shown()));
    }

    [UnityTest]
    public IEnumerator BothCharactersAdvanceThroughAllSixDepthsAndTwoConsumedFramesInEveryTrap()
    {
        for (int character = 0; character < 2; character++)
        foreach (SKTrapMotionKind kind in Enum.GetValues(typeof(SKTrapMotionKind)))
        {
            yield return Begin(character, kind);
            float started = grab.HeldSince;
            for (int stage = 0; stage < 6; stage++)
            {
                int expected = stage;
                yield return Until(() => Progress >= (expected + .05f) / 6f, 1f, "Next absorption depth");
                if (Native != null)
                    yield return Until(() => Shown() == Native.Held(Progress, Time.time - grab.HeldSince, false), .2f, "Real HP loss selects the native depth");
                else yield return Until(() => Shown().name.Contains("_swallow_f" + expected.ToString("D2")), .2f, "Real progress selects the drawn depth");
                Assert.AreEqual(1f - health.CurrentHp / health.MaximumHp, Progress, .001f,
                    "Day 15: the drawn depth is the HP already lost");
                if (Native != null && kind != SKTrapMotionKind.Wall) Assert.IsFalse(envelope.IsVisible, "The body owns the complete native composite while alive");
                else if (kind != SKTrapMotionKind.Wall) Assert.IsFalse(envelope.IsVisible, "No procedural layer over a complete dedicated composite");
            }
            yield return Until(() => Consumed, .8f, "Four-second absorption completes");
            Assert.GreaterOrEqual(Time.time - started, 3.95f);
            Assert.AreEqual(0f, health.CurrentHp);
            Assert.IsTrue(bodyView.forceRenderingOff);
            var frames = new HashSet<Sprite>();
            float end = Time.time + .42f;
            while (Time.time < end) { frames.Add(Shown()); yield return null; }
            if (Native != null) Assert.GreaterOrEqual(frames.Count, 3, "The new consumed scene continues moving");
            else Assert.AreEqual(2, frames.Count, "Existing slime still pulses with its original two frames");
            foreach (var frame in frames) Assert.IsTrue(OwnsDrawn(frame));
        }
    }

    [UnityTest]
    public IEnumerator ActualResistanceAndSuccessfulEscapeUseTheirDedicatedFramesForAllSixComposites()
    {
        for (int character = 0; character < 2; character++)
        foreach (SKTrapMotionKind kind in Enum.GetValues(typeof(SKTrapMotionKind)))
        {
            yield return Begin(character, kind);
            yield return Until(() => Progress > .5f, 2.3f, "Reach a visible depth before resistance");
            Sprite deep = Shown();
            yield return Tap(Key.J);
            Assert.Greater(Progress, .5f, "Day 15: a press pauses the HP loss without giving HP back");
            var resistance = new HashSet<Sprite>();
            float resistEnd = grab.LastStruggleAt + .32f;
            while (Time.time < resistEnd) { resistance.Add(Shown()); yield return null; }
            Assert.AreEqual(2, resistance.Count);
            foreach (var frame in resistance)
            {
                Assert.IsTrue(OwnsDrawn(frame));
                if (Native != null) StringAssert.Contains("_native_", frame.name);
                else { StringAssert.Contains("_resist_stage3_", frame.name); Assert.AreNotSame(deep, frame); }
            }
            for (int i = 0; i < 16 && grab.IsHeld && !grab.EscapeReady; i++) yield return Tap(i % 2 == 0 ? Key.Space : Key.J);
            yield return Until(() => !grab.IsHeld, 1f, "Existing mash escape");
            Assert.IsTrue(health.IsInvulnerable);
            yield return Until(() => Shown().name.Contains("_escape_f"), .1f, "Escape is rendered after the owning update");
            var escape = new HashSet<Sprite>();
            float escapeAt = grab.EscapedAt;
            while (Time.time < escapeAt + .29f) { escape.Add(Shown()); yield return null; }
            Assert.AreEqual(3, escape.Count);
            foreach (var frame in escape) StringAssert.Contains("_escape_f", frame.name);
            yield return Until(() => Time.time >= escapeAt + .34f, .2f, "Dedicated escape completes");
            Assert.IsFalse(Library.Owns(Shown()), "A completed escape does not leave a trap frame on the player");
        }
    }

    [UnityTest]
    public IEnumerator HitStopFreezesTheSelectedFrameAndTheAuthoritativeProgressTogether()
    {
        foreach (SKTrapMotionKind kind in Enum.GetValues(typeof(SKTrapMotionKind)))
        {
            yield return Begin(0, kind);
            yield return Until(() => Progress > .3f, 1.5f, "Some depth");
            Sprite before = Shown();
            float progress = Progress, hp = health.CurrentHp;
            for (int i = 0; i < 5; i++)
            {
                SKHitStop.Request(.1f, player.gameObject.scene);
                yield return null;
                Assert.AreSame(before, Shown());
                Assert.AreEqual(progress, Progress, .00001f);
                Assert.AreEqual(hp, health.CurrentHp);
            }
            SKHitStop.CancelActive();
        }
    }

    [UnityTest]
    public IEnumerator ResetDisableAndCharacterSwitchCannotLeaveDedicatedFramesBehind()
    {
        foreach (SKTrapMotionKind kind in Enum.GetValues(typeof(SKTrapMotionKind)))
        {
            yield return Begin(0, kind);
            var library = Library;
            view.enabled = false;
            Assert.IsFalse(library.Owns(bodyView.sprite), "Disabling the presentation adapter restores a native base pose");
            Assert.IsFalse(envelope.IsVisible);
            view.enabled = true;
            Assert.IsTrue(OwnsDrawn(Shown()));
            yield return Tap(Key.R);
            view.Refresh();
            Assert.IsFalse(library.Owns(bodyView.sprite));
            Assert.IsFalse(envelope.IsVisible);
            Assert.IsFalse(bodyView.forceRenderingOff);
            Assert.AreEqual(health.MaximumHp, health.CurrentHp);
            yield return Tap(Key.Tab);
            Assert.AreEqual(1, player.SelectedCharacter);
            Assert.IsFalse(library.Owns(view.Refresh()));

            yield return Begin(1, kind);
            yield return Until(() => Consumed, 4.3f, "Complete absorption before cleanup");
            if (kind == SKTrapMotionKind.Wall) wall.enabled = false;
            else activeCapture.enabled = false;
            yield return null;
            view.Refresh();
            Assert.IsFalse(envelope.IsVisible);
            Assert.IsFalse(bodyView.forceRenderingOff);

            yield return Begin(0, kind);
            for (int i = 0; i < 16 && grab.IsHeld && !grab.EscapeReady; i++) yield return Tap(i % 2 == 0 ? Key.Space : Key.J);
            yield return Until(() => !grab.IsHeld, 1f, "Escape before a new committed action");
            Assert.IsTrue(combat.TryAttack(), "A fresh attack is still allowed immediately after escape");
            StringAssert.StartsWith("cecilia_windup_", view.Refresh().name);
            yield return null;
            Assert.IsFalse(bodyView.forceRenderingOff, "A wall escape cannot keep the new attack hidden");
            player.SwitchCharacter();
            Assert.AreEqual(1, player.SelectedCharacter);
            Assert.IsFalse(SKTrapAnimationLibrary.Load(0, kind).Owns(view.Refresh()), "Switch cancels the former character's escape presentation");
        }
    }
}
