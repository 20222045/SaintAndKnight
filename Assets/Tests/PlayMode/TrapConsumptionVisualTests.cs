using System;
using System.Collections;
using System.IO;
using System.Text;
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
using Object = UnityEngine.Object;

/// <summary>One explicit graphics test records all 23 dedicated frames for each native character/trap pair.</summary>
public sealed class TrapConsumptionVisualTests
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
    private Rigidbody2D body;
    private BoxCollider2D box;
    private SKEnemyTrainingSession session;
    private SKCharacterSpriteView view;
    private SKTrapEnvelopeView envelope;
    private SKFloorSpike[] spikes;
    private SKSlimePool pool;
    private SKFleshWall wall;
    private SKEnvironmentalCapture captureOwner;
    private SKTrapMotionKind currentKind;
    private SKTrapAnimationLibrary library;
    private readonly StringBuilder index = new StringBuilder();
    private int captures;
    private string folder;
    private static readonly float[] ProgressSamples = { 0f, .18f, .35f, .52f, .70f, .88f };

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        SKHitStop.CancelActive();
        Time.timeScale = 1f;
        previousCaptureDelta = Time.captureDeltaTime;
        Time.captureDeltaTime = 1f/60f;
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
        health = player.GetComponent<SKHealth>();
        body = player.GetComponent<Rigidbody2D>();
        box = player.GetComponent<BoxCollider2D>();
        session = Object.FindFirstObjectByType<SKEnemyTrainingSession>();
        view = player.GetComponent<SKCharacterSpriteView>();
        envelope = player.GetComponent<SKTrapEnvelopeView>();
        spikes = Object.FindObjectsByType<SKFloorSpike>(FindObjectsSortMode.None);
        pool = Object.FindFirstObjectByType<SKSlimePool>();
        wall = Object.FindFirstObjectByType<SKFleshWall>();
        foreach (var enemy in Object.FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None)) enemy.gameObject.SetActive(false);
        string run = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
        folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/TrapDedicatedMotion/Captures/" + run));
        Assert.IsFalse(Directory.Exists(folder), "A graphics run never overwrites existing evidence");
        Directory.CreateDirectory(folder);
        captures = 0;
        index.Clear();
        index.AppendLine("# Dedicated trap motion: actual-scene frame captures\n");
        index.AppendLine("Two native characters x three traps x 23 dedicated frames. Graphics evidence is separate from manual feel review.\n");
        index.AppendLine("| Capture | Actual displayed sprite | Authoritative progress | PNG |");
        index.AppendLine("| --- | --- | --- | --- |");
        yield return Until(() => player.IsGrounded, 2f, "player settles");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        SKHitStop.CancelActive();
        Time.timeScale = 1f;
        if (session != null) session.ResetTraining();
        Time.captureDeltaTime = previousCaptureDelta;
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        InputSystem.settings.backgroundBehavior = previousBackground;
#if UNITY_EDITOR
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
#endif
        yield return null;
    }

    [UnityTest, Explicit("Graphics required; writes all 138 dedicated native trap-motion frame captures")]
    public IEnumerator CaptureNativeDotCaptureResistanceAndConsumption()
    {
        foreach (int character in new[] {0,1})
        {
            foreach (var kind in new[] { SKTrapMotionKind.Slime, SKTrapMotionKind.Tentacle, SKTrapMotionKind.Wall })
            {
                string name = (character == 0 ? "cecilia" : "adela") + "_" + kind.ToString().ToLowerInvariant();
                yield return BeginCapture(character, kind);
                yield return CaptureSwallowAndConsumed(name);
                // Each visual depth gets a separate real hold starting from full HP.
                for (int stage = 0; stage < ProgressSamples.Length; stage++)
                {
                    yield return BeginCapture(character, kind);
                    yield return CaptureResistancePair(name, stage);
                }
                yield return BeginCapture(character, kind);
                yield return CaptureEscape(name);
                yield return ResetAndSelect(character);
                Assert.IsFalse(library.Owns(view.Refresh()), "R removes the dedicated body frame");
                Assert.IsFalse(library.Owns(wall.GetComponent<SpriteRenderer>().sprite), "R removes the dedicated wall frame");
                Assert.IsFalse(player.GetComponent<SpriteRenderer>().forceRenderingOff);
                if (envelope != null) Assert.IsFalse(envelope.IsVisible, "R removes the consumed foreground");
            }
        }
        Assert.AreEqual(138, captures, "23 frames for each of two characters and three trap kinds");
        string indexPath = Path.Combine(folder, "index.md");
        Assert.IsFalse(File.Exists(indexPath));
        File.WriteAllText(indexPath, index.ToString());
        Debug.Log("TRAP_DEDICATED_MOTION_CAPTURE_FOLDER=" + folder);
    }

    private float Progress => currentKind == SKTrapMotionKind.Wall ? wall.AbsorptionProgress : captureOwner.AbsorptionProgress;
    private bool Consumed => currentKind == SKTrapMotionKind.Wall ? wall.IsPlayerConsumed : captureOwner.IsPlayerConsumed;
    private SaintKnight.SpriteCook.SKNativeDefeatLibrary Native => SaintKnight.SpriteCook.SKNativeDefeatLibrary.Load(player.SelectedCharacter,
        currentKind == SKTrapMotionKind.Wall ? "fleshwall" : currentKind == SKTrapMotionKind.Tentacle ? "tentacle" :
        currentKind == SKTrapMotionKind.BossSlime ? "boss_spit" : null, defeat: health.IsDead);

    private Sprite Displayed()
    {
        view.Refresh();
        if (currentKind == SKTrapMotionKind.Wall) return wall.GetComponent<SpriteRenderer>().sprite;
        if (Consumed)
        {
            Assert.IsNotNull(envelope);
            envelope.Refresh();
            return envelope.Current;
        }
        return view.Current;
    }

    private IEnumerator AwaitFrame(Sprite expected, float timeout, string what)
    {
        if (Native != null && !expected.name.Contains("_escape_"))
        {
            if (expected.name.Contains("_consumed_"))
            {
                int sample = expected.name.EndsWith("01", StringComparison.Ordinal) ? 11 : 8;
                var nativeExpected = Native.Sample((sample + .01f) / SaintKnight.SpriteCook.SKNativeDefeatLibrary.FramesPerSecond);
                yield return Until(() => Displayed() == nativeExpected, 1.5f, what);
            }
            else
            {
                if (expected.name.Contains("_resist_") && expected.name.EndsWith("01", StringComparison.Ordinal))
                    yield return new WaitForSeconds(.1f);
                yield return Until(() => Displayed().texture == Native.Atlas, timeout, what);
            }
            Assert.AreSame(Native.Atlas, Displayed().texture, "The visible sprite is the current native Resources composite");
            if (currentKind == SKTrapMotionKind.Wall || Consumed)
                Assert.IsTrue(player.GetComponent<SpriteRenderer>().forceRenderingOff);
            if (currentKind == SKTrapMotionKind.Wall) Assert.IsFalse(envelope.IsVisible, "Only the wall renderer draws its full composite");
            else Assert.AreEqual(Consumed, envelope.IsVisible, "Only a hidden consumed body needs the foreground renderer");
            yield break;
        }
        yield return Until(() => Displayed() == expected, timeout, what);
        Assert.IsTrue(library.Owns(Displayed()), "The visible sprite comes from the dedicated Resources atlas");
        if (currentKind == SKTrapMotionKind.Wall)
            Assert.IsTrue(player.GetComponent<SpriteRenderer>().forceRenderingOff, "The wall composite owns the character image");
        else if (Consumed)
        {
            Assert.IsTrue(player.GetComponent<SpriteRenderer>().forceRenderingOff);
            Assert.IsTrue(envelope.IsVisible, "The consumed mass replaces the hidden solo character");
        }
        else
        {
            Assert.IsFalse(player.GetComponent<SpriteRenderer>().forceRenderingOff);
            if (envelope != null) Assert.IsFalse(envelope.IsVisible,
                "Held composites already include the foreground; no duplicate layer");
        }
    }

    private IEnumerator CaptureSwallowAndConsumed(string name)
    {
        for (int stage = 0; stage < ProgressSamples.Length; stage++)
        {
            float target = ProgressSamples[stage];
            yield return Until(() => Progress >= target, 1.2f, "Swallow progress " + target);
            yield return AwaitFrame(library.Sample(SKTrapMotionPhase.Swallow, target, 0f), .3f, "Dedicated swallow stage " + stage);
            Assert.AreEqual(stage, SKTrapAnimationLibrary.StageFor(Progress));
            Shoot(name + "_stage" + stage);
        }
        yield return Until(() => Consumed, 1f, "Fully consumed");
        Assert.IsTrue(health.IsDead);
        Assert.IsTrue(player.GetComponent<SpriteRenderer>().forceRenderingOff);
        for (int frame = 0; frame < 2; frame++)
        {
            var expected = library.Sample(SKTrapMotionPhase.Consumed, 1f,
                frame * SKTrapAnimationLibrary.ConsumedFrameSeconds + .001f);
            yield return AwaitFrame(expected, .3f, "Dedicated consumed frame " + frame);
            Shoot(name + "_consumed_frame" + frame);
        }
    }

    private IEnumerator CaptureResistancePair(string name, int stage)
    {
        float target = ProgressSamples[stage];
        yield return Until(() => Progress >= target, 4f, "Reach resistance depth " + stage);
        yield return AwaitFrame(library.Sample(SKTrapMotionPhase.Swallow, target, 0f), .3f, "Pre-resistance swallow frame");
        Assert.IsFalse(SKHitStop.IsActive, "Inputs are sent only after the accepted-hit stop finishes");
        float before = Progress;
        yield return Tap(Key.J);
        Assert.IsFalse(float.IsNegativeInfinity(combat.Grab.LastStruggleAt));
        // Day 15: depth = HP already lost. A struggle pauses the loss; it does not give HP back.
        Assert.GreaterOrEqual(Progress, before, "A struggle input does not restore lost HP");
        Assert.Less(Progress - before, .02f, "A real struggle input pauses the HP loss");
        Sprite first = library.Sample(SKTrapMotionPhase.Resist, target, 0f);
        Sprite second = library.Sample(SKTrapMotionPhase.Resist, target, .10f);
        Assert.AreNotSame(first, second);
        yield return AwaitFrame(first, .12f, "Resistance keeps the pre-input visual depth, frame 0");
        Shoot(name + "_resist_stage" + stage + "_frame0");
        yield return AwaitFrame(second, .16f, "Resistance keeps the pre-input visual depth, frame 1");
        Shoot(name + "_resist_stage" + stage + "_frame1");
    }

    private IEnumerator CaptureEscape(string name)
    {
        yield return Until(() => !SKHitStop.IsActive, .5f, "Hit stop ends before escape inputs");
        for (int i = 0; i < 14 && combat.Grab.IsHeld && !combat.Grab.EscapeReady; i++)
            yield return Tap(i % 2 == 0 ? Key.Space : Key.J);
        yield return Until(() => !combat.Grab.IsHeld, .5f, "Real mash input escapes");
        Assert.IsFalse(health.IsDead);
        Assert.IsFalse(Consumed);
        for (int frame = 0; frame < 3; frame++)
        {
            yield return AwaitFrame(library.Sample(SKTrapMotionPhase.Escape, 0f, frame * .1f + .001f),
                .16f, "Dedicated escape frame " + frame);
            Shoot(name + "_escape_frame" + frame);
        }
        yield return Until(() => !library.Owns(Displayed()), .3f, "Escape clip gives ownership back to normal motion");
    }

    private IEnumerator BeginCapture(int character, SKTrapMotionKind kind)
    {
        yield return ResetAndSelect(character);
        currentKind = kind;
        library = SKTrapAnimationLibrary.Load(character, kind);
        Assert.IsNotNull(library, "Every character/trap pair needs its dedicated atlas and manifest");
        captureOwner = null;
        switch (kind)
        {
            case SKTrapMotionKind.Slime:
                pool.gameObject.SetActive(true);
                Place(pool.transform.position);
                yield return Until(() => pool.Capture.IsHoldingPlayer, 3f, "Slime captures through actual ground dwell");
                captureOwner = pool.Capture;
                break;
            case SKTrapMotionKind.Tentacle:
                spikes[0].enabled = true;
                spikes[0].ResetCycle();
                Place(spikes[0].transform.position);
                spikes[0].Arm(spikes[0].transform.position, 0f);
                yield return Until(() => spikes[0].Capture.IsHoldingPlayer, 2f, "Accepted floor-tentacle hit captures");
                captureOwner = spikes[0].Capture;
                break;
            default:
                wall.enabled = true;
                wall.ResetWall();
                Place(wall.transform.position + Vector3.right * (character == 0 ? -.3f : .3f));
                yield return Until(() => wall.IsHoldingPlayer, 2f, "Wall captures from the selected side");
                break;
        }
        yield return Until(() => !SKHitStop.IsActive, .5f, "Capture hit stop finishes");
        yield return AwaitFrame(library.Sample(SKTrapMotionPhase.Swallow, 0f, 0f), .4f, "Dedicated first swallow frame");
    }

    private IEnumerator ResetAndSelect(int character)
    {
        Hold();
        foreach (var spike in spikes) spike.enabled = false;
        pool.gameObject.SetActive(false);
        wall.enabled = false;
        session.ResetTraining();
        foreach (var enemy in Object.FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None)) enemy.gameObject.SetActive(false);
        yield return Until(() => player.IsGrounded && Mathf.Abs(body.linearVelocity.y) < .05f, 2f, "reset settled");
        if (player.SelectedCharacter != character) player.SwitchCharacter();
        yield return null;
        Assert.AreEqual(character, player.SelectedCharacter);
        Assert.IsFalse(health.IsDead);
        // Both directions are represented in the six actual-scene combinations.
        Hold(character == 0 ? Key.D : Key.A);
        yield return null;
        Hold();
        yield return null;
    }

    private void Hold(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));

    private IEnumerator Tap(Key key)
    {
        Hold(key);
        yield return null;
        Hold();
        yield return null;
    }

    private void Place(Vector3 floor)
    {
        var at = new Vector2(floor.x, floor.y - (box.bounds.min.y-body.position.y) + .02f);
        player.transform.position = new Vector3(at.x,at.y,player.transform.position.z);
        body.position = at;
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }

    private static IEnumerator Until(Func<bool> condition, float seconds, string what)
    {
        float end = Time.time+seconds, realEnd = Time.realtimeSinceStartup+30f;
        while (!condition() && Time.time < end && Time.realtimeSinceStartup < realEnd) yield return null;
        Assert.IsTrue(condition(),what);
    }

    private void Shoot(string name)
    {
        Sprite shown = Displayed();
        Assert.IsTrue(library.Owns(shown) || Native != null && shown.texture == Native.Atlas,
            "Each screenshot records an original or native composite actually selected by the scene");
        string destination = Path.Combine(folder, name + ".png");
        Assert.IsFalse(File.Exists(destination), "Existing screenshot evidence must not be overwritten");
        var cam = Camera.main;
        var target = new RenderTexture(1280,720,24);
        var oldRect = cam.rect;
        var oldTarget = cam.targetTexture;
        var oldActive = RenderTexture.active;
        var capture = new Texture2D(1280,720,TextureFormat.RGB24,false);
        try
        {
            cam.rect = new Rect(0,0,1,1);
            cam.targetTexture = target;
            cam.Render();
            RenderTexture.active = target;
            capture.ReadPixels(new Rect(0,0,1280,720),0,0);
            capture.Apply();
            File.WriteAllBytes(destination,capture.EncodeToPNG());
            captures++;
            index.AppendLine("| " + name + " | " + shown.name + " | " + Progress.ToString("0.000") +
                " | [PNG](" + name + ".png) |");
        }
        finally
        {
            cam.rect = oldRect;
            cam.targetTexture = oldTarget;
            RenderTexture.active = oldActive;
            target.Release();
            Object.Destroy(target);
            Object.Destroy(capture);
        }
    }
}
