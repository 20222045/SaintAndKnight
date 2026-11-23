using SaintKnight.Day01;
using SaintKnight.Day02;
using SaintKnight.Day10;
using UnityEngine;

namespace SaintKnight.Day06
{
    /// <summary>
    /// Native-pixel trap layer over the existing character sprite. It reads the trap's restraint
    /// state and owns no damage, input, movement or timers. Character atlas pixels are never changed.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class SKTrapEnvelopeView : MonoBehaviour
    {
        private const int Width = 128, Height = 96, Ground = 93;
        private const float Ppu = 48f;
        private SpriteRenderer body, layer;
        private SKDay01Player player;
        private SKPlayerCombat combat;
        private SKHealth health;
        private SKCharacterSpriteView owner;
        private SKEnvironmentalCapture lastCapture;
        private Texture2D texture;
        private Sprite sprite;
        private readonly Color32[] pixels = new Color32[Width * Height];
        private readonly System.Collections.Generic.Dictionary<Sprite,int> nativeTops =
            new System.Collections.Generic.Dictionary<Sprite,int>();
        private int lastKey = int.MinValue;
        private int lastNativeTop = 38;

        public bool IsVisible => layer != null && layer.enabled;
        public Sprite Current => layer != null ? layer.sprite : null;

        private enum Envelope { Slime, Tentacle, Wall }
        private static readonly Color32[] Slime = {
            new Color32(23,32,25,255), new Color32(49,58,34,255),
            new Color32(113,122,59,255), new Color32(197,206,121,255)
        };
        private static readonly Color32[] Tentacle = {
            new Color32(30,16,24,255), new Color32(69,32,49,255),
            new Color32(113,49,63,255), new Color32(220,138,123,255)
        };
        private static readonly Color32[] Flesh = {
            new Color32(22,13,23,255), new Color32(44,23,36,255),
            new Color32(115,56,62,255), new Color32(199,119,101,255)
        };

        private void Awake()
        {
            body = GetComponent<SpriteRenderer>();
            player = GetComponent<SKDay01Player>();
            combat = GetComponent<SKPlayerCombat>();
            health = GetComponent<SKHealth>();
            owner = GetComponent<SKCharacterSpriteView>();
            var child = new GameObject("Native Trap Envelope");
            child.transform.SetParent(transform, false);
            layer = child.AddComponent<SpriteRenderer>();
            if (body != null) layer.sharedMaterial = body.sharedMaterial;
            layer.enabled = false;
            texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
            {
                name = "Native trap envelope (runtime)", filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            sprite = Sprite.Create(texture, new Rect(0, 0, Width, Height),
                new Vector2(.5f, (3f + .47f * Ppu) / Height), Ppu, 0, SpriteMeshType.FullRect);
            sprite.name = "native_trap_envelope";
            layer.sprite = sprite;
        }

        // Explicitly called after the original sprite is selected, including manual Refresh calls.
        public void Refresh()
        {
            if (!isActiveAndEnabled || (owner != null && !owner.isActiveAndEnabled) ||
                layer == null || player == null || body == null || !player.isActiveAndEnabled ||
                health == null || !health.isActiveAndEnabled) { Hide(); return; }

            if (owner != null && owner.IsBossCarryActive) { Hide(); return; }
            if (owner != null && owner.UsesNativeMotion)
            {
                if (owner.NativeSceneKey == "fleshwall") { Hide(); return; } // The wall renderer owns its full native canvas.
                if (body.enabled && !body.forceRenderingOff) { Hide(); return; } // The body already draws the complete scene.
                // Also visible after the trap hides the solo renderer at HP 0.
                layer.sprite = owner.Current;
                layer.transform.localPosition = Vector3.zero;
                if (owner.NativeSceneKey == "fleshwall")
                {
                    var nativeWall = combat != null ? combat.Grab.Holder as SKFleshWall : null;
                    if (nativeWall == null && health.LastCause is Component nativeCause)
                        nativeWall = nativeCause.GetComponentInParent<SKFleshWall>();
                    if (nativeWall != null) layer.transform.position = nativeWall.transform.position;
                }
                layer.flipX = owner.NativeFlipX;
                layer.sortingLayerID = body.sortingLayerID;
                layer.sortingOrder = body.sortingOrder + 1;
                layer.color = Color.white;
                layer.enabled = true;
                return;
            }

            Envelope kind;
            float progress;
            bool consumed = false;
            var grab = combat != null ? combat.Grab : null;
            if (grab != null && grab.IsHeld && grab.Holder is SKFleshWall wall)
            {
                lastCapture = null;
                if (SKTrapAnimationLibrary.Load(player.SelectedCharacter, SKTrapMotionKind.Wall) != null)
                { Hide(); return; } // The dedicated wall composite already includes its foreground.
                kind = Envelope.Wall;
                progress = wall.AbsorptionProgress;
                var wallBody = wall.GetComponent<SpriteRenderer>();
                bool flip = wallBody != null && wallBody.flipX;
                // Native 160x112 wall composites use these existing actor anchors.
                int anchor = 67, feet = 110;
                string pose = wallBody != null && wallBody.sprite != null ? wallBody.sprite.name : "";
                if (pose.EndsWith("swallow_f00", System.StringComparison.Ordinal)) { anchor = 57; feet = 111; }
                else if (pose.EndsWith("swallow_f01", System.StringComparison.Ordinal)) { anchor = 67; feet = 109; }
                else if (pose.EndsWith("struggle_f00", System.StringComparison.Ordinal)) { anchor = 70; feet = 108; }
                layer.transform.position = wall.transform.position +
                    new Vector3((flip ? 80f-anchor : anchor-80f) / Ppu, .47f+(112f-feet)/Ppu, -.01f);
                layer.flipX = flip;
                layer.sortingLayerID = wallBody != null ? wallBody.sortingLayerID : body.sortingLayerID;
                layer.sortingOrder = wallBody != null ? wallBody.sortingOrder + 1 : body.sortingOrder + 1;
            }
            else
            {
                if (grab != null && grab.IsHeld && grab.Holder is SKEnvironmentalCapture capture)
                    lastCapture = capture;
                if (lastCapture == null || (!lastCapture.IsHoldingPlayer && !lastCapture.IsPlayerConsumed))
                    lastCapture = SKEnvironmentalCapture.ForPlayer(player);
                if (lastCapture == null || (!lastCapture.IsHoldingPlayer && !lastCapture.IsPlayerConsumed))
                { lastCapture = null; Hide(); return; }
                consumed = lastCapture.IsPlayerConsumed;
                kind = lastCapture.Kind == SKEnvironmentalCaptureKind.Slime ? Envelope.Slime : Envelope.Tentacle;
                var dedicated = SKTrapAnimationLibrary.For(player.SelectedCharacter, lastCapture);
                if (dedicated != null)
                {
                    if (!consumed) { Hide(); return; } // Held composites are drawn by the body renderer.
                    layer.sprite = dedicated.Sample(SKTrapMotionPhase.Consumed, 1f, Time.time - lastCapture.ConsumedAt);
                    layer.transform.localPosition = Vector3.zero;
                    layer.flipX = body.flipX;
                    layer.sortingLayerID = body.sortingLayerID;
                    layer.sortingOrder = body.sortingOrder + 1;
                    layer.color = Color.white;
                    layer.enabled = true;
                    return;
                }
                progress = consumed ? 1f : lastCapture.AbsorptionProgress;
                layer.transform.localPosition = Vector3.zero;
                layer.flipX = body.flipX;
                layer.sortingLayerID = body.sortingLayerID;
                layer.sortingOrder = body.sortingOrder + 1;
            }

            // All motion is scaled game time, so the layer freezes together with a hit-stop.
            int phase = Mathf.FloorToInt(Time.time * 7f) % 2;
            int band = Mathf.FloorToInt(Mathf.Clamp01(progress) * 32f);
            int nativeTop = consumed ? lastNativeTop : NativeTop(body.sprite, player.SelectedCharacter == 0 ? 55 : 62);
            if (!consumed) lastNativeTop = nativeTop;
            int key = (int)kind * 1000000 + player.SelectedCharacter * 100000 +
                (consumed ? 50000 : 0) + nativeTop*100 + band * 2 + phase;
            if (key != lastKey)
            {
                Draw(kind, Ground-nativeTop, band / 32f, phase, consumed);
                lastKey = key;
            }
            layer.color = Color.white;
            layer.sprite = sprite;
            layer.enabled = true;
        }

        public void Hide()
        {
            if (layer != null) { layer.enabled = false; layer.sprite = sprite; }
            lastKey = int.MinValue;
        }

        private void LateUpdate() => Refresh();

        private void OnDisable() { lastCapture = null; Hide(); }

        private void OnDestroy()
        {
            if (sprite != null) Destroy(sprite);
            if (texture != null) Destroy(texture);
            if (layer != null) Destroy(layer.gameObject);
        }

        private int NativeTop(Sprite original, int fallbackHeight)
        {
            int fallback = Ground-fallbackHeight;
            if (original == null || original.texture == null || !original.texture.isReadable) return fallback;
            if (nativeTops.TryGetValue(original,out int top)) return top;
            Rect rect = original.textureRect;
            if (rect.width != Width || rect.height != Height) return fallback;
            var source = original.texture.GetPixels((int)rect.x,(int)rect.y,Width,Height);
            top = fallback;
            // Head/hair in the native body column; exclude the chain, sword and outstretched hands.
            for (int y = 15; y < Ground-25; y++)
            {
                int opaque = 0;
                for (int x = 57; x <= 78; x++) if (source[(Height-1-y)*Width+x].a > .5f) opaque++;
                if (opaque >= 3) { top = y; break; }
            }
            nativeTops.Add(original,top);
            return top;
        }

        private void Draw(Envelope kind, int characterHeight, float progress, int phase, bool consumed)
        {
            System.Array.Clear(pixels, 0, pixels.Length);
            Color32[] palette = kind == Envelope.Slime ? Slime : kind == Envelope.Wall ? Flesh : Tentacle;
            int top = Ground - characterHeight;
            int sway = phase == 0 ? -1 : 1;

            // Anchors at the floor pull both arms and the shoulders down into the same mass.
            Cord(new Vector2(43,92), new Vector2(46,75), new Vector2(49+sway,top+22), 1, palette);
            Cord(new Vector2(85,92), new Vector2(84,75), new Vector2(79-sway,top+22), 1, palette);
            Ellipse(64,92,25,3,palette[0]);
            Ellipse(64,91,23,2,palette[2]);

            // The original lower-body pixels disappear behind the trap, then the upper body.
            int coveringTop = Mathf.RoundToInt(progress <= .82f
                ? Mathf.Lerp(87,top+15,progress/.82f)
                : Mathf.Lerp(top+15,top-2,(progress-.82f)/.18f));
            for (int y = coveringTop; y <= Ground; y++)
            {
                float t = Mathf.InverseLerp(coveringTop, Ground, y);
                int half = Mathf.RoundToInt(Mathf.Lerp(11f, 19f, t));
                if (y < coveringTop+4)
                {
                    float cap = (y-coveringTop-4)/4f;
                    half = Mathf.RoundToInt(half*Mathf.Sqrt(Mathf.Max(0f,1f-cap*cap)));
                }
                if (consumed) half += 2;
                int centre = 64 + ((y/6 + phase)%2 == 0 ? 1 : -1);
                for (int x = centre-half; x <= centre+half; x++)
                {
                    bool edge = x < centre-half+2 || x > centre+half-2 || y == coveringTop;
                    Put(x,y,edge ? palette[1] : palette[2]);
                }
                if ((y+phase)%7 == 0) { Put(centre-half+4,y,palette[3]); Put(centre-half+5,y,palette[3]); }
            }

            // Whole-body bands also read before the character is submerged up to the shoulders.
            for (int i = 0; i < 3; i++)
            {
                int y = top + 21 + i*Mathf.Max(0,characterHeight-22)/3;
                int tilt = i%2 == 0 ? 1 : -1;
                Cord(new Vector2(49,y+tilt*3+sway), new Vector2(63,y-tilt*3), new Vector2(79,y-tilt*2-sway),
                    kind == Envelope.Slime ? 1 : 2, palette);
            }
            if (consumed)
            {
                // A closed, opaque cocoon replaces the hidden solo sprite until retry/reset.
                Ellipse(64,top+8,13,11,palette[1]);
                Ellipse(64,top+8,11,9,palette[2]);
                Cord(new Vector2(52,top+10),new Vector2(64,top+6+sway),new Vector2(76,top+11),2,palette);
            }
            else if (progress < .82f)
            {
                // Keep the existing tiny face readable until the final head-first closing phase.
                for (int y = top; y < top+17; y++)
                    for (int x = 54; x <= 82; x++) Put(x,y,new Color32(0,0,0,0));
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }

        private void Put(int x, int y, Color32 color)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height) pixels[(Height-1-y)*Width+x] = color;
        }

        private void Ellipse(int cx, int cy, int rx, int ry, Color32 color)
        {
            for (int y = cy-ry; y <= cy+ry; y++)
                for (int x = cx-rx; x <= cx+rx; x++)
                    if ((x-cx)*(x-cx)*(float)(ry*ry) + (y-cy)*(y-cy)*(float)(rx*rx) <= rx*rx*ry*ry)
                        Put(x,y,color);
        }

        private void Cord(Vector2 start, Vector2 bend, Vector2 end, int radius, Color32[] palette)
        {
            for (int shade = 0; shade < 4; shade++)
            {
                int r = shade == 0 ? radius+1 : shade == 1 ? radius : shade == 2 ? Mathf.Max(0,radius-1) : 0;
                for (int i = 0; i <= 45; i++)
                {
                    float t = i/45f, one = 1f-t;
                    Vector2 at = one*one*start + 2f*one*t*bend + t*t*end;
                    int lift = shade >= 2 ? -1 : 0;
                    Ellipse(Mathf.RoundToInt(at.x),Mathf.RoundToInt(at.y)+lift,r,r,palette[shade]);
                }
            }
        }
    }
}
