using System;
using System.Collections.Generic;
using UnityEngine;

namespace SaintKnight.SpriteCook.Editor
{
    public static partial class SKNativeDefeatBuilder
    {
        [Serializable] private sealed class StageFrame
        {
            public string character,key,phase,leftPart,rightPart;
            public int frame,actorPixels,visibleActorPixels,attachments;
            public float actorX,actorY,leftActorGap,rightActorGap,leftHoldGap,rightHoldGap;
        }
        [Serializable] private sealed class StageManifest { public int version=1; public StageFrame[] frames; }
        private static readonly List<StageFrame> StageFrames=new List<StageFrame>();

        private static Vector2 NearestPixel(Image source,Vector2 wanted)
        {
            Vector2 result=wanted; float nearest=10000;
            for(int y=(int)wanted.y-4;y<=(int)wanted.y+4;y++) for(int x=(int)wanted.x-4;x<=(int)wanted.x+4;x++)
                if(source.Get(x,y).a!=0 && (V(x,y)-wanted).sqrMagnitude<nearest)
                { nearest=(V(x,y)-wanted).sqrMagnitude; result=V(x,y); }
            return result;
        }

        // Both the sleeves and the hand patches come from the current enemy's original art.
        private static void EnemyArm(Image target,Image source,Vector2 shoulder,Vector2 elbow,Vector2 hand,
            Vector2 sourceShoulder,Vector2 sourceElbow,Vector2 sourceHand,float radius)
        {
            Limb(target,source,sourceShoulder,sourceElbow,shoulder,elbow,radius);
            Limb(target,source,sourceElbow,sourceHand,elbow,hand,radius*.8f);
            var p=NearestPixel(source,sourceHand);
            target.Copy(source.Crop((int)p.x-2,(int)p.y-2,5,5),Mathf.RoundToInt(hand.x)-2,Mathf.RoundToInt(hand.y)-2);
        }

        private static Image StageActor(string ch,bool duck,Vector2 origin,Vector2 left,Vector2 right,bool mirror=false)
        {
            var image=NativeActor(ch,duck?5:0,mirror?V(-origin.x,origin.y):origin,
                mirror?V(127-right.x,right.y):left,mirror?V(127-left.x,left.y):right,1f,false);
            if(mirror) image=image.MirrorMaterial();
            actorRaster=image; actorLeft=left; actorRight=right;
            return image;
        }

        private static Image TurnActor(Image source,Vector2 pivot,Vector2 centre,float degrees)
        {
            var result=new Image(); float angle=degrees*Mathf.Deg2Rad,c=Mathf.Cos(angle),s=Mathf.Sin(angle);
            for(int y=0;y<H;y++) for(int x=0;x<W;x++)
            {
                var delta=V(x,y)-centre;
                var p=pivot+V(c*delta.x+s*delta.y,-s*delta.x+c*delta.y);
                result.Put(x,y,source.Get(Mathf.RoundToInt(p.x),Mathf.RoundToInt(p.y)));
            }
            return result;
        }

        private static Vector2 TurnPoint(Vector2 p,Vector2 pivot,Vector2 centre,float degrees)
        {
            float a=degrees*Mathf.Deg2Rad,c=Mathf.Cos(a),s=Mathf.Sin(a); p-=pivot;
            return centre+V(c*p.x-s*p.y,s*p.x+c*p.y);
        }

        private static void StageDroppedGear(Image target,string ch,int floorOffset=0)
        {
            var parts=new Image();
            if(ch=="cecilia")
            {
                var r=Rig(ch,0); Equipment(parts,r,r.pw,V(-1000,-1000));
                var chain=QuarterTurn(TightCrop(parts)); target.Copy(chain,8,93-chain.height+floorOffset);
                return;
            }
            DroppedEquipment(parts,ch);
            // Keep the shield out of the kneeling figure's legs and preserve every equipment pixel.
            target.Copy(parts.Crop(27,75,48,21),8,75+floorOffset);
            target.Copy(parts.Crop(76,75,52,21),31,75+floorOffset);
        }

        private static void StageVine(Image target,string ch,bool wall,Vector2 a,Vector2 b,Vector2 c,
            Vector2 end,float radius,float reveal=1f)
        {
            var mask=new Image(target.width,target.height); var saved=vineRaster; vineRaster=null;
            var palette=wall?Wall:RedVine; Vine(mask,a,b,c,end,radius,palette,reveal); vineRaster=saved;
            var material=wall?Trap(ch,"wall",21):RootMaterial(ch,21);
            for(int y=0;y<mask.height;y++) for(int x=0;x<mask.width;x++)
            {
                var ink=mask.Get(x,y); if(ink.a==0||ink.Equals(palette[0])) continue;
                var texel=material.Get((wall?91:57)+x%8,(wall?24:28)+y%24);
                if(texel.a==0) continue;
                if(ink.Equals(palette[1])) texel=C((byte)(texel.r*2/3),(byte)(texel.g*2/3),(byte)(texel.b*2/3));
                mask.Put(x,y,texel);
            }
            target.Copy(mask); if(vineRaster!=null) vineRaster.Copy(mask);
        }

        private static Image FinishStage(Image result,string ch,string key,int frame,string phase,int attachments)
        {
            if(collectFits)
            {
                int count=0,visible=0; float sx=0,sy=0;
                if(actorRaster!=null) for(int y=0;y<actorRaster.height;y++) for(int x=0;x<actorRaster.width;x++)
                {
                    var c=actorRaster.Get(x,y); if(c.a==0) continue;
                    count++; sx+=x; sy+=y;
                    if(c.Equals(result.Get(x,y))) visible++;
                }
                StageFrames.Add(new StageFrame { character=ch,key=key,frame=frame,phase=phase,
                    actorPixels=count,visibleActorPixels=visible,actorX=count>0?sx/count:0,actorY=count>0?sy/count:0,
                    attachments=attachments,leftPart=actorLeftPart,rightPart=actorRightPart,
                    leftActorGap=attachments>0?PixelGap(actorRaster,actorLeft):0,
                    rightActorGap=attachments>1?PixelGap(actorRaster,actorRight):0,
                    leftHoldGap=attachments>0?PixelGap(vineRaster,actorLeft):0,
                    rightHoldGap=attachments>1?PixelGap(vineRaster,actorRight):0 });
            }
            actorRaster=null; vineRaster=null;
            return result;
        }

        private static Image StageScene(string ch,string key,int frame)
        {
            float phase=(frame-8)*Mathf.PI*2/10;
            int sway=frame<8?0:Mathf.RoundToInt(Mathf.Sin(phase));
            int breathe=frame<8?0:Mathf.RoundToInt(Mathf.Cos(phase));
            actorLeftPart=actorRightPart="upper_arm";
            if(key=="monk") return MonkScene(ch,frame,sway,breathe);
            if(key=="executioner") return ExecutionerScene(ch,frame,sway,breathe);
            if(key=="tentacle") return FloorSwallowScene(ch,frame,sway,breathe);
            return WallSwallowScene(ch,frame,sway,breathe);
        }

        private static Image MonkScene(string ch,int frame,int sway,int breathe)
        {
            var d=new Image(); var arms=new Image(); var native=Enemy("monk","grab-windup");
            d.Copy(Enemy("monk",frame<2?"grab-windup":"idle",0),-17,frame<8?0:Mathf.Max(0,-breathe));
            bool duck=frame>=3;
            var r=Rig(ch,duck?5:0); var origin=V(12+sway,duck?breathe:0);
            var left=r.lw+origin; var right=r.rw+origin;
            if(duck) { left=V(72+sway,77+breathe); right=V(85+sway,73+breathe); }
            var hero=StageActor(ch,duck,origin,left,right);
            if(frame<3)
            {
                hero=new Image(); hero.Copy(Pose(ch,frame==0?"Pounced":frame==1?"Hurt":"Held",0),12,0);
            }
            var gripL=NearestPixel(hero,(frame==2?Rig(ch,5).ls:r.ls)+origin+V(0,3));
            var gripR=NearestPixel(hero,(frame==2?Rig(ch,5).rs:r.rs)+origin+V(1,3));
            // Back sleeve, actor, then front sleeve: pressure from both sides of the upper arms.
            EnemyArm(arms,native,V(56,55),V(73+sway,53+breathe),gripR,V(64,59),V(76,62),V(89,66),4f);
            d.Copy(arms); d.Copy(hero);
            var front=new Image();
            EnemyArm(front,native,V(48,58),V(52+sway,77+breathe),gripL,V(64,59),V(76,62),V(89,66),4.5f);
            d.Copy(front); arms.Copy(front);
            if(frame>=3) StageDroppedGear(d,ch);
            actorRaster=hero; vineRaster=arms; actorLeft=gripL; actorRight=gripR;
            return FinishStage(d,ch,"monk",frame,duck?"kneeling_hold":"guard_fails",frame>=3?2:0);
        }

        private static Image ExecutionerScene(string ch,int frame,int sway,int breathe)
        {
            var d=new Image(); var native=Enemy("executioner","idle");
            var enemy=Enemy("executioner",frame<2?"idle":"recovery",0);
            d.Copy(enemy,-5,-16+(frame<8?0:Mathf.Max(0,breathe)));
            var r=Rig(ch,5); var baseActor=StageActor(ch,true,V(0,0),r.lw,r.rw);
            float fold=Mathf.SmoothStep(0,1,Mathf.Clamp01((frame-1)/5f));
            float angle=-90*fold;
            var centre=V(77+sway,76-11*fold+breathe); var pivot=V(66,76);
            var hero=TurnActor(baseActor,pivot,centre,angle);
            if(frame<2)
            {
                hero=new Image(); hero.Copy(Pose(ch,frame==0?"Hurt":"Held",0),12,0);
            }
            var upper=NearestPixel(hero,TurnPoint(r.ls+V(0,3),pivot,centre,angle));
            var knee=NearestPixel(hero,TurnPoint(V(67,84),pivot,centre,angle));
            var back=new Image();
            EnemyArm(back,native,V(68,45),V(89,60+breathe),knee,V(71,52),V(77,66),V(78,75),5f);
            d.Copy(back); d.Copy(hero);
            var front=new Image();
            EnemyArm(front,native,V(47,45),V(45+sway,68+breathe),upper,V(48,52),V(44,65),V(43,75),5f);
            d.Copy(front); back.Copy(front); if(frame>=2) StageDroppedGear(d,ch);
            actorRaster=hero; vineRaster=back; actorLeft=upper; actorRight=knee; actorRightPart="outer_knee";
            return FinishStage(d,ch,"executioner",frame,frame<6?"fold_sideways":"sideways_hold",frame>=2?2:0);
        }

        private static Image FloorSwallowScene(string ch,int frame,int sway,int breathe)
        {
            var d=new Image(); vineRaster=new Image(); actorRaster=null;
            actorLeftPart=actorRightPart="wrist";
            if(frame>=8)
            {
                d.Copy(RootMaterial(ch,21+(frame-8)%2));
                StageVine(d,ch,false,V(42,94),V(34,84),V(34+sway,60),V(41+sway,56+breathe),1.7f);
                StageVine(d,ch,false,V(86,94),V(95,83),V(95-sway,59),V(88-sway,54-breathe),1.7f);
                StageDroppedGear(d,ch);
                return FinishStage(d,ch,"tentacle",frame,"consumed",0);
            }
            int[] depth={0,-2,-8,-11,-7,5,27,54};
            int dy=depth[frame]; var origin=V(0,dy);
            var lw=V(54,57+dy); var rw=V(77,54+dy);
            float growth=Mathf.Clamp01(frame/2f);
            d.Ellipse(64,88,25,6,DarkVine[0]);
            StageVine(d,ch,false,V(43,94),V(37,73),V(41,40),V(49,48),2.3f,growth);
            StageVine(d,ch,false,V(85,94),V(90,73),V(86,40),V(79,48),2.5f,growth);
            var hero=StageActor(ch,false,origin,lw,rw);
            // The same floor mouth occludes the feet first; nothing is drawn below its grounded lip.
            for(int y=Ground;y<H;y++) for(int x=0;x<W;x++) hero.Put(x,y,default);
            d.Copy(hero);
            StageVine(d,ch,false,V(43,94),V(38,79),V(41,58+dy),lw,1.9f,growth);
            StageVine(d,ch,false,V(85,94),V(90,76),V(85,58+dy),rw,1.9f,growth);
            if(frame>=2) StageVine(d,ch,false,V(45,84+dy),V(52,75+dy),V(78,87+dy),V(80,76+dy),1.8f);
            if(frame>=4)
            {
                int close=frame-4;
                StageVine(d,ch,false,V(43,94),V(35,72),V(46+close*2,48),V(56,69-close*4),2.7f);
                StageVine(d,ch,false,V(85,94),V(93,70),V(82-close*2,47),V(73,68-close*4),2.8f);
                d.Copy(RootMaterial(ch,21).Crop(31,75-close*8,67,21+close*8),31,75-close*8);
            }
            actorRaster=hero; actorLeft=lw; actorRight=rw;
            StageDroppedGear(d,ch);
            return FinishStage(d,ch,"tentacle",frame,frame<2?"boots_caught":frame<5?"lifted_wrap":"drawn_into_floor",frame>=2&&frame<=5?2:0);
        }

        private static Image WallSwallowScene(string ch,int frame,int sway,int breathe)
        {
            var d=new Image(160,112); vineRaster=new Image(160,112); actorRaster=null;
            d.Copy(Trap(ch,"wall",21));
            if(frame>=8)
            {
                StageVine(d,ch,true,V(102,34),V(87+sway,50),V(111-sway,73),V(102,95),2.6f);
                StageVine(d,ch,true,V(136,109),V(152,88),V(145-sway,61),V(139+sway,65+breathe),2.5f);
                StageDroppedGear(d,ch,16);
                return FinishStage(d,ch,"fleshwall",frame,"consumed",0);
            }
            d.Ellipse(101,69,13,30,Wall[0]);
            int[] pull={0,2,5,10,17,28,47,68};
            bool duck=frame>=2; var origin=V(-22+pull[frame],0);
            var r=Rig(ch,duck?5:0);
            var left=duck?V(52+origin.x,(ch=="cecilia"?54:48)+origin.y):V(127-r.rw.x+origin.x,r.rw.y+origin.y);
            var right=duck?V(65+origin.x,72+origin.y):V(127-r.lw.x+origin.x,r.lw.y+origin.y);
            var posed=StageActor(ch,duck,origin,left,right,true);
            var hero=new Image(160,112); hero.Copy(posed,0,16);
            var upperL=NearestPixel(hero,V((ch=="cecilia"?54:53)+origin.x,(ch=="cecilia"?62:59)+16));
            var upperR=NearestPixel(hero,V(65+origin.x,81));
            StageVine(d,ch,true,V(100,52),V(91,40),V(67,48),upperL,2.5f);
            StageVine(d,ch,true,V(103,84),V(92,84),V(78,76),upperR,2.4f);
            int seam=frame<5?103:92;
            for(int y=0;y<hero.height;y++) for(int x=seam;x<hero.width;x++) hero.Put(x,y,default);
            d.Copy(hero);
            StageVine(d,ch,true,V(100,52),V(91,40),V(67,48),upperL,2.5f);
            StageVine(d,ch,true,V(103,84),V(92,84),V(78,76),upperR,2.4f);
            // The foreground rim hides the entering side; the outside hand and head disappear last.
            StageVine(d,ch,true,V(108,35),V(91,49),V(92,87),V(106,101),3.3f);
            if(frame<6) StageVine(d,ch,true,V(100,105),V(85,108),V(70,101),V(65+origin.x,100),2.4f);
            StageDroppedGear(d,ch,16);
            actorRaster=hero; actorLeft=upperL; actorRight=upperR;
            return FinishStage(d,ch,"fleshwall",frame,frame<2?"arms_caught":frame<5?"kneeling_pull":"drawn_sideways",frame<6?2:0);
        }
    }
}
