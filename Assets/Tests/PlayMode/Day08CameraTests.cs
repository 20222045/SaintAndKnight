using System;
using System.Collections;
using NUnit.Framework;
using SaintKnight.Day01;
using SaintKnight.Day04;
using SaintKnight.Day08;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class Day08CameraTests
{
    private Keyboard keyboard;
    private InputSettings.BackgroundBehavior previousBackground;
#if UNITY_EDITOR
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
#endif
    private Camera camera;
    private Rigidbody2D player;

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
        yield return SceneManager.LoadSceneAsync("Day08_Scrolling");
        yield return null;
        foreach (var enemy in Object.FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None))
            enemy.gameObject.SetActive(false);
        camera = Camera.main;
        player = Object.FindFirstObjectByType<SKDay01Player>().GetComponent<Rigidbody2D>();
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
    public IEnumerator WalkScrollsTheWorldAndResetReturnsTheCameraToTheSpawnView()
    {
        Assert.IsTrue(camera.TryGetComponent<SKHorizontalCamera>(out _));
        var spawn = player.transform.position;
        var opening = camera.transform.position;
        // 2026-11-16 user layout: the play field fills the screen; the HUD floats over it.
        // (16:9 view fitted into the window: one axis spans the whole screen)
        Assert.That(Mathf.Max(camera.rect.width, camera.rect.height), Is.EqualTo(1f).Within(.0001f));
        Assert.That(camera.aspect, Is.EqualTo(16f / 9f).Within(.001f));

        Hold(Key.D);
        yield return Until(() => camera.transform.position.x > opening.x + 2f);
        Hold(Key.Space);
        yield return Until(() => player.transform.position.y > spawn.y + .3f);
        Assert.That(camera.transform.position.x, Is.EqualTo(player.transform.position.x).Within(.12f));
        Assert.That(camera.transform.position.y, Is.EqualTo(opening.y).Within(.001f));
        Assert.That(camera.transform.position.z, Is.EqualTo(opening.z).Within(.001f));

        Hold(Key.R);
        yield return Until(() => Mathf.Abs(player.transform.position.x - spawn.x) < .05f);
        yield return null;
        Assert.That(camera.transform.position.x, Is.EqualTo(opening.x).Within(.001f));
    }

    [UnityTest]
    public IEnumerator EndWallsStopThePlayerAndKeepTheViewOnTheBackground()
    {
        var body = player.GetComponent<BoxCollider2D>();
        var left = GameObject.Find("Training_Left_Boundary").GetComponent<BoxCollider2D>();
        var right = GameObject.Find("Training_Right_Boundary").GetComponent<BoxCollider2D>();
        var firstTile = GameObject.Find("Pixel_Background_Day01").GetComponent<SpriteRenderer>();
        var secondTile = GameObject.Find("Pixel_Background_Right_Mirrored").GetComponent<SpriteRenderer>();
        Assert.That(firstTile.bounds.max.x, Is.EqualTo(secondTile.bounds.min.x).Within(.001f));

        Hold(Key.A);
        yield return Until(() => body.bounds.min.x <= left.bounds.max.x + .03f);
        yield return new WaitForSeconds(.2f);
        Assert.That(body.bounds.min.x, Is.EqualTo(left.bounds.max.x).Within(.04f));
        // 2026-11-16: the level's left end stops where the HUD illustration ends, not at the screen edge.
        float inset = camera.GetComponent<SKHorizontalCamera>().LeftInset;
        Assert.That(camera.ViewportToWorldPoint(new Vector3(inset, .5f, 10f)).x,
            Is.EqualTo(firstTile.bounds.min.x).Within(.002f));
        Assert.That(camera.WorldToViewportPoint(body.bounds.min).x, Is.GreaterThanOrEqualTo(inset - .001f),
            "At the left wall the player stands clear of the illustration");

        player.position = new Vector2(right.bounds.min.x - 1f, player.position.y);
        Hold(Key.D);
        yield return Until(() => body.bounds.max.x >= right.bounds.min.x - .03f);
        yield return new WaitForSeconds(.2f);
        Assert.That(body.bounds.max.x, Is.EqualTo(right.bounds.min.x).Within(.04f));
        Assert.That(camera.ViewportToWorldPoint(new Vector3(1f, .5f, 10f)).x,
            Is.EqualTo(secondTile.bounds.max.x).Within(.002f));
        Assert.That(camera.WorldToViewportPoint(body.bounds.max).x, Is.LessThan(1f));
        Assert.That(player.GetComponent<SKDay01Player>().IsGrounded, Is.True);
    }

    private void Hold(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));

    private static IEnumerator Until(Func<bool> condition)
    {
        float end = Time.realtimeSinceStartup + 5f;
        while (!condition() && Time.realtimeSinceStartup < end) yield return null;
        Assert.That(condition(), Is.True, "Expected movement did not complete within five seconds.");
    }
}
