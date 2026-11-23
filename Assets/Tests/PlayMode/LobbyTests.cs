using System;
using System.Collections;
using NUnit.Framework;
using SaintKnight.Day01;
using SaintKnight.Day02;
using SaintKnight.Lobby;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>Sanctuary lobby: sword switches character + background, W interactions, door to training.</summary>
public sealed class LobbyTests
{
    private Keyboard keyboard;
    private SKDay01Player player;
    private SKLobby lobby;
    private SpriteRenderer background;
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
        yield return SceneManager.LoadSceneAsync("Lobby_Sanctuary");
        yield return null;
        player = Object.FindFirstObjectByType<SKDay01Player>();
        lobby = Object.FindFirstObjectByType<SKLobby>();
        background = lobby.GetComponent<SpriteRenderer>();
        yield return new WaitForSeconds(.3f);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
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

    private IEnumerator StandAt(float pixelX)
    {
        player.transform.position = new Vector3((pixelX - 320f) / 48f, player.transform.position.y, 0f);
        player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        yield return null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator PlayerStandsOnTheWalkLineAndStartsAsCecilia()
    {
        Assert.IsTrue(player.IsGrounded, "Player must land on the lobby floor");
        float feetPixelY = 180f - (player.transform.position.y - .47f) * 48f;
        Assert.That(feetPixelY, Is.InRange(300f, 320f), "Feet should sit on the 310 px walk line");
        Assert.AreEqual(0, player.SelectedCharacter);
        StringAssert.Contains("cecilia", background.sprite.name);
        yield return null;
    }

    [UnityTest]
    public IEnumerator SwordSwitchesCharacterAndBackground()
    {
        yield return StandAt(216f);
        StringAssert.Contains("SWORD", lobby.PromptText);
        yield return Tap(Key.W);
        Assert.AreEqual(1, player.SelectedCharacter, "Sword should switch to Adela");
        yield return null;
        StringAssert.Contains("adela", background.sprite.name);
        StringAssert.Contains("adela", FrontProps().sprite.name);
        StringAssert.Contains("ADELA", lobby.PromptText);
        yield return Tap(Key.UpArrow);
        Assert.AreEqual(0, player.SelectedCharacter, "Up arrow also interacts");
        yield return null;
        StringAssert.Contains("cecilia", background.sprite.name);
    }

    private static SpriteRenderer FrontProps() => GameObject.Find("Lobby_FrontProps").GetComponent<SpriteRenderer>();

    [UnityTest]
    public IEnumerator SwordAndMapTableAreDrawnInFrontOfThePlayer()
    {
        var front = FrontProps();
        var body = player.GetComponent<SpriteRenderer>();
        Assert.Greater(front.sortingOrder, body.sortingOrder, "Front props must sort over the player");
        Assert.Less(front.transform.position.z, player.transform.position.z, "...and sit closer to the camera");
        StringAssert.Contains("front_cecilia", front.sprite.name);
        yield return null;
    }

    [UnityTest]
    public IEnumerator TabSwitchAlsoUpdatesTheSwordLighting()
    {
        yield return StandAt(380f);
        yield return Tap(Key.Tab);
        Assert.AreEqual(1, player.SelectedCharacter);
        yield return null;
        StringAssert.Contains("adela", background.sprite.name);
    }

    [UnityTest]
    public IEnumerator WAwayFromSpotsDoesNothingAndAltarRestoresHp()
    {
        yield return StandAt(250f);
        Assert.AreEqual(-1, lobby.NearestSpot);
        yield return Tap(Key.W);
        Assert.AreEqual(0, player.SelectedCharacter, "No spot nearby: W must not switch");
        var health = player.GetComponent<SKHealth>();
        health.AdjustForTraining(-40f);
        Assert.Less(health.CurrentHp, health.MaximumHp);
        yield return StandAt(414f);
        StringAssert.Contains("ALTAR", lobby.PromptText);
        yield return new WaitForSeconds(.6f); // let the training hit's stun/invulnerability pass
        yield return Tap(Key.W);
        Assert.AreEqual(health.MaximumHp, health.CurrentHp);
    }

    /// <summary>Visual check only: writes Logs/LobbyPlayCapture_*.png. Run with graphics and -testFilter.</summary>
    [UnityTest, Explicit]
    public IEnumerator CaptureLobbyInPlayMode()
    {
        yield return StandAt(509f); // behind the map table
        yield return new WaitForSeconds(.3f);
        yield return null;
        Capture("Logs/LobbyPlayCapture_cecilia.png");
        yield return StandAt(216f);
        yield return Tap(Key.W);
        yield return new WaitForSeconds(.2f);
        yield return null;
        Capture("Logs/LobbyPlayCapture_adela.png");
    }

    private static void Capture(string path)
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
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
    }

    [UnityTest]
    public IEnumerator DoorLoadsTheTrainingScene()
    {
        yield return StandAt(575f);
        StringAssert.Contains("DOOR", lobby.PromptText);
        yield return Tap(Key.W);
        float end = Time.realtimeSinceStartup + 5f;
        while (SceneManager.GetActiveScene().name != "Day09_Stage1" && Time.realtimeSinceStartup < end)
            yield return null;
        Assert.AreEqual("Day09_Stage1", SceneManager.GetActiveScene().name);
    }
}
