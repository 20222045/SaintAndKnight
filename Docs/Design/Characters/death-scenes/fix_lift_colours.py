# Restore Cecilia's original colours/contrast in the generated lift frames (the generator washes her out).
import sys
from PIL import Image
S=sys.argv[1]
atlas=Image.open('Assets/SaintKnightDay06/Resources/SaintKnightDay06/cecilia_motion.png').convert('RGBA')
ref=atlas.crop((2*128,2*96,3*128,3*96))                                   # cell 26, the pose she was lifted in
ex=Image.open('Docs/Design/Enemies/Day09/executioner/idle.png').convert('RGBA')
def colours(im): return [p[:3] for p in im.getdata() if p[3]>200]
cp=colours(ref); ep=set(colours(ex))
cpal=sorted(set(cp)); epal=sorted(ep)
lum=lambda c:.299*c[0]+.587*c[1]+.114*c[2]
ref_l=sorted(lum(c) for c in cp)
def d2(a,b): return (a[0]-b[0])**2+(a[1]-b[1])**2+(a[2]-b[2])**2
def near(c,pal): return min(pal,key=lambda p:d2(c,p))
def fix(f):
    px=f.load(); mine=[]
    for y in range(96):
        for x in range(128):
            r,g,b,a=px[x,y]
            if a<128: px[x,y]=(0,0,0,0); continue
            c=(r,g,b)
            # hers = right of his body and closer to her palette than to his
            if x>=68 and d2(c,near(c,cpal))<=d2(c,near(c,epal)): mine.append((x,y,c))
            else: px[x,y]=(r,g,b,255)
    order=sorted(range(len(mine)),key=lambda i:lum(mine[i][2]))            # match her luminance spread to the original
    for rank,i in enumerate(order):
        x,y,c=mine[i]; target=ref_l[rank*len(ref_l)//len(mine)]
        k=target/max(1.0,lum(c)); c2=tuple(min(255,int(v*k)) for v in c)
        px[x,y]=near(c2,cpal)+(255,)
    return f
out=[]
for i in range(1,5):
    f=fix(Image.open(f'{S}/exec/f{i}.png').convert('RGBA')); f.save(f'{S}/lift_fixed_f{i}.png'); out.append(f)
strip=Image.new('RGBA',(128*4,96*2),(40,38,48,255))
for n in range(4):
    strip.alpha_composite(Image.open(f'{S}/exec/f{n+1}.png').convert('RGBA'),(128*n,0)); strip.alpha_composite(out[n],(128*n,96))
strip.crop((0,0,512,192)).resize((512*3,192*3),Image.NEAREST).save(f'{S}/lift_fixed_compare.png')
