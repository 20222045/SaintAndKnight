using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SaintKnight.Day01;
using SaintKnight.Day02;
using SaintKnight.Day04;
using SaintKnight.Day08;
using SaintKnight.Day09;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

// Day 9 first stage. Numbers mirror Docs/Design/Stage/make_day09_art.py (art pixels, 48 PPU, y down).
public sealed class Day09StageTests
{
    private const float Ppu = 48f, Left = -6.6666665f;
    private static float X(float px) => Left + px / Ppu;
    private static float Y(float py) => (180f - py) / Ppu;
    private static readonly float Floor = Y(310f), Upper = Y(250f), Ceiling = Y(170f);

    private Keyboard keyboard;
    private InputSettings.BackgroundBehavior previousBackground;
#if UNITY_EDITOR
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
#endif
    private SKDay01Player player;
    private Rigidbody2D body;
    private BoxCollider2D box;
    private SKEnemyAgent[] enemies;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
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
        body = player.GetComponent<Rigidbody2D>();
        box = player.GetComponent<BoxCollider2D>();
        enemies = Object.FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None).OrderBy(e => e.name).ToArray();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        InputSystem.RemoveDevice(keyboard);
        InputSystem.settings.backgroundBehavior = previousBackground;
#if UNITY_EDITOR
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
#endif
        yield return null;
    }

    [UnityTest]
    public IEnumerator EveryActorStartsOnThePaintedFloorInLearningOrder()
    {
        CollectionAssert.AreEqual(new[] { "Enemy_A1_Monk", "Enemy_A2_Monk", "Enemy_A3_Monk",
            "Enemy_B1_Executioner", "Enemy_B2_Executioner" }, enemies.Select(e => e.name).ToArray());
        foreach (var e in enemies)
            Assert.AreEqual(e.name.Contains("_A"), e.GrabEnabled, e.name + ": only monks grab");
        // Lone A -> B -> A on the upper floor -> A + B.
        var byX = enemies.OrderBy(e => e.transform.position.x).Select(e => e.name.Substring(6, 2)).ToArray();
        CollectionAssert.AreEqual(new[] { "A1", "B1", "A2", "A3", "B2" }, byX);

        yield return new WaitForSeconds(.6f);
        Assert.IsTrue(player.IsGrounded, "Player spawns on the floor");
        Assert.AreEqual(Floor, box.bounds.min.y, .03f);
        foreach (var e in enemies)
        {
            float feet = e.GetComponent<BoxCollider2D>().bounds.min.y;
            float expected = e.name == "Enemy_A2_Monk" ? Upper : Floor;
            Assert.AreEqual(expected, feet, .03f, e.name + " stands on its floor");
            Assert.IsTrue(e.TryGetComponent<SKEnemySpriteView>(out _), e.name + " has the Day 9 drawing");
            var sprite = e.GetComponent<SpriteRenderer>().sprite;
            StringAssert.DoesNotContain("NOT_FINAL", sprite.texture.name);
            // Every Day 9 cell has its lowest foot pixel 3 px above the cell bottom (frames.json groundY).
            float drawnFeet = e.transform.position.y + (3f - sprite.pivot.y) / Ppu;
            Assert.AreEqual(feet, drawnFeet, .01f, e.name + " drawing stands on its collider");
        }
    }

    [UnityTest]
    public IEnumerator CameraShowsTheWholeStripAndStopsAtBothWalls()
    {
        Disable(enemies);
        Object.FindFirstObjectByType<SKSceneDoor>().gameObject.SetActive(false); // the east wall is behind the door
        var camera = Camera.main;
        var art = GameObject.Find("Stage1_Background").GetComponent<SpriteRenderer>();
        Assert.AreEqual(Left, art.bounds.min.x, .002f);
        Assert.AreEqual(Left + 2560f / Ppu, art.bounds.max.x, .002f);

        Hold(Key.A);
        yield return Until(() => body.linearVelocity.x > -.01f && box.bounds.min.x < X(40f), 4f);
        Assert.AreEqual(Left, box.bounds.min.x, .04f, "Left wall at the art edge");
        // 2026-11-16: the art's left edge stops where the HUD illustration ends, so she stays in view.
        float inset = camera.GetComponent<SKHorizontalCamera>().LeftInset;
        Assert.AreEqual(art.bounds.min.x, camera.ViewportToWorldPoint(new Vector3(inset, .5f, 10f)).x, .002f);

        body.position = new Vector2(X(2450f), body.position.y);
        Hold(Key.D);
        yield return Until(() => box.bounds.max.x > X(2540f) && body.linearVelocity.x < .01f, 4f);
        yield return new WaitForSeconds(.1f);
        Assert.AreEqual(art.bounds.max.x, box.bounds.max.x, .04f, "Right wall at the art edge");
        Assert.AreEqual(art.bounds.max.x, camera.ViewportToWorldPoint(new Vector3(1f, .5f, 10f)).x, .002f);
        Assert.IsTrue(player.IsGrounded);
    }

    [UnityTest]
    public IEnumerator ChapelDoorTakesThePlayerIntoTheFixedBossChapel()
    {
        Disable(enemies);
        var door = Object.FindFirstObjectByType<SKSceneDoor>();
        Assert.AreEqual("Day09_BossChapel", door.TargetScene);
        // Day 11: reaching the door is enough (no key, no lever); short of it nothing happens.
        Place(2460f, Floor);
        yield return new WaitForSeconds(.3f);
        Assert.AreEqual("Day09_Stage1", SceneManager.GetActiveScene().name, "Just short of the door nothing happens");
        Place(2500f, Floor);
        float end = Time.realtimeSinceStartup + 5f;
        while (SceneManager.GetActiveScene().name != "Day09_BossChapel" && Time.realtimeSinceStartup < end)
            yield return null;
        Assert.AreEqual("Day09_BossChapel", SceneManager.GetActiveScene().name);
        yield return new WaitForSeconds(.5f);
        var boss = Object.FindFirstObjectByType<SKBossAgent>();
        Assert.IsNotNull(boss, "The Rose Window Madonna waits in the chapel");
        boss.gameObject.SetActive(false); // this test checks the room itself

        // Boss chapel: one fixed screen, entrance on the left, flat floor at y310.
        player = Object.FindFirstObjectByType<SKDay01Player>();
        body = player.GetComponent<Rigidbody2D>();
        box = player.GetComponent<BoxCollider2D>();
        var camera = Camera.main;
        var art = GameObject.Find("Boss_Chapel_Background").GetComponent<SpriteRenderer>();
        Assert.IsTrue(player.IsGrounded);
        Assert.AreEqual(Floor, box.bounds.min.y, .03f);
        Assert.AreEqual(X(72f), player.transform.position.x, .05f, "Spawns in front of the entrance");
        // 2026-11-16: the one-screen room now pans a little so the entrance is not under the HUD illustration.
        float inset = camera.GetComponent<SKHorizontalCamera>().LeftInset;
        Assert.GreaterOrEqual(camera.WorldToViewportPoint(box.bounds.min).x, inset - .01f, "Spawn is clear of the illustration");
        Hold(Key.D);
        yield return Until(() => box.bounds.max.x > X(620f) && body.linearVelocity.x < .01f, 6f);
        Hold();
        Assert.AreEqual(art.bounds.max.x, camera.ViewportToWorldPoint(new Vector3(1f, .5f, 10f)).x, .002f, "Right end shows the room's edge");
        Assert.AreEqual(art.bounds.max.x, box.bounds.max.x, .04f, "Right wall at the room's edge");
    }

    [UnityTest]
    public IEnumerator LowCeilingCapsTheJumpOnTheUpperFloorButNotOnTheStep()
    {
        Disable(enemies);
        IEnumerator Jump(float px, float floor, Action<float> top)
        {
            Place(px, floor);
            Hold();
            yield return Until(() => player.IsGrounded && Mathf.Abs(body.linearVelocity.y) < .01f, 2f);
            float max = float.MinValue;
            Hold(Key.Space);
            float end = Time.time + .8f;
            while (Time.time < end) { max = Mathf.Max(max, box.bounds.max.y); yield return null; }
            Hold();
            top(max);
        }

        float upperTop = 0f, stepTop = 0f;
        yield return Jump(1630f, Upper, v => upperTop = v);
        Assert.AreEqual(Ceiling, upperTop, .05f, "Head stops at the low ceiling on the upper floor");
        float enemyTop = Upper + 1.15f;
        Assert.Less(upperTop - box.size.y, enemyTop, "Feet can never clear a monk standing up there");

        yield return Jump(1470f, Y(280f), v => stepTop = v); // still under the ceiling's west end
        Assert.Less(stepTop, Ceiling - .05f, "From the step the jump stays clear of the ceiling");
        Assert.Greater(stepTop - box.size.y, Upper, "and still reaches the upper floor");
    }

    [UnityTest]
    public IEnumerator UpperMonkStaysUpThereAndMeetsThePlayerInTheCrampedSpace()
    {
        var monk = enemies.First(e => e.name == "Enemy_A2_Monk");
        Disable(enemies.Where(e => e != monk));
        var feet = monk.GetComponent<BoxCollider2D>();
        void OnUpperFloor()
        {
            Assert.AreEqual(Upper, feet.bounds.min.y, .05f, "Monk never leaves the upper floor");
            Assert.That(feet.bounds.center.x, Is.InRange(X(1500f), X(1760f)));
        }
        // Alone: patrol both ways without walking off either drop.
        float end = Time.time + 3.5f;
        while (Time.time < end) { OnUpperFloor(); yield return null; }

        // The step under the west ledge is a blind spot (the floor edge blocks its sight),
        // so the fight happens up on the floor, under the low ceiling.
        Place(1530f, Upper);
        end = Time.time + 2.5f;
        bool engaged = false;
        while (Time.time < end && !engaged)
        {
            OnUpperFloor();
            engaged = monk.State != SKEnemyState.Patrol && monk.State != SKEnemyState.Return;
            yield return null;
        }
        Assert.IsTrue(engaged, "The monk comes at a player who climbs up");
    }

    [UnityTest]
    public IEnumerator EnemyDrawingFollowsTellStrikeRecoveryHitAndDefeat()
    {
        foreach (var name in new[] { "Enemy_A1_Monk", "Enemy_B1_Executioner" })
        {
            var enemy = enemies.First(e => e.name == name);
            Disable(enemies.Where(e => e != enemy));
            enemy.gameObject.SetActive(true);
            bool monk = name.Contains("Monk");
            var shown = enemy.GetComponent<SpriteRenderer>();
            // In strike range but outside the monk's grab reach.
            Place(XOf(enemy) - (monk ? .9f : 1.3f) * Ppu, Floor);
            yield return Until(() => enemy.State == SKEnemyState.Windup, 4f, () => name + " windup (state " + enemy.State + ", dx " + (XOf(enemy) - (player.transform.position.x - Left) * Ppu) + "px, dy " + (enemy.transform.position.y - player.transform.position.y) + ")");
            yield return null;
            StringAssert.StartsWith(monk ? "claw-windup" : "windup", shown.sprite.texture.name);
            yield return Until(() => enemy.State == SKEnemyState.Attacking, 2f, () => name + " strike, state " + enemy.State);
            yield return null;
            StringAssert.StartsWith(monk ? "claw-strike" : "strike", shown.sprite.texture.name);
            yield return Until(() => enemy.State == SKEnemyState.Recovery, 2f, () => name + " recovery, state " + enemy.State);
            yield return null;
            StringAssert.StartsWith(monk ? "claw-recovery" : "recovery", shown.sprite.texture.name);

            enemy.Health.TryTakeDamage(1f, player.transform.position, 0f);
            yield return null;
            Assert.AreEqual(SKEnemyState.Hurt, enemy.State);
            StringAssert.StartsWith("hit", shown.sprite.texture.name);
            yield return new WaitForSeconds(.2f); // past the enemy's 0.12 s hit immunity
            Assert.IsTrue(enemy.Health.TryTakeDamage(999f, player.transform.position, 0f));
            yield return new WaitForSeconds(.6f);
            Assert.AreEqual(SKEnemyState.Dead, enemy.State);
            Assert.AreEqual("defeat_f01", shown.sprite.texture.name, "Defeat ends on its last frame");
            Object.FindFirstObjectByType<SKEnemyTrainingSession>().ResetTraining();
        }
    }

    [UnityTest]
    public IEnumerator MonkGrabShowsOneMergedDrawingAndGivesThePlayerBackOnEscape()
    {
        var monk = enemies.First(e => e.name == "Enemy_A1_Monk");
        Disable(enemies.Where(e => e != monk));
        var view = monk.GetComponent<SKEnemySpriteView>();
        var sets = (SKEnemySpriteView.GrabSet[])typeof(SKEnemySpriteView)
            .GetField("grab", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
        var cecilia = sets[0].pounce.Concat(sets[0].hold).Concat(sets[0].resist).ToArray();
        var shown = monk.GetComponent<SpriteRenderer>();
        var playerShown = player.GetComponent<SpriteRenderer>();
        var grab = player.GetComponent<SKPlayerCombat>().Grab;

        Place(XOf(monk) - .4f * Ppu, Floor);
        yield return Until(() => grab.IsHeld, 4f);
        yield return null;
        CollectionAssert.Contains(cecilia, shown.sprite, "Held: the monk draws the merged Cecilia scene");
        Assert.IsFalse(playerShown.enabled, "and the player's own sprite is hidden");
        yield return new WaitForSeconds(.3f);
        CollectionAssert.Contains(sets[0].hold, shown.sprite, "Pounce gives way to the hold loop");
        yield return Tap(Key.J);
        CollectionAssert.Contains(sets[0].resist, shown.sprite, "A mash shows resistance");

        for (int i = 0; i < 14 && grab.IsHeld && !grab.EscapeReady; i++) yield return Tap(i % 2 == 0 ? Key.Space : Key.J);
        yield return Until(() => !grab.IsHeld, 2f);
        yield return null;
        Assert.AreSame(sets[0].escape[0], shown.sprite, "Breaking free plays the merged escape frame");
        Assert.IsFalse(playerShown.enabled);
        yield return new WaitForSeconds(.25f);
        Assert.IsTrue(playerShown.enabled, "then each body is drawn on its own again");
        Assert.AreNotSame(sets[0].escape[0], shown.sprite);
    }

    [UnityTest]
    public IEnumerator HoldingOutTheGrabEndsInTheMergedThrowAndResetCancelsIt()
    {
        var monk = enemies.First(e => e.name == "Enemy_A1_Monk");
        Disable(enemies.Where(e => e != monk));
        var sets = (SKEnemySpriteView.GrabSet[])typeof(SKEnemySpriteView)
            .GetField("grab", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(monk.GetComponent<SKEnemySpriteView>());
        var shown = monk.GetComponent<SpriteRenderer>();
        var playerShown = player.GetComponent<SpriteRenderer>();
        var grab = player.GetComponent<SKPlayerCombat>().Grab;
        yield return Tap(Key.Tab); // Adela's set
        Place(XOf(monk) - .4f * Ppu, Floor);
        yield return Until(() => grab.IsHeld, 4f);
        yield return Until(() => !grab.IsHeld, 5f); // no mashing: thrown after 4 s
        yield return null;
        Assert.AreSame(sets[1].thrown[0], shown.sprite, "Timed-out hold plays Adela's merged throw");
        Assert.IsFalse(playerShown.enabled);
        yield return new WaitForSeconds(.35f);
        Assert.IsTrue(playerShown.enabled);

        Object.FindFirstObjectByType<SKEnemyTrainingSession>().ResetTraining();
        yield return new WaitForSeconds(.8f);
        Place(XOf(monk) - .4f * Ppu, Floor);
        yield return Until(() => grab.IsHeld, 4f);
        Object.FindFirstObjectByType<SKEnemyTrainingSession>().ResetTraining();
        yield return null;
        Assert.IsTrue(playerShown.enabled, "Reset during a hold shows no release frame");
        Assert.AreNotSame(sets[1].escape[0], shown.sprite);
        Assert.AreNotSame(sets[1].thrown[0], shown.sprite);
    }

    [UnityTest]
    public IEnumerator AdelaCuttingTheHeavySwingShowsTheArmorBreak()
    {
        var heavy = enemies.First(e => e.name == "Enemy_B1_Executioner");
        Disable(enemies.Where(e => e != heavy));
        yield return Tap(Key.Tab);
        Assert.AreEqual(1, player.SelectedCharacter);
        Place(XOf(heavy) - .7f * Ppu, Floor);
        yield return Until(() => heavy.State == SKEnemyState.Windup, 4f);
        yield return Tap(Key.J);
        yield return Until(() => heavy.State == SKEnemyState.Hurt, 1f);
        yield return null;
        Assert.AreEqual("armor-break_f00", heavy.GetComponent<SpriteRenderer>().sprite.texture.name);
    }

    /// <summary>Visual check only: writes Logs/Day09Capture_*.png. Run with graphics and -testFilter.</summary>
    [UnityTest, Explicit]
    public IEnumerator CaptureEachSection()
    {
        var spots = new[] { (300f, Floor), (900f, Floor), (1560f, Upper), (2250f, Floor) };
        for (int i = 0; i < spots.Length; i++)
        {
            Place(spots[i].Item1, spots[i].Item2);
            yield return new WaitForSeconds(.4f);
            Shoot(i);
        }
        var heavy = enemies.First(e => e.name == "Enemy_B1_Executioner");
        var monk = enemies.First(e => e.name == "Enemy_A1_Monk");
        Disable(enemies.Where(e => e != heavy && e != monk));
        Place(XOf(heavy) - 1.3f * Ppu, Floor);
        yield return Until(() => heavy.State == SKEnemyState.Attacking, 4f);
        yield return null;
        Shoot(4);                                   // high scythe swing
        var grab = player.GetComponent<SKPlayerCombat>().Grab;
        for (int c = 0; c < 2; c++)
        {
            Object.FindFirstObjectByType<SKEnemyTrainingSession>().ResetTraining();
            if (c == 1) yield return Tap(Key.Tab);
            yield return new WaitForSeconds(.8f);
            Place(XOf(monk) - .4f * Ppu, Floor);
            yield return Until(() => grab.IsHeld, 4f);
            yield return new WaitForSeconds(.35f);
            Shoot(5 + c);                           // merged hold: Cecilia, then Adela
        }
        Object.FindFirstObjectByType<SKEnemyTrainingSession>().ResetTraining();
        Disable(enemies);
        Place(2460f, Floor);
        yield return new WaitForSeconds(.4f);
        Shoot(7);                                   // just short of the door
        Place(2500f, Floor);                        // reaching it enters the chapel
        float end = Time.realtimeSinceStartup + 5f;
        while (SceneManager.GetActiveScene().name != "Day09_BossChapel" && Time.realtimeSinceStartup < end)
            yield return null;
        yield return new WaitForSeconds(.5f);
        Shoot(8);                                   // boss chapel
    }

    private static void Shoot(int i)
    {
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
        System.IO.File.WriteAllBytes("Logs/Day09Capture_" + i + ".png", tex.EncodeToPNG());
    }

    private void Place(float px, float floor)
    {
        // Move the Transform too: body.position alone reaches the Transform only on the next physics step.
        var at = new Vector2(X(px), floor - (box.bounds.min.y - body.position.y) + .02f);
        player.transform.position = new Vector3(at.x, at.y, player.transform.position.z);
        body.position = at;
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }

    private static void Disable(System.Collections.Generic.IEnumerable<SKEnemyAgent> list)
    {
        foreach (var e in list) e.gameObject.SetActive(false);
    }

    private float XOf(SKEnemyAgent enemy) => (enemy.transform.position.x - Left) * Ppu;

    private IEnumerator Tap(Key key)
    {
        Hold(key);
        yield return null;
        Hold();
        yield return null;
    }

    private void Hold(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));

    private static IEnumerator Until(Func<bool> condition, float seconds, Func<string> what = null)
    {
        float end = Time.realtimeSinceStartup + seconds;
        yield return null;
        while (!condition() && Time.realtimeSinceStartup < end) yield return null;
        Assert.That(condition(), Is.True, (what?.Invoke() ?? "Condition") + " not reached within " + seconds + "s");
    }
}
