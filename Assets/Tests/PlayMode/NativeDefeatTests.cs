using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using SaintKnight.Day01;
using SaintKnight.Day02;
using SaintKnight.Day04;
using SaintKnight.Day06;
using SaintKnight.Day09;
using SaintKnight.Day10;
using SaintKnight.SpriteCook;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class NativeDefeatRulesTests
{
    internal static readonly string[] Keys = { "monk", "executioner", "tentacle", "fleshwall", "boss_spikes", "boss_grab", "boss_spit", "boss1", "boss2", "boss3", "boss4", "boss5", "boss6" };
    [Serializable] private sealed class Source { public string path, sha256; }
    [Serializable] private sealed class Manifest { public Source[] sources; }
    [Serializable] private sealed class Cell { public int cell; }
    [Serializable] private sealed class MotionClip { public string motion; public Cell[] frames; }
    [Serializable] private sealed class Motions { public MotionClip[] clips; }
    [Serializable] private sealed class ContactFrame
    {
        public string character,key; public int frame;
        public float leftVineGap,rightVineGap,leftActorGap,rightActorGap,bootX,bootY;
    }
    [Serializable] private sealed class Contacts { public int version; public ContactFrame[] frames; }
    [Serializable] private sealed class StageFrame
    {
        public string character,key,phase; public int frame,actorPixels,visibleActorPixels,attachments;
        public float actorX,actorY,leftActorGap,rightActorGap,leftHoldGap,rightHoldGap;
    }
    [Serializable] private sealed class StageContacts { public int version; public StageFrame[] frames; }

    [Test]
    public void StageDefeatsHaveNativeContactsAndKeepTheTwoSwallowDirectionsDistinct()
    {
        var data=JsonUtility.FromJson<StageContacts>(Resources.Load<TextAsset>(SKNativeDefeatLibrary.Folder+"stage_contacts").text);
        Assert.AreEqual(1,data.version); Assert.AreEqual(144,data.frames.Length);
        var frames=new Dictionary<string,StageFrame>(); int contacts=0;
        foreach(var f in data.frames)
        {
            string key=f.character+"/"+f.key+"/"+f.frame; frames.Add(key,f);
            if(f.attachments>0)
            {
                contacts++; Assert.LessOrEqual(f.leftActorGap,1.5f,key+": held native arm exists");
                Assert.LessOrEqual(f.leftHoldGap,1.5f,key+": actual sleeve/root reaches it");
            }
            if(f.attachments>1)
            {
                Assert.LessOrEqual(f.rightActorGap,1.5f,key+": second native contact exists");
                Assert.LessOrEqual(f.rightHoldGap,1.5f,key+": second sleeve/root reaches it");
            }
        }
        Assert.Greater(contacts,70);
        foreach(string ch in new[]{"cecilia","adela"})
        {
            StageFrame F(string key,int f)=>frames[ch+"/"+key+"/"+f];
            Assert.Greater(F("monk",8).actorY,F("monk",0).actorY+4f,"Knees buckle instead of repeating a standing hold");
            Assert.Greater(F("monk",8).visibleActorPixels,250,"Native costume remains visible between the sleeves");
            Assert.AreEqual("sideways_hold",F("executioner",8).phase);
            Assert.Greater(F("executioner",8).visibleActorPixels,250,"The curled figure remains readable in the executioner's arms");
            Assert.Greater(F("tentacle",6).actorY,F("tentacle",3).actorY+12f,"Floor absorption goes down into the same grounded mouth");
            Assert.Greater(F("fleshwall",5).actorX,F("fleshwall",2).actorX+15f,"Wall absorption goes sideways");
            Assert.AreEqual(F("fleshwall",2).actorY,F("fleshwall",5).actorY,3f,"Kneeling body does not fall into a floor hole");
            foreach(string key in new[]{"tentacle","fleshwall"}) for(int f=8;f<18;f++)
            { Assert.AreEqual(0,F(key,f).actorPixels); Assert.AreEqual(0,F(key,f).visibleActorPixels,"No detached actor fragments after closure"); }
        }
    }

    [Test]
    public void MonksKneelingGarmentRetainsTheOriginalFullSpriteDetails()
    {
        for(int character=0;character<2;character++)
        {
            string ch=character==0?"cecilia":"adela";
            var original=Resources.Load<Texture2D>("SaintKnightDay06/"+ch+"_motion");
            var motions=JsonUtility.FromJson<Motions>(Resources.Load<TextAsset>("SaintKnightDay06/"+ch+"_motion_frames").text);
            int cell=-1;foreach(var clip in motions.clips)if(clip.motion=="Crouch")cell=clip.frames[0].cell;
            var source=original.GetPixels32();var library=SKNativeDefeatLibrary.Load(character,"monk");var atlas=library.Atlas;var result=atlas.GetPixels32();
            int opaque=0,matching=0,top=character==0?80:78;
            for(int y=top;y<top+8;y++)for(int x=62;x<69;x++)
            {
                var a=source[(original.height-1-cell/12*96-y)*original.width+cell%12*128+x];if(a.a==0)continue;opaque++;
                var b=result[(95-y-1)*atlas.width+8*128+x+12];if(a.Equals(b))matching++;
            }
            Assert.Greater(opaque,20);Assert.GreaterOrEqual(matching/(float)opaque,.75f,ch+": original garment/armour pixels remain at native scale");
        }
    }

    [Test]
    public void EachBossSceneRendersItsOwnPaintedCharacterKey()
    {
        string[] scenes = { "boss_spikes", "boss_grab", "boss_spit", "boss1", "boss2", "boss3", "boss4" };
        for (int character=0; character<2; character++)
        {
            string name=character==0?"cecilia":"adela";
            var source=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                Assert.IsTrue(source.LoadImage(File.ReadAllBytes(Path.Combine(Application.dataPath,
                    "SaintKnightSpriteCook/Editor/"+name+"_boss_pose_keys.png"))));
                Assert.AreEqual(128*scenes.Length,source.width); Assert.AreEqual(96,source.height);
                var keys=source.GetPixels32();
                for (int pose=0;pose<scenes.Length;pose++)
                {
                    var otherColours=new HashSet<Color32>();
                    var colours=new HashSet<Color32>();
                    for(int y=0;y<96;y++) for(int x=0;x<source.width;x++)
                    {
                        var c=keys[y*source.width+x]; if(c.a==0) continue;
                        (x/128==pose?colours:otherColours).Add(c);
                    }
                    colours.ExceptWith(otherColours);
                    var atlas=SKNativeDefeatLibrary.Load(character,scenes[pose]).Atlas;
                    var visible=new HashSet<Color32>();
                    var frame=atlas.GetPixels(8*128,0,128,96);
                    foreach(var c in frame) if(colours.Contains((Color32)c)) visible.Add((Color32)c);
                    Assert.Greater(visible.Count,8,name+"/"+scenes[pose]+" contains recognisable pixels exclusive to its painted pose");
                }
            }
            finally { Object.DestroyImmediate(source); }
        }
    }

    [Test]
    public void HeldArmsAndWristsRemainInContactWithTheRenderedVinesThroughoutTheLoop()
    {
        var data=JsonUtility.FromJson<Contacts>(Resources.Load<TextAsset>(SKNativeDefeatLibrary.Folder+"contacts").text);
        Assert.AreEqual(3,data.version,"Each composition supplies its own boot attachment as well as both arms");
        var cases=new HashSet<string>();
        var frames=new HashSet<string>();
        foreach(var f in data.frames)
        {
            string label=f.character+"/"+f.key+"/"+f.frame;
            Assert.IsTrue(frames.Add(label),"Duplicate attachment frame "+label);
            Assert.That(f.bootX,Is.InRange(0f,127f)); Assert.That(f.bootY,Is.InRange(1f,95f));
            if(f.frame<8) continue; // During the entrance the receiving limb is still growing.
            cases.Add(f.character+"/"+f.key);
            Assert.LessOrEqual(f.leftVineGap,1.5f,label+": left held area reaches real vine pixels");
            Assert.LessOrEqual(f.rightVineGap,1.5f,label+": right held area reaches real vine pixels");
            Assert.LessOrEqual(f.leftActorGap,1.5f,label+": left held arm has visible native art");
            Assert.LessOrEqual(f.rightActorGap,1.5f,label+": right held arm has visible native art");
        }
        Assert.AreEqual(18,cases.Count);
        foreach (string ch in new[]{"cecilia","adela"})
        foreach (string key in new[]{"boss_spikes","boss_grab","boss_spit","boss1","boss2","boss3","boss4","boss5","boss6"})
        {
            int first = key=="boss1" || key=="boss2" || key=="boss3" || key=="boss4" ? 0 : 8;
            int count = key=="boss_grab" ? 32 : 18;
            for (int i=first;i<count;i++) Assert.IsTrue(frames.Contains(ch+"/"+key+"/"+i));
        }
    }

    [Test]
    public void FatalGrabUsesTheFourPaintedEntranceKeysInOrder()
    {
        for (int character=0;character<2;character++)
        {
            string name=character==0?"cecilia":"adela";
            var keys=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                Assert.IsTrue(keys.LoadImage(File.ReadAllBytes(Path.Combine(Application.dataPath,
                    "SaintKnightSpriteCook/Editor/"+name+"_grab_entrance.png"))));
                var source=keys.GetPixels32();
                var atlas=SKNativeDefeatLibrary.Load(character,"boss_grab").Atlas;
                var pixels=atlas.GetPixels32();
                for (int frame=1;frame<8;frame++)
                {
                    int cell=(frame-1)/2, visible=0, matching=0;
                    for(int y=0;y<96;y++) for(int x=0;x<128;x++)
                    {
                        var c=source[y*keys.width+128*cell+x];
                        if(c.a==0) continue;
                        visible++;
                        if(c.Equals(pixels[y*atlas.width+128*frame+x])) matching++;
                    }
                    Assert.Greater(visible,350);
                    Assert.Greater(matching/(float)visible,.7f,name+" entrance frame "+frame);
                }
            }
            finally { Object.DestroyImmediate(keys); }
        }
    }

    [Test]
    public void InitialGrabCopiesTheCurrentCharacterPixelsAtTheirOriginalCoordinates()
    {
        for(int character=0;character<2;character++)
        {
            string name=character==0?"cecilia":"adela";
            var original=Resources.Load<Texture2D>("SaintKnightDay06/"+name+"_motion");
            var motions=JsonUtility.FromJson<Motions>(Resources.Load<TextAsset>("SaintKnightDay06/"+name+"_motion_frames").text);
            int cell=-1; foreach(var clip in motions.clips) if(clip.motion=="Hurt") cell=clip.frames[0].cell;
            Assert.GreaterOrEqual(cell,0);
            var source=original.GetPixels32(); var atlas=SKNativeDefeatLibrary.Load(character,"boss_grab").Atlas;
            var result=atlas.GetPixels32(); int originalOpaque=0, exact=0;
            for(int y=0;y<96;y++) for(int x=0;x<128;x++)
            {
                var a=source[(original.height-(cell/12+1)*96+y)*original.width+cell%12*128+x];
                if(a.a==0) continue; originalOpaque++;
                var b=result[y*atlas.width+x]; if(a.Equals(b)) exact++;
            }
            Assert.Greater((float)exact/originalOpaque,.8f,name+": unoccluded source pixels have unchanged shape, colour and scale");
        }
    }

    [Test]
    public void AllTwentySixClipsAreNativeTransparentAndKeepMovingAfterTheirIntro()
    {
        for (int character = 0; character < 2; character++) foreach (string key in Keys)
        {
            var library = SKNativeDefeatLibrary.Load(character, key);
            Assert.IsNotNull(library, character + "/" + key);
            Assert.AreEqual(FilterMode.Point, library.Atlas.filterMode);
            var introHashes = new HashSet<ulong>(); var loopHashes = new HashSet<ulong>();
            var pixels = library.Atlas.GetPixels32();
            Assert.AreEqual(key=="boss_grab"?32:18,library.Length);
            for (int frame = 0; frame < library.Length; frame++)
            {
                var sprite = library.Sample((frame + .01f) / SKNativeDefeatLibrary.FramesPerSecond);
                Assert.AreEqual(48f, sprite.pixelsPerUnit); Assert.AreEqual(key == "fleshwall" ? 0f : 25.56f, sprite.pivot.y, .001f);
                ulong hash = 1469598103934665603; int opaque = 0, clear = 0;
                unchecked
                {
                    for (int y = 0; y < library.FrameHeight; y++) for (int x = 0; x < library.FrameWidth; x++)
                    {
                        Color32 c = pixels[y * library.Atlas.width + frame * library.FrameWidth + x];
                        if (c.a > 0) opaque++; else clear++;
                        hash = (hash ^ c.r) * 1099511628211;
                        hash = (hash ^ c.g) * 1099511628211;
                        hash = (hash ^ c.b) * 1099511628211;
                        hash = (hash ^ c.a) * 1099511628211;
                    }
                }
                Assert.Greater(opaque, 120, key + " has visible art"); Assert.Greater(clear, 2500, key + " has genuine transparency");
                if (frame<8) introHashes.Add(hash); else loopHashes.Add(hash);
            }
            if (SKNativeDefeatLibrary.IsBossPattern(key) || key=="boss1" || key=="boss2" || key=="boss3" || key=="boss4")
                Assert.GreaterOrEqual(introHashes.Count,6,key+" has a changing entrance silhouette");
            introHashes.UnionWith(loopHashes);
            Assert.GreaterOrEqual(introHashes.Count,6,key+" has at least six distinct animation frames");
            Assert.GreaterOrEqual(loopHashes.Count, 3, key + " continues moving after the introduction");
            Assert.AreSame(library.Sample(1.001f), library.Sample(library.Length/8f+.001f), "Only the settled frames loop");
            if (SKNativeDefeatLibrary.IsBossPattern(key))
            {
                var live=SKNativeDefeatLibrary.LoadHeld(character,key);
                Assert.IsNotNull(live); Assert.AreEqual(18,live.Length);
                Assert.AreNotSame(live.Atlas,library.Atlas,"Living captures must retain their original sheet");
            }
        }
        Assert.IsNull(SKNativeDefeatLibrary.Load(2, "monk"));
        Assert.IsNull(SKNativeDefeatLibrary.Load(0, "slime"), "User retained ordinary slime");
        Assert.IsNull(SKNativeDefeatLibrary.Load(0, "boss_sweep"), "User retained sweep");
    }

    [Test]
    public void EveryUsedOriginalSourceStillMatchesItsRecordedBytes()
    {
        var manifest = JsonUtility.FromJson<Manifest>(Resources.Load<TextAsset>(SKNativeDefeatLibrary.Folder + "manifest").text);
        var used=new HashSet<string>(); foreach(var source in manifest.sources) used.Add(source.path);
        // Native atlas articulation removes the old collapse/merged-pose input files.
        // Verify complete character/enemy/trap inputs instead of assuming a minimum file count.
        foreach(string ch in new[]{"cecilia","adela"})
        {
            Assert.IsTrue(used.Contains("Assets/SaintKnightDay06/Resources/SaintKnightDay06/"+ch+"_motion.png"));
            foreach(string kind in new[]{"tentacle","wall","bossslime"})
                Assert.IsTrue(used.Contains("Assets/SaintKnightDay10/Resources/SaintKnightDay10/TrapMotion/"+ch+"_"+kind+".png"));
        }
        foreach(string path in new[]{"monk/idle","monk/grab-windup","executioner/idle","executioner/recovery"})
            Assert.IsTrue(used.Contains("Assets/SaintKnightDay09/Art/Enemies/"+path+"_f00.png"));
        Assert.IsTrue(used.Contains("Assets/SaintKnightDay09/Art/Boss/boss_grab_reach_f00_NOT_FINAL.png"));
        foreach (var source in manifest.sources)
        {
            Assert.IsTrue(source.path.StartsWith("Assets/", StringComparison.Ordinal));
            using (var sha = SHA256.Create())
            {
                string actual = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(Application.dataPath, "..", source.path)))).Replace("-", "").ToLowerInvariant();
                Assert.AreEqual(source.sha256, actual, source.path + " was copied, never overwritten");
            }
        }
    }
}

public sealed class NativeDefeatIntegrationTests
{
    private Keyboard keyboard;
    private InputSettings.BackgroundBehavior oldBackground;
#if UNITY_EDITOR
    private InputSettings.EditorInputBehaviorInPlayMode oldInput;
#endif
    private float oldCapture;
    private SKDay01Player player;
    private SKHealth hp;
    private SKCharacterSpriteView view;
    private SKTrapEnvelopeView envelope;
    private SKEnemyTrainingSession session;
    private Rigidbody2D body;
    private BoxCollider2D box;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        SKHitStop.CancelActive(); Time.timeScale = 1f; oldCapture = Time.captureDeltaTime; Time.captureDeltaTime = 1f / 60f;
        oldBackground = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
        oldInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        keyboard = InputSystem.AddDevice<Keyboard>(); yield return null;
    }
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        SKHitStop.CancelActive(); Time.timeScale = 1f; Time.captureDeltaTime = oldCapture;
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        InputSystem.settings.backgroundBehavior = oldBackground;
#if UNITY_EDITOR
        InputSystem.settings.editorInputBehaviorInPlayMode = oldInput;
#endif
        yield return null;
    }
    private IEnumerator Load(string scene)
    {
        yield return SceneManager.LoadSceneAsync(scene); yield return null;
        player = Object.FindFirstObjectByType<SKDay01Player>(); hp = player.GetComponent<SKHealth>();
        view = player.GetComponent<SKCharacterSpriteView>(); envelope = player.GetComponent<SKTrapEnvelopeView>();
        body = player.GetComponent<Rigidbody2D>(); box = player.GetComponent<BoxCollider2D>();
        session = Object.FindFirstObjectByType<SKEnemyTrainingSession>();
        foreach (var enemy in Object.FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None)) enemy.enabled = false;
        foreach (var spike in Object.FindObjectsByType<SKFloorSpike>(FindObjectsSortMode.None)) spike.enabled = false;
    }
    private static IEnumerator Until(Func<bool> condition, float seconds, string message)
    {
        float gameEnd = Time.time + seconds, realEnd = Time.realtimeSinceStartup + 30f;
        while (!condition() && Time.time < gameEnd && Time.realtimeSinceStartup < realEnd) yield return null;
        Assert.IsTrue(condition(), message);
    }
    private IEnumerator Tap(Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
    }
    private void Place(float x, float floor)
    {
        Vector2 at = new Vector2(x, floor - (box.bounds.min.y - body.position.y) + .02f);
        body.position = at; body.linearVelocity = Vector2.zero; player.transform.position = at; Physics2D.SyncTransforms();
    }

    [UnityTest]
    public IEnumerator AnimatedDefeatWaitsForRetryThenRestoresBothOriginalRenderers()
    {
        yield return Load("Day09_Stage1");
        SKEnemyAgent monk = null;
        foreach (var enemy in Object.FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None)) if (enemy.DisplayName.EndsWith("MONK")) monk = enemy;
        var actor = player.GetComponent<SpriteRenderer>(); int originalOrder = actor.sortingOrder;
        var originalMaterial = actor.sharedMaterial;
        hp.Drain(hp.CurrentHp, monk);
        yield return null; view.Refresh();
        Assert.AreEqual("monk", view.NativeSceneKey); Assert.IsTrue(view.HasNativeDeathMotion);
        Assert.IsTrue(session.HoldRetry); Assert.IsFalse(monk.GetComponent<SpriteRenderer>().enabled);
        Assert.AreEqual(SKNativeDefeatLibrary.DefeatSortingOrder, actor.sortingOrder);
        StringAssert.Contains("Sprite-Unlit", actor.sharedMaterial.shader.name, "Defeat remains readable under world lighting");
        var first = view.Current;
        yield return new WaitForSeconds(session.AutoRetrySeconds + .3f);
        Assert.IsTrue(hp.IsDead); Assert.AreNotSame(first, view.Current);
        yield return Tap(Key.R); yield return null;
        Assert.IsFalse(hp.IsDead); Assert.IsFalse(view.UsesNativeMotion); Assert.IsFalse(envelope.IsVisible);
        Assert.IsTrue(monk.GetComponent<SpriteRenderer>().enabled); Assert.IsFalse(session.HoldRetry);
        Assert.AreEqual(originalOrder, actor.sortingOrder, "Retry restores normal world layering");
        Assert.AreSame(originalMaterial, actor.sharedMaterial, "Retry restores the character's original combat material");
    }

    [UnityTest]
    public IEnumerator BothStageEnemyDefeatsFaceTheirActualAttackerAndRestoreOnRetry()
    {
        yield return Load("Day09_Stage1");
        var pool=Object.FindFirstObjectByType<SKSlimePool>();
        var renderer=player.GetComponent<SpriteRenderer>();var original=renderer.sharedMaterial;
        for(int character=0;character<2;character++) foreach(string key in new[]{"monk","executioner"}) foreach(float side in new[]{-1f,1f})
        {
            session.ResetTraining();yield return null;if(player.SelectedCharacter!=character)player.SwitchCharacter();
            Place(pool.transform.position.x-2.5f,pool.transform.position.y);
            yield return Until(()=>player.IsGrounded,1f,"Enemy defeat floor");
            SKEnemyAgent killer=null;foreach(var e in Object.FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None))
                if(e.DisplayName.EndsWith(key.ToUpperInvariant()))killer=e;
            Assert.IsNotNull(killer);var enemyBody=killer.GetComponent<Rigidbody2D>();
            enemyBody.position=new Vector2(player.transform.position.x+side,enemyBody.position.y);
            killer.transform.position=new Vector3(enemyBody.position.x,killer.transform.position.y,killer.transform.position.z);
            hp.Drain(hp.CurrentHp,killer);yield return null;view.Refresh();
            Assert.AreEqual(key,view.NativeSceneKey);Assert.AreEqual(side>0,view.NativeFlipX);
            Assert.IsFalse(killer.GetComponent<SpriteRenderer>().enabled);Assert.IsTrue(session.HoldRetry);
            yield return new WaitForSeconds(1.25f);view.Refresh();
            Assert.IsTrue(view.UsesNativeMotion);Assert.IsFalse(view.IsBossCarryActive);
            Assert.IsFalse(envelope.IsVisible,"No unrelated trap foreground covers the enemy scene");
            yield return Tap(Key.R);yield return null;view.Refresh();
            Assert.IsFalse(hp.IsDead);Assert.IsFalse(view.UsesNativeMotion);Assert.IsTrue(renderer.enabled);
            Assert.IsTrue(killer.GetComponent<SpriteRenderer>().enabled);Assert.AreSame(original,renderer.sharedMaterial);
        }
    }

    [UnityTest]
    public IEnumerator LiveTentacleDepthFreezesWithCombatAndDisablingTheSourceClearsTheForeground()
    {
        yield return Load("Day09_Stage1");
        var spike = Object.FindFirstObjectByType<SKFloorSpike>();
        yield return Until(() => player.IsGrounded, 2f, "Grounded");
        Assert.IsTrue(spike.Capture.TryCapture(player));
        yield return new WaitForSeconds(1.8f); view.Refresh();
        Assert.AreEqual("tentacle", view.NativeSceneKey); Assert.IsTrue(view.UsesNativeMotion);
        float before = hp.CurrentHp; var frozen = view.Current;
        for (int i = 0; i < 5; i++)
        {
            SKHitStop.Request(.1f, player.gameObject.scene); yield return null;
            Assert.AreEqual(before, hp.CurrentHp); Assert.AreSame(frozen, view.Refresh());
        }
        SKHitStop.CancelActive(); hp.Drain(hp.CurrentHp, spike.Capture); yield return null; view.Refresh();
        Assert.IsTrue(spike.Capture.IsPlayerConsumed); Assert.IsTrue(envelope.IsVisible);
        spike.Capture.enabled = false; yield return null; view.Refresh();
        Assert.IsFalse(envelope.IsVisible); Assert.IsFalse(player.GetComponent<SpriteRenderer>().forceRenderingOff);
    }

    [UnityTest]
    public IEnumerator FatalBossGrabPlaysFourSecondsThenTheFourReferencesCycleInOrder()
    {
        yield return Load("Day09_BossChapel"); var boss = Object.FindFirstObjectByType<SKBossAgent>();
        var ready = typeof(SKBossAgent).GetField("readyAt", BindingFlags.Instance | BindingFlags.NonPublic);
        Place(boss.transform.position.x - 2f, box.bounds.min.y);
        boss.ForceNext(SKBossAttack.Grab); ready.SetValue(boss, 0f);
        yield return Until(() => boss.Attack == SKBossAttack.Grab, 3f, "Actual boss grab starts");
        hp.Drain(hp.CurrentHp, boss); yield return null; view.Refresh();
        yield return Until(()=>boss.Attack==SKBossAttack.None,.2f,"Boss fixed step cancels the lethal attack");
        Assert.AreEqual(SKBossAttack.None, boss.Attack, "Boss cancels combat after death");
        Assert.AreEqual("boss_grab", view.NativeSceneKey, "The accepted lethal cause was retained");
        yield return new WaitForSeconds(2.5f); view.Refresh(); Assert.AreEqual("boss_grab", view.NativeSceneKey);
        Assert.IsTrue(view.BossCarry.TetherVisible,"The actual boss holds the kneeling figure");
        Assert.IsFalse(view.IsBossCarryActive,"The figure is still on the floor before the delivery");
        yield return Until(()=> { view.Refresh(); return view.NativeSceneKey=="boss1"; },1.7f,"The held pose gives way to the first common composition");
        StringAssert.Contains("_f00", view.Current.name, "The receiving roots grow around the low pose");
        var seen = new List<string>();
        for (int i = 0; i < 4; i++)
        {
            session.ResetTraining(); yield return null; boss.enabled = false;
            hp.Drain(hp.CurrentHp, boss); yield return null; view.Refresh(); seen.Add(view.NativeSceneKey);
            Assert.IsTrue(session.HoldRetry);
        }
        CollectionAssert.AreEqual(new[] { "boss2","boss3","boss4","boss1" }, seen);
    }

    [UnityTest]
    public IEnumerator AnAcceptedBossFloorHitRetainsItsOwnPatternEvenDuringASweepCombo()
    {
        yield return Load("Day09_BossChapel"); var boss = Object.FindFirstObjectByType<SKBossAgent>(); boss.enabled = false;
        yield return Until(() => player.IsGrounded, 2f, "Settled");
        var spike = boss.GetComponentInChildren<SKFloorSpike>(true); spike.enabled = true;
        typeof(SKBossAgent).GetField("<Attack>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(boss, SKBossAttack.Sweep);
        hp.Drain(hp.CurrentHp - 1f);
        spike.Arm(new Vector2(player.transform.position.x, box.bounds.min.y), 0f);
        yield return Until(() => hp.IsDead, 1.5f, "The real spike hit is lethal");
        yield return null; view.Refresh();
        Assert.AreEqual("boss_spikes", view.NativeSceneKey, "Accepted-hit source refines the display without changing health ownership");
        Assert.IsTrue(view.HasNativeDeathMotion);
    }

    [UnityTest]
    public IEnumerator BothCharactersAreDeliveredToTheActualBossFromEitherSideAndRetryRestoresThePlayer()
    {
        yield return Load("Day09_BossChapel"); var boss=Object.FindFirstObjectByType<SKBossAgent>(); boss.enabled=false;
        var attack=typeof(SKBossAgent).GetField("<Attack>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic);
        var renderer=player.GetComponent<SpriteRenderer>(); var originalMaterial=renderer.sharedMaterial;
        for(int character=0;character<2;character++) foreach(float side in new[]{-1f,1f})
        {
            session.ResetTraining(); yield return null; boss.enabled=false;
            if(player.SelectedCharacter!=character) player.SwitchCharacter();
            Place(boss.transform.position.x+side*2.8f,box.bounds.min.y);
            yield return Until(()=>player.IsGrounded,1f,"Settled before boss defeat");
            var physical=body.position; attack.SetValue(boss,SKBossAttack.None);
            hp.Drain(hp.CurrentHp,boss); yield return null; view.Refresh();
            var carry=view.BossCarry; Assert.IsTrue(carry.IsActive); Assert.IsFalse(renderer.enabled);
            Assert.IsFalse(envelope.IsVisible,"No second figure remains at the old trap position");
            Assert.AreEqual(side>0f,view.NativeFlipX,"The actual boss owns the direction");
            float before=Mathf.Abs(carry.WorldPosition.x-boss.transform.position.x);
            var gearAt=carry.DroppedPosition;
            yield return new WaitForSeconds(1.3f); view.Refresh();
            Assert.Greater(carry.Progress,0f); Assert.Less(carry.Progress,1f);
            var frozen=carry.WorldPosition;
            for(int i=0;i<3;i++)
            { SKHitStop.Request(.1f,player.gameObject.scene); yield return null; view.Refresh(); Assert.AreEqual(frozen,carry.WorldPosition); }
            SKHitStop.CancelActive();
            yield return Until(()=>carry.AtBoss,5f,"Figure reaches the boss"); view.Refresh();
            Assert.Less(Mathf.Abs(carry.WorldPosition.x-boss.transform.position.x),before*.5f);
            Assert.Greater(carry.WorldPosition.y,physical.y+.4f);
            Assert.Less(Vector2.Distance(body.position,physical),.06f,"Only the visual moves, not the player collider");
            Assert.AreEqual(gearAt,carry.DroppedPosition,"Dropped gear is not carried across the floor");
            Assert.IsTrue(carry.TetherVisible); Assert.IsTrue(carry.ActorRenderer.enabled);
            Assert.AreEqual(48f,carry.ActorRenderer.sprite.pixelsPerUnit);
            yield return Tap(Key.R); yield return null; view.Refresh();
            Assert.IsFalse(carry.IsActive); Assert.IsFalse(carry.TetherVisible); Assert.IsTrue(renderer.enabled);
            Assert.AreSame(originalMaterial,renderer.sharedMaterial); Assert.IsFalse(hp.IsDead);
        }
    }

    [UnityTest]
    public IEnumerator BossDeliveryRetainsTheAcceptedPuddleOwnerAndCleansUpOnDisable()
    {
        yield return Load("Day09_BossChapel"); var boss=Object.FindFirstObjectByType<SKBossAgent>(); boss.enabled=false;
        Place(boss.transform.position.x-2.6f,box.bounds.min.y);
        yield return Until(()=>player.IsGrounded,1f,"Pool floor");
        var pool=boss.GetComponentInChildren<SKSlimePool>(true);
        pool.Spawn(new Vector2(player.transform.position.x,box.bounds.min.y),8f);
        yield return null; Assert.IsTrue(pool.Capture.TryCapture(player));
        hp.Drain(hp.CurrentHp,pool.Capture); yield return null; view.Refresh();
        Assert.AreEqual("boss_spit",view.NativeSceneKey);
        Assert.IsFalse(view.NativeFlipX,"The fatal spray arrives from its boss, independent of the puddle position");
        yield return new WaitForSeconds(2.4f); view.Refresh(); Assert.IsTrue(view.IsBossCarryActive);
        Assert.IsFalse(view.NativeFlipX,"Puddle at the player's feet does not reverse the boss direction");
        pool.Capture.enabled=false; yield return null; view.Refresh();
        Assert.IsTrue(view.IsBossCarryActive,"Accepted boss ownership survives the transient puddle");
        yield return Until(()=>view.BossCarry.AtBoss,5f,"Puddle victim delivered");
        view.enabled=false; yield return null;
        Assert.IsFalse(view.BossCarry.IsActive); Assert.IsTrue(player.GetComponent<SpriteRenderer>().enabled);
        view.enabled=true; pool.Capture.enabled=true;
        yield return Tap(Key.R); yield return null; view.Refresh();
        Assert.IsFalse(view.IsBossCarryActive); Assert.IsFalse(envelope.IsVisible); Assert.IsFalse(hp.IsDead);
    }

    private static string CaptureFolder(string subfolder, string defaultRoot)
    {
        string root = Environment.GetEnvironmentVariable("SK_NATIVE_CAPTURE_ROOT");
        string path = Path.Combine(string.IsNullOrEmpty(root) ? defaultRoot : root, subfolder);
        Directory.CreateDirectory(path);
        return path + Path.DirectorySeparatorChar;
    }

    [UnityTest, Explicit("Capture current game renders for each reachable native scene; graphics device required."), Timeout(600000)]
    public IEnumerator CaptureAllNativeScenes()
    {
        Assert.AreNotEqual(GraphicsDeviceType.Null, SystemInfo.graphicsDeviceType);
        string folder = CaptureFolder("Screens","Logs/EnemyDefeat20261118");
        var lines = new List<string> { "character,requested_scene,frame,visible_scene,file" };
        yield return Load("Day09_Stage1");
        for (int character = 0; character < 2; character++)
        {
            session.ResetTraining(); yield return null;
            if (player.SelectedCharacter != character) player.SwitchCharacter();
            Assert.AreEqual(character, player.SelectedCharacter);
            foreach (string key in new[] { "monk", "executioner", "tentacle", "fleshwall" })
            {
                session.ResetTraining(); yield return null;
                Component cause = null;
                if (key == "monk" || key == "executioner")
                {
                    foreach (var enemy in Object.FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None))
                        if (enemy.DisplayName.EndsWith(key.ToUpperInvariant())) cause = enemy;
                    var pool = Object.FindFirstObjectByType<SKSlimePool>(); Place(pool.transform.position.x - 2.5f, pool.transform.position.y);
                }
                else if (key == "tentacle")
                {
                    var spike = Object.FindFirstObjectByType<SKFloorSpike>(); cause = spike.Capture;
                    Place(spike.transform.position.x, spike.transform.position.y);
                    yield return Until(() => player.IsGrounded, 1f, "Trap floor"); Assert.IsTrue(spike.Capture.TryCapture(player));
                }
                else
                {
                    var wall = Object.FindFirstObjectByType<SKFleshWall>(); cause = wall;
                    Place(wall.transform.position.x - .7f, wall.transform.position.y);
                    yield return Until(() => wall.IsHoldingPlayer, 2f, "Actual wall capture");
                }
                yield return Until(() => player.IsGrounded, 1f, "Capture grounded");
                if (key == "tentacle" || key == "fleshwall")
                    yield return CaptureClip(character, key, folder, lines, key + "_capture");
                hp.Drain(hp.CurrentHp, cause); yield return null;
                yield return CaptureClip(character, key, folder, lines);
            }
        }
        yield return Load("Day09_BossChapel"); var boss = Object.FindFirstObjectByType<SKBossAgent>(); boss.enabled = false;
        var attack = typeof(SKBossAgent).GetField("<Attack>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        for (int character = 0; character < 2; character++)
        {
            session.ResetTraining(); yield return null;
            if (player.SelectedCharacter != character) player.SwitchCharacter();
            Assert.AreEqual(character, player.SelectedCharacter);
            foreach (string key in new[] { "boss_spikes", "boss_grab", "boss_spit", "boss1", "boss2", "boss3", "boss4" })
            {
                session.ResetTraining(); yield return null; boss.enabled = false;
                Place(boss.transform.position.x - 2.4f, box.bounds.min.y);
                attack.SetValue(boss, key == "boss_spikes" ? SKBossAttack.Spikes : key == "boss_grab" ? SKBossAttack.Grab : key == "boss_spit" ? SKBossAttack.Spit : SKBossAttack.None);
                if (key.Length == 5 && key.StartsWith("boss"))
                    typeof(SKCharacterSpriteView).GetField("nativeBossDeaths", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(view, key[4] - '1');
                Component cause = boss;
                if (key == "boss_spit")
                {
                    var pool = boss.GetComponentInChildren<SKSlimePool>(true);
                    pool.Spawn(new Vector2(player.transform.position.x, box.bounds.min.y), 8f);
                    yield return null; Assert.IsTrue(pool.Capture.TryCapture(player));
                    cause = pool.Capture;
                    yield return CaptureClip(character, key, folder, lines, key + "_capture");
                }
                hp.Drain(hp.CurrentHp, cause); yield return null;
                yield return CaptureClip(character, key, folder, lines);
            }
        }
        File.WriteAllLines(folder + "index.csv", lines);
        Debug.Log("NATIVE_DEFEAT_SCREENS=" + Path.GetFullPath(folder));
    }

    private IEnumerator CaptureClip(int character, string key, string folder, List<string> lines, string recordName = null)
    {
        recordName = recordName ?? key;
        string name = character == 0 ? "cecilia" : "adela";
        float started = Time.time;
        bool common = key.Length==5 && key.StartsWith("boss",StringComparison.Ordinal);
        int samples = common || key=="boss_grab" ? 32 : 18;
        var expected=SKNativeDefeatLibrary.Load(character,key,defeat:hp.IsDead);
        for (int f = 0; f < samples; f++)
        {
            while (Time.time-started+.0001f < f/8f) yield return null;
            // Coroutines resume after Update, before LateUpdate: apply the actual final display state
            // after SKHealth's generic dead tint, just as SKCharacterSpriteView.LateUpdate does on screen.
            view.Refresh();
            Assert.IsTrue(view.UsesNativeMotion, key+" is actually drawn by the current scene");
            Assert.AreEqual(key,view.NativeSceneKey); Assert.AreSame(expected.Atlas,view.Current.texture);
            var screen = RenderCurrentGame();
            try
            {
                if (f == 0 || f == 8 || f == samples-1)
                {
                    string file = name + "_" + recordName + "_" + f.ToString("D2") + ".png";
                    File.WriteAllBytes(folder + file, screen.EncodeToPNG());
                    lines.Add(name + "," + recordName + "," + f + "," + view.NativeSceneKey + "," + file);
                }
                Vector3 center = Camera.main.WorldToViewportPoint(view.VisualPosition + Vector3.up * .35f);
                center.x *= screen.width; center.y *= screen.height;
                int cw = Mathf.Min(384, screen.width), ch = Mathf.Min(256, screen.height);
                int left = Mathf.Clamp(Mathf.RoundToInt(center.x) - cw / 2, 0, screen.width - cw);
                int bottom = Mathf.Clamp(Mathf.RoundToInt(center.y) - ch / 2, 0, screen.height - ch);
                var crop = new Texture2D(cw, ch, TextureFormat.RGB24, false);
                crop.SetPixels(screen.GetPixels(left, bottom, cw, ch)); crop.Apply();
                File.WriteAllBytes(folder + name + "_" + recordName + "_motion_" + f.ToString("D2") + ".png", crop.EncodeToPNG());
                Object.Destroy(crop);
            }
            finally { Object.Destroy(screen); }
        }
    }

    [UnityTest, Explicit("Actual fixed-camera boss delivery, both characters and both sides."), Timeout(240000)]
    public IEnumerator CaptureBossDelivery()
    {
        Assert.AreNotEqual(GraphicsDeviceType.Null,SystemInfo.graphicsDeviceType);
        yield return Load("Day09_BossChapel"); var boss=Object.FindFirstObjectByType<SKBossAgent>(); boss.enabled=false;
        var attack=typeof(SKBossAgent).GetField("<Attack>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic);
        string folder=CaptureFolder("Delivery","Logs/BossCarry20261118");
        var lines=new List<string>{"character,side,cause,frame,progress,at_boss,actor_x,actor_y,boss_x,file"};
        for(int character=0;character<2;character++) foreach(float side in new[]{-1f,1f})
        {
            session.ResetTraining(); yield return null; boss.enabled=false;
            if(player.SelectedCharacter!=character) player.SwitchCharacter();
            Place(boss.transform.position.x+side*2.8f,box.bounds.min.y);
            yield return Until(()=>player.IsGrounded,1f,"Delivery capture floor");
            Component cause=boss; string kind=side<0?"common":character==0?"grab":"spit";
            attack.SetValue(boss,kind=="grab"?SKBossAttack.Grab:kind=="spit"?SKBossAttack.Spit:SKBossAttack.None);
            if(kind=="spit")
            {
                var pool=boss.GetComponentInChildren<SKSlimePool>(true);
                pool.Spawn(new Vector2(player.transform.position.x,box.bounds.min.y),12f);
                yield return null; Assert.IsTrue(pool.Capture.TryCapture(player)); cause=pool.Capture;
            }
            float midX=(player.transform.position.x+boss.transform.position.x)*.5f;
            var center=Camera.main.WorldToViewportPoint(new Vector3(midX,player.transform.position.y+.9f,0));
            int left=Mathf.Clamp(Mathf.RoundToInt(center.x*1280)-320,320,640);
            int bottom=Mathf.Clamp(Mathf.RoundToInt(center.y*720)-200,0,320);
            hp.Drain(hp.CurrentHp,cause); yield return null;
            string name=(character==0?"cecilia":"adela")+"_"+(side<0?"left":"right");
            for(int f=0;f<48;f++)
            {
                yield return null; view.Refresh();
                var screen=RenderCurrentGame(); var crop=new Texture2D(640,400,TextureFormat.RGB24,false);
                crop.SetPixels(screen.GetPixels(left,bottom,640,400));crop.Apply();
                string file=name+"_"+f.ToString("D2")+".png";
                File.WriteAllBytes(folder+file,crop.EncodeToPNG());Object.Destroy(crop);
                if(f==0||f==16||f==32||f==47) File.WriteAllBytes(folder+name+"_full_"+f.ToString("D2")+".png",screen.EncodeToPNG());
                Object.Destroy(screen);
                var pos=view.VisualPosition;var carry=view.BossCarry;
                lines.Add(string.Join(",",name,side,kind,f,carry.Progress,carry.AtBoss,pos.x,pos.y,boss.transform.position.x,file));
                yield return new WaitForSeconds(.125f);
            }
            Assert.IsTrue(view.BossCarry.AtBoss,name+" actually arrives in the live scene");
        }
        File.WriteAllLines(folder+"index.csv",lines);
        Debug.Log("NATIVE_BOSS_DELIVERY_SCREENS="+Path.GetFullPath(folder));
    }

    // Same play-mode camera/HUD capture path used by the project's existing graphics probes.
    // Batch mode has no end-of-frame backbuffer callback.
    private static Texture2D RenderCurrentGame()
    {
        var camera = Camera.main;
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        var modes = new RenderMode[canvases.Length]; var cameras = new Camera[canvases.Length];
        var planes = new float[canvases.Length];
        var target = new RenderTexture(1280, 720, 24);
        var previousTarget = camera.targetTexture; var previousRect = camera.rect;
        var previousActive = RenderTexture.active;
        try
        {
            for (int i = 0; i < canvases.Length; i++)
            {
                modes[i] = canvases[i].renderMode; cameras[i] = canvases[i].worldCamera; planes[i] = canvases[i].planeDistance;
                if (modes[i] != RenderMode.ScreenSpaceOverlay) continue;
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera;
                canvases[i].planeDistance = camera.nearClipPlane + .1f;
            }
            camera.rect = new Rect(0, 0, 1, 1); camera.targetTexture = target;
            Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply(); return image;
        }
        finally
        {
            camera.targetTexture = previousTarget; camera.rect = previousRect; RenderTexture.active = previousActive;
            for (int i = 0; i < canvases.Length; i++)
            { canvases[i].renderMode = modes[i]; canvases[i].worldCamera = cameras[i]; canvases[i].planeDistance = planes[i]; }
            Object.Destroy(target);
        }
    }
}
