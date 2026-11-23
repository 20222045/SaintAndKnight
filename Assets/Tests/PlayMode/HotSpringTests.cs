using System.Collections;
using NUnit.Framework;
using SaintKnight.Day01;
using SaintKnight.Day02;
using SaintKnight.Day20;
using SaintKnight.Day26;
using SaintKnight.Lobby;
using SaintKnight.Rest;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>2026-11-17: the sanctuary hot spring (opens after the first boss clear) and its one-time epilogue.</summary>
public sealed class HotSpringTests
{
    private Keyboard keyboard;
    private SKDay01Player player;
    private SKHealth health;
    private SKLobby lobby;
    private InputSettings.BackgroundBehavior previousBackground;
#if UNITY_EDITOR
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
#endif

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Time.timeScale = 1f;
        SKSave.ResetAll();
        SKSave.Data.clears = 1;                      // the spring opens after the first boss clear
        SKSave.Data.hotSpringEpilogueSeen = true;    // most tests start past the one-time epilogue
        previousBackground = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
        previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        keyboard = InputSystem.AddDevice<Keyboard>();
        yield return SceneManager.LoadSceneAsync("Village");   // 2026-11-19: spring and salon moved to the village
        yield return null;
        player = Object.FindFirstObjectByType<SKDay01Player>();
        health = player.GetComponent<SKHealth>();
        lobby = Object.FindFirstObjectByType<SKLobby>();
        yield return new WaitForSeconds(.3f);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        var menu = Object.FindFirstObjectByType<SKPauseMenu>();
        if (menu != null) menu.CloseEverything();
        if (SKShop.Current != null) SKShop.Current.Close();
        SKPause.IsPaused = false;
        Time.timeScale = 1f;
        SKSave.ResetAll();
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
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

    [UnityTest]
    public IEnumerator HotSpringRestHealsHp()
    {
        health.AdjustForTraining(-60f);
        player.transform.position = new Vector3((598f - 320f) / 48f, player.transform.position.y, 0f);
        Physics2D.SyncTransforms();
        yield return null;
        yield return null;
        Assert.AreEqual(0, lobby.NearestSpot, "The hot spring spot is reachable inside the right wall");
        StringAssert.Contains("HOT SPRING", lobby.PromptText);
        Assert.IsNotNull(GameObject.Find("HotSpring_Steam"), "Steam marks the entrance");
        yield return Tap(Key.W);
        var shop = SKShop.Current;
        Assert.IsNotNull(shop);
        Assert.AreEqual("성소 온천", shop.Def.Title);
        Assert.AreEqual(SKShop.Mode.Establishing, shop.State, "The place picture first");
        Assert.IsNotNull(Object.FindFirstObjectByType<SKHotSpringScene>(), "Steam and water live on the spring picture");
        Assert.AreEqual(0f, Time.timeScale, "Play pauses while resting");
        shop.Confirm();
        Soak();
        Assert.AreEqual(health.MaximumHp, health.CurrentHp);
        Assert.AreEqual(1, SKSave.Data.hotSpringVisits);
        if (SKEpilogue.Playing)                               // private soak pictures: the bath scene plays first
        {
            yield return Tap(Key.Escape);                     // skip it
            Assert.IsFalse(SKEpilogue.Playing);
            Assert.AreSame(shop, SKShop.Current, "Skipping the soak scene returns to the spring");
            Assert.AreEqual(0f, Time.timeScale, "...still paused");
            yield return null;
        }
        StringAssert.Contains("가벼워", shop.Note, "The soak says it healed");
        yield return Tap(Key.Escape);
        Assert.IsNull(SKShop.Current);
        Assert.AreEqual(1f, Time.timeScale);
    }

    [UnityTest, Explicit("Needs a graphics device; writes screenshots")]
    public IEnumerator CaptureSoakScene()
    {
        const string folder = "Logs/HotSpringHD/Screens/";
        System.IO.Directory.CreateDirectory(folder);
        lobby.Interact(0);
        yield return new WaitForSecondsRealtime(1.5f);
        Day17To30Tests.Shoot("00_place", folder);
        var shop = SKShop.Current;
        shop.Confirm();
        yield return new WaitForSecondsRealtime(.6f);
        Day17To30Tests.Shoot("01_room", folder);
        shop.FaceSpot(1);
        shop.Confirm();
        yield return new WaitForSecondsRealtime(1f);
        Day17To30Tests.Shoot("02_talk_" + shop.Shown.name, folder);
        FinishTalk(shop);
        yield return new WaitForSecondsRealtime(.5f);
        Day17To30Tests.Shoot("03_after_talk_" + shop.Shown.name, folder);
        foreach (var host in new[] { "그레타", "소렐", "릴리", "루네" })               // befriended in the shops
            for (int i = 0; i < SKTavern.FriendTalks; i++)
                SKSave.Data.salonBonds = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Append(SKSave.Data.salonBonds, host));
        shop.Back();
        Soak();
        yield return null;
        var film = Object.FindFirstObjectByType<SKEpilogue>();
        int shot = -1;
        float giveUp = Time.realtimeSinceStartup + 180f;
        while (SKEpilogue.Playing && Time.realtimeSinceStartup < giveUp)
        {
            if (film.CutIndex != shot)
            {
                yield return new WaitForSecondsRealtime(2.2f);   // faded in, first line typed
                shot = film.CutIndex;
                Day17To30Tests.Shoot((10 + shot) + "_" + film.ShownPicture?.name, folder);
            }
            film.Advance();
            yield return new WaitForSecondsRealtime(.05f);
        }
    }

    [Test]
    public void GuestsJoinTheSpringOnlyAsFriends()
    {
        var none = new bool[4];
        CollectionAssert.AreEqual(new[] { "spring_enter", "spring_arrival", "spring_steps", "spring_company" },
            SKEpilogue.SoakPictures(true, none, 0, out int mask), "Before any friendship the two bathe alone");
        Assert.AreEqual(0, mask);
        CollectionAssert.AreEqual(new[] { "spring_enter", "spring_company" }, SKEpilogue.SoakPictures(false, none, 0, out _));
        var some = new[] { true, false, false, true };            // Greta and Rune are friends
        CollectionAssert.AreEqual(new[] { "spring_enter", "spring_company", "spring_greta", "spring_rune" },
            SKEpilogue.SoakPictures(false, some, 0, out mask), "Each new friend walks in once, the bunny girl last");
        Assert.AreEqual(0b1001, mask);
        CollectionAssert.AreEqual(new[] { "spring_enter", "spring_company" }, SKEpilogue.SoakPictures(false, some, mask, out _),
            "Friends who already came are not shown again on their own");
        var all = new[] { true, true, true, true };
        CollectionAssert.AreEqual(new[] { "spring_enter", "spring_company", "spring_sorel", "spring_lili", "spring_all" },
            SKEpilogue.SoakPictures(false, all, mask, out mask));
        CollectionAssert.AreEqual(new[] { "spring_enter", "spring_all" }, SKEpilogue.SoakPictures(false, all, mask, out _),
            "Once everyone came, a soak is undress, then all six");
    }

    [UnityTest]
    public IEnumerator SalonTalksMakeFriendsWhoJoinTheBath()
    {
        Assert.IsFalse(SKTavern.IsFriend("그레타"));
        var menu = Object.FindFirstObjectByType<SKPauseMenu>();
        for (int visit = 0; visit < SKTavern.FriendTalks; visit++)
        {
            var shop = SKShop.Open(SKTavern.BeastBar);
            yield return null;
            shop.Confirm();                       // past the place picture
            shop.FaceSpot(1);                     // the bartender (0 = the wolf bouncer since 2026-11-22)
            shop.Confirm();
            yield return new WaitForSecondsRealtime(SKShop.ApproachSeconds + .05f);
            FinishTalk(shop);
            Assert.AreEqual(SKShop.Mode.Idle, shop.State, "Her default look stays after the talk");
            shop.Confirm();                       // again in the same visit: no extra bond
            FinishTalk(shop);
            Assert.AreEqual(visit + 1, SKTavern.Bond("그레타"), "One bond per visit");
            shop.Close();
            yield return null;
        }
        Assert.IsTrue(SKTavern.IsFriend("그레타"));
        Assert.IsFalse(SKTavern.IsFriend("루네"));
        if (Resources.Load<Texture2D>(SKEpilogue.Folder + "spring_enter") == null) yield break;
        yield return EnterSpring();
        Soak();
        yield return null;
        var film = Object.FindFirstObjectByType<SKEpilogue>();
        var seen = new System.Collections.Generic.List<string>();
        float giveUp = Time.realtimeSinceStartup + 60f;
        while (SKEpilogue.Playing && Time.realtimeSinceStartup < giveUp)
        {
            string shown = film.ShownPicture?.name;
            if (shown != null && (seen.Count == 0 || seen[seen.Count - 1] != shown)) seen.Add(shown);
            Assert.AreEqual("", film.Shown, "Pictures only, no dialogue");
            film.Advance();
            yield return new WaitForSecondsRealtime(.05f);
        }
        CollectionAssert.AreEqual(new[] { "spring_enter", "spring_arrival", "spring_steps", "spring_company", "spring_greta" }, seen);
        Assert.IsNotNull(SKShop.Current, "Back in the spring afterwards");
        Assert.AreEqual(0f, Time.timeScale, "The spring keeps the game paused");
        Soak();                                                                      // next soak: the two, no repeat of Greta
        yield return null;
        float end = Time.realtimeSinceStartup + 12f;                                 // no input: it moves on by itself
        while (SKEpilogue.Playing && Time.realtimeSinceStartup < end) yield return null;
        Assert.IsFalse(SKEpilogue.Playing, "Two wordless cuts finish on their own");
    }

    [UnityTest]
    public IEnumerator TalkingShowsTheSpeakersBathPicture()
    {
        if (Resources.Load<Texture2D>(SKEpilogue.Folder + "spring_cecilia") == null)
            Assert.Ignore("Private bath pictures are not in this checkout");
        yield return EnterSpring();
        var shop = SKShop.Current;
        var talk = SKHotSpring.Pick();
        shop.FaceSpot(1);                                      // walk to Cecilia
        shop.Confirm();
        var shown = shop.Shown;
        Assert.AreEqual(talk[0][0] == "세실리아" ? "spring_cecilia" : "spring_adela", shown.name, "The speaker's own bath picture");
        Assert.AreEqual(FilterMode.Bilinear, shown.filterMode, "HD illustrations are smoothed, not pixel-snapped");
        FinishTalk(shop);
        Assert.AreEqual(SKShop.Mode.Idle, shop.State);
        Assert.AreEqual("spring_company", shop.Shown.name, "When the talk ends the default look (both in the water) stays");
        Assert.AreEqual(0, SKSave.Data.salonBonds.Length, "The two heroines are not salon friends");
        shop.Back();
        Soak();
        yield return null;
        if (SKEpilogue.Playing) Object.FindFirstObjectByType<SKEpilogue>().Finish();
        yield return null;
        Assert.AreEqual("spring_company", shop.Shown.name);
    }

    [UnityTest, Explicit("Needs a graphics device; writes screenshots")]
    public IEnumerator CaptureRestScreens()
    {
        const string folder = "Logs/RestInjury/Screens/";
        System.IO.Directory.CreateDirectory(folder);
        player.transform.position = new Vector3((580f - 320f) / 48f, player.transform.position.y, 0f);
        Physics2D.SyncTransforms();
        yield return new WaitForSeconds(1.8f);
        Day17To30Tests.Shoot("1_lobby_steam", folder);
        yield return EnterSpring();
        SKShop.Current.FaceSpot(1);
        SKShop.Current.Confirm();
        yield return new WaitForSecondsRealtime(1f);
        Day17To30Tests.Shoot("2_hot_spring_talk", folder);
        SKShop.Current.Back();
        Soak();
        yield return null;
        if (SKEpilogue.Playing) Object.FindFirstObjectByType<SKEpilogue>().Finish();
        yield return new WaitForSecondsRealtime(.5f);
        Day17To30Tests.Shoot("3_hot_spring_rested", folder);
    }

    [UnityTest, Explicit("Needs a graphics device; writes screenshots")]
    public IEnumerator CaptureEpilogueCuts()
    {
        const string folder = "Logs/RestInjury/Screens/";
        System.IO.Directory.CreateDirectory(folder);
        SKSave.Data.hotSpringEpilogueSeen = false;
        lobby.Interact(0);
        yield return null;
        var epilogue = Object.FindFirstObjectByType<SKEpilogue>();
        int shot = -1;
        float giveUp = Time.realtimeSinceStartup + 120f;
        while (SKEpilogue.Playing && Time.realtimeSinceStartup < giveUp)
        {
            if (epilogue.CutIndex != shot)
            {
                yield return new WaitForSecondsRealtime(2.2f);   // faded in, first line typed
                shot = epilogue.CutIndex;
                Day17To30Tests.Shoot("ep_" + shot, folder);
            }
            epilogue.Advance();
            yield return new WaitForSecondsRealtime(.05f);
        }
    }

    [UnityTest]
    public IEnumerator TheSpringStaysHiddenUntilTheMadonnaFalls()
    {
        SKSave.Data.clears = 0;
        yield return SceneManager.LoadSceneAsync("Lobby_Sanctuary");
        yield return null;
        player = Object.FindFirstObjectByType<SKDay01Player>();
        lobby = Object.FindFirstObjectByType<SKLobby>();
        player.transform.position = new Vector3((152f - 320f) / 48f, player.transform.position.y, 0f);
        Physics2D.SyncTransforms();
        yield return null;
        yield return null;
        Assert.AreNotEqual(0, lobby.NearestSpot, "No hot spring spot before the first clear");
        Assert.IsNull(GameObject.Find("HotSpring_Steam"), "No steam before the first clear");
        player.transform.position = new Vector3((358f - 320f) / 48f, player.transform.position.y, 0f);
        Physics2D.SyncTransforms();
        yield return null;
        yield return null;
        Assert.AreEqual(-1, lobby.NearestSpot, "No tavern gate before the first clear either");
        Assert.IsNull(GameObject.Find("Tavern_Lantern"));
    }

    [UnityTest]
    public IEnumerator TheEpiloguePlaysOnceThenTheSpringOpens()
    {
        SKSave.Data.hotSpringEpilogueSeen = false;
        lobby.Interact(0);
        yield return null;
        var epilogue = Object.FindFirstObjectByType<SKEpilogue>();
        Assert.IsNotNull(epilogue, "First visit plays the epilogue");
        Assert.IsTrue(SKEpilogue.Playing);
        Assert.AreEqual(0f, Time.timeScale, "Play pauses during the epilogue");
        int lastCut = 0;
        float giveUp = Time.realtimeSinceStartup + 60f;
        while (SKEpilogue.Playing && Time.realtimeSinceStartup < giveUp)
        {
            Assert.GreaterOrEqual(epilogue.CutIndex, lastCut, "Cuts only move forward");
            lastCut = epilogue.CutIndex;
            epilogue.Advance();
            yield return new WaitForSecondsRealtime(.05f);
        }
        Assert.IsFalse(SKEpilogue.Playing, "Every cut was reached and the epilogue ended");
        Assert.AreEqual(SKEpilogue.Cuts.Length - 1, lastCut);
        Assert.IsTrue(SKSave.Data.hotSpringEpilogueSeen);
        yield return null;
        Assert.AreEqual("성소 온천", SKShop.Current?.Def.Title, "Then the hot spring opens");
        Assert.IsFalse(SKEpilogue.ShouldPlay, "Only once");
    }

    // 2026-11-22: the spring is walked into like the shops
    private IEnumerator EnterSpring()
    {
        lobby.Interact(0);
        yield return null;
        Assert.IsNotNull(SKShop.Current, "The spring opens as a place");
        SKShop.Current.Confirm();                 // past the place picture
    }

    private static void Soak()
    {
        SKShop.Current.FaceSpot(2);               // the water
        SKShop.Current.Confirm();
    }

    private static void FinishTalk(SKShop shop)
    {
        for (int guard = 0; shop.State == SKShop.Mode.Talk && guard < 200; guard++) shop.Confirm();
    }

    // 2026-11-22 (user): two shops instead of the salon; the place picture once, then a first-person room to walk through
    [UnityTest]
    public IEnumerator ShopsOpenInFirstPersonAndKeepTheHostOnScreen()
    {
        player.transform.position = new Vector3((238f - 320f) / 48f, player.transform.position.y, 0f);
        Physics2D.SyncTransforms();
        yield return null;
        yield return null;
        StringAssert.Contains("GAMBLING", lobby.PromptText);
        Assert.IsNotNull(GameObject.Find("Tavern_Lantern"), "A lantern glows over the door");
        yield return Tap(Key.W);
        var shop = SKShop.Current;
        Assert.IsNotNull(shop);
        Assert.AreSame(SKTavern.Gamble, shop.Def);
        Assert.AreEqual(SKShop.Mode.Establishing, shop.State, "The place picture shows first, not a talk");
        Assert.IsTrue(SKPause.IsPaused);
        Assert.AreEqual(0f, Time.timeScale);
        yield return new WaitForSecondsRealtime(SKShop.EstablishSeconds + .1f);
        Assert.AreEqual(SKShop.Mode.Room, shop.State, "…then the room in first person");

        float pan = shop.Pan;
        shop.Look(.2f);
        Assert.Greater(shop.Pan, pan, "Looking around turns the view");
        shop.FaceSpot(1);                         // the bottle shelf: an object
        Assert.AreEqual(1, shop.Focused);
        shop.Walk(.5f);                           // W held: walking toward it, step by step
        Assert.AreEqual(SKShop.Mode.Room, shop.State, "Halfway there, still walking");
        Assert.AreEqual(.5f, shop.Walked, 1e-4f);
        pan = shop.Pan;
        shop.Look(.3f);
        Assert.AreEqual(pan, shop.Pan, "No turning while walking");
        shop.Walk(-1f);                           // S held: back where she stood
        Assert.AreEqual(0f, shop.Walked);
        shop.Walk(.6f);
        shop.Walk(.6f);
        Assert.AreEqual(SKShop.Mode.Talk, shop.State, "Arriving opens it");
        Assert.IsNull(shop.Speaker, "Touching an object narrates");
        FinishTalk(shop);
        Assert.AreEqual(SKShop.Mode.Room, shop.State, "…then back to looking around");

        shop.FaceSpot(4);                         // walk to the gambling table (2 = Noela since 2026-11-22)
        shop.Confirm();
        Assert.AreEqual("Gamble/bg_table_top", shop.Room.Picture);
        var draw = SKTavern.Draw;
        SKTavern.RuneLosses = 0;
        try
        {
            SKTavern.Draw = () => (12, 0);        // K against A: Rune loses
            shop.FaceSpot(0);
            shop.Confirm();
            yield return new WaitForSecondsRealtime(SKShop.ApproachSeconds + .05f);
            Assert.AreEqual(SKShop.Mode.Talk, shop.State);
            Assert.AreEqual("루네", shop.Speaker);
            FinishTalk(shop);
            Assert.AreEqual(1, SKTavern.RuneLosses);
            Assert.AreEqual(SKShop.Mode.Idle, shop.State);
            if (SKShop.Art("salon_rune_table") != null)
                Assert.AreEqual("salon_rune_table", shop.Shown.name, "Her default look stays when the talk ends");
        }
        finally { SKTavern.Draw = draw; }
        shop.Back();
        Assert.AreEqual(SKShop.Mode.Room, shop.State);
        Assert.Greater(shop.Walked, 0f, "Stepping back from her leaves you partway");
        shop.Back();
        Assert.AreEqual(0f, shop.Walked);
        shop.Back();
        Assert.AreSame(SKTavern.Gamble.Main, shop.Room, "Esc steps back from the table");
        shop.Back();
        yield return null;
        Assert.IsNull(SKShop.Current);
        Assert.AreEqual(1f, Time.timeScale);
        Assert.IsFalse(SKPause.IsPaused);

        player.transform.position = new Vector3((527f - 320f) / 48f, player.transform.position.y, 0f);
        Physics2D.SyncTransforms();
        yield return null;
        yield return null;
        StringAssert.Contains("BEASTKIN", lobby.PromptText);
        yield return Tap(Key.W);
        Assert.AreSame(SKTavern.BeastBar, SKShop.Current?.Def, "The blue door is the beastkin bar");
        SKShop.Current.Close();
        yield return null;
        // 2026-11-22: two more shops on the market street
        foreach (var (px, def) in new[] { (305f, SKTavern.Forge), (455f, SKTavern.Apothecary) })
        {
            player.transform.position = new Vector3((px - 320f) / 48f, player.transform.position.y, 0f);
            Physics2D.SyncTransforms();
            yield return null;
            yield return null;
            yield return Tap(Key.W);
            Assert.AreSame(def, SKShop.Current?.Def, def.Title + " opens from its door");
            SKShop.Current.Confirm();
            SKShop.Current.FaceSpot(System.Array.FindIndex(def.Main.Spots, sp => sp.Host != null));
            SKShop.Current.Confirm();
            Assert.AreEqual(SKShop.Mode.Talk, SKShop.Current.State, "Its host talks");
            SKShop.Current.Close();
            yield return null;
        }
    }

    [Test]
    public void ConversationFollowsTheRun()
    {
        Assert.AreEqual(SKHotSpring.Intro, SKHotSpring.Pick(), "First visit");
        SKSave.Data.hotSpringVisits = 1;
        SKSave.Data.deaths = 3;
        Assert.AreEqual(SKHotSpring.Falls, SKHotSpring.Pick());
        SKSave.Data.bossDefeated = true;
        Assert.AreEqual(SKHotSpring.Epilogue, SKHotSpring.Pick(), "After the Madonna falls");
    }

    // 2026-11-19: bust morph on listed pictures only, settles back to a gentle breath after a bounce
    [UnityTest]
    public IEnumerator BustMorphMovesOnlyListedPictures()
    {
        var canvas = new GameObject("Bust_Test").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var raw = new GameObject("Picture").AddComponent<UnityEngine.UI.RawImage>();
        raw.transform.SetParent(canvas.transform, false);
        var listed = new Texture2D(64, 64) { name = "bust_test" };
        var plain = new Texture2D(64, 64) { name = "plain_test" };
        SKBustMorph.Regions["bust_test"] = new[] { new Vector4(20, 30, 8, 8), new Vector4(40, 30, 8, 8) };
        bool was = SKBustMorph.Enabled;
        try
        {
            Assert.IsFalse(SKBustMorph.Enabled, "Off by default (user, 2026-11-19: no morphing)");
            raw.texture = listed;
            var morph = SKBustMorph.Attach(raw);
            yield return null;
            Assert.IsFalse(morph.Active, "Switched off: even a listed picture draws normally");
            Assert.AreNotEqual("SaintKnight/BustMorph", raw.material.shader.name);
            SKBustMorph.Enabled = true;   // the mechanism is kept for later
            yield return null;
            Assert.IsTrue(morph.Active);
            Assert.AreEqual("SaintKnight/BustMorph", raw.material.shader.name, "Listed picture draws through the morph shader");
            yield return new WaitForSecondsRealtime(3f);
            SKBustMorph.BounceAll(1f);
            float peak = 0f, squash = 0f, apart = 0f;
            for (float until = Time.realtimeSinceStartup + .8f; Time.realtimeSinceStartup < until; )
            {
                yield return null;
                peak = Mathf.Max(peak, morph.Offset(0).y, morph.Offset(1).y);
                squash = Mathf.Max(squash, Mathf.Abs(morph.Squash(0).y), Mathf.Abs(morph.Squash(1).y));
                apart = Mathf.Max(apart, Mathf.Abs(morph.Offset(0).y - morph.Offset(1).y));
            }
            Assert.Greater(squash, .005f, "Moving fast squashes/stretches the region (2026-11-19 natural morph)");
            Assert.LessOrEqual(squash, SKBustMorph.MaxSquash + 1e-4f);
            Assert.Greater(apart, .05f, "The two sides drift out of step instead of moving glued together");
            Assert.Greater(peak, 1.5f, "A bounce lifts the region by a couple of pixels");
            Assert.LessOrEqual(peak, SKBustMorph.MaxShare * 8f + .01f, "Never more than its share of the radius");
            yield return new WaitForSecondsRealtime(3f);
            Assert.Less(morph.Offset(0).magnitude, SKBustMorph.BreathPixels + .3f, "Settles back to breathing");
            raw.texture = plain;
            yield return null;
            Assert.IsFalse(morph.Active);
            Assert.AreNotEqual("SaintKnight/BustMorph", raw.material.shader.name, "Unlisted picture draws normally");
        }
        finally
        {
            SKBustMorph.Enabled = was;
            SKBustMorph.Regions.Remove("bust_test");
            Object.Destroy(canvas.gameObject);
            Object.Destroy(listed);
            Object.Destroy(plain);
        }
    }

    // 2026-11-19: the real lobby → side gate → salon, Rune greeting at the door, then a bounce on her line
    [UnityTest, Explicit("Needs a graphics device; writes screenshots")]
    public IEnumerator CaptureSalonRune()
    {
        const string folder = "Logs/RestCG/SalonRune/";
        System.IO.Directory.CreateDirectory(folder);
        Assume.That(Resources.Load<Texture2D>(SKTavern.Folder + "salon_rune") != null, "Private art not in this checkout");
        player.transform.position = new Vector3((238f - 320f) / 48f, player.transform.position.y, 0f);
        Physics2D.SyncTransforms();
        yield return new WaitForSeconds(1.5f);
        Day17To30Tests.Shoot("0_village_door", folder);
        yield return Tap(Key.W);
        yield return new WaitForSecondsRealtime(1.2f);
        Day17To30Tests.Shoot("1_place", folder);
        var shop = SKShop.Current;
        shop.Confirm();
        yield return new WaitForSecondsRealtime(.4f);
        Day17To30Tests.Shoot("2_room", folder);
        shop.FaceSpot(0);
        yield return new WaitForSecondsRealtime(.3f);
        Day17To30Tests.Shoot("3_focus", folder);
        shop.Confirm();
        yield return new WaitForSecondsRealtime(SKShop.ApproachSeconds + 1.2f);
        Day17To30Tests.Shoot("4_talk", folder);
        FinishTalk(shop);
        yield return new WaitForSecondsRealtime(.5f);
        Day17To30Tests.Shoot("5_default_look", folder);
        shop.Close();
        var bar = SKShop.Open(SKTavern.BeastBar);
        yield return new WaitForSecondsRealtime(1.2f);
        Day17To30Tests.Shoot("6_beastbar_place", folder);
        bar.Confirm();
        bar.FaceSpot(0);
        yield return new WaitForSecondsRealtime(.4f);
        Day17To30Tests.Shoot("7_beastbar_room", folder);
        bar.Close();
        yield return null;
    }

    [UnityTest, Explicit("Needs a graphics device; writes screenshots")]
    public IEnumerator CaptureBustMorph()
    {
        const string folder = "Logs/RestCG/BustMorph/";
        System.IO.Directory.CreateDirectory(folder);
        var tex = Resources.Load<Texture2D>(SKEpilogue.Folder + "ep_towels");
        Assume.That(tex != null, "Private art not in this checkout");
        tex.filterMode = FilterMode.Point;
        var canvas = new GameObject("Bust_Capture").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var raw = new GameObject("Picture").AddComponent<UnityEngine.UI.RawImage>();
        raw.transform.SetParent(canvas.transform, false);
        raw.rectTransform.anchorMin = Vector2.zero;
        raw.rectTransform.anchorMax = Vector2.one;
        raw.rectTransform.offsetMin = raw.rectTransform.offsetMax = Vector2.zero;
        raw.texture = tex;
        bool was = SKBustMorph.Enabled;
        SKBustMorph.Enabled = true;   // capture of the (switched-off) mechanism, on demand
        SKBustMorph.Attach(raw);
        yield return new WaitForSecondsRealtime(2.5f);
        Day17To30Tests.Shoot("still", folder);
        SKBustMorph.BounceAll(1.4f);
        for (int i = 0; i < 12; i++)
        {
            yield return new WaitForSecondsRealtime(.05f);
            Day17To30Tests.Shoot("bounce_" + i.ToString("00"), folder);
        }
        SKBustMorph.Enabled = was;
        Object.Destroy(canvas.gameObject);
    }
}
