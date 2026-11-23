using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace SaintKnight.SpriteCook.Editor
{
    /// <summary>Bakes game-sized defeat animation without overwriting original art.
    /// The seven active boss scenes use character-specific painted poses and marked attachments.</summary>
    public static partial class SKNativeDefeatBuilder
    {
        private const int W = 128, H = 96, Ground = 92, Count = 18;
        private const string Output = "Assets/SaintKnightSpriteCook/Resources/SaintKnightSpriteCook/DefeatMotion/";
        private const string Evidence = "Docs/Design/DeathScenes-20261117/";
        private static readonly string[] Keys = {
            "monk", "executioner", "tentacle", "fleshwall", "boss_spikes", "boss_grab", "boss_spit",
            "boss1", "boss2", "boss3", "boss4", "boss5", "boss6"
        };
        private static readonly Color32[] Root = { C(30,16,24), C(69,32,49), C(113,49,63), C(220,138,123) };
        private static readonly Color32[] Wall = { C(22,13,23), C(44,23,36), C(115,56,62), C(199,119,101) };
        private static readonly Color32[] Robe = { C(23,17,31), C(53,33,52), C(97,65,83), C(166,127,119) };
        private static readonly Color32[] Slime = { C(42,25,39), C(82,45,66), C(137,85,110), C(203,158,167) };
        private static readonly Color32[] Cloth = { C(25,21,21), C(54,42,33), C(89,67,49), C(135,107,78) };
        private static readonly Color32[] DarkVine = { C(18,16,22), C(36,29,38), C(66,48,61), C(111,75,86) };
        private static readonly Color32[] RedVine = { C(32,15,27), C(75,29,47), C(122,48,65), C(189,99,108) };
        private static readonly Color32[] BarkVine = { C(25,18,23), C(58,36,40), C(108,63,61), C(163,104,86) };
        private static readonly Dictionary<string, Image> Images = new Dictionary<string, Image>();
        private static readonly Dictionary<string, string> Hashes = new Dictionary<string, string>();
        private static readonly Dictionary<string, Image> Materials = new Dictionary<string, Image>();
        private static Image vineRaster,actorRaster;
        private static Vector2 actorLeft,actorRight,actorBoot;
        private static string actorLeftPart="wrist",actorRightPart="wrist";
        private static bool collectFits;
        private static readonly List<ContactFrame> ContactFrames=new List<ContactFrame>();

        [Serializable] private sealed class Frame { public int cell; }
        [Serializable] private sealed class Clip { public string motion; public Frame[] frames; }
        [Serializable] private sealed class Motion { public Clip[] clips; }
        [Serializable] private sealed class ContactFrame
        {
            public string character,key;
            public int frame;
            public string leftPart,rightPart;
            public float leftX,leftY,rightX,rightY,bootX,bootY,leftVineGap,rightVineGap,leftActorGap,rightActorGap;
        }
        [Serializable] private sealed class ContactManifest { public int version=3; public ContactFrame[] frames; }
        [Serializable] private sealed class SourceRecord { public string path, sha256; }
        [Serializable] private sealed class ClipRecord { public string character, key, path; public int frames = Count, width = W, height = H; }
        [Serializable] private sealed class Manifest
        {
            public int cellWidth = W, cellHeight = H, frameCount = Count, loopStart = 8;
            public float framesPerSecond = 8f;
            public string method = "Seven painted boss poses per character with marked arm and boot contacts. Original game figures supply stage and legacy scenes. Live captures have separate sheets.";
            public ClipRecord[] clips;
            public SourceRecord[] sources;
        }

        private sealed class Image
        {
            internal readonly int width, height;
            internal readonly Color32[] data; // top-to-bottom
            internal Image(int width = W, int height = H) { this.width=width; this.height=height; data=new Color32[width*height]; }
            internal Color32 Get(int x,int y) => x>=0 && y>=0 && x<width && y<height ? data[y*width+x] : default;
            internal void Put(int x,int y,Color32 c) { if(x>=0 && y>=0 && x<width && y<height) data[y*width+x]=c; }
            internal void Copy(Image from,int dx=0,int dy=0)
            {
                for(int y=0;y<from.height;y++) for(int x=0;x<from.width;x++)
                { var c=from.Get(x,y); if(c.a!=0) Put(x+dx,y+dy,c); }
            }
            internal int Top()
            {
                for(int y=0;y<height;y++) for(int x=45;x<Mathf.Min(width,91);x++) if(Get(x,y).a!=0) return y;
                return 38;
            }
            internal Image Crop(int x,int y,int w,int h)
            {
                var result=new Image(w,h);
                for(int j=0;j<h;j++) for(int i=0;i<w;i++) result.Put(i,j,Get(x+i,y+j));
                return result;
            }
            internal Image MirrorMaterial()
            {
                var result=new Image(width,height);
                for(int y=0;y<height;y++) for(int x=0;x<width;x++) result.Put(width-1-x,y,Get(x,y));
                return result;
            }
            internal void Ellipse(int cx,int cy,int rx,int ry,Color32 color)
            {
                rx=Mathf.Max(1,rx); ry=Mathf.Max(1,ry);
                for(int y=cy-ry;y<=cy+ry;y++) for(int x=cx-rx;x<=cx+rx;x++)
                    if((x-cx)*(x-cx)*(float)(ry*ry)+(y-cy)*(y-cy)*(float)(rx*rx)<=rx*rx*ry*ry) Put(x,y,color);
            }
            internal void Cord(Vector2 a,Vector2 b,Vector2 c,int r,Color32[] palette)
            {
                for(int shade=0;shade<4;shade++) for(int i=0;i<=96;i++)
                {
                    float t=i/96f, n=1-t; Vector2 p=n*n*a+2*n*t*b+t*t*c;
                    int radius=shade==0?r+1:shade==1?r:shade==2?Mathf.Max(1,r-1):1;
                    Ellipse(Mathf.RoundToInt(p.x),Mathf.RoundToInt(p.y)-(shade>1?1:0),radius,radius,palette[shade]);
                }
            }
            internal void Save(string path)
            {
                var t=new Texture2D(width,height,TextureFormat.RGBA32,false);
                var pixels=new Color32[data.Length];
                for(int y=0;y<height;y++) Array.Copy(data,y*width,pixels,(height-1-y)*width,width);
                t.SetPixels32(pixels); t.Apply(); File.WriteAllBytes(path,t.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(t);
            }
        }

        private static Color32 C(byte r,byte g,byte b) => new Color32(r,g,b,255);
        private static string Hash(byte[] bytes)
        { using(var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant(); }
        private static Image Load(string path)
        {
            if(Images.TryGetValue(path,out var cached)) return cached;
            byte[] bytes=File.ReadAllBytes(path); Hashes[path]=Hash(bytes);
            var t=new Texture2D(2,2,TextureFormat.RGBA32,false);
            if(!t.LoadImage(bytes)) throw new InvalidDataException(path);
            var result=new Image(t.width,t.height); var pixels=t.GetPixels32();
            for(int y=0;y<t.height;y++) Array.Copy(pixels,(t.height-1-y)*t.width,result.data,y*t.width,t.width);
            UnityEngine.Object.DestroyImmediate(t); Images.Add(path,result); return result;
        }
        private static Image Cell(Image source,int cell,int columns)
        {
            var image=new Image(); int left=cell%columns*W, top=cell/columns*H;
            for(int y=0;y<H;y++) for(int x=0;x<W;x++) image.Put(x,y,source.Get(x+left,y+top));
            return image;
        }
        private static Image Pose(string character,string motion,int frame)
        {
            string stem="Assets/SaintKnightDay06/Resources/SaintKnightDay06/"+character+"_motion";
            var clips=JsonUtility.FromJson<Motion>(File.ReadAllText(stem+"_frames.json")).clips;
            foreach(var clip in clips) if(clip.motion==motion)
                return Cell(Load(stem+".png"),clip.frames[Mathf.Abs(frame)%clip.frames.Length].cell,12);
            throw new InvalidDataException(motion);
        }
        private static Image Fall(string character,int frame) => Load("Assets/SaintKnightDay06/Art/Characters/"+character+"_collapse_f"+Mathf.Clamp(frame,0,11).ToString("D2")+".png");
        private static Image Applied(string character,string kind,int cell) => Cell(Load("Assets/SaintKnightSpriteCook/Resources/SaintKnightSpriteCook/"+character+"_"+kind+".png"),cell,4);
        private static Image Enemy(string kind,string pose,int frame=0) => Load("Assets/SaintKnightDay09/Art/Enemies/"+kind+"/"+pose+"_f"+frame.ToString("D2")+".png");

        private static Image Trap(string ch,string kind,int frame)
        {
            int w=kind=="wall"?160:W, h=kind=="wall"?112:H;
            var source=Load("Assets/SaintKnightDay10/Resources/SaintKnightDay10/TrapMotion/"+ch+"_"+kind+".png");
            return source.Crop(frame%8*w,frame/8*h,w,h);
        }
        private static int Colour(Color32 c) => c.r<<24|c.g<<16|c.b<<8|c.a;
        private static Image RootMaterial(string ch,int stage)
        {
            string key=ch+"/root/"+stage;
            if(Materials.TryGetValue(key,out var cached)) return cached;
            var all=new HashSet<int>(); var character=new HashSet<int>();
            foreach(var c in Trap(ch,"tentacle",21).data) if(c.a!=0) all.Add(Colour(c));
            foreach(var c in Trap(ch,"tentacle",22).data) if(c.a!=0) all.Add(Colour(c));
            foreach(var c in Load("Assets/SaintKnightDay06/Resources/SaintKnightDay06/"+ch+"_motion.png").data) if(c.a!=0) character.Add(Colour(c));
            var unique=new HashSet<int>(all); unique.ExceptWith(character);
            var from=Trap(ch,"tentacle",stage); var result=new Image();
            for(int y=0;y<H;y++) for(int x=0;x<W;x++)
            {
                var c=from.Get(x,y); if(c.a==0||!all.Contains(Colour(c))) continue;
                bool material=unique.Contains(Colour(c));
                for(int j=-2;j<=2&&!material;j++) for(int i=-2;i<=2;i++)
                    if(from.Get(x+i,y+j).a!=0 && unique.Contains(Colour(from.Get(x+i,y+j)))) { material=true; break; }
                if(material) result.Put(x,y,c);
            }
            Materials.Add(key,result); return result;
        }
        private static Image BossArt(string name) => Load("Assets/SaintKnightDay09/Art/Boss/"+name+"_f00_NOT_FINAL.png");

        // Body art is exclusively the game's original full sprites. No generated pose atlas is sampled.
        private static Image BossPose(string ch,int pose) => Pose(ch,pose==1||pose==4||pose==5?"Crouch":"Idle",0);

        private sealed class ActorRig
        {
            internal Image art,prototype;
            internal Vector2 ls,le,lw,rs,re,rw,ps,pe,pw;
            internal int coreLeft,coreRight;
            internal bool duck,ceci;
        }
        private static ActorRig Rig(string ch,int pose)
        {
            bool ceci=ch=="cecilia",duck=pose==1||pose==4||pose==5;
            var r=new ActorRig { art=BossPose(ch,pose),prototype=Pose(ch,"Idle",0),ceci=ceci,duck=duck };
            r.ps=ceci?V(61,56):V(58,50); r.pe=ceci?V(58,64):V(57,59); r.pw=ceci?V(54,71):V(55,67);
            r.ls=r.ps; r.le=r.pe; r.lw=r.pw;
            r.rs=ceci?V(72,55):V(68,49); r.re=ceci?V(76,60):V(72,53); r.rw=ceci?V(76,68):V(76,53);
            r.coreLeft=ceci?62:61; r.coreRight=ceci?73:72;
            if(duck)
            {
                r.ls=ceci?V(60,63):V(62,62); r.le=ceci?V(54,68):V(60,69); r.lw=ceci?V(50,74):V(57,74);
                r.rs=ceci?V(69,62):V(71,62); r.re=ceci?V(71,66):V(73,65); r.rw=ceci?V(70,69):V(77,64);
                r.coreLeft=ceci?60:62; r.coreRight=ceci?70:74;
            }
            return r;
        }
        private static float SegmentDistance(Vector2 p,Vector2 a,Vector2 b)
        {
            float t=Mathf.Clamp01(Vector2.Dot(p-a,b-a)/Mathf.Max(.001f,(b-a).sqrMagnitude));
            return (p-Vector2.Lerp(a,b,t)).magnitude;
        }
        private static Vector2 Curve(Vector2 a,Vector2 b,Vector2 c,Vector2 end,float reveal)
        {
            // Vine rasterizes 128 samples. Use its actual last drawn sample, not the intended endpoint.
            float t=Mathf.Floor(Mathf.Clamp01(reveal)*128)/128f,n=1-t;
            return n*n*n*a+3*n*n*t*b+3*n*t*t*c+t*t*t*end;
        }
        private static Vector2 Elbow(Vector2 shoulder,Vector2 hand,float upper,float lower)
        {
            Vector2 v=hand-shoulder; float distance=Mathf.Max(.01f,v.magnitude);
            float reach=upper+lower;
            if(distance>reach) { float stretch=Mathf.Min(1.25f,distance/reach); upper*=stretch; lower*=stretch; }
            float along=(upper*upper-lower*lower+distance*distance)/(2*distance);
            float height=Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
            var direction=v/distance; var normal=V(-direction.y,direction.x);
            if(normal.y<0) normal=-normal; // elbows hang below the held wrist under gravity
            return shoulder+direction*along+normal*height;
        }
        private static void Limb(Image target,Image source,Vector2 sa,Vector2 sb,Vector2 da,Vector2 db,float radius)
        {
            Vector2 sv=sb-sa,dv=db-da; float sl=sv.magnitude,dl=dv.magnitude;
            if(sl<.01f||dl<.01f) return;
            Vector2 su=sv/sl,du=dv/dl,sn=V(-su.y,su.x),dn=V(-du.y,du.x);
            int left=Mathf.FloorToInt(Mathf.Min(da.x,db.x)-radius-1),right=Mathf.CeilToInt(Mathf.Max(da.x,db.x)+radius+1);
            int top=Mathf.FloorToInt(Mathf.Min(da.y,db.y)-radius-1),bottom=Mathf.CeilToInt(Mathf.Max(da.y,db.y)+radius+1);
            for(int y=top;y<=bottom;y++) for(int x=left;x<=right;x++)
            {
                var delta=V(x,y)-da; float t=Vector2.Dot(delta,du)/dl,side=Vector2.Dot(delta,dn);
                if(t<-.09f||t>1.09f||Mathf.Abs(side)>radius) continue;
                var sample=sa+su*(Mathf.Clamp01(t)*sl)+sn*side;
                var c=source.Get(Mathf.RoundToInt(sample.x),Mathf.RoundToInt(sample.y));
                if(c.a!=0) target.Put(x,y,c);
            }
        }
        private static bool BackCloth(Color32 c,bool ceci) => c.a!=0 && (ceci?c.b>=c.g && c.b>c.r*.85f:c.r>c.g*1.3f && c.r>c.b*1.08f);
        private static Image BodyWithoutArms(ActorRig r)
        {
            var body=new Image(); body.Copy(r.art);
            for(int y=0;y<H;y++) for(int x=0;x<W;x++)
            {
                var p=V(x,y);
                bool arm=SegmentDistance(p,r.ls,r.le)<3.1f||SegmentDistance(p,r.le,r.lw)<2.8f||
                    SegmentDistance(p,r.rs,r.re)<3.1f||SegmentDistance(p,r.re,r.rw)<2.8f;
                bool core=x>=r.coreLeft && x<=r.coreRight;
                bool gear;
                if(r.ceci)
                {
                    var ball=r.duck?V(44,82):V(47,86);
                    gear=SegmentDistance(p,r.lw,ball)<1.7f||(p-ball).sqrMagnitude<30f;
                }
                else
                {
                    var bladeStart=r.duck?r.lw+V(-2,-1):r.lw+V(0,1);
                    var bladeEnd=r.duck?r.lw+V(-15,18):r.lw+V(-12,25);
                    bool sword=SegmentDistance(p,bladeStart,bladeEnd)<3f && !BackCloth(r.art.Get(x,y),false);
                    float shieldLeft=r.duck?73+Mathf.Max(0,y-75)*.45f:74+Mathf.Max(0,y-60)*.22f;
                    float shieldRight=r.duck?85-Mathf.Max(0,y-75)*.4f:87-Mathf.Max(0,y-60)*.15f;
                    bool shield=y>=(r.duck?58:45) && y<=(r.duck?88:90) && x>=shieldLeft && x<=shieldRight;
                    gear=sword || shield;
                }
                if(!gear && (!arm||core)) continue;
                Color32 fill=default;
                if(!core)
                {
                    int step=x<(r.coreLeft+r.coreRight)/2?-1:1;
                    for(int offset=4;offset<=7;offset++)
                    { var donor=r.art.Get(x+step*offset,y); if(BackCloth(donor,r.ceci)) { fill=donor; break; } }
                }
                body.Put(x,y,fill);
            }
            return body;
        }
        private static void Equipment(Image target,ActorRig r,Vector2 left,Vector2 right)
        {
            int ldx=Mathf.RoundToInt(left.x-r.pw.x),ldy=Mathf.RoundToInt(left.y-r.pw.y);
            var original=r.prototype;
            for(int y=0;y<H;y++) for(int x=0;x<W;x++)
            {
                var c=original.Get(x,y); if(c.a==0) continue;
                bool leftGear=r.ceci?SegmentDistance(V(x,y),V(54,73),V(47,86))<1.6f||(V(x,y)-V(47,86)).sqrMagnitude<27f:
                    y>=68 && SegmentDistance(V(x,y),V(55,68),V(43,92))<2.5f && !BackCloth(c,false);
                if(leftGear) target.Put(x+ldx,y+ldy,c);
                if(!r.ceci && y>=49 && y<=90 && Mathf.Abs(x-(80+Mathf.Clamp01((y-49)/41f)))<=Mathf.Lerp(6.5f,.8f,Mathf.Clamp01((y-49)/41f)))
                    target.Put(x+Mathf.RoundToInt(right.x-76),y+Mathf.RoundToInt(right.y-53),c);
            }
        }
        private static Image NativeActor(string ch,int pose,Vector2 origin,Vector2 left,Vector2 right,float hold,
            bool gear=true, Vector2? raisedElbow=null)
        {
            var r=Rig(ch,pose); var actor=new Image();
            var body=BodyWithoutArms(r); actor.Copy(body,Mathf.RoundToInt(origin.x),Mathf.RoundToInt(origin.y));
            left=Vector2.Lerp(r.lw+origin,left,hold); right=Vector2.Lerp(r.rw+origin,right,hold);
            float upper=(r.pe-r.ps).magnitude,lower=(r.pw-r.pe).magnitude;
            foreach(int side in new[]{-1,1})
            {
                var shoulder=(side<0?r.ls:r.rs)+origin; var hand=side<0?left:right;
                var elbow=side>0 && raisedElbow.HasValue ? raisedElbow.Value : Elbow(shoulder,hand,upper,lower);
                Limb(actor,r.prototype,r.ps,r.pe,shoulder,elbow,r.ceci?2.35f:2.7f);
                Limb(actor,r.prototype,r.pe,r.pw,elbow,hand,r.ceci?1.8f:2.1f);
                actor.Copy(r.prototype.Crop(Mathf.RoundToInt(r.pw.x)-1,Mathf.RoundToInt(r.pw.y)-1,3,3),
                    Mathf.RoundToInt(hand.x)-1,Mathf.RoundToInt(hand.y)-1);
            }
            if(gear) Equipment(actor,r,left,right);
            actorRaster=actor; actorLeft=left; actorRight=right; actorLeftPart="wrist"; actorRightPart="wrist";
            return actor;
        }

        private static Image MirroredActor(string ch,Vector2 origin,Vector2 left,Vector2 right,float hold)
        {
            var native=NativeActor(ch,5,V(-origin.x,origin.y),V(127-right.x,right.y),V(127-left.x,left.y),hold,false);
            var actor=native.MirrorMaterial();
            actorRaster=actor; actorLeft=Vector2.Lerp(V(127-Rig(ch,5).rw.x+origin.x,Rig(ch,5).rw.y+origin.y),left,hold);
            actorRight=Vector2.Lerp(V(127-Rig(ch,5).lw.x+origin.x,Rig(ch,5).lw.y+origin.y),right,hold);
            return actor;
        }
        private static Image TightCrop(Image source)
        {
            int left=source.width,right=0,top=source.height,bottom=0;
            for(int y=0;y<source.height;y++) for(int x=0;x<source.width;x++) if(source.Get(x,y).a!=0)
            { left=Mathf.Min(left,x);right=Mathf.Max(right,x+1);top=Mathf.Min(top,y);bottom=Mathf.Max(bottom,y+1); }
            return right>left?source.Crop(left,top,right-left,bottom-top):new Image(1,1);
        }
        private static Image QuarterTurn(Image source)
        {
            var result=new Image(source.height,source.width);
            for(int y=0;y<source.height;y++) for(int x=0;x<source.width;x++) result.Put(source.height-1-y,x,source.Get(x,y));
            return result;
        }
        private static void DroppedEquipment(Image target,string ch)
        {
            if(ch=="cecilia") return; // Her weighted chain remains hanging from the lowered hand.
            var r=Rig(ch,0); var left=new Image();
            Equipment(left,r,r.pw,V(-1000,-1000));
            var weapon=QuarterTurn(TightCrop(left)); target.Copy(weapon,27,93-weapon.height);
            if(!r.ceci)
            {
                var right=new Image(); Equipment(right,r,V(-1000,-1000),V(76,53));
                var shield=QuarterTurn(TightCrop(right)); target.Copy(shield,76,93-shield.height);
            }
        }
        private static Image KneelingBossScene(string ch,string key,int frame)
        {
            var d=new Image(); bool ceci=ch=="cecilia"; vineRaster=new Image(); actorRaster=null;
            actorBoot=V(65,85);
            int variant=key[4]-'1'; float phase=(frame-8)*Mathf.PI*2/10+variant*.3f;
            int sway=frame<8?0:Mathf.RoundToInt(Mathf.Sin(phase));
            int breathe=frame<8?0:Mathf.RoundToInt(Mathf.Cos(phase));
            float p=Mathf.SmoothStep(0,1,Mathf.Clamp01(frame/7f));
            float hold=Mathf.SmoothStep(0,1,Mathf.Clamp01((frame-3)/4f));
            int high=ceci?0:-6;
            // The boss stays behind the kneeling figure. Contact is confined to outer sleeves/arms.
            // Narrow outer-arm strands replace the flat wing-like mass; the world tether joins the boss.
            Vine(d,V(79,68),V(74,58),V(65+sway,51+high),V(57,58+high),1.8f,DarkVine,p);
            Vine(d,V(81,77),V(80,69),V(74-sway,62),V(68,65+breathe),1.8f,DarkVine,p);
            var leftEnd=V(48+sway,(ceci?54:48)+breathe);
            var rightEnd=V(61+sway,70+breathe);
            var leftBind=V(ceci?50+sway:49+sway,(ceci?62:59)+breathe);
            var leftContact=Curve(V(78,67),V(70,58+high),V(61+sway,55+high),leftBind,p);
            var lw=leftEnd;
            var rw=Curve(V(80,73),V(75,74),V(68-sway,66+breathe),rightEnd,p);
            Vector2 freeHand=default;
            if(frame<4)
            {
                string motion=frame<2?"Hurt":"Held";
                d.Copy(Pose(ch,motion,0).MirrorMaterial(),-4+sway,0);
            }
            else
            {
                d.Copy(MirroredActor(ch,V(-4+sway,breathe),lw,rw,hold));
                freeHand=actorLeft;
                actorLeft=leftContact; actorLeftPart="upper_arm";
                if(ceci)
                {
                    var heldGear=new Image();
                    Equipment(heldGear,Rig(ch,0),V(127-actorRight.x,actorRight.y),V(-1000,-1000));
                    d.Copy(heldGear.MirrorMaterial());
                }
            }
            Vine(d,V(78,67),V(70,58+high),V(61+sway,55+high),leftBind,1.8f,DarkVine,p);
            Vine(d,V(80,73),V(75,74),V(68-sway,66+breathe),rightEnd,1.8f,DarkVine,p);
            Vine(d,V(78,69),V(74,66),V(69,59+breathe),V(64+sway,64+breathe),1.5f,DarkVine,p);
            if(frame>=7)
            {
                Grip(d,Mathf.RoundToInt(leftContact.x),Mathf.RoundToInt(leftContact.y),DarkVine,sway);
                Grip(d,Mathf.RoundToInt(rw.x),Mathf.RoundToInt(rw.y),DarkVine,-sway);
            }
            Vine(d,V(78,88),V(74,92),V(68,83+breathe),V(65+sway,85+breathe),1.5f,DarkVine,p);
            if(frame>=4)
            {
                int x=Mathf.RoundToInt(freeHand.x)-2,y=Mathf.RoundToInt(freeHand.y)-2;
                d.Copy(actorRaster.Crop(x,y,5,5),x,y); // The free hand reads in front at the forehead.
            }
            return FinishBoss(d,ch,key,frame);
        }

        private static float PixelGap(Image image,Vector2 point)
        {
            float gap=99f;
            for(int y=Mathf.FloorToInt(point.y)-4;y<=Mathf.CeilToInt(point.y)+4;y++)
                for(int x=Mathf.FloorToInt(point.x)-4;x<=Mathf.CeilToInt(point.x)+4;x++)
                    if(image.Get(x,y).a!=0) gap=Mathf.Min(gap,Vector2.Distance(point,V(x,y)));
            return gap;
        }
        private static Image FinishBoss(Image result,string ch,string key,int frame)
        {
            if(collectFits && actorRaster!=null && (frame>=8 || IsReferenceFinish(key) || key=="boss_grab"))
                ContactFrames.Add(new ContactFrame { character=ch,key=key,frame=frame,
                    leftX=actorLeft.x,leftY=actorLeft.y,rightX=actorRight.x,rightY=actorRight.y,
                    leftPart=actorLeftPart,rightPart=actorRightPart,bootX=actorBoot.x,bootY=actorBoot.y,
                    leftVineGap=PixelGap(vineRaster,actorLeft),rightVineGap=PixelGap(vineRaster,actorRight),
                    leftActorGap=PixelGap(actorRaster,actorLeft),rightActorGap=PixelGap(actorRaster,actorRight) });
            vineRaster=null; actorRaster=null;
            return result;
        }

        private static Vector2 V(float x,float y) => new Vector2(x,y);
        private static void Vine(Image image,Vector2 a,Vector2 b,Vector2 c,Vector2 end,float radius,Color32[] palette,float reveal=1f)
        {
            if(reveal<=0) return;
            // One continuous, tapered curved body. Broken lit ridges keep the game's pixel texture.
            var nearest=new float[image.data.Length]; var shades=new byte[image.data.Length];
            for(int k=0;k<nearest.Length;k++) nearest[k]=float.MaxValue;
            for(int i=0;i<=128*Mathf.Clamp01(reveal);i++)
            {
                float t=i/128f,n=1-t;
                Vector2 p=n*n*n*a+3*n*n*t*b+3*n*t*t*c+t*t*t*end;
                float r=Mathf.Lerp(radius,1.05f,t*t);
                for(int yy=Mathf.FloorToInt(p.y-r-1);yy<=Mathf.CeilToInt(p.y+r+1);yy++)
                    for(int xx=Mathf.FloorToInt(p.x-r-1);xx<=Mathf.CeilToInt(p.x+r+1);xx++)
                    {
                        var delta=V(xx-p.x,yy-p.y);
                        if(xx<0||xx>=image.width||yy<0||yy>=image.height) continue;
                        float distance=delta.sqrMagnitude/(r*r);
                        int index=yy*image.width+xx;
                        if(distance>1.3f || distance>=nearest[index]) continue;
                        nearest[index]=distance;
                        float light=(delta.x*.45f+delta.y*.9f)/r;
                        int shade=distance>.78f?0:light<-.15f?3:light<.45f?2:1;
                        if(shade==3 && (i/5+xx+yy)%6<2) shade=2;
                        shades[index]=(byte)shade;
                    }
            }
            for(int k=0;k<nearest.Length;k++) if(nearest[k]!=float.MaxValue)
            {
                image.data[k]=palette[shades[k]];
                if(vineRaster!=null) vineRaster.data[k]=palette[shades[k]];
            }
        }
        private static void Grip(Image d,int x,int y,Color32[] palette,int sway)
        {
            Vine(d,V(x-3,y-2),V(x+5,y-5),V(x+5,y+5+sway),V(x-2,y+3),1.65f,palette);
        }

        private static bool IsReferenceFinish(string key) =>
            key == "boss1" || key == "boss2" || key == "boss3" || key == "boss4";

        private static Image Draw(string ch,string key,int frame)
        {
            if (SKNativeDefeatLibrary.IsBossPattern(key) || IsReferenceFinish(key))
                return ReferenceBossScene(ch,key,frame);
            if (key == "boss5" || key == "boss6") return KneelingBossScene(ch,key,frame);
            return StageScene(ch,key,frame);
        }

        [MenuItem("Tools/SaintKnight/Art/Bake Native Defeat Motions")]
        public static void Bake()
        {
            Images.Clear(); Hashes.Clear(); Materials.Clear(); ContactFrames.Clear(); StageFrames.Clear();
            poseBook=null; grabEntranceBook=null; collectFits=true;
            Directory.CreateDirectory(Output); Directory.CreateDirectory(Evidence);
            var clips=new List<ClipRecord>();
            foreach(string ch in new[]{"cecilia","adela"}) foreach(string key in Keys)
            {
                int width=key=="fleshwall"?160:W, height=key=="fleshwall"?112:H;
                int frames=key=="boss_grab"?SKNativeDefeatLibrary.GrabDefeatFrames:Count;
                var sheet=new Image(width*frames,height);
                for(int f=0;f<frames;f++) sheet.Copy(Draw(ch,key,f),width*f,0);
                string suffix=SKNativeDefeatLibrary.IsBossPattern(key)?"_defeat":"";
                string path=Output+ch+"_"+key+suffix+".png"; sheet.Save(path);
                clips.Add(new ClipRecord { character=ch,key=key,path=path,width=width,height=height,frames=frames });
            }
            collectFits=false;
            var contactJson=JsonUtility.ToJson(new ContactManifest { frames=ContactFrames.ToArray() },true);
            File.WriteAllText(Output+"contacts.json",contactJson);
            // Historical contact reports remain with their original review. The game reads the live manifest above.
            string stageJson=JsonUtility.ToJson(new StageManifest { frames=StageFrames.ToArray() },true);
            File.WriteAllText(Output+"stage_contacts.json",stageJson);
            Directory.CreateDirectory(Evidence+"StageRevision");
            File.WriteAllText(Evidence+"StageRevision/contacts-20261118.json",stageJson);
            foreach(string ch in new[]{"cecilia","adela"})
            {
                var equipment=new Image(); DropBossGear(equipment,ch);
                equipment.Save(Output+ch+"_boss_drop.png");
                Pose(ch,"Idle",0).Save(Evidence+ch+"_start.png");
                var posePreview=new Image(W*3,H*2);
                for(int pose=0;pose<6;pose++) posePreview.Copy(BossPose(ch,pose),pose%3*W,pose/3*H);
                posePreview.Save(Evidence+"BossRevision/"+ch+"_poses_native.png");
                int[][] groups={new[]{0,1},new[]{2,3,4,5,6},new[]{7,8,9,10,11,12}};
                for(int g=0;g<groups.Length;g++)
                {
                    var preview=new Image(160*5,112*groups[g].Length);
                    for(int row=0;row<groups[g].Length;row++)
                    {
                        preview.Copy(Pose(ch,"Idle",0),16,row*112+16);
                        int[] samples={0,3,7,12};
                        for(int col=0;col<samples.Length;col++) preview.Copy(Draw(ch,Keys[groups[g][row]],samples[col]),(col+1)*160,row*112);
                    }
                    preview.Save(Evidence+ch+"_native_preview_"+g+".png");
                }
            }
            BossArt("boss_grab_reach").Crop(24,20,64,8).Save(Output+"boss_tether_material.png");
            var records=new List<SourceRecord>();
            foreach(var entry in Hashes)
            {
                if(Hash(File.ReadAllBytes(entry.Key))!=entry.Value) throw new IOException("Source changed: "+entry.Key);
                records.Add(new SourceRecord { path=entry.Key,sha256=entry.Value });
            }
            records.Sort((a,b)=>string.CompareOrdinal(a.path,b.path));
            var manifest=new Manifest { clips=clips.ToArray(),sources=records.ToArray() };
            string json=JsonUtility.ToJson(manifest,true);
            File.WriteAllText(Output+"manifest.json",json); File.WriteAllText(Evidence+"native-manifest.json",json);
            AssetDatabase.Refresh();
            foreach(var clip in clips)
            {
                var importer=AssetImporter.GetAtPath(clip.path) as TextureImporter;
                importer.textureType=TextureImporterType.Default; importer.isReadable=true;
                importer.sRGBTexture=true; importer.alphaIsTransparency=true; importer.mipmapEnabled=false;
                importer.filterMode=FilterMode.Point; importer.wrapMode=TextureWrapMode.Clamp;
                importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.maxTextureSize=4096; importer.npotScale=TextureImporterNPOTScale.None;
                importer.SaveAndReimport();
            }
            foreach(string name in new[]{"cecilia_boss_drop","adela_boss_drop","boss_tether_material"})
            {
                var importer=AssetImporter.GetAtPath(Output+name+".png") as TextureImporter;
                importer.textureType=TextureImporterType.Default; importer.isReadable=true;
                importer.alphaIsTransparency=true; importer.mipmapEnabled=false;
                importer.filterMode=FilterMode.Point; importer.wrapMode=TextureWrapMode.Clamp;
                importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.npotScale=TextureImporterNPOTScale.None; importer.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"NATIVE_DEFEAT_BAKE|{clips.Count} clips|all source SHA-256 preserved");
        }

        public static void BuildWindows()
        {
            string path="Builds/Win64_NativeDefeat/SaintAndKnight.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var scenes=new List<string>(); foreach(var scene in EditorBuildSettings.scenes) if(scene.enabled) scenes.Add(scene.path);
            var report=BuildPipeline.BuildPlayer(scenes.ToArray(),path,BuildTarget.StandaloneWindows64,BuildOptions.None);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Windows build: "+report.summary.result);
            File.Copy(Evidence+"BossReference/PixelFX-LICENSE.txt",Path.Combine(Path.GetDirectoryName(path),"PixelFX-LICENSE.txt"),true);
            Debug.Log("NATIVE_DEFEAT_BUILD="+Path.GetFullPath(path));
        }
    }
}
