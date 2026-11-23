using System;
using UnityEngine;

namespace SaintKnight.SpriteCook.Editor
{
    public static partial class SKNativeDefeatBuilder
    {
        // Ink, shadow, local colour, turning plane, lit edge, small specular glints.
        private static readonly Color32[] Pod = { C(25,23,32), C(47,42,51), C(69,65,73), C(94,89,94), C(125,119,119), C(165,153,146) };
        private static readonly Color32[] Ink = { C(18,16,25), C(30,27,37), C(45,40,54), C(62,55,72), C(86,76,92), C(133,119,135) };
        private static readonly Color32[] Husk = { C(29,22,26), C(54,37,37), C(83,53,46), C(111,75,58), C(147,103,77), C(186,142,103) };
        private static readonly Color32[] Crimson = { C(35,17,31), C(65,27,44), C(105,38,59), C(149,54,76), C(191,80,92), C(229,132,128) };
        private static readonly Color32[] Thorn = { C(22,28,29), C(35,47,39), C(49,69,48), C(68,88,57), C(99,116,73), C(143,151,102) };
        private static readonly Color32[] Pearl = { C(36,33,55), C(61,57,84), C(87,84,118), C(120,119,157), C(158,158,191), C(205,203,219) };
        private static readonly Color32[] Iron = { C(30,27,37), C(52,48,59), C(76,69,80), C(106,99,107), C(144,137,141), C(185,176,171) };
        private const float Turn = Mathf.PI * 2f;

        private static float Ease(int frame, float from, float to) =>
            Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, frame));

        // A sleeve, hand and boot are sampled from the SAME posed body used for the final frame.
        // Their pixel coordinates also drive the three world-space tethers during delivery.
        private sealed class BossFigure
        {
            internal Image pixels;
            internal Vector2 left, right, boot, face, lastHand;
        }

        private enum PaintedPose { Floor, Grab, Spit, Cocoon, Corolla, Briar, Spiral }
        [Serializable] private sealed class PoseMarks { public Vector2 left,right,boot,face; public int height; }
        [Serializable] private sealed class PoseBook { public PoseMarks[] cecilia,adela; }
        private static PoseBook poseBook, grabEntranceBook;

        private static PoseBook LoadPoseBook(string filename)
        {
            string path = "Assets/SaintKnightSpriteCook/Editor/"+filename;
            var bytes = System.IO.File.ReadAllBytes(path);
            Hashes[path] = Hash(bytes);
            return JsonUtility.FromJson<PoseBook>(System.Text.Encoding.UTF8.GetString(bytes));
        }

        // The ankles stay planted during a lean. The upper body follows the impact and the
        // hands and face use the identical transform as the painted pixels.
        private static BossFigure Figure(string ch, PaintedPose pose, Vector2 offset, float degrees,
            float lean, float length = 1f)
        {
            if (poseBook == null) poseBook = LoadPoseBook("BossPoseMarks.json");
            var marks = (ch=="cecilia" ? poseBook.cecilia : poseBook.adela)[(int)pose];
            var sheet = Load("Assets/SaintKnightSpriteCook/Editor/"+ch+"_boss_pose_keys.png");
            var bent = new Image();
            float Bend(float y) => lean * Mathf.SmoothStep(0,1,(90-y)/marks.height);
            for (int y=0; y<H; y++)
            {
                float sy = 90+(y-90)/length;
                int dx = Mathf.RoundToInt(Bend(sy)), row = Mathf.RoundToInt(sy);
                for (int x=0; x<W; x++)
                {
                    int sx=x-dx;
                    if (sx>=0 && sx<W) bent.Put(x,y,sheet.Get((int)pose*W+sx,row));
                }
            }
            var pivot = V(64,90);
            var pixels = TurnActor(bent,pivot,pivot+offset,degrees);
            Vector2 At(Vector2 p)
            {
                var bentPoint=V(p.x+Bend(p.y),90+(p.y-90)*length);
                return NearestPixel(pixels,TurnPoint(bentPoint,pivot,pivot+offset,degrees));
            }
            return new BossFigure {
                pixels=pixels, left=At(marks.left), right=At(marks.right), boot=At(marks.boot),
                face=At(marks.face), lastHand=At(marks.face+V(2,-2))
            };
        }

        private static void DropBossGear(Image image,string ch)
        {
            if(ch=="cecilia") StageDroppedGear(image,ch);
            else DroppedEquipment(image,ch);
        }

        private static void Contact(BossFigure figure, bool upperArm = false)
        {
            actorRaster = figure.pixels;
            actorLeft = figure.left;
            actorRight = figure.right;
            actorBoot = figure.boot;
            actorLeftPart = upperArm ? "upper_arm" : "wrist";
            actorRightPart = "wrist";
        }

        // A continuous, tapered pixel ribbon. The bands follow the curve, so the texture
        // sticks to a moving limb. Small broken ridges leave broad areas of local colour.
        private static void Coil(Image target, Color32[] ramp, Vector2 start, Vector2 a, Vector2 b,
            Vector2 end, float width, float tip, float growth = 1f, float ribs = 5f)
        {
            if (growth <= 0f) return;
            float pad = Mathf.Max(width, tip) * 1.25f + 2;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(Mathf.Min(start.x,end.x),Mathf.Min(a.x,b.x))-pad));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(Mathf.Min(start.y,end.y),Mathf.Min(a.y,b.y))-pad));
            int x1 = Mathf.Min(W-1, Mathf.CeilToInt(Mathf.Max(Mathf.Max(start.x,end.x),Mathf.Max(a.x,b.x))+pad));
            int y1 = Mathf.Min(H-1, Mathf.CeilToInt(Mathf.Max(Mathf.Max(start.y,end.y),Mathf.Max(a.y,b.y))+pad));
            int stride = x1-x0+1;
            if (stride <= 0 || y1 < y0) return;
            var coverage = new float[stride*(y1-y0+1)];
            int last = Mathf.CeilToInt(Mathf.Clamp01(growth)*96);
            for (int i=0; i<=last; i++)
            {
                float t = Mathf.Min(i/96f,growth), n = 1-t;
                Vector2 p = n*n*n*start + 3*n*n*t*a + 3*n*t*t*b + t*t*t*end;
                float band = t*ribs;
                float radius = Mathf.Lerp(width,tip,t) * (1f+.075f*Mathf.Sin(Turn*band));
                radius *= Mathf.Clamp01((growth-t)*12f + Mathf.Lerp(.35f,1f,growth));
                radius = Mathf.Max(.7f,radius);
                for (int y=Mathf.Max(y0,Mathf.FloorToInt(p.y-radius)); y<=Mathf.Min(y1,Mathf.CeilToInt(p.y+radius)); y++)
                for (int x=Mathf.Max(x0,Mathf.FloorToInt(p.x-radius)); x<=Mathf.Min(x1,Mathf.CeilToInt(p.x+radius)); x++)
                {
                    float dx=(x-p.x)/radius, dy=(y-p.y)/radius, depth=1-dx*dx-dy*dy;
                    int ix=(y-y0)*stride+x-x0;
                    if (depth <= coverage[ix]) continue;
                    coverage[ix]=depth;
                    float light = -dx*.43f-dy*.88f;
                    float ridge = Mathf.Repeat(band+dx*.13f+dy*.08f,1f);
                    int colour = depth<.22f ? 0 : light<-.44f ? 1 : light<-.02f ? 2 : light<.42f ? 3 : 4;
                    if (colour>=2 && ridge>.81f) colour--;
                    if (colour==4 && ridge>.24f && ridge<.48f && light>.52f) colour=5;
                    target.Put(x,y,ramp[colour]);
                    vineRaster?.Put(x,y,ramp[colour]);
                }
            }
        }

        // Lit, ridged masses for seed husks, compact root knots, and the low seat.
        private static void Lobe(Image d, Color32[] ramp, float cx, float cy, float rx, float ry, float tilt, int seed)
        {
            if (rx<1 || ry<1) return;
            float a = tilt*Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            int bound = Mathf.CeilToInt(Mathf.Max(rx,ry))+2;
            for (int y=Mathf.Max(0,(int)cy-bound); y<=Mathf.Min(H-1,(int)cy+bound); y++)
            for (int x=Mathf.Max(0,(int)cx-bound); x<=Mathf.Min(W-1,(int)cx+bound); x++)
            {
                float dx=x-cx,dy=y-cy,u=(c*dx+s*dy)/rx,v=(-s*dx+c*dy)/ry;
                float dist=u*u+v*v;
                if (dist>1f) continue;
                float ridge=Mathf.Repeat(u*1.7f+v*v*.45f+seed*.31f,1f);
                float lit=-.34f*u-.71f*v+Mathf.Sqrt(1-dist)*.34f;
                int ink=dist>.90f ? 0 : lit<-.3f ? 1 : lit<.05f ? 2 : lit<.38f ? 3 : 4;
                if (ink>1 && ridge>.77f) ink--;
                if (ink==4 && ridge>.15f && ridge<.34f && lit>.7f) ink=5;
                d.Put(x,y,ramp[ink]);
            }
        }

        private static void Cuff(Image d, Vector2 point, Color32[] ramp, float angle)
        {
            Vector2 side=V(3.1f,angle), rise=V(0,2.4f);
            Coil(d,ramp,point-side,point-side-rise,point+side-rise,point+side,1.55f,1.25f);
            Coil(d,ramp,point+side,point+side+rise,point-side+rise,point-side+V(1,1),1.25f,.85f);
        }

        private static void Join(Image d, Color32[] ramp, Vector2 basePt, Vector2 bend, Vector2 end,
            float width, float growth, float angle)
        {
            Coil(d,ramp,basePt,bend,Vector2.Lerp(bend,end,.57f)+V(0,3),end,width,1.55f,growth);
            if (growth>=.99f) Cuff(d,end,ramp,angle);
        }

        private static Image ReferenceBossScene(string ch, string key, int frame)
        {
            vineRaster = new Image();
            actorRaster = null;
            switch (key)
            {
                case "boss_spikes": return FloorCradle(ch,key,frame);
                case "boss_grab": return TempleGrab(ch,key,frame);
                case "boss_spit": return InkMantle(ch,key,frame);
                case "boss1": return RootWindow(ch,key,frame);
                case "boss2": return RedCorolla(ch,key,frame);
                case "boss3": return BriarSeat(ch,key,frame);
                case "boss4": return PaleSpiral(ch,key,frame);
                default: throw new ArgumentOutOfRangeException(nameof(key));
            }
        }

        private static float Wave(int frame, float offset = 0f) =>
            Mathf.Sin(Turn*Mathf.Max(0,frame-8)/10f+offset)*Ease(frame,4,8);

        private static Image FloorCradle(string ch, string key, int f)
        {
            var d = new Image();
            float g=Ease(f,0,7), curl=Ease(f,2,8), w=Wave(f), lag=Wave(f,-1.3f);
            var fig=Figure(ch,PaintedPose.Floor,V(w,-2*curl),-5+5*curl+2*w,3*curl+lag,1.08f-.08f*curl);
            // Two lobes open from a single grounded base, then fold towards the shoulders.
            Lobe(d,Pod,63,89,7+23*g,3+5*g,0,3);
            for (int sign=-1; sign<=1; sign+=2)
            {
                float swell=sign<0?w:lag;
                Coil(d,Pod,V(63+sign*5,89),V(63+sign*(43+swell),94),V(63+sign*30,55+swell),
                    V(63+sign*(20-4*curl),59+swell),7.2f,3.8f,g,6);
                Lobe(d,Pod,63+sign*(17+5*curl),86-13*g+swell,3+7*g,3+12*g,sign*23,sign+3);
            }
            if (f<3) d.Copy(Pose(ch,f==0?"Pounced":"Held",f));
            else d.Copy(fig.pixels);
            Join(d,Pod,V(39,84),V(39,67+w),fig.left,4.1f,curl,-1);
            Join(d,Pod,V(87,83),V(92,64-lag),fig.right,4.3f,curl,1);
            Coil(d,Pod,V(37,80+w),V(39,95),V(65,92-lag),V(73,81+lag),6,2.4f,curl,4);
            Coil(d,Pod,V(88,78-lag),V(90,93),V(66,93),V(59,85+w),5.6f,2.2f,curl,4);
            if (f>=3) { DropBossGear(d,ch); Contact(fig); }
            return FinishBoss(d,ch,key,f);
        }

        private static BossFigure GrabEntrance(string ch, int frame)
        {
            if (grabEntranceBook == null) grabEntranceBook = LoadPoseBook("GrabEntranceMarks.json");
            // Two ticks each for the stumble, drop and landing. The fourth drawing meets the held pose.
            int cell=(frame-1)/2;
            var art=Load("Assets/SaintKnightSpriteCook/Editor/"+ch+"_grab_entrance.png").Crop(cell*W,0,W,H);
            var marks=(ch=="cecilia"?grabEntranceBook.cecilia:grabEntranceBook.adela)[cell];
            return new BossFigure { pixels=art, left=NearestPixel(art,marks.left), right=NearestPixel(art,marks.right),
                boot=NearestPixel(art,marks.boot), face=marks.face, lastHand=marks.face+V(2,-2) };
        }

        private static Image TempleGrab(string ch, string key, int f)
        {
            var d = new Image();
            float catchArm=Ease(f,0,6), kneel=Ease(f,1,6);
            // 4 seconds in the reference. The long first hold breaks near 2.4 seconds.
            float bow=Ease(f,17,21)-.22f*Ease(f,26,31);
            float pulse=Mathf.Sin(f*Turn/4.7f)*Mathf.Min(1,kneel);
            float shoulder=Mathf.Sin((f-1)*Turn/4.7f);
            var fig=f>=1 && f<8 ? GrabEntrance(ch,f) : Figure(ch,PaintedPose.Grab,V(bow,-3+3*kneel),-6+5*kneel+5*bow+shoulder,
                -2+4*bow+pulse,1.06f-.06f*kneel-.10f*bow);
            Coil(d,Ink,V(93,88),V(83,80),V(83,58+shoulder),fig.left,3.1f,1.3f,catchArm);
            Coil(d,Ink,V(93,87),V(85,75),V(79,68+pulse),fig.right,2.4f,1.4f,Ease(f,1,7));
            if (f==0) d.Copy(Pose(ch,"Hurt",0));
            else
            {
                // Long strands pass behind the head and torso. Only the wrapping tips cross in front.
                Join(d,Ink,V(83,69),V(76,58+shoulder),fig.left,2.3f,catchArm,-1);
                Join(d,Ink,V(81,78),V(74,78+pulse),fig.right,2.2f,catchArm,1);
                d.Copy(fig.pixels);
                if (catchArm>=.99f) { Cuff(d,fig.left,Ink,-1); Cuff(d,fig.right,Ink,1); }
                Coil(d,Ink,V(85,87),V(78,94),V(72,92),fig.boot,2.4f,1.25f,Ease(f,3,8));
                if (f>=7)
                {
                    int hx=(int)fig.lastHand.x-2,hy=(int)fig.lastHand.y-2;
                    d.Copy(fig.pixels.Crop(hx,hy,5,5),hx,hy);
                }
                DropBossGear(d,ch);
                Contact(fig,true);
            }
            return FinishBoss(d,ch,key,f);
        }

        private static void InkDrop(Image d, int index, int x, int y)
        {
            const string path="Assets/SaintKnightSpriteCook/Editor/InkDrip.png";
            var sheet=Load(path);
            int col=index%5*72+13, row=index/5*88+43;
            // Composite the exported PixelFX RGBA, including low-alpha splash rings.
            for (int v=0;v<43;v++) for (int u=0;u<46;u++)
            {
                Color32 src=sheet.Get(col+u,row+v), dst=d.Get(x+u,y+v);
                if (src.a==0) continue;
                if (dst.a==0 || src.a==255) { d.Put(x+u,y+v,src); continue; }
                int back=dst.a*(255-src.a)/255, alpha=src.a+back;
                d.Put(x+u,y+v,new Color32((byte)((src.r*src.a+dst.r*back)/alpha),
                    (byte)((src.g*src.a+dst.g*back)/alpha),(byte)((src.b*src.a+dst.b*back)/alpha),(byte)alpha));
            }
        }

        private static Image InkMantle(string ch, string key, int f)
        {
            var d=new Image();
            float splash=Ease(f,0,4), run=Ease(f,2,8), w=Wave(f), lag=Wave(f,-1.3f);
            bool ceci=ch=="cecilia";
            var fig=Figure(ch,PaintedPose.Spit,V(-1+w*.6f,lag*.5f),-5+6*run+w,-4+4*run+lag);
            float shoulderY=ceci?54:49;
            Lobe(d,Ink,63,90,7+28*run,2+3*run,1,2);
            if (f>=3)
            {
                int phase=f<8 ? f-3 : Mathf.FloorToInt((f-8)*19/10f);
                InkDrop(d,phase%19,13,55);
                InkDrop(d,(phase+8)%19,64,54);
            }
            // Old boss slime contributes its native mottling to the descending sheets.
            var grain=Trap(ch,"bossslime",21);
            for (int side=-1;side<=1;side+=2)
            for (int strand=0;strand<5;strand++)
            {
                float spread=13+strand*2.8f, swing=side<0?w:lag;
                float x=64+side*spread, top=shoulderY-2+Mathf.Abs(strand-2);
                float bottom=90-(strand*7%12)+swing*2;
                var a=V(64+side*(9+strand),top);
                var end=V(x+side*swing,bottom);
                Coil(d,Ink,a,V(x+side*4,top+4),V(x-2*side,76),end,3.8f-strand*.22f,1.25f,run,2);
                if (f<6) continue;
                for (int dot=0;dot<3;dot++)
                {
                    int yy=Mathf.RoundToInt(Mathf.Lerp(top+6,bottom-3,Mathf.Repeat(dot*.31f+f*.1f,1)));
                    var texel=grain.Get(53+strand,(yy%19)+59);
                    if (texel.a>0 && d.Get((int)x,yy).a!=0)
                        d.Put((int)x,yy,Ink[texel.r>80?4:3]);
                }
            }
            if (f<3) d.Copy(Pose(ch,"Hurt",f));
            else d.Copy(fig.pixels);
            if (f<5)
                Coil(d,Ink,V(110,38),V(95,33),V(78,38),fig.right,3f,1.6f,Mathf.Sin(splash*Mathf.PI));
            // A hood frames the face, with clear air above the collar. The front drapes end on the same floor.
            Coil(d,Ink,fig.left+V(-16,18),fig.left+V(-15,2),fig.face+V(-15,-15),fig.face+V(-3,-8),
                4.4f,1.6f,splash,3);
            Coil(d,Ink,fig.face+V(-3,-8),fig.face+V(12,-12),fig.right+V(8,-23),fig.right+V(10,12),
                2.6f,2.4f,splash,3);
            Join(d,Ink,V(39,91),V(41+w,59),fig.left,3.7f,run,-1);
            Join(d,Ink,V(91,90),V(89-lag,58),fig.right,3.5f,run,1);
            Coil(d,Ink,fig.left+V(-2,0),V(53+w,70),V(59-lag,79),V(54+lag,91),3.4f,1.4f,run,2);
            Coil(d,Ink,fig.right+V(1,-2),V(78+w,70),V(69,76),V(72+lag,90),3.8f,1.35f,run,2);
            Coil(d,Ink,fig.right+V(4,0),V(84+w,68),V(77,78),V(80+lag,87),2.5f,.9f,run,2);
            if (f>=3) { DropBossGear(d,ch); Contact(fig); }
            return FinishBoss(d,ch,key,f);
        }

        private static Image RootWindow(string ch,string key,int f)
        {
            var d=new Image();
            float fold=Ease(f,0,7), close=Ease(f,2,8), w=Wave(f), lag=Wave(f,-1.6f);
            var fig=Figure(ch,PaintedPose.Cocoon,V(2,-1-6*fold+lag),-6-14*fold+2*w,-2-fold+w,1-.06f*fold);
            // An overlapping horizontal husk. The head sits in the opening between the upper and lower shell.
            Lobe(d,Husk,65,84,12+22*fold,5+8*fold,-5,1);
            Lobe(d,Husk,75,72-lag,8+17*fold,5+12*fold,-24,4);
            for (int i=0;i<4;i++)
            {
                float x=38+i*12;
                Coil(d,Husk,V(x-6,88),V(x-19,65+w),V(x+4,35+Mathf.Abs(i-2)*5+lag),
                    V(x+17,57+w),5.6f,1.5f,fold,5);
            }
            d.Copy(fig.pixels);
            Lobe(d,Husk,76,83,6+13*close,3+5*close,-26,7);
            Coil(d,Husk,V(35,82),V(30,94),V(72,97-w*2),V(87,66+lag),5.2f,3,close,5);
            Coil(d,Husk,V(92,72),V(99,59+lag),V(74,45-w),V(63,53+lag),6.1f,2.4f,close,5);
            Join(d,Husk,V(33,80),V(31,62+w),fig.left,4.1f,close,1);
            Join(d,Husk,V(85,86),V(87,71+lag),fig.right,4.2f,close,-1);
            Coil(d,Husk,V(91,76),V(102,81),V(90,88),fig.boot,3.1f,1.8f,close,4);
            int fx=(int)fig.face.x-3,fy=(int)fig.face.y-3;
            d.Copy(fig.pixels.Crop(fx,fy,7,7),fx,fy);
            Contact(fig);
            return FinishBoss(d,ch,key,f);
        }

        private static Image RedCorolla(string ch,string key,int f)
        {
            var d=new Image();
            float fan=Ease(f,0,7), close=Ease(f,3,8), w=Wave(f), lag=Wave(f,-1.8f);
            var fig=Figure(ch,PaintedPose.Corolla,V(w*.7f,-9*fan+lag),-4+4*fan+2*w,-3+3*fan+w,.93f+.07f*fan);
            Lobe(d,Crimson,64,76,9+9*fan,7+13*fan,0,4);
            for (int side=-1;side<=1;side+=2)
            {
                float s=side<0?w:lag;
                Coil(d,Crimson,V(64+side*6,76),V(64+side*52,79+s*3),V(64+side*17,48),
                    V(64+side*(38+s*2),36-s*3),5.4f,1.0f,fan,6);
                Coil(d,Crimson,V(64,74),V(64+side*34,59+s),V(64+side*8,36),
                    V(64+side*(22+s),27+s*3),3.5f,1.0f,fan,5);
                Coil(d,Crimson,V(64+side*6,76),V(64+side*30,65),V(64+side*48,83+s*2),
                    V(64+side*(39-s*3),58+s*2),4.2f,1.1f,fan,5);
                Coil(d,Crimson,V(64,79),V(64+side*26,77),V(64+side*26,99),V(64+side*(12+s),88),
                    4.4f,1.0f,fan,4);
            }
            d.Copy(fig.pixels);
            Join(d,Crimson,V(40,77+w),V(32,65+lag),fig.left,4.3f,close,-1);
            Join(d,Crimson,V(87,76+lag),V(96,63+w),fig.right,4.3f,close,1);
            Coil(d,Crimson,V(42,78),V(46,94+w),V(65,83+lag),fig.boot,3.5f,1.7f,close,4);
            Contact(fig);
            return FinishBoss(d,ch,key,f);
        }

        private static void Rose(Image d, Vector2 at, float scale)
        {
            if (scale<.25f) return;
            float x=at.x,y=at.y;
            // Unequal overlapping petals keep the flower from reading as a red dot at game size.
            Lobe(d,Crimson,x,y,4.6f*scale,4.1f*scale,-22,2);
            Lobe(d,Crimson,x-2*scale,y+.7f*scale,2.8f*scale,2.2f*scale,29,1);
            Lobe(d,Crimson,x+1.6f*scale,y+1.1f*scale,2.6f*scale,2.3f*scale,-37,3);
            d.Put(Mathf.RoundToInt(x),Mathf.RoundToInt(y-1),Crimson[5]);
            d.Put(Mathf.RoundToInt(x+1),Mathf.RoundToInt(y),Crimson[1]);
        }

        private static void HangingChain(Image d, Vector2 pin, float length, float sway)
        {
            for (float y=2; y<length; y+=4)
            {
                float t=y/length;
                int x=Mathf.RoundToInt(pin.x+sway*t*t), top=Mathf.RoundToInt(pin.y+y);
                if (((int)y/4)%2==0)
                {
                    d.Put(x,top-1,Iron[3]); d.Put(x-1,top,Iron[4]); d.Put(x+1,top,Iron[1]);
                    d.Put(x-1,top+1,Iron[2]); d.Put(x+1,top+1,Iron[3]); d.Put(x,top+2,Iron[1]);
                }
                else { d.Put(x,top-1,Iron[2]); d.Put(x,top,Iron[4]); d.Put(x,top+1,Iron[3]); d.Put(x,top+2,Iron[1]); }
            }
        }

        private static Image BriarSeat(string ch,string key,int f)
        {
            var d=new Image();
            float rise=Ease(f,0,7), grow=Ease(f,2,8), w=Wave(f), lag=Wave(f,-1.4f);
            var fig=Figure(ch,PaintedPose.Briar,V(0,-2*rise+lag*.6f),-5+4*rise+w,-3+3*rise+lag);
            for (int side=-1;side<=1;side+=2)
            {
                float s=side<0?w:lag;
                var tip=V(64+side*(32+s),19+(side<0?3:0)+s);
                var start=V(64+side*12,87);
                var a=V(64+side*39,79+s);
                var b=V(64+side*21,44);
                Coil(d,Thorn,start,a,b,tip,5.1f,1.1f,rise,6);
                for (int j=1;j<=5;j++)
                {
                    float along=j/6f;
                    if (along>rise) break;
                    var stem=Curve(start,a,b,tip,along);
                    var leaf=stem+V(side*(j%2==0?-5:4),-4);
                    Lobe(d,Thorn,leaf.x,leaf.y,4.7f,1.9f,side*(j%2==0?40:-45),j);
                    d.Put((int)stem.x-side*3,(int)stem.y+2,Thorn[4]);
                }
                Coil(d,Thorn,V(64+side*21,64),V(64+side*21,45),V(64+side*10,38),V(64+side*8,24+s),2.5f,.8f,grow,4);
                if (rise>.6f)
                {
                    var bloom=Curve(start,a,b,tip,Mathf.Min(.89f,rise));
                    Rose(d,bloom,grow);
                    var joint=Curve(start,a,b,tip,Mathf.Min(.72f,rise));
                    var pin=joint+V(-side*8,-3);
                    Coil(d,Thorn,joint,joint+V(-side*3,0),pin+V(side*2,0),pin,1.8f,.9f,grow,2);
                    HangingChain(d,pin,26*grow,-side*(2.5f+s*1.4f));
                    Rose(d,Curve(start,a,b,tip,.47f)+V(side*1,0),grow*.75f);
                }
            }
            Lobe(d,Thorn,65,86,12+15*rise,4+4*rise,1,3);
            d.Copy(fig.pixels);
            Join(d,Thorn,V(37,82),V(39+w,73),fig.left,3.2f,grow,1);
            Join(d,Thorn,V(91,80),V(90,66+lag),fig.right,3.2f,grow,-1);
            Coil(d,Thorn,V(39,86),V(50,97),V(67,83+lag),fig.boot,3.8f,1.6f,grow,5);
            if (grow>.5f) Rose(d,V(39+w,80+lag),grow*.85f);
            Contact(fig);
            return FinishBoss(d,ch,key,f);
        }

        private static Image PaleSpiral(string ch,string key,int f)
        {
            var d=new Image();
            float rise=Ease(f,0,7), wind=Ease(f,2,8), w=Wave(f), lag=Wave(f,-1.7f);
            var fig=Figure(ch,PaintedPose.Spiral,V(w*.6f,-2*rise+lag),-5+3*rise+lag,-3*wind+w,1-.07f*wind);
            Lobe(d,Pearl,64,86,10+19*rise,3+6*rise,-2,2);
            for (int side=-1;side<=1;side+=2)
            {
                float s=side<0?w:lag;
                var crown=V(64+side*(20+2*s),42+2*s);
                var stem=V(64+side*9,84);
                Coil(d,Pearl,stem,V(64+side*38,93),V(64+side*37,61+s),crown,6.3f,5.4f,rise,5);
                var baseOfCurl=Curve(stem,V(64+side*38,93),V(64+side*37,61+s),crown,rise);
                float coilBack=Ease(f,4,8);
                Coil(d,Pearl,baseOfCurl,V(64+side*37,33+s),V(64+side*13,19+s*2),
                    V(64+side*10,42+s),7.2f,3.2f,coilBack,4);
            }
            d.Copy(fig.pixels);
            Join(d,Pearl,V(39,67+w),V(37,60+w),fig.left,5.8f,wind,1);
            Join(d,Pearl,V(91,64+lag),V(88,61-lag),fig.right,5.9f,wind,-1);
            Coil(d,Pearl,V(35,73),V(36,91+w),V(78,88+lag),V(81,71+lag),5.7f,3.3f,wind,6);
            Coil(d,Pearl,V(89,76+lag),V(98,93),V(63,95-w),V(52,86+w),5.5f,2.4f,wind,5);
            Coil(d,Pearl,V(49,88),V(50,82),V(60,82+lag),fig.boot,2.8f,1.2f,wind,3);
            Contact(fig);
            return FinishBoss(d,ch,key,f);
        }
    }
}
