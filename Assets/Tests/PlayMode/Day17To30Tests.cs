using System.Collections;
using System.Linq;
using NUnit.Framework;
using SaintKnight.Day01;
using SaintKnight.Day02;
using SaintKnight.Day04;
using SaintKnight.Day09;
using SaintKnight.Day18;
using SaintKnight.Day20;
using SaintKnight.Day26;
using SaintKnight.Lobby;
using SaintKnight.Rest;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Days 17~30 (2026-11-16): enemy HP over enemies, checkpoints, save, carry-over, boss clear exit,
// hit effects, pause / title menus, records & gallery, HUD hold slot.
public sealed class Day17To30Tests
{
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        SKCarry.Clear();
        SKSave.ResetAll();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        SKCarry.Clear();
        if (SKPause.IsPaused) { SKPause.IsPaused = false; }
        SKBindings.ResetToDefaults();
        Time.timeScale = 1f;
        SKSave.ResetAll();
        yield return null;
    }

    private static IEnumerator Load(string scene)
    {
        yield return SceneManager.LoadSceneAsync(scene);
        yield return null;
        yield return null;
    }

    // 2026-11-16 user decision: no enemy / player HP, name, damage stage or control keys on screen.
    [UnityTest]
    public IEnumerator PlayScreenShowsNoHpNameDamageOrControlText()
    {
        yield return Load("Day09_Stage1");
        var texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var status = texts.First(t => t.name == "Enemy_Training_Status");
        StringAssert.DoesNotContain("MONK", status.text);
        StringAssert.DoesNotContain("EXECUTIONER", status.text);
        foreach (var t in texts)
            if (t.enabled && t.gameObject.activeInHierarchy)
                foreach (var banned in new[] { "HP ", "CECILIA NOEL", "ADELA CROW", "DAMAGE STAGE", "MOVE A/D" })
                    StringAssert.DoesNotContain(banned, t.text, t.name + " is still drawn");
        foreach (var g in Object.FindObjectsByType<Graphic>(FindObjectsSortMode.None))
            if (g.name == "HP_FILL" || g.name == "HP_BACK") Assert.IsFalse(g.enabled, g.name);

        yield return Load("Day09_BossChapel");
        foreach (var r in GameObject.Find("Boss_HP_Bar").GetComponentsInChildren<Renderer>(true))
            Assert.IsFalse(r.enabled, r.name + " of the boss bar is drawn");
    }

    // 2026-11-16 user layout (reference screenshot): no bands or side panel; the play field fills the screen,
    // the illustration stands in the lower-left corner cut by the screen bottom, the orb is top-left.
    [UnityTest]
    public IEnumerator HudFloatsOverAFullScreenPlayField()
    {
        yield return Load("Day09_Stage1");
        var cam = Camera.main;
        Assert.AreEqual(1f, Mathf.Max(cam.rect.width, cam.rect.height), .0001f, "Play field reaches the screen edges");
        foreach (var g in Object.FindObjectsByType<Graphic>(FindObjectsSortMode.None))
            if (g.name == "HUD_Left_Panel" || g.name == "TOP_STATUS_BACKDROP" || g.name == "HUD_Title" || g.name == "BOTTOM_CONTROLS")
                Assert.IsFalse(g.enabled, g.name + " still drawn");
        var hud = Object.FindFirstObjectByType<SKDay01HUD>();
        var canvas = hud.GetComponentInParent<Canvas>().transform;
        var portrait = hud.Portrait;   // pixel portrait or, when present, the full-size NovelAI stage art (2026-11-19)
        StringAssert.StartsWith("cecilia_", portrait.sprite.name);
        Assert.AreSame(canvas, portrait.transform.parent);
        Assert.AreEqual(SKDay01HUD.PortraitTop, portrait.rectTransform.anchorMax.y, .0001f);
        Assert.Less(portrait.rectTransform.anchorMin.y, 0f, "Feet run off the bottom of the screen");
        Assert.IsNull(portrait.GetComponent<SKPortraitSway>(), "2026-11-20: the illustration stays still");
        // 2026-11-20: from stage 2 the whole picture is on screen, bottom on the screen edge
        var health = Object.FindFirstObjectByType<SKDay01Player>().GetComponent<SKHealth>();
        health.AdjustForTraining(-health.MaximumHp * .6f);
        Assert.GreaterOrEqual(health.DamageStage, 2);
        Assert.AreEqual(0f, portrait.rectTransform.anchorMin.y, .0001f, "Whole picture shown from stage 2");
        Assert.AreEqual(SKDay01HUD.PortraitTop, portrait.rectTransform.anchorMax.y, .0001f);
        health.ResetHealth();
        Assert.Less(portrait.rectTransform.anchorMin.y, 0f, "Back to the cut-off framing when healed");
        Assert.AreSame(canvas, hud.OrbFrame.transform.parent, "Orb sits on its own in the corner");
        Assert.AreEqual(new Vector2(0f, 1f), hud.OrbFrame.rectTransform.anchorMin);
    }

    [UnityTest]
    public IEnumerator AnyKeyRestartsAfterGameOver()
    {
        yield return Load("Day09_Stage1");
        var hp = Object.FindFirstObjectByType<SKDay01Player>().GetComponent<SKHealth>();
        var background = InputSystem.settings.backgroundBehavior;
#if UNITY_EDITOR
        var editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        try
        {
            hp.AdjustForTraining(-hp.MaximumHp * 2f);
            Assert.IsTrue(hp.IsDead);
            yield return new WaitForSeconds(SKEnemyTrainingSession.AnyKeyGrace + .3f);
            Assert.IsTrue(hp.IsDead, "Still down until a key is pressed");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.K));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            Assert.IsFalse(hp.IsDead, "Any key restarts the stage after game over");
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = background;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
#endif
        }
    }

    [UnityTest]
    public IEnumerator CheckpointLightsSavesAndBecomesTheRetryPoint()
    {
        yield return Load("Day09_Stage1");
        var player = Object.FindFirstObjectByType<SKDay01Player>();
        var hp = player.GetComponent<SKHealth>();
        var session = Object.FindFirstObjectByType<SKEnemyTrainingSession>();
        var cp = Object.FindObjectsByType<SKCheckpoint>(FindObjectsSortMode.None).First(c => c.name == "Checkpoint_Nave");
        Vector3 start = session.Spawn;
        player.transform.position = cp.RespawnPoint();
        player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        yield return new WaitForSeconds(.2f);
        Assert.IsTrue(cp.IsLit);
        Assert.AreEqual("Day09_Stage1", SKSave.Data.scene);
        Assert.AreEqual("Checkpoint_Nave", SKSave.Data.checkpoint);
        Assert.IsTrue(System.IO.File.Exists(SKSave.FilePath));

        hp.Drain(999f);
        yield return null;
        session.ResetTraining();
        yield return null;
        Assert.AreEqual(cp.RespawnPoint().x, player.transform.position.x, .01f, "Retry returns to the checkpoint");
        Assert.AreNotEqual(start.x, player.transform.position.x);
    }

    [UnityTest]
    public IEnumerator DoorCarriesCharacterAndHpButPlainLoadsStartFresh()
    {
        SKCarry.Set(1, 40f);
        yield return Load("Day09_BossChapel");
        var player = Object.FindFirstObjectByType<SKDay01Player>();
        Assert.AreEqual(1, player.SelectedCharacter);
        // carried 40, then Day 15 regen may already have added a frame or two
        Assert.AreEqual(40f, player.GetComponent<SKHealth>().CurrentHp, 1f);
        Assert.IsFalse(SKCarry.Pending, "Carry is used once");
        Assert.AreEqual("Day09_BossChapel", SKSave.Data.scene, "Entering by door saves the room");

        yield return Load("Day09_BossChapel");
        player = Object.FindFirstObjectByType<SKDay01Player>();
        Assert.AreEqual(0, player.SelectedCharacter);
        Assert.AreEqual(player.GetComponent<SKHealth>().MaximumHp, player.GetComponent<SKHealth>().CurrentHp);
    }

    [UnityTest]
    public IEnumerator ContinueStartsAtTheSavedCheckpoint()
    {
        SKSave.Data.scene = "Day09_Stage1";
        SKSave.Data.checkpoint = "Checkpoint_Forecourt";
        SKCarry.Set(0, -1f, SKSave.Data.checkpoint);
        yield return Load("Day09_Stage1");
        var cp = Object.FindObjectsByType<SKCheckpoint>(FindObjectsSortMode.None).First(c => c.name == "Checkpoint_Forecourt");
        var player = Object.FindFirstObjectByType<SKDay01Player>();
        Assert.IsTrue(cp.IsLit);
        Assert.AreEqual(cp.RespawnPoint().x, player.transform.position.x, .05f);
        Assert.AreEqual(cp.RespawnPoint().x, Object.FindFirstObjectByType<SKEnemyTrainingSession>().Spawn.x, .01f);
    }

    [UnityTest]
    public IEnumerator BossClearIsRecordedAndOpensAnExitThatWaitsForW()
    {
        yield return Load("Day09_BossChapel");
        var boss = Object.FindFirstObjectByType<SKBossAgent>();
        boss.Health.DamageScale = 1f;
        boss.Health.TryTakeDamage(5000f, Vector2.zero);
        yield return new WaitForSeconds(.3f);
        Assert.IsTrue(SKSave.Data.bossDefeated);
        Assert.AreEqual(1, SKSave.Data.clears);
        Assert.IsTrue(SKSave.IsUnlocked("boss_rose_madonna"));
        Assert.IsNull(SKRunDirector.Current.ExitDoor);
        yield return new WaitForSeconds(SKRunDirector.ExitDelay);
        var exit = SKRunDirector.Current.ExitDoor;
        Assert.IsNotNull(exit);
        Assert.AreEqual(SKRunDirector.VillageScene, exit.TargetScene, "Clearing stage 1 leads to the village");
        Assert.IsTrue(exit.RequiresUp, "Standing near it never leaves by itself");
        Assert.AreEqual("Day09_BossChapel", SceneManager.GetActiveScene().name);
    }

    [UnityTest, Explicit("Needs graphics; captures the village street.")]
    public IEnumerator CaptureVillage()
    {
        SKSave.Data.clears = 1;
        SKSave.Data.returnRevealPending = false;
        SKSave.Data.clearTalkSeen = SKSave.Data.lobbyTalkSeen = true;
        yield return Load(SKRunDirector.VillageScene);
        var player = Object.FindFirstObjectByType<SKDay01Player>();
        var lobby = Object.FindFirstObjectByType<SKLobby>();
        Assert.IsTrue(player.IsGrounded, "She stands on the street");
        // 2026-11-22: street 1260 px wide, the old 640 px art at x = 310 (doors = old px + 310)
        foreach (var (px, name) in new[] { (40f, "0_left_end"), (380f, "0_arch"), (548f, "1_salon"), (908f, "2_spring"), (1230f, "2_right_end") })
        {
            player.transform.position = new Vector3((px - 630f) / 48f, player.transform.position.y, 0f);
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(.4f);
            Shoot("village_" + name, "Logs/Village/Screens/");
        }
        player.transform.position = new Vector3((908f - 630f) / 48f, player.transform.position.y, 0f);
        Physics2D.SyncTransforms();
        yield return null;
        Assert.AreEqual(0, lobby.NearestSpot, "The bathhouse door is still reachable");
        player.transform.position = new Vector3(0f, player.transform.position.y, 0f);
        try
        {
            foreach (var (phase, name) in new[] { (0f, "3_night"), (.26f, "4_sunrise"), (.5f, "5_noon"), (.72f, "6_dusk"), (.8f, "7_evening") })
            {
                SKVillageSky.PhaseOverride = phase;
                yield return new WaitForSeconds(.3f);
                Shoot("village_" + name, "Logs/Village/Screens/");
            }
        }
        finally { SKVillageSky.PhaseOverride = -1f; }
    }

    [UnityTest]
    public IEnumerator VillageSkyTurnsWithTheDayAndChimneysSmoke()
    {
        SKSave.Data.clears = 1;
        SKSave.Data.returnRevealPending = false;
        SKSave.Data.clearTalkSeen = SKSave.Data.lobbyTalkSeen = true;
        yield return Load(SKRunDirector.VillageScene);
        var sky = Object.FindFirstObjectByType<SKVillageSky>();
        Assert.IsNotNull(sky);
        Assert.AreEqual(10, sky.SmokeCount, "Every chimney smokes (6 old + 4 on the extended sides, 2026-11-22)");
        var lobby = Object.FindFirstObjectByType<SKLobby>();
        var player = Object.FindFirstObjectByType<SKDay01Player>();
        player.transform.position = new Vector3(0f, player.transform.position.y, 0f);
        Physics2D.SyncTransforms();
        yield return null;
        Assert.IsNull(lobby.HintText(1), "No key hint away from the door");
        player.transform.position = new Vector3((238f - 320f) / 48f, player.transform.position.y, 0f);
        Physics2D.SyncTransforms();
        yield return null;
        yield return null;
        StringAssert.Contains("들어가기", lobby.HintText(1), "At the door the key to go in shows over the shop");
        StringAssert.StartsWith("[W]", lobby.HintText(1));
        try
        {
            SKVillageSky.PhaseOverride = .5f;
            yield return null;
            var noonSky = sky.SkyTop; var noonTint = sky.TownTint;
            SKVillageSky.PhaseOverride = 0f;
            yield return null;
            Assert.Greater(noonSky.b, noonSky.r, "Noon is a cold grey-blue");
            Assert.Less(noonTint.grayscale, .8f, "Even noon stays dim (2026-11-22 darker village)");
            Assert.Less(sky.SkyTop.grayscale, noonSky.grayscale - .2f, "Midnight is darker");
            Assert.Less(sky.TownTint.grayscale, noonTint.grayscale - .2f, "The town darkens at night");
            SKVillageSky.PhaseOverride = .499f;
            yield return null;
            Assert.Less(Mathf.Abs(sky.SkyTop.grayscale - noonSky.grayscale), .01f, "The change is gradual");
        }
        finally { SKVillageSky.PhaseOverride = -1f; }
        Assert.GreaterOrEqual(sky.Phase, 0f);
    }

    [UnityTest]
    public IEnumerator FirstBossReturnRevealsTheRestWingOnce()
    {
        yield return CheckFirstReturn(null);
    }

    [UnityTest, Explicit("Needs graphics; captures the boss return, epilogue, and salon.")]
    public IEnumerator CaptureFirstClearRestWing()
    {
        const string folder = "Logs/FirstClearRest/Frames/";
        System.IO.Directory.CreateDirectory(folder);
        yield return CheckFirstReturn(folder);
    }

    private static IEnumerator CheckFirstReturn(string capture)
    {
        yield return Load(SKRunDirector.LobbyScene);
        var lobby = Object.FindFirstObjectByType<SKLobby>();
        var menu = Object.FindFirstObjectByType<SKPauseMenu>();
        lobby.Interact(2);   // village gate: shut before the first clear
        Assert.AreEqual(SKRunDirector.LobbyScene, SceneManager.GetActiveScene().name);
        Assert.IsFalse(menu.IsPausedOpen);
        Assert.IsNull(SKShop.Current);
        Assert.IsNull(GameObject.Find("HotSpring_Steam"));
        Assert.IsNull(GameObject.Find("Tavern_Lantern"));
        if (capture != null) Shoot("00_before_clear", capture);

        yield return Load(SKRunDirector.BossScene);
        var boss = Object.FindFirstObjectByType<SKBossAgent>();
        boss.Health.DamageScale = 1f;
        boss.Health.TryTakeDamage(5000f, Vector2.zero);
        yield return new WaitForSeconds(SKRunDirector.ExitDelay + .4f);
        var exit = SKRunDirector.Current.ExitDoor;
        Assert.IsNotNull(exit);
        Assert.IsTrue(SKSave.Data.returnRevealPending);
        Assert.IsTrue(JsonUtility.FromJson<SKSaveData>(System.IO.File.ReadAllText(SKSave.FilePath)).returnRevealPending);
        var player = Object.FindFirstObjectByType<SKDay01Player>();
        player.transform.position = exit.transform.position;
        player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        var background = InputSystem.settings.backgroundBehavior;
#if UNITY_EDITOR
        var editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        try
        {
            yield return null;
            Assert.AreEqual(SKRunDirector.BossScene, SceneManager.GetActiveScene().name);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = background;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
#endif
        }
        for (int frame = 0; frame < 30 && SceneManager.GetActiveScene().name != SKRunDirector.VillageScene; frame++)
            yield return null;
        Assert.AreEqual(SKRunDirector.VillageScene, SceneManager.GetActiveScene().name);
        yield return null;
        yield return null;

        lobby = Object.FindFirstObjectByType<SKLobby>();
        menu = Object.FindFirstObjectByType<SKPauseMenu>();
        var reveal = Object.FindFirstObjectByType<SKEpilogue>();
        Assert.IsNotNull(reveal);
        Assert.IsTrue(SKPause.IsPaused);
        Assert.IsNotNull(GameObject.Find("HotSpring_Steam"));
        Assert.IsNotNull(GameObject.Find("Tavern_Lantern"));
        Assert.IsFalse(SKSave.Data.hotSpringEpilogueSeen);
        Assert.IsFalse(menu.IsPausedOpen);
        if (capture == null)
        {
            float end = Time.realtimeSinceStartup + 60f;   // up to four story cuts (2026-11-22)
            while (SKEpilogue.Playing && Time.realtimeSinceStartup < end) yield return null;
        }
        else
        {
            yield return new WaitForSecondsRealtime(1.5f);
            CollectionAssert.Contains(new[] { "return_fall", "return_threshold" }, reveal.ShownPicture?.name);
            Shoot("01_first_return", capture);
            while (reveal.CutIndex == 0) { reveal.Advance(); yield return new WaitForSecondsRealtime(.05f); }
            yield return new WaitForSecondsRealtime(1f);
            Shoot("02_water_returns", capture);
            reveal.Finish();
        }
        Assert.IsFalse(SKEpilogue.Playing);
        Assert.IsTrue(SKSave.Data.clearTalkSeen);
        Assert.IsFalse(SKSave.Data.returnRevealPending);
        Assert.IsFalse(SKSave.Data.hotSpringEpilogueSeen);
        Assert.AreEqual(1f, Time.timeScale);
        if (capture != null) Shoot("03_unlocked_lobby", capture);

        yield return Load(SKRunDirector.LobbyScene);
        Assert.IsNull(GameObject.Find("HotSpring_Steam"), "The spring is in the village now, not the lobby");
        Object.FindFirstObjectByType<SKLobby>().Interact(2);   // the side gate leads to the village
        yield return null;
        yield return null;
        Assert.AreEqual(SKRunDirector.VillageScene, SceneManager.GetActiveScene().name);
        yield return null;
        Assert.IsFalse(SKEpilogue.Playing, "The reveal will not repeat on another visit");
        menu = Object.FindFirstObjectByType<SKPauseMenu>();
        menu.OpenHotSpring();
        var epilogue = Object.FindFirstObjectByType<SKEpilogue>();
        Assert.IsNotNull(epilogue);
        if (capture != null)
        {
            for (int cut = 0; cut < SKEpilogue.Cuts.Length; cut++)
            {
                while (epilogue.CutIndex < cut) { epilogue.Advance(); yield return new WaitForSecondsRealtime(.05f); }
                yield return new WaitForSecondsRealtime(1.8f);
                Assert.AreEqual(SKEpilogue.Cuts[cut].picture, epilogue.ShownPicture?.name);
                Shoot("1" + cut + "_" + epilogue.ShownPicture.name, capture);
            }
        }
        epilogue.Finish();
        Assert.IsTrue(SKSave.Data.hotSpringEpilogueSeen);
        yield return null;
        Assert.AreEqual("성소 온천", SKShop.Current?.Def.Title, "Then the hot spring opens as a place");
        if (capture != null) Shoot("20_spring_place", capture);
        SKShop.Current.Close();
        yield return null;
        menu.OpenHotSpring();
        Assert.IsFalse(SKEpilogue.Playing, "The hot spring film will not repeat");
        SKShop.Current?.Close();
        yield return null;

        // 2026-11-22: the salon became two first-person shops
        var shop = SKShop.Open(SKTavern.Gamble);
        Assert.AreEqual(SKShop.Mode.Establishing, shop.State);
        if (capture != null)
        {
            yield return new WaitForSecondsRealtime(1.2f);
            Shoot("21_gamble_enter", capture);
        }
        shop.Confirm();
        shop.FaceSpot(0);
        shop.Confirm();
        yield return new WaitForSecondsRealtime(SKShop.ApproachSeconds + .1f);
        Assert.AreEqual("루네", shop.Speaker ?? "루네");
        if (capture != null)
        {
            yield return new WaitForSecondsRealtime(1.5f);
            Shoot("22_rune", capture);
        }
        shop.Close();
        yield return null;
    }

    [UnityTest]
    public IEnumerator DeathsAreRecordedByCauseAndUnlockTheGallery()
    {
        yield return Load("Day09_Stage1");
        var hp = Object.FindFirstObjectByType<SKDay01Player>().GetComponent<SKHealth>();
        var monk = Object.FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None).First(e => e.DisplayName.EndsWith("MONK"));
        hp.TryTakeDamage(999f, monk.transform.position, cause: monk);
        yield return null;
        yield return null;
        Assert.AreEqual(1, SKSave.Data.deaths);
        CollectionAssert.Contains(SKSave.Data.deathKeys, "cecilia_MONK");
        Assert.IsTrue(SKSave.IsUnlocked("death_cecilia_monk"));
        Assert.IsTrue(SKGallery.Entries.Any(e => e.id == "death_cecilia_monk"));
    }

    [UnityTest]
    public IEnumerator HitsSpawnEffectsAndPlayerHitsShakeOnlyVertically()
    {
        yield return Load("Day09_Stage1");
        var cam = Camera.main;
        var enemy = Object.FindFirstObjectByType<SKEnemyAgent>();
        int before = SKHitFx.Spawned;
        enemy.Health.TryTakeDamage(5f, enemy.transform.position + Vector3.left);
        yield return null;
        Assert.Greater(SKHitFx.Spawned, before);
        float x = cam.transform.position.x;
        Object.FindFirstObjectByType<SKDay01Player>().GetComponent<SKHealth>().TryTakeDamage(5f, Vector2.zero);
        yield return null;
        Assert.Greater(SKScreenShake.Until, Time.time);
        Assert.AreEqual(x, cam.transform.position.x, .001f);
        yield return new WaitForSeconds(.3f);
        Assert.AreEqual(0f, SKScreenShake.Offset);
    }

    [UnityTest]
    public IEnumerator PauseFreezesTimeAndShowsTheSettingsBoard()
    {
        yield return Load("Day09_Stage1");
        var pause = Object.FindFirstObjectByType<SKPauseMenu>();
        Assert.IsNotNull(pause, "Every playable scene gets a pause menu");
        pause.Open();
        Assert.IsTrue(SKPause.IsPaused);
        Assert.AreEqual(0f, Time.timeScale);
        Assert.IsTrue(pause.Panel.IsOpen, "Esc shows the one-screen settings board");
        Assert.AreEqual("top_0", pause.Panel.FocusedId, "Return to Sanctuary is first");
        pause.Panel.Focus("bgm");
        float before = SKSave.Data.volume;
        pause.Panel.StepFocused(-1);
        Assert.AreEqual(before - .1f, SKSave.Data.volume, .001f);
        pause.Panel.Close();
        Assert.IsFalse(SKPause.IsPaused);
        Assert.AreEqual(1f, Time.timeScale);
    }

    [UnityTest]
    public IEnumerator KeysCanBeRebound()
    {
        yield return null;
        Assert.AreEqual(UnityEngine.InputSystem.Key.Space, SKBindings.Primary(SKAction.Jump));
        SKBindings.Rebind(SKAction.Jump, UnityEngine.InputSystem.Key.K);
        Assert.AreEqual(UnityEngine.InputSystem.Key.K, SKBindings.Primary(SKAction.Jump));
        SKBindings.Rebind(SKAction.Attack, UnityEngine.InputSystem.Key.K);   // taken: Jump gets Attack's old key
        Assert.AreEqual(UnityEngine.InputSystem.Key.K, SKBindings.Primary(SKAction.Attack));
        Assert.AreEqual(UnityEngine.InputSystem.Key.J, SKBindings.Primary(SKAction.Jump));
        SKBindings.Reload();
        Assert.AreEqual(UnityEngine.InputSystem.Key.K, SKBindings.Primary(SKAction.Attack), "Saved");
        SKBindings.ResetToDefaults();
        Assert.AreEqual(UnityEngine.InputSystem.Key.J, SKBindings.Primary(SKAction.Attack));
    }

    [UnityTest]
    public IEnumerator TitleSceneIsFirstAndOffersTheMainChoices()
    {
        Assert.AreEqual("Title", System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(0)));
        yield return Load("Title");
        var title = Object.FindFirstObjectByType<SKTitleMenu>();
        Assert.IsTrue(title.WaitingForKey, "Logo first, then press any key");
        Assert.IsNull(title.Top);
        title.ShowMenu();
        var labels = title.Top.Items.Select(i => i.Label()).ToArray();
        CollectionAssert.AreEqual(new[] { "Continue", "New Game", "Records", "Settings", "Quit" }, labels);
        Assert.IsFalse(title.Top.Items[0].Enabled(), "Nothing to continue on a fresh save");
        title.Push(title.RecordsPage());
        StringAssert.Contains("FALLS  0", title.Top.Body());
    }

    [UnityTest]
    public IEnumerator SettingsStepVolumeAndPersist()
    {
        SKSave.Data.volume = 1f;
        SKSettings.StepVolume(-1);
        Assert.AreEqual(.9f, JsonUtility.FromJson<SKSaveData>(System.IO.File.ReadAllText(SKSave.FilePath)).volume, .001f);
        SKSettings.StepVolume(1);
        yield return null;
    }

    [UnityTest]
    public IEnumerator FollowCameraShowsNoBlackBandLeftOfTheStage()
    {
        // 2026-11-19 user: the area under the illustration shows the stage, not a black band past the level's end.
        yield return Load("Day09_Stage1");
        var cam = Camera.main;
        var player = Object.FindFirstObjectByType<SKDay01Player>();
        var left = GameObject.Find("Training_Left_Boundary").GetComponent<BoxCollider2D>();
        player.transform.position = new Vector3(left.bounds.max.x + .3f, player.transform.position.y, 0f);
        Physics2D.SyncTransforms();
        yield return null;
        yield return null;
        Assert.AreEqual(0f, cam.GetComponent<SaintKnight.Day08.SKHorizontalCamera>().LeftInset, 1e-4f);
        Assert.GreaterOrEqual(cam.ViewportToWorldPoint(new Vector3(0f, .5f, 10f)).x, left.bounds.max.x - .05f,
            "The screen's left edge is inside the level");
    }

    /// <summary>Graphics only: Logs/Day17_30/Screens/*.png for a visual check of the new screens.</summary>
    [UnityTest, Explicit("Needs a graphics device; writes screenshots")]
    public IEnumerator CaptureNewScreens()
    {
        System.IO.Directory.CreateDirectory("Logs/Day17_30/Screens");
        yield return Load("Title");
        yield return new WaitForSecondsRealtime(3.4f);   // prologue on black
        Shoot("0_title_prologue");
        yield return new WaitForSecondsRealtime(1.6f);   // the chain snaps, the altar scene comes up
        Shoot("0b_title_snap");
        yield return new WaitForSecondsRealtime(3f);     // logo, heroine, "press any key"
        Shoot("1_title");
        var titleMenu = Object.FindFirstObjectByType<SKTitleMenu>();
        titleMenu.ShowMenu();
        yield return new WaitForSecondsRealtime(.6f);
        Shoot("1b_title_menu");
        titleMenu.Panel.Open("Settings", ("Back", (System.Action)titleMenu.Panel.Close));
        yield return null;
        Shoot("1c_title_settings");
        titleMenu.Panel.Close();

        yield return Load("Day09_Stage1");
        var player = Object.FindFirstObjectByType<SKDay01Player>();
        var cp = Object.FindObjectsByType<SKCheckpoint>(FindObjectsSortMode.None).First(c => c.name == "Checkpoint_Nave");
        player.transform.position = cp.RespawnPoint() + Vector3.left * .3f;
        Physics2D.SyncTransforms();
        yield return new WaitForSeconds(.3f);
        Shoot("2_checkpoint_lit");
        var monk = Object.FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None).First(e => e.name == "Enemy_A1_Monk");
        monk.Health.TryTakeDamage(25f, monk.transform.position + Vector3.left);
        player.transform.position = monk.transform.position + Vector3.left * 2.2f;
        Physics2D.SyncTransforms();
        yield return new WaitForSeconds(.05f);
        Shoot("3_enemy_hit_spark");
        var pause = Object.FindFirstObjectByType<SKPauseMenu>();
        pause.Open();
        yield return null;
        Shoot("4_pause");
        pause.CloseEverything();

        yield return Load("Day09_BossChapel");
        var boss = Object.FindFirstObjectByType<SKBossAgent>();
        boss.Health.DamageScale = 1f;
        boss.Health.TryTakeDamage(5000f, Vector2.zero);
        yield return new WaitForSeconds(SKRunDirector.ExitDelay + .4f);
        Shoot("6_boss_exit");
        Object.FindFirstObjectByType<SKPauseMenu>().OpenRecords();
        yield return null;
        Shoot("7_records");
        Object.FindFirstObjectByType<SKPauseMenu>().CloseEverything();
    }

    internal static void Shoot(string name, string folder = "Logs/Day17_30/Screens/")
    {
        var cam = Camera.main;
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        var modes = canvases.Select(c => c.renderMode).ToArray();
        var orders = canvases.Select(c => c.sortingOrder).ToArray();
        var layers = SortingLayer.layers;
        var layerIds = canvases.Select(c => c.sortingLayerID).ToArray();
        foreach (var c in canvases)
        {
            if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            c.renderMode = RenderMode.ScreenSpaceCamera;
            c.worldCamera = cam;
            c.planeDistance = cam.nearClipPlane + .1f;
            // overlay UI is always on top in the game; keep it there in the capture too
            c.sortingLayerID = layers[layers.Length - 1].id;
            c.sortingOrder = 30000 + c.sortingOrder;   // above the world, same order among themselves
        }
        Canvas.ForceUpdateCanvases();
        var rt = new RenderTexture(1280, 720, 24);
        var rect = cam.rect;
        cam.rect = new Rect(0f, 0f, 1f, 1f);   // UI needs the whole screen; the opaque HUD panels then cover the world
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        tex.Apply();
        cam.targetTexture = null;
        cam.rect = rect;
        RenderTexture.active = null;
        for (int i = 0; i < canvases.Length; i++)
        {
            canvases[i].renderMode = modes[i];
            canvases[i].sortingOrder = orders[i];
            canvases[i].sortingLayerID = layerIds[i];
        }
        System.IO.File.WriteAllBytes(folder + name + ".png", tex.EncodeToPNG());
    }

    [UnityTest]
    public IEnumerator HudKnowsWhatIsHoldingHer()
    {
        yield return Load("Day09_Stage1");
        var player = Object.FindFirstObjectByType<SKDay01Player>();
        var hud = Object.FindFirstObjectByType<SKDay01HUD>();
        var monk = Object.FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None).First(e => e.DisplayName.EndsWith("MONK"));
        var grab = player.GetComponent<SKPlayerCombat>().Grab;
        Assert.IsTrue(grab.TryBegin(monk, 99f, 0f));
        yield return null;
        Assert.AreEqual("MONK", hud.HeldKey);
        grab.Release(monk);
        yield return null;
        Assert.AreEqual("", hud.HeldKey);
    }

    // 2026-11-19 Flower Survivors reels: the HUD illustration bounces — head still, body below lifts, ~0.53 s.
    [Test]
    public void PortraitSwayBouncesBelowTheHead()
    {
        var go = new GameObject("sway_test", typeof(RectTransform), typeof(Image));
        try
        {
            var sway = SKPortraitSway.Attach(go.GetComponent<Image>());
            float peak = 0f, head = 0f, low = 1f;
            for (int k = 0; k < 64; k++)   // two periods at 60 fps
            {
                sway.Step(1f / 60f);
                float lift = sway.LiftAt(.37f);   // screen bottom of the HUD picture
                peak = Mathf.Max(peak, lift);
                low = Mathf.Min(low, lift);
                head = Mathf.Max(head, Mathf.Abs(sway.LiftAt(SKPortraitSway.HeadShare)));
            }
            Assert.AreEqual(0f, head, 1e-5f, "The head is the pivot");
            Assert.Greater(peak, .01f, "The body visibly lifts");
            Assert.AreEqual(0f, low, 1e-4f, "And settles back each period");
            Assert.AreEqual(1f, SKPortraitSway.Pulse(.3f), 1e-5f);
            Assert.AreEqual(0f, SKPortraitSway.Pulse(.9f), 1e-5f);
        }
        finally { Object.DestroyImmediate(go); }
    }
}
