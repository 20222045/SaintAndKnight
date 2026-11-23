using System.Collections.Generic;
using SaintKnight.Day09;
using UnityEngine;

namespace SaintKnight.SpriteCook
{
    /// <summary>Visual-only delivery of the defeated figure to the actual boss. Never moves a collider.</summary>
    [DisallowMultipleComponent]
    public sealed class SKBossDefeatCarryView : MonoBehaviour
    {
        public const float SettleSeconds = .75f;
        private const int Ppu = 48, Height = 160;
        private SpriteRenderer body, actor, tether, dropped;
        private SKBossAgent receiver;
        private Transform heart;
        private Vector3 start;
        private float side, duration;
        private bool savedEnabled;
        private bool receivedFromGrab;
        private Texture2D texture, materialTile;
        private Sprite ropeSprite;
        private Color32[] pixels, tilePixels;
        private float[] distances;
        private int width, drawnTick = int.MinValue;
        private Vector3 lastFrom,lastEnd;
        private int lastGrowth=-1;
        [System.Serializable] private sealed class Contact { public string character,key; public int frame; public float leftX,leftY,rightX,rightY,bootX,bootY; }
        [System.Serializable] private sealed class ContactFile { public int version; public Contact[] frames; }
        private readonly Dictionary<string,Contact> contacts=new Dictionary<string,Contact>();
        private readonly Dictionary<int, Sprite> gear = new Dictionary<int, Sprite>();
        private static readonly Color32[] Colours = {
            new Color32(18,16,22,255), new Color32(36,29,38,255),
            new Color32(66,48,61,255), new Color32(111,75,86,255)
        };

        public bool IsActive { get; private set; }
        public float Progress { get; private set; }
        public Vector3 WorldPosition => IsActive ? actor.transform.position : transform.position;
        public Vector3 Destination { get; private set; }
        public bool AtBoss => IsActive && Progress >= 1f;
        public bool TetherVisible => tether != null && tether.enabled;
        public Vector3 DroppedPosition => dropped != null ? dropped.transform.position : transform.position;
        public SpriteRenderer ActorRenderer => actor;

        private void Awake() => body = GetComponent<SpriteRenderer>();

        private SpriteRenderer NewLayer(string label)
        {
            var child = new GameObject(label);
            child.transform.SetParent(transform, false);
            var result = child.AddComponent<SpriteRenderer>();
            result.sharedMaterial = Resources.Load<Material>(SKNativeDefeatLibrary.Folder + "DefeatPresentation");
            result.enabled = false;
            return result;
        }

        private void EnsureLayers()
        {
            if (actor != null) return;
            actor = NewLayer("Boss delivery - original character");
            tether = NewLayer("Boss delivery - attached pixel tendrils");
            dropped = NewLayer("Boss delivery - dropped equipment");
            materialTile = Resources.Load<Texture2D>(SKNativeDefeatLibrary.Folder + "boss_tether_material");
            if (materialTile != null && materialTile.isReadable) tilePixels = materialTile.GetPixels32();
            var data=Resources.Load<TextAsset>(SKNativeDefeatLibrary.Folder+"contacts");
            if(data!=null)
            {
                var file=JsonUtility.FromJson<ContactFile>(data.text);
                if(file.version>=2) foreach(var c in file.frames) contacts[c.character+"/"+c.key+"/"+c.frame]=c;
            }
        }

        private void LocateBoss(SKBossAgent boss)
        {
            receiver = boss;
            heart = boss.transform.Find("Core_Heart");
            start = transform.position;
            side = start.x < boss.transform.position.x ? -1f : 1f;
            drawnTick = int.MinValue;
        }

        public void ShowGrabTether(Sprite pose, SKBossAgent boss, int character, float seconds, bool flip)
        {
            if (!isActiveAndEnabled || body == null || pose == null || boss == null) { ResetVisual(); return; }
            EnsureLayers();
            if (IsActive) ResetVisual();
            if (receiver != boss) LocateBoss(boss);
            DrawTether(seconds,flip,pose,character,"boss_grab");
        }

        public void Refresh(Sprite pose, SKBossAgent boss, int character, string key, float commonSeconds, bool flip)
        {
            if (!isActiveAndEnabled || body == null || pose == null || boss == null ||
                !boss.gameObject.activeInHierarchy || boss.Health == null || boss.Health.IsDead)
            { ResetVisual(); return; }
            EnsureLayers();
            if (!IsActive || receiver != boss)
            {
                bool keepTether = receiver == boss && tether.enabled;
                ResetVisual(); LocateBoss(boss); receivedFromGrab = keepTether;
                savedEnabled = body.enabled; IsActive = true; drawnTick = int.MinValue;
                Destination = ReceivePosition();
                duration = Mathf.Clamp(Mathf.Abs(Destination.x - start.x) / 1.2f, 1.5f, 3.5f);
                dropped.transform.position = start;
            }
            Destination = ReceivePosition();
            Progress = Mathf.Clamp01((commonSeconds - SettleSeconds) / duration);
            float eased = Progress * Progress * (3f - 2f * Progress);
            Vector3 position = Vector3.Lerp(start, Destination, eased);
            position.y += Mathf.Sin(eased * Mathf.PI) * .15f;
            position.x = Mathf.Round(position.x * Ppu) / Ppu;
            position.y = Mathf.Round(position.y * Ppu) / Ppu;
            actor.transform.position = position; actor.sprite = pose; actor.flipX = flip;
            actor.sortingLayerID = body.sortingLayerID;
            actor.sortingOrder = SKNativeDefeatLibrary.DefeatSortingOrder + 1;
            actor.color = Color.white; actor.enabled = true;
            body.enabled = false;
            dropped.sprite = DroppedGear(character); dropped.flipX = flip;
            dropped.transform.position = start;
            dropped.sortingLayerID = body.sortingLayerID;
            dropped.sortingOrder = SKNativeDefeatLibrary.DefeatSortingOrder;
            dropped.enabled = dropped.sprite != null;
            DrawTether(receivedFromGrab ? commonSeconds+SettleSeconds : commonSeconds, flip, pose, character, key);
        }

        private Vector3 ReceivePosition()
        {
            float stop = Mathf.Min(.65f, Mathf.Abs(start.x - receiver.transform.position.x) * .3f);
            float y = Mathf.Max(start.y + .55f, heart != null ? heart.position.y + .25f : start.y + .65f);
            return new Vector3(receiver.transform.position.x + side * stop, y, start.z);
        }

        private Sprite DroppedGear(int character)
        {
            if (gear.TryGetValue(character, out var found)) return found;
            var image = Resources.Load<Texture2D>(SKNativeDefeatLibrary.Folder +
                (character == 1 ? "adela" : "cecilia") + "_boss_drop");
            if (image == null) return null;
            var result = Sprite.Create(image, new Rect(0, 0, 128, 96),
                new Vector2(.5f, 25.56f / 96f), Ppu, 0, SpriteMeshType.FullRect);
            result.name = "native_boss_stationary_dropped_gear";
            gear.Add(character, result); return result;
        }

        private void EnsureCanvas(float span)
        {
            int wanted = Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.CeilToInt(span * Ppu) + 48), 256, 2048);
            if (texture != null && width >= wanted) return;
            if (ropeSprite != null) Destroy(ropeSprite);
            if (texture != null) Destroy(texture);
            width = wanted; pixels = new Color32[width * Height]; distances = new float[pixels.Length];
            texture = new Texture2D(width, Height, TextureFormat.RGBA32, false) {
                name = "Original pixel boss delivery tendrils", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };
            ropeSprite = Sprite.Create(texture, new Rect(0, 0, width, Height), new Vector2(.5f, .5f), Ppu, 0, SpriteMeshType.FullRect);
            tether.sprite = ropeSprite; drawnTick = int.MinValue;
        }

        private Vector3 ContactWorld(Vector2 point,Sprite pose,bool flip)
        {
            float x=(point.x+.5f-pose.pivot.x)/Ppu;
            float y=(95.5f-point.y-pose.pivot.y)/Ppu;
            return WorldPosition+new Vector3(flip?-x:x,y,0);
        }

        private void DrawTether(float seconds, bool flip,Sprite pose,int character,string key)
        {
            if(seconds<.45f) { tether.enabled=false; return; }
            Vector3 from = heart != null ? heart.position : receiver.transform.position + Vector3.up * .4f;
            from.x += side * .12f; from.y += .12f;
            int frame=Mathf.RoundToInt(pose.rect.x/SKNativeDefeatLibrary.Width);
            string prefix=(character==1?"adela":"cecilia")+"/"+key+"/";
            if (!contacts.TryGetValue(prefix+frame,out var contact) && frame<SKNativeDefeatLibrary.LoopStart)
                contacts.TryGetValue(prefix+SKNativeDefeatLibrary.LoopStart,out contact);
            Vector2 left=contact!=null?new Vector2(contact.leftX,contact.leftY):new Vector2(character==1?49:50,character==1?59:62);
            Vector2 right=contact!=null?new Vector2(contact.rightX,contact.rightY):new Vector2(61,70);
            Vector2 boot=contact!=null && contact.bootY>0 ? new Vector2(contact.bootX,contact.bootY) : new Vector2(65,85);
            var ends=new[]{ContactWorld(left,pose,flip),ContactWorld(right,pose,flip),ContactWorld(boot,pose,flip)};
            Vector3 end=(ends[0]+ends[1]+ends[2])/3f;
            EnsureCanvas(Mathf.Max(Mathf.Abs(from.x-ends[0].x),Mathf.Max(Mathf.Abs(from.x-ends[1].x),Mathf.Abs(from.x-ends[2].x))));
            Vector3 centre = (from + end) * .5f;
            centre.x = Mathf.Round(centre.x * Ppu) / Ppu; centre.y = Mathf.Round(centre.y * Ppu) / Ppu;
            tether.transform.position = new Vector3(centre.x, centre.y, start.z);
            tether.sortingLayerID = body.sortingLayerID;
            tether.sortingOrder = SKNativeDefeatLibrary.DefeatSortingOrder - (IsActive ? 0 : 1);
            tether.color = Color.white; tether.enabled = true;
            int tick = Mathf.FloorToInt(seconds * 8f);
            int growth=Mathf.Clamp(Mathf.FloorToInt((seconds-.45f)/.3f*128),0,128);
            if (tick == drawnTick && growth==lastGrowth && (from-lastFrom).sqrMagnitude<.0001f && (end-lastEnd).sqrMagnitude<.0001f) return;
            lastGrowth=growth;
            drawnTick = tick; lastFrom=from; lastEnd=end; System.Array.Clear(pixels, 0, pixels.Length);
            Vector2 a = Pixel(from, centre);
            float phase = tick * Mathf.PI * 2f / 10f;
            float slack = Mathf.Lerp(16f, 3f, Progress);
            for (int strand = 0; strand < 3; strand++)
            {
                float wave = Mathf.Sin(phase + strand * 2f) * 2f;
                Vector2 offset = new Vector2(0, (strand - 1) * 3f),d=Pixel(ends[strand],centre);
                Vector2 b = Vector2.Lerp(a, d, .32f) + new Vector2(0, -slack + wave);
                Vector2 c = Vector2.Lerp(a, d, .72f) + new Vector2(0, slack * .35f - wave);
                Cord(a + offset, b + offset, c - offset * .3f, d, 2.1f - strand * .25f, tick,growth);
            }
            texture.SetPixels32(pixels); texture.Apply(false, false);
        }

        private Vector2 Pixel(Vector3 world, Vector3 centre) =>
            new Vector2((world.x - centre.x) * Ppu + width * .5f, (world.y - centre.y) * Ppu + Height * .5f);

        private void Cord(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float radius, int tick,int growth)
        {
            for (int i = 0; i < distances.Length; i++) distances[i] = float.MaxValue;
            for (int step = 0; step <= growth; step++)
            {
                float t = step / 128f, n = 1f - t;
                Vector2 p = n*n*n*a + 3*n*n*t*b + 3*n*t*t*c + t*t*t*d;
                float r = Mathf.Lerp(radius, 1.15f, t);
                for (int y = Mathf.FloorToInt(p.y-r-1); y <= Mathf.CeilToInt(p.y+r+1); y++)
                    for (int x = Mathf.FloorToInt(p.x-r-1); x <= Mathf.CeilToInt(p.x+r+1); x++)
                    {
                        if (x < 0 || x >= width || y < 0 || y >= Height) continue;
                        float dx = x-p.x, dy = y-p.y, distance = (dx*dx+dy*dy)/(r*r);
                        int index = y*width+x;
                        if (distance > 1.25f || distance >= distances[index]) continue;
                        distances[index] = distance;
                        int shade = distance > .78f ? 0 : dy > .2f ? 3 : dy < -.3f ? 1 : 2;
                        if (tilePixels != null)
                        {
                            var texel = tilePixels[(Mathf.Abs(y) % materialTile.height)*materialTile.width + (step*2+tick)%materialTile.width];
                            if (texel.a != 0 && texel.r+texel.g+texel.b < 180) shade = Mathf.Max(0, shade-1);
                        }
                        pixels[index] = Colours[shade];
                    }
            }
        }

        public void ResetVisual()
        {
            if (IsActive && body != null) body.enabled = savedEnabled;
            IsActive = false; receivedFromGrab = false; Progress = 0f; receiver = null; heart = null;
            if (actor != null) actor.enabled = false;
            if (tether != null) tether.enabled = false;
            if (dropped != null) dropped.enabled = false;
        }

        private void OnDisable() => ResetVisual();
        private void OnDestroy()
        {
            ResetVisual();
            foreach (var sprite in gear.Values) if (sprite != null) Destroy(sprite);
            if (ropeSprite != null) Destroy(ropeSprite);
            if (texture != null) Destroy(texture);
            if (actor != null) Destroy(actor.gameObject);
            if (tether != null) Destroy(tether.gameObject);
            if (dropped != null) Destroy(dropped.gameObject);
        }
    }
}
