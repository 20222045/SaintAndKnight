using System.Collections;
using System.Collections.Generic;
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

/// <summary>Day 6 pixel sprites follow the combat state they only read, and stand on the collider.</summary>
public sealed class Day06SpriteViewTests
{
    private Keyboard keyboard;
    private SKDay01Player player;
    private SKPlayerCombat combat;
    private SKHealth health;
    private SKCharacterSpriteView view;
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
        foreach (var enemy in Object.FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None)) enemy.AttacksEnabled = false;
        player = Object.FindFirstObjectByType<SKDay01Player>();
        combat = player.GetComponent<SKPlayerCombat>();
        health = player.GetComponent<SKHealth>();
        view = player.GetComponent<SKCharacterSpriteView>();
        Assert.IsNotNull(view, "Day 5 player needs SKCharacterSpriteView (Tools > SaintKnight > Day 06)");
        yield return new WaitForSeconds(.5f); // Land and settle.
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

    private IEnumerator Tap(Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
    }

    private IEnumerator WaitFor(System.Func<bool> condition, float timeout = 2f)
    {
        // Game time, so an editor stall cannot use up the window; real-time cap for a frozen game.
        float end = Time.time + timeout, realEnd = Time.realtimeSinceStartup + timeout * 5f + 5f;
        while (!condition() && Time.time < end && Time.realtimeSinceStartup < realEnd) yield return null;
        Assert.IsTrue(condition(), "Timed out");
    }

    private string Shown() => view.Refresh().name;

    private static string MotionPrefix(string character, string motion) => $"{character}_{motion}_f";

    private bool ShowsMotion(string character, string motion) =>
        Shown().StartsWith(MotionPrefix(character, motion), System.StringComparison.Ordinal);

    private void AssertMotion(string character, string motion) =>
        StringAssert.StartsWith(MotionPrefix(character, motion), Shown());

    [UnityTest]
    public IEnumerator IdleFollowsSelectedCharacter()
    {
        AssertMotion("cecilia", "idle");
        Sprite first = view.Current;
        yield return WaitFor(() => view.Refresh() != first);
        AssertMotion("cecilia", "idle");
        yield return Tap(Key.Tab);
        Assert.AreEqual(1, player.SelectedCharacter);
        AssertMotion("adela", "idle");
    }

    [UnityTest]
    public IEnumerator AttackShowsWindupThenStrikeThenIdle()
    {
        yield return Tap(Key.J);
        yield return WaitFor(() => combat.State == SKCombatState.Windup || combat.State == SKCombatState.Attacking);
        if (combat.State == SKCombatState.Windup)
        {
            AssertMotion("cecilia", "windup");
            yield return WaitFor(() => combat.State == SKCombatState.Attacking);
        }
        AssertMotion("cecilia", "attack");
        yield return WaitFor(() => combat.State == SKCombatState.Ready);
        AssertMotion("cecilia", "idle");
    }

    [UnityTest]
    public IEnumerator AttackPoseEndsShortlyAfterTheStrikeNotAtCooldownEnd()
    {
        player.SwitchCharacter(); // Adela: 0.36 s of recovery after the strike.
        yield return null;
        Assert.IsTrue(combat.TryAttack());
        yield return WaitFor(() => combat.State == SKCombatState.Recovery);
        AssertMotion("adela", "followthrough");
        yield return new WaitForSeconds(.18f);
        Assert.AreEqual(SKCombatState.Recovery, combat.State, "Still cooling down");
        AssertMotion("adela", "idle");
    }

    [UnityTest]
    public IEnumerator SwitchDuringWindupLeavesNoAttackPoseOnTheNewCharacter()
    {
        player.SwitchCharacter(); // Adela has the longer windup.
        yield return null;
        Assert.IsTrue(combat.TryAttack());
        Assert.AreEqual(SKCombatState.Windup, combat.State);
        player.SwitchCharacter(); // Cancels the windup; cooldown is kept (Recovery).
        Assert.AreEqual(SKCombatState.Recovery, combat.State);
        AssertMotion("cecilia", "idle");
        yield return null;
    }

    [UnityTest]
    public IEnumerator SideHudShowsCeciliaIllustrationForTheDamageStage()
    {
        var hud = Object.FindFirstObjectByType<SKDay01HUD>();
        var portrait = (UnityEngine.UI.Image)typeof(SKDay01HUD)
            .GetField("portrait", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .GetValue(hud);
        // the full-size NovelAI stages (Git-ignored, 2026-11-19) win when present, else the serialized pixel portraits
        string Expected(int stage) => SKDay01HUD.StagePortrait(0, stage)?.name ?? "cecilia_portrait_stage" + stage;
        Assert.AreEqual(Expected(0), portrait.sprite.name);
        Assert.IsTrue(health.TryTakeDamage(60f, player.transform.position + Vector3.right));
        yield return null;
        Assert.AreEqual(Expected(2), portrait.sprite.name, "HP 40 = stage 2");
        yield return WaitFor(() => combat.CanAct, 3f);
        player.SwitchCharacter();
        yield return null;
        StringAssert.Contains("adela", portrait.sprite.name, "Adela shows her own portrait");
        player.SwitchCharacter();
        yield return null;
        Assert.AreEqual(Expected(2), portrait.sprite.name);
    }

    [UnityTest]
    public IEnumerator HitThenDamagedIdleMatchesHpStage()
    {
        Assert.IsTrue(health.TryTakeDamage(30f, player.transform.position + Vector3.right));
        Assert.AreEqual(SKCombatState.Hurt, combat.State);
        AssertMotion("cecilia", "hurt");
        yield return WaitFor(() => combat.State == SKCombatState.Ready &&
            player.GetComponent<Rigidbody2D>().linearVelocity.magnitude < .05f && player.IsGrounded);
        Assert.AreEqual("cecilia_stage1", Shown(), "70% HP = damage stage 1");
    }

    [UnityTest]
    public IEnumerator DeathPlaysCollapseOnceAndHoldsLastFrame()
    {
        Assert.IsTrue(health.TryTakeDamage(1000f, player.transform.position + Vector3.right));
        Assert.IsTrue(health.IsDead);
        Assert.AreEqual("cecilia_collapse_f00", Shown());
        yield return new WaitForSeconds(1.3f); // 12 frames @ 10 fps, before the Day 5 auto-retry (1.6 s).
        Assert.IsTrue(health.IsDead);
        Assert.AreEqual("cecilia_collapse_f11", Shown());
    }

    [UnityTest]
    public IEnumerator HitboxFarEdgeMatchesDrawnWeaponTip()
    {
        float previousCaptureDelta = Time.captureDeltaTime;
        Time.captureDeltaTime = 1f / 60f; // Sample every authored active frame, including in batch mode.
        try
        {
            foreach (var name in new[] { "cecilia", "adela" })
            {
                if (name == "adela") { player.SwitchCharacter(); yield return null; }
                var profile = (SKPlayerCombat.AttackProfile)typeof(SKPlayerCombat)
                    .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(combat);
                float hitboxFar = profile.offset + profile.size.x * .5f;
                var visibleFrames = new HashSet<Sprite>();
                Assert.IsTrue(combat.TryAttack());
                yield return WaitFor(() => combat.State == SKCombatState.Attacking);
                while (combat.State == SKCombatState.Attacking)
                {
                    AssertMotion(name, "attack");
                    if (visibleFrames.Add(view.Current)) AssertWeaponReach(view.Current, hitboxFar);
                    yield return null;
                }
                Assert.AreEqual(3, visibleFrames.Count, "All three strike sprites must play in the active interval");
                yield return WaitFor(() => combat.State == SKCombatState.Ready);
            }
        }
        finally { Time.captureDeltaTime = previousCaptureDelta; }
    }

    private static void AssertWeaponReach(Sprite sprite, float hitboxFar)
    {
        var tex = sprite.texture;
        var rect = sprite.rect;
        int tipX = -1;
        for (int x = (int)rect.width - 1; x >= 0 && tipX < 0; x--)
            for (int y = 0; y < (int)rect.height; y++)
                if (tex.GetPixel((int)rect.x + x, (int)rect.y + y).a > .5f) { tipX = x; break; }
        float drawnTip = (tipX + 1 - sprite.pivot.x) / sprite.pixelsPerUnit;
        Assert.AreEqual(hitboxFar, drawnTip, 1.5f / 48f, sprite.name + ": hitbox must end where the drawn strike ends");
    }

    [UnityTest]
    public IEnumerator RunAndJumpAdvanceThroughFramesInDayFiveAndLobby()
    {
        yield return CheckRunAndJumpFrames();
        yield return SceneManager.LoadSceneAsync("Lobby_Sanctuary");
        player = Object.FindFirstObjectByType<SKDay01Player>();
        view = player.GetComponent<SKCharacterSpriteView>();
        Assert.IsNotNull(view);
        yield return CheckRunAndJumpFrames();
    }

    private IEnumerator CheckRunAndJumpFrames()
    {
        yield return WaitFor(() => player.IsGrounded);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
        yield return WaitFor(() => ShowsMotion("cecilia", "run"));
        Sprite firstRun = view.Current;
        yield return WaitFor(() => ShowsMotion("cecilia", "run") && view.Current != firstRun);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return WaitFor(() => ShowsMotion("cecilia", "idle"));
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
        yield return WaitFor(() => !player.IsGrounded && ShowsMotion("cecilia", "jump"));
        Sprite firstJump = view.Current;
        yield return WaitFor(() => ShowsMotion("cecilia", "jump") && view.Current != firstJump);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return WaitFor(() => player.IsGrounded && ShowsMotion("cecilia", "idle"));
    }

    [UnityTest]
    public IEnumerator RealEnemyGrabShowsImpactBraceStruggleAndEscape()
    {
        var enemy = System.Array.Find(Object.FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None),
            candidate => candidate.DisplayName.StartsWith("ENEMY A", System.StringComparison.Ordinal));
        Assert.IsNotNull(enemy);
        enemy.AttacksEnabled = true;
        player.transform.position = enemy.transform.position + new Vector3(-.35f, -.05f);
        player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        const BindingFlags privateField = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(SKEnemyAgent).GetField("readyAt", privateField).SetValue(enemy, 0f);
        typeof(SKEnemyAgent).GetField("phase", privateField).SetValue(enemy, SKEnemyState.Patrol);
        yield return WaitFor(() => enemy.State == SKEnemyState.GrabWindup);
        yield return WaitFor(() => combat.Grab.IsHeld);
        AssertMotion("cecilia", "pounced");
        yield return WaitFor(() => ShowsMotion("cecilia", "held"));
        yield return Tap(Key.J);
        yield return WaitFor(() => ShowsMotion("cecilia", "resist"));
        Assert.IsTrue(combat.Grab.IsHeld, "A single press must not end the hold");
        for (int i = 0; i < 12 && combat.Grab.IsHeld && !combat.Grab.EscapeReady; i++)
            yield return Tap(i % 2 == 0 ? Key.Space : Key.J);
        yield return WaitFor(() => !combat.Grab.IsHeld);
        Assert.AreEqual(SKEnemyState.Recovery, enemy.State);
        Assert.IsTrue(health.IsInvulnerable);
        AssertMotion("cecilia", "escape");
        Object.FindFirstObjectByType<SKEnemyTrainingSession>().ResetTraining();
        Assert.IsFalse(ShowsMotion("cecilia", "escape"), "Reset clears the release pose immediately");
    }

    /// <summary>Graphics-only: saves Logs/Day06Capture_*.png (idle, Cecilia strike, Adela strike) at 2x of 640x360.</summary>
    [UnityTest, Explicit("Needs a graphics device; writes screenshots")]
    public IEnumerator CaptureDay05Poses()
    {
        Capture("Logs/Day06Capture_idle.png");
        yield return Tap(Key.J);
        yield return WaitFor(() => combat.State == SKCombatState.Attacking);
        Capture("Logs/Day06Capture_cecilia_attack.png");
        yield return WaitFor(() => combat.State == SKCombatState.Ready);
        yield return Tap(Key.Tab);
        yield return Tap(Key.J);
        yield return WaitFor(() => combat.State == SKCombatState.Attacking);
        Capture("Logs/Day06Capture_adela_attack.png");
    }

    /// <summary>Graphics-only: full screen incl. the overlay HUD at HP stages 0~4 (Logs/Day06Hud_stage*.png).</summary>
    [UnityTest, Explicit("Needs a graphics device; writes screenshots with the HUD")]
    public IEnumerator CaptureHudStages()
    {
        float[] hp = { 100f, 70f, 45f, 20f, 0f };
        for (int stage = 0; stage < 5; stage++)
        {
            yield return WaitFor(() => !health.IsInvulnerable, 3f); // Damage steps go through TryTakeDamage.
            health.AdjustForTraining(hp[stage] - health.CurrentHp);
            Assert.AreEqual(hp[stage], health.CurrentHp);
            yield return new WaitForSeconds(.3f);
            CaptureWithHud("Logs/Day06Hud_stage" + stage + ".png");
        }
    }

    /// <summary>Graphics-only: scripted play recorded at 15 fps with the HUD (Logs/Day06Play/frame_NNN.png).</summary>
    [UnityTest, Explicit("Needs a graphics device; records a short play clip")]
    public IEnumerator CapturePlayRecording()
    {
        System.IO.Directory.CreateDirectory("Logs/Day06Play");
        foreach (var old in System.IO.Directory.GetFiles("Logs/Day06Play", "frame_*.png")) System.IO.File.Delete(old);
        int frame = 0;
        IEnumerator Rec(float seconds)
        {
            for (float t = 0f; t < seconds; t += 1f / 15f)
            {
                CaptureWithHud("Logs/Day06Play/frame_" + (frame++).ToString("000") + ".png");
                yield return new WaitForSeconds(1f / 15f);
            }
        }
        void Hold(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));

        yield return Rec(.6f);                                   // Cecilia idle
        Hold(Key.D); yield return Rec(1f);                       // run right
        Hold(); yield return Tap(Key.J); yield return Rec(.7f);  // strike
        Hold(Key.D); yield return Rec(.3f);
        Hold(Key.D, Key.Space); yield return Rec(.15f);          // running jump
        Hold(Key.D); yield return Rec(.9f);
        Hold(); yield return Rec(.3f);
        yield return Tap(Key.Tab); yield return Rec(.5f);        // switch to Adela
        yield return Tap(Key.J); yield return Rec(.9f);          // strike
        Hold(Key.A); yield return Rec(1f);                       // run left
        Hold(); yield return Rec(.5f);
    }

    // Batch tests cannot wait for end of frame, so the overlay HUD is drawn through the game camera for the shot.
    private void CaptureWithHud(string path)
    {
        view.Refresh();
        var cam = Camera.main;
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        var modes = new RenderMode[canvases.Length];
        for (int i = 0; i < canvases.Length; i++)
        {
            modes[i] = canvases[i].renderMode;
            if (modes[i] != RenderMode.ScreenSpaceOverlay) continue;
            canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
            canvases[i].worldCamera = cam;
            canvases[i].planeDistance = cam.nearClipPlane + .1f;
        }
        Canvas.ForceUpdateCanvases();
        Capture(path);
        for (int i = 0; i < canvases.Length; i++) canvases[i].renderMode = modes[i];
    }

    /// <summary>Graphics-only: one contact sheet (Logs/Day06Review.png) of the states a manual review would check.</summary>
    [UnityTest, Explicit("Needs a graphics device; writes a review sheet")]
    public IEnumerator CaptureReviewSheet()
    {
        var shots = new System.Collections.Generic.List<Texture2D>();
        var body = player.GetComponent<Rigidbody2D>();
        IEnumerator Shot() { yield return null; shots.Add(Crop()); }

        yield return Shot();                                                   // 1 idle right
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
        yield return new WaitForSeconds(.25f);
        yield return Shot();                                                   // 2 run left
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return new WaitForSeconds(.3f);
        yield return Shot();                                                   // 3 idle left
        yield return Tap(Key.J);
        yield return WaitFor(() => combat.State == SKCombatState.Attacking);
        shots.Add(Crop());                                                     // 4 strike left
        yield return WaitFor(() => combat.State == SKCombatState.Ready);
        yield return Tap(Key.Space);
        yield return new WaitForSeconds(.15f);
        yield return Shot();                                                   // 5 airborne
        yield return new WaitForSeconds(.8f);
        health.TryTakeDamage(60f, player.transform.position + Vector3.left);
        yield return Shot();                                                   // 6 hit (faces attacker)
        yield return WaitFor(() => combat.State == SKCombatState.Ready && body.linearVelocity.magnitude < .05f, 3f);
        yield return Shot();                                                   // 7 HP 40 → stage 2 idle
        player.SwitchCharacter();
        yield return Shot();                                                   // 8 Adela stage 2 idle
        Assert.IsTrue(combat.TryAttack());
        yield return null;
        player.SwitchCharacter();
        yield return Shot();                                                   // 9 switch during windup → Cecilia, no leftover
        yield return WaitFor(() => !health.IsInvulnerable, 3f);               // The step-6 hit grants immunity.
        Assert.IsTrue(health.TryTakeDamage(1000f, player.transform.position + Vector3.right));
        yield return new WaitForSeconds(1.3f);
        Assert.IsTrue(health.IsDead);
        yield return Shot();                                                   // 10 collapse held

        const int cell = 160;
        var sheet = new Texture2D(cell * 5, cell * 2, TextureFormat.RGB24, false);
        for (int i = 0; i < shots.Count; i++)
            sheet.SetPixels((i % 5) * cell, (1 - i / 5) * cell, cell, cell, shots[i].GetPixels());
        sheet.Apply();
        System.IO.File.WriteAllBytes("Logs/Day06Review.png", sheet.EncodeToPNG());
    }

    // 160x160 screen pixels (80x80 game pixels at 2x) centred on the player's feet.
    private Texture2D Crop()
    {
        view.Refresh();
        var cam = Camera.main;
        var rt = new RenderTexture(1280, 720, 24);
        var rect = cam.rect;
        cam.rect = new Rect(0f, 0f, 1f, 1f);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        Vector3 p = cam.WorldToViewportPoint(player.transform.position);
        int x = Mathf.Clamp(Mathf.RoundToInt(p.x * 1280) - 80, 0, 1280 - 160);
        int y = Mathf.Clamp(Mathf.RoundToInt(p.y * 720) - 40, 0, 720 - 160);
        var tex = new Texture2D(160, 160, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(x, y, 160, 160), 0, 0);
        tex.Apply();
        cam.targetTexture = null;
        cam.rect = rect;
        RenderTexture.active = null;
        return tex;
    }

    private void Capture(string path)
    {
        view.Refresh();
        var cam = Camera.main;
        var rt = new RenderTexture(1280, 720, 24);
        var rect = cam.rect;
        cam.rect = new Rect(0f, 0f, 1f, 1f);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        tex.Apply();
        cam.targetTexture = null;
        cam.rect = rect;
        RenderTexture.active = null;
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
    }

    [UnityTest]
    public IEnumerator FeetStandOnColliderBottomAndFlipWithFacing()
    {
        var sprite = player.GetComponent<SpriteRenderer>();
        var body = player.GetComponent<BoxCollider2D>();
        view.Refresh();
        float feet = sprite.bounds.min.y + 3f / 48f; // 3px transparent margin under the feet.
        Assert.AreEqual(body.bounds.min.y, feet, .5f / 48f, "Feet must sit on the collider bottom");
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
        Assert.AreEqual(-1f, player.Facing);
        Assert.IsTrue(sprite.flipX, "Facing still owns the flip");
    }
}
