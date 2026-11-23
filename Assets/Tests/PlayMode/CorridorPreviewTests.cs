#if UNITY_EDITOR
using System.Collections;
using System.IO;
using NUnit.Framework;
using SaintKnight.Day01;
using SaintKnight.Day04;
using SaintKnight.Day09;
using SaintKnight.Day10;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class CorridorPreviewTests
{
    private const string ScenePath = "Assets/SaintKnightDay09/Scenes/Corridor3DPreview.unity";
    private const string CaptureDirectory = "Logs/Corridor3D/Frames";
    private const int Width = SKCorridorPreview.Width, Height = SKCorridorPreview.Height;

    [UnityTest, Explicit]
    public IEnumerator WalkingCeciliaKeepsTheArchOnThePixelGrid()
    {
        Directory.CreateDirectory(CaptureDirectory);
        var previousBackground = InputSystem.settings.backgroundBehavior;
        var previousEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
        int previousCaptureRate = Time.captureFramerate;
        Keyboard keyboard = null;
        var frame = new RenderTexture(Width, Height, 24);
        var pixels = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
        try
        {
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            foreach (var enemy in Object.FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None))
                enemy.gameObject.SetActive(false);
            foreach (var spike in Object.FindObjectsByType<SKFloorSpike>(FindObjectsSortMode.None))
                spike.gameObject.SetActive(false);
            Time.captureFramerate = 24;
            var player = Object.FindFirstObjectByType<SKDay01Player>();
            var preview = Object.FindFirstObjectByType<SKCorridorPreview>();
            var geometry = preview.GetComponentInChildren<Camera>();
            var main = Camera.main;

            yield return new WaitForSeconds(.25f);
            Assert.That(player.SelectedCharacter, Is.Zero);
            Assert.That(player.IsGrounded, Is.True);
            Assert.That(geometry.targetTexture.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(geometry.targetTexture.antiAliasing, Is.EqualTo(1));
            float start = main.transform.position.x;
            float playerStart = player.transform.position.x;
            Vector2Int previousCenter = Vector2Int.zero;
            Color32[] previousLayer = null;

            keyboard.MakeCurrent();
            // A graphics batch can lack Game View focus, so write the virtual key directly.
            InputState.Change(keyboard, new KeyboardState(Key.RightArrow));
            yield return new WaitForSeconds(.16f);
            Assert.That(Keyboard.current.rightArrowKey.isPressed, Is.True, "The preview must receive the movement input.");
            Assert.That(player.transform.position.x, Is.GreaterThan(playerStart + .1f));
            for (int i = 0; i < 32; i++)
            {
                yield return null;
                Vector2 position = main.transform.position;
                Vector2 pixelPosition = position * SKCorridorPreview.PixelsPerUnit;
                Vector2Int center = Vector2Int.RoundToInt(pixelPosition);
                Assert.That(Vector2.Distance(pixelPosition, center), Is.LessThan(.0001f));
                Assert.That(geometry.transform.position, Is.EqualTo(main.transform.position));

                geometry.Render();
                Read(geometry.targetTexture, pixels);
                var layer = pixels.GetPixels32();
                if (previousLayer != null)
                    AssertStableOutline(previousLayer, layer, center - previousCenter);
                else
                    Assert.That(System.Array.FindAll(layer, pixel => pixel.a == 255).Length, Is.InRange(4_000, 22_000));
                previousCenter = center;
                previousLayer = layer;

                preview.ShowGeometry = false;
                Capture(main, frame, pixels, $"original_{i:D2}");
                preview.ShowGeometry = true;
                Capture(main, frame, pixels, $"geometry_{i:D2}");
            }

            Assert.That(main.transform.position.x - start, Is.GreaterThan(3f));
            Assert.That(player.IsGrounded, Is.True);
            Assert.That(player.GetComponent<SpriteRenderer>().sortingOrder, Is.GreaterThan(-9));
        }
        finally
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = previousBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditor;
            Time.captureFramerate = previousCaptureRate;
            Object.Destroy(frame);
            Object.Destroy(pixels);
        }
    }

    private static void AssertStableOutline(Color32[] previous, Color32[] current, Vector2Int movement)
    {
        int changed = 0;
        for (int y = 0; y < Height; y++)
        {
            int beforeY = y + movement.y;
            if (beforeY < 0 || beforeY >= Height) continue;
            for (int x = 0; x < Width; x++)
            {
                int beforeX = x + movement.x;
                if (beforeX >= 0 && beforeX < Width &&
                    previous[beforeY * Width + beforeX].a != current[y * Width + x].a)
                    changed++;
            }
        }
        Assert.That(changed, Is.LessThanOrEqualTo(3), "The outline changed after accounting for camera movement.");
    }

    private static void Capture(Camera camera, RenderTexture target, Texture2D pixels, string name)
    {
        var originalRect = camera.rect;
        camera.rect = new Rect(0, 0, 1, 1);
        camera.targetTexture = target;
        try
        {
            camera.Render();
            Read(target, pixels);
            File.WriteAllBytes(Path.Combine(CaptureDirectory, name + ".png"), pixels.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null;
            camera.rect = originalRect;
        }
    }

    private static void Read(RenderTexture target, Texture2D pixels)
    {
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        pixels.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        pixels.Apply();
        RenderTexture.active = previous;
    }
}
#endif
