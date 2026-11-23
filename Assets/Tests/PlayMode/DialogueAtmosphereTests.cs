using System.Collections;
using NUnit.Framework;
using SaintKnight.Art;
using SaintKnight.Day01;
using SaintKnight.Day20;
using SaintKnight.Day26;
using SaintKnight.Rest;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>2026-11-19: INARI-direction over-the-head talk in the sanctuary and the per-stage colour grade.</summary>
public sealed class DialogueAtmosphereTests
{
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Time.timeScale = 1f;
        SKPause.IsPaused = false;
        SKSave.ResetAll();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (SKDialogue.Current != null) Object.Destroy(SKDialogue.Current.gameObject);
        SKPause.IsPaused = false;
        Time.timeScale = 1f;
        SKSave.ResetAll();
        yield return null;
    }

    [UnityTest]
    public IEnumerator FirstArrivalTalkTypesAboveThePlayerWithoutPausing()
    {
        yield return SceneManager.LoadSceneAsync("Lobby_Sanctuary");
        yield return null;
        var talk = SKDialogue.Current;
        Assert.IsNotNull(talk, "first arrival starts the sanctuary talk");
        Assert.AreEqual(SKDialogue.LobbyTalk[0][0], talk.Speaker);
        Assert.IsFalse(SKPause.IsPaused);
        Assert.AreEqual(1f, Time.timeScale);
        yield return new WaitForSecondsRealtime(.5f);
        Assert.Greater(talk.Shown.Length, 0, "text types in");

        var player = Object.FindFirstObjectByType<SKDay01Player>();
        var box = talk.transform.Find("Box");
        var head = Camera.main.WorldToScreenPoint(player.transform.position);
        Assert.Greater(box.position.y, head.y, "box floats above the speaker");

        // Cecilia's line shows her face; Adela has no illustration yet
        while (talk != null && talk.Speaker != SKDialogue.Cecilia) { talk.Next(); talk.Next(); }
        Assert.IsNotNull(talk);
        Assert.IsNotNull(talk.Face, "Cecilia's face pops up beside the box");

        while (SKDialogue.Current != null) { SKDialogue.Current.Next(); yield return null; }
        Assert.IsTrue(SKSave.Data.lobbyTalkSeen);

        yield return SceneManager.LoadSceneAsync("Lobby_Sanctuary");
        yield return null;
        Assert.IsNull(SKDialogue.Current, "plays once");
    }

    [UnityTest]
    public IEnumerator ClearTalkPlaysOnceAfterTheFirstClear()
    {
        SKSave.Data.clears = 1;
        SKSave.Data.hotSpringEpilogueSeen = true;
        yield return SceneManager.LoadSceneAsync("Lobby_Sanctuary");
        yield return null;
        Assert.IsNotNull(SKDialogue.Current);
        SKDialogue.Current.Next();   // finish typing
        Assert.AreEqual(SKDialogue.Plain(SKDialogue.ClearTalk[0][1]), SKDialogue.Current.Shown, "the clear talk comes before the arrival talk");
        while (SKDialogue.Current != null) { SKDialogue.Current.Next(); yield return null; }
        Assert.IsTrue(SKSave.Data.clearTalkSeen && SKSave.Data.lobbyTalkSeen);
    }

    /// <summary>Graphics run only: before/after frames of each graded scene plus the talk box, to Logs/Story/InariCaptures.</summary>
    [UnityTest, Explicit("needs a graphics device")]
    public IEnumerator CaptureGradeAndTalk()
    {
        var dir = System.IO.Path.Combine(Application.dataPath, "../Logs/Story/InariCaptures");
        System.IO.Directory.CreateDirectory(dir);
        foreach (var scene in new[] { "Lobby_Sanctuary", "Day09_Stage1", "Day09_BossChapel" })
        {
            SKSave.ResetAll();
            SKSave.Data.lobbyTalkSeen = scene != "Lobby_Sanctuary";
            yield return SceneManager.LoadSceneAsync(scene);
            yield return new WaitForSecondsRealtime(2.5f);
            Shoot(System.IO.Path.Combine(dir, scene + "_after.png"));
            if (SKDialogue.Current != null)
            {
                var c = SKDialogue.Current.GetComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceCamera;   // overlay canvases do not reach a camera render
                c.worldCamera = Camera.main;
                c.planeDistance = 1f;
                for (int i = 0; i < 2; i++)
                {
                    while (SKDialogue.Current != null && SKDialogue.Current.Speaker != SKDialogue.Cecilia) { SKDialogue.Current.Next(); SKDialogue.Current?.Next(); }
                    if (SKDialogue.Current == null) break;
                    yield return new WaitForSecondsRealtime(1.2f);
                    Shoot(System.IO.Path.Combine(dir, "talk_cecilia_" + i + ".png"));
                    SKDialogue.Current.Next(); SKDialogue.Current?.Next();
                    if (SKDialogue.Current == null) break;
                    yield return new WaitForSecondsRealtime(1.2f);
                    Shoot(System.IO.Path.Combine(dir, "talk_adela_" + i + ".png"));
                }
            }
            SKAtmosphere.Active.enabled = false;
            yield return new WaitForSecondsRealtime(.2f);
            Shoot(System.IO.Path.Combine(dir, scene + "_before.png"));
        }
    }

    private static void Shoot(string path)
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
    public IEnumerator StagesGetTheirColourGrade([Values("Lobby_Sanctuary", "Day09_Stage1", "Day09_BossChapel")] string scene)
    {
        SKSave.Data.lobbyTalkSeen = true;
        yield return SceneManager.LoadSceneAsync(scene);
        yield return null;
        Assert.IsNotNull(SKAtmosphere.Active, scene);
        Assert.IsTrue(SKAtmosphere.Active.isGlobal);
        Assert.IsTrue(SKAtmosphere.Active.sharedProfile.Has<SplitToning>());
        Assert.IsTrue(Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing);
    }
}
