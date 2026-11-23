using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

// The human layer is never scaled, recolored or redrawn. Generated material supplies the occluding layer.
public static class SaintKnightDedicatedTrapFrames
{
    private const int NativeWidth=128, NativeHeight=96, Ground=93;
    private sealed class Pixels
    {
        public int Width,Height; public int[] Data;
        public Pixels(int width,int height) {Width=width;Height=height;Data=new int[width*height];}
        public Pixels Copy() {var p=new Pixels(Width,Height);Array.Copy(Data,p.Data,Data.Length);return p;}
        public int At(int x,int y) {return x>=0&&x<Width&&y>=0&&y<Height?Data[y*Width+x]:0;}
        public void Put(int x,int y,int color) {if(x>=0&&x<Width&&y>=0&&y<Height)Data[y*Width+x]=color;}
        public static Pixels Read(string path)
        {
            using(var image=new Bitmap(path)) {
                var p=new Pixels(image.Width,image.Height);
                var bits=image.LockBits(new Rectangle(0,0,image.Width,image.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
                try {for(int y=0;y<p.Height;y++)Marshal.Copy(IntPtr.Add(bits.Scan0,y*bits.Stride),p.Data,y*p.Width,p.Width);}
                finally{image.UnlockBits(bits);}return p;
            }
        }
        public void Save(string path)
        {
            using(var image=new Bitmap(Width,Height,PixelFormat.Format32bppArgb)) {
                var bits=image.LockBits(new Rectangle(0,0,Width,Height),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
                try{for(int y=0;y<Height;y++)Marshal.Copy(Data,y*Width,IntPtr.Add(bits.Scan0,y*bits.Stride),Width);}
                finally{image.UnlockBits(bits);}image.Save(path,ImageFormat.Png);
            }
        }
    }
    public sealed class Bounds {public int X,Y,Width,Height;public Bounds(int x,int y,int w,int h){X=x;Y=y;Width=w;Height=h;}}
    public sealed class FrameReport {
        public int Cell,Stage,Phase,NativeCell,OffsetX,OffsetY,SourceBodyPixels,PlacedBodyPixels,VisibleBodyPixels,OccludedBodyPixels,FrontMaterialPixels;
        public int UnchangedVisibleBodyPixels,RecoloredVisibleBodyPixels;
        public string Motion,Pose,Hash,Occlusion; public Bounds VisibleBodyBounds; public bool BodyFlipped,UnscaledBody;
    }
    public sealed class SheetReport {
        public string Character,Trap;public int FrameWidth,FrameHeight,FrameCount,UniqueFrames,MinimumVisibleBodyPixels,MaximumVisibleBodyPixels;
        public FrameReport[] Frames;
    }
    private static readonly int[] SlimePalette={Color(23,32,25),Color(49,58,34),Color(78,87,48),Color(113,122,59),Color(154,171,85),Color(197,206,121)};
    private static readonly int[] TentaclePalette={Color(22,13,23),Color(44,23,36),Color(69,32,49),Color(113,49,63),Color(173,88,96),Color(220,138,123)};
    private static readonly int[] WallPalette={Color(22,13,23),Color(44,23,36),Color(73,35,48),Color(115,56,62),Color(162,83,80),Color(199,119,101)};
    private static int Color(int r,int g,int b){return unchecked((int)0xff000000)|(r<<16)|(g<<8)|b;}
    private static int Alpha(int c){return (int)((uint)c>>24);}
    private static Bounds FindBounds(Pixels p,int x0,int y0,int x1,int y1,int threshold=1)
    {
        int l=x1,r=-1,t=y1,b=-1;
        for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)if(Alpha(p.At(x,y))>=threshold){l=Math.Min(l,x);r=Math.Max(r,x);t=Math.Min(t,y);b=Math.Max(b,y);}
        return r<0?new Bounds(0,0,0,0):new Bounds(l,t,r-l+1,b-t+1);
    }
    private static Pixels Crop(Pixels p,int x,int y,int w,int h)
    {
        var c=new Pixels(w,h);for(int yy=0;yy<h;yy++)for(int xx=0;xx<w;xx++)c.Put(xx,yy,p.At(x+xx,y+yy));return c;
    }
    private static int NearestPalette(int color,int[] palette)
    {
        int r=(color>>16)&255,g=(color>>8)&255,b=color&255,best=palette[0],dist=int.MaxValue;
        foreach(int c in palette){int dr=r-((c>>16)&255),dg=g-((c>>8)&255),db=b-(c&255);int d=dr*dr+dg*dg+db*db;if(d<dist){dist=d;best=c;}}return best;
    }
    private static Pixels[] Materials(Pixels sheet,int w,int h,int characterHeight,string trap)
    {
        var bounds=new Bounds[6];int maxH=1,maxW=1;
        for(int i=0;i<6;i++) {int c=i%3,r=i/3;bounds[i]=FindBounds(sheet,c*sheet.Width/3,r*sheet.Height/2,(c+1)*sheet.Width/3,(r+1)*sheet.Height/2,128);maxH=Math.Max(maxH,bounds[i].Height);maxW=Math.Max(maxW,bounds[i].Width);}
        int maxHeight=trap=="wall"?98:characterHeight+7,maxWidth=trap=="wall"?116:88;
        double scale=Math.Min((double)maxHeight/maxH,(double)maxWidth/maxW);
        int centre=trap=="wall"?80:64,ground=trap=="wall"?111:93;
        int[] palette=trap=="slime"?SlimePalette:trap=="wall"?WallPalette:TentaclePalette;
        var frames=new Pixels[6];
        for(int i=0;i<6;i++) {
            frames[i]=new Pixels(w,h);Bounds b=bounds[i];
            int dw=Math.Max(1,(int)Math.Round(b.Width*scale)),dh=Math.Max(1,(int)Math.Round(b.Height*scale));int dx=centre-dw/2,dy=ground-dh+1;
            for(int yy=0;yy<dh;yy++)for(int xx=0;xx<dw;xx++) {
                int sx=b.X+Math.Min(b.Width-1,(int)((xx+.5)*b.Width/dw)),sy=b.Y+Math.Min(b.Height-1,(int)((yy+.5)*b.Height/dh));int color=sheet.At(sx,sy);
                if(Alpha(color)>=128)frames[i].Put(dx+xx,dy+yy,NearestPalette(color,palette));
            }
        }return frames;
    }
    private static string Hash(Pixels p)
    {
        byte[] bytes=new byte[p.Data.Length*4];Buffer.BlockCopy(p.Data,0,bytes,0,bytes.Length);
        using(var sha=SHA256.Create()){return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
    }
    private static void Over(Pixels dst,Pixels src,int dx=0,int dy=0)
    {for(int y=0;y<src.Height;y++)for(int x=0;x<src.Width;x++){int c=src.At(x,y);if(Alpha(c)>0)dst.Put(x+dx,y+dy,c);}}
    private static Pixels Body(Pixels atlas,int cell){return Crop(atlas,(cell%12)*128,(cell/12)*96,128,96);}
    private static string Pose(string character,int cell)
    {
        int p=character=="cecilia"?43:53,r=character=="cecilia"?50:60,e=character=="cecilia"?59:69;
        return cell>=e?"Escape":cell>=r?"Resist":cell>=p?"Pounced":"Held";
    }
    private static int NearestOpaqueOnRow(Pixels material,int x,int y,int fallback)
    {
        for(int d=0;d<material.Width;d++)foreach(int xx in new[]{x-d,x+d}) {int c=material.At(xx,y);if(Alpha(c)>0)return c;}return fallback;
    }
    private static Pixels Compose(string character,string trap,string motion,int stage,int phase,int cell,Pixels atlas,Pixels[] material,Pixels wall,out FrameReport report)
    {
        bool isWall=trap=="wall",escape=motion=="Escape",consumed=motion=="Consumed";
        int w=isWall?160:128,h=isWall?112:96,ground=isWall?111:93;
        var output=new Pixels(w,h);var human=new Pixels(w,h);var humanOwner=new bool[w*h];var covered=new bool[w*h];
        int[] sink={0,2,5,8,14,22};int[] anchor={48,54,61,68,75,82};int[] wallSink={0,0,1,2,4,8};
        int dx=isWall?anchor[stage]-64:0,dy=isWall?19+wallSink[stage]:sink[stage];
        if(motion=="Resist") {dx+=phase==0?-2:2;dy+=phase==0?-2:0;}
        if(escape){dx=isWall?new[]{0,-14,-28}[stage]:new[]{-2,-6,-10}[stage];dy=isWall?17:0;}
        Pixels mat=material[consumed?5:Math.Min(stage,5)].Copy();
        if(motion=="Resist") {
            // Attached folds pull sideways with the actor while the bottom roots stay fixed.
            var pulled=mat.Copy();int pull=phase==0?-1:1;
            for(int y=0;y<ground-5;y++)for(int x=0;x<w;x++)pulled.Put(x,y,mat.At(x-pull,y));
            mat=pulled;
        }
        if(isWall)Over(output,wall);
        if(!escape)Over(output,mat);
        int native=cell,total=0,placed=0,visible=0,front=0;
        Bounds humanBounds=new Bounds(0,0,0,0);
        if(!consumed) {
            var pose=Body(atlas,cell);Bounds sourceBounds=FindBounds(pose,0,0,128,96);humanBounds=new Bounds(sourceBounds.X+dx,sourceBounds.Y+dy,sourceBounds.Width,sourceBounds.Height);
            for(int y=0;y<96;y++)for(int x=0;x<128;x++) {
                int c=pose.At(x,y);if(Alpha(c)==0)continue;total++;
                int tx=x+dx,ty=y+dy;
                // At the last two depths, the lower body is behind the enclosing trap interior.
                bool retained=true;
                if(!escape&&stage>=4)retained=y<=sourceBounds.Y+(stage==4?11:3);
                if(!retained||tx<0||tx>=w||ty<0||ty>=h||(!escape&&!isWall&&ty>=ground))continue;
                human.Put(tx,ty,c);humanOwner[ty*w+tx]=true;placed++;
            }
            Over(output,human);
        }
        if(escape) {
            // Only material attached to the wrist/hem follows the actor; no anchored floor slab.
            if(!isWall&&stage<2) {
                Pixels tail=material[2];
                int low=ground-(stage==0?42:30),high=ground-(stage==0?11:15);
                var edge=new List<int>();
                for(int y=low;y<=high;y++){int right=-1;for(int x=0;x<w;x++)if(Alpha(tail.At(x,y))>0)right=x;if(right>=64)edge.Add(right);}
                if(edge.Count>0) {
                    edge.Sort();int shiftX=humanBounds.X+humanBounds.Width-2-edge[edge.Count/2];
                    int shiftY=humanBounds.Y+humanBounds.Height/2-(low+high)/2;
                    for(int y=low;y<=high;y++) {
                        int right=-1;for(int x=0;x<w;x++)if(Alpha(tail.At(x,y))>0)right=x;if(right<64)continue;
                        double alongTail=(double)(y-low)/Math.Max(1,high-low);
                        int thickness=Math.Max(1,(int)Math.Round((stage==0?3:2)*Math.Sin(Math.PI*alongTail)));
                        for(int x=right-thickness+1;x<=right;x++) {
                            int c=tail.At(x,y);if(Alpha(c)==0)continue;
                            output.Put(x+shiftX,y+shiftY,c);front++;
                        }
                    }
                }
            } else if(isWall) {
                Pixels tail=material[Math.Max(1,3-stage)];
                for(int y=0;y<h;y++)for(int x=0;x<w;x++) {
                    int c=tail.At(x,y);if(Alpha(c)==0||y>=ground-7)continue;
                    bool piece=stage==0?x>79&&y>ground-40:stage==1?x>86&&y>ground-24:false;
                    if(piece) {output.Put(x-4,y-3,c);front++;}
                }
            }
        } else {
            double[] fraction={.98,.78,.57,.34,.14,.035};
            int coverY=humanBounds.Y+(int)Math.Round(humanBounds.Height*fraction[stage]);
            int wallFrontX=new[]{67,70,73,76,79,83}[stage];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++) {
                int c=mat.At(x,y);if(Alpha(c)==0)continue;
                int ripple=((x/4+stage+phase)%3)-1;
                bool foreground=consumed||y>=coverY+ripple;
                if(isWall)foreground=consumed||x>=wallFrontX||y>=new[]{109,105,99,92,80,65}[stage]+ripple;
                if(trap=="tentacle")foreground=consumed||y>=coverY+ripple||((x<53||x>76)&&y>humanBounds.Y+11);
                if(!consumed&&stage<3&&y<humanBounds.Y+10&&x>=humanBounds.X&&x<humanBounds.X+humanBounds.Width)foreground=false;
                if(foreground){output.Put(x,y,c);covered[y*w+x]=true;front++;}
            }
            // A late-depth interior is closed by organic material, not by a rectangular body fade.
            if(!consumed&&trap=="slime"&&stage>=3) {
                Pixels closed=material[5];
                for(int y=Math.Max(0,coverY);y<h;y++)for(int x=0;x<w;x++) {
                    int c=closed.At(x,y);if(Alpha(c)==0)continue;
                    if(y>=coverY+((x/3+phase)%3)-1){output.Put(x,y,c);covered[y*w+x]=true;front++;}
                }
            }
            if(consumed) {
                // Upper folds breathe by one native pixel; the anchored base stays at the same ground row.
                var breathed=output.Copy();
                int breath=phase==0?-1:1;
                for(int y=0;y<ground-3;y++)for(int x=0;x<w;x++)breathed.Put(x,y,output.At(x,y+breath));
                output=breathed;
            }
        }
        int l=w,r=-1,t=h,b=-1;
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(humanOwner[y*w+x]&&!covered[y*w+x]&&output.At(x,y)==human.At(x,y)) {
            visible++;l=Math.Min(l,x);r=Math.Max(r,x);t=Math.Min(t,y);b=Math.Max(b,y);
        }
        report=new FrameReport {Stage=stage,Phase=phase,NativeCell=consumed?-1:native,OffsetX=dx,OffsetY=dy,SourceBodyPixels=total,PlacedBodyPixels=placed,VisibleBodyPixels=visible,
            OccludedBodyPixels=placed-visible,FrontMaterialPixels=front,Motion=motion,Pose=consumed?"none":Pose(character,native),Hash=Hash(output),
            VisibleBodyBounds=r<0?new Bounds(0,0,0,0):new Bounds(l,t,r-l+1,b-t+1),BodyFlipped=false,
            UnchangedVisibleBodyPixels=visible,RecoloredVisibleBodyPixels=0,UnscaledBody=true,
            Occlusion="Original trap material split front/back; organic surface plus late-depth body behind interior; no body resampling or fade"};
        return output;
    }
    private static void CopyCell(Pixels dst,Pixels cell,int index,int columns)
    {int dx=index%columns*cell.Width,dy=index/columns*cell.Height;for(int y=0;y<cell.Height;y++)Array.Copy(cell.Data,y*cell.Width,dst.Data,(dy+y)*dst.Width+dx,cell.Width);}
    private static Pixels Preview(Pixels[] cells,int columns,int scale)
    {
        int rows=(cells.Length+columns-1)/columns,cw=cells[0].Width,ch=cells[0].Height;var p=new Pixels(columns*cw*scale,rows*ch*scale);
        for(int i=0;i<p.Data.Length;i++)p.Data[i]=Color(40,40,48);
        for(int i=0;i<cells.Length;i++)for(int y=0;y<ch;y++)for(int x=0;x<cw;x++) {
            int c=cells[i].At(x,y);if(Alpha(c)==0)continue;
            for(int yy=0;yy<scale;yy++)for(int xx=0;xx<scale;xx++)p.Put((i%columns*cw+x)*scale+xx,(i/columns*ch+y)*scale+yy,c);
        }return p;
    }
    public static SheetReport[] Build(string root,string sources,string outputDirectory)
    {
        var reports=new List<SheetReport>();var previews=new Dictionary<string,List<Pixels>>();
        var wall=Pixels.Read(Path.Combine(root,"Assets/SaintKnightDay10/Art/flesh_wall_idle_f00.png"));
        foreach(string character in new[]{"cecilia","adela"}) {
            var atlas=Pixels.Read(Path.Combine(root,"Assets/SaintKnightDay06/Resources/SaintKnightDay06/"+character+"_motion.png"));
            int[] swallow=character=="cecilia"?new[]{43,45,47,39,53,56}:new[]{53,55,57,48,63,66};
            int[] resist=character=="cecilia"?new[]{44,45,50,51,52,53,54,55,56,57,57,58}:new[]{54,55,60,61,62,63,64,65,66,67,67,68};
            int[] escape=character=="cecilia"?new[]{60,63,67}:new[]{70,73,77};
            foreach(string trap in new[]{"slime","tentacle","wall"}) {
                bool isWall=trap=="wall";int w=isWall?160:128,h=isWall?112:96;
                var material=Materials(Pixels.Read(Path.Combine(sources,trap+"_material.png")),w,h,character=="cecilia"?55:62,trap);
                var cells=new Pixels[24];var frames=new List<FrameReport>();var unique=new HashSet<string>();
                for(int i=0;i<24;i++)cells[i]=new Pixels(w,h);
                for(int i=0;i<23;i++) {
                    string motion=i<6?"Swallow":i<18?"Resist":i<21?"Escape":"Consumed";
                    int stage=i<6?i:i<18?(i-6)/2:i<21?i-18:5,phase=i<6?0:i<18?(i-6)%2:i<21?0:i-21;
                    int native=i<6?swallow[stage]:i<18?resist[i-6]:i<21?escape[stage]:-1;
                    FrameReport fr;cells[i]=Compose(character,trap,motion,stage,phase,native,atlas,material,wall,out fr);fr.Cell=i;frames.Add(fr);unique.Add(fr.Hash);
                }
                var packed=new Pixels(w*8,h*3);for(int i=0;i<24;i++)CopyCell(packed,cells[i],i,8);
                string name=character+"_"+trap;packed.Save(Path.Combine(outputDirectory,name+".png"));
                string manifest="{\n  \"schemaVersion\":1,\n  \"character\":\""+character+"\",\n  \"trap\":\""+trap+"\",\n  \"frameWidth\":"+w+",\n  \"frameHeight\":"+h+",\n  \"columns\":8,\n  \"rows\":3,\n  \"usedFrames\":23,\n  \"ppu\":48,\n  \"pivotX\":0.5,\n  \"pivotY\":"+(isWall?"0":"0.26625")+",\n  \"swallowStart\":0, \"swallowCount\":6,\n  \"resistStart\":6, \"resistStages\":6, \"resistFramesPerStage\":2,\n  \"escapeStart\":18, \"escapeCount\":3,\n  \"consumedStart\":21, \"consumedCount\":2\n}\n";
                File.WriteAllText(Path.Combine(outputDirectory,name+".json"),manifest);
                Preview(cells,8,3).Save(Path.Combine(outputDirectory,name+"_contact.png"));
                if(!previews.ContainsKey(trap))previews.Add(trap,new List<Pixels>());for(int i=0;i<6;i++)previews[trap].Add(cells[i]);
                int min=int.MaxValue,max=0;foreach(var f in frames){min=Math.Min(min,f.VisibleBodyPixels);max=Math.Max(max,f.VisibleBodyPixels);}
                reports.Add(new SheetReport {Character=character,Trap=trap,FrameWidth=w,FrameHeight=h,FrameCount=23,UniqueFrames=unique.Count,
                    MinimumVisibleBodyPixels=min,MaximumVisibleBodyPixels=max,Frames=frames.ToArray()});
            }
        }
        foreach(var entry in previews)Preview(entry.Value.ToArray(),6,4).Save(Path.Combine(outputDirectory,"swallow_"+entry.Key+"_preview.png"));
        return reports.ToArray();
    }
}
