# Cecilia x executioner defeat scene, 9 cells of 128x96:
# 1 caught (original pixels) -> 2-5 lifted (PixelLab frames, her colours restored by fix_lift_colours.py)
# -> 6 dropped -> 7 he bends down over her -> 8-9 he kneels over her while she lies flat in front of him
# (these two loop). Clothed throughout. Run fix_lift_colours.py first; argv[1] = its output folder.
import sys
from PIL import Image
S=sys.argv[1]
E='Docs/Design/Enemies/Day09/executioner/'
R='Assets/SaintKnightSpriteCook/Resources/SaintKnightSpriteCook/'
atlas=Image.open('Assets/SaintKnightDay06/Resources/SaintKnightDay06/cecilia_motion.png').convert('RGBA')
death=Image.open(R+'cecilia_enemy_death.png').convert('RGBA')
def cell(im,i,cols): x,y=(i%cols)*128,(i//cols)*96; return im.crop((x,y,x+128,y+96))
def ex(name,frame): return Image.open(E+name+'.png').convert('RGBA').crop((frame*192,16,frame*192+192,112))  # feet 3px above the bottom
def col(i): return Image.open(f'Assets/SaintKnightDay06/Art/Characters/cecilia_collapse_f{i:02d}.png').convert('RGBA')
def put(dst,src,dx,dy):
    t=Image.new('RGBA',(528,296)); t.alpha_composite(src,(200+dx,100+dy)); dst.alpha_composite(t.crop((200,100,328,196)))
def frame(heroine,hx,enemy,ex_dx,ex_dy=0,on_top=False):
    c=Image.new('RGBA',(128,96))
    if not on_top: put(c,enemy,ex_dx,ex_dy)
    put(c,heroine,hx,0)
    if on_top: put(c,enemy,ex_dx,ex_dy)
    return c
idle0,idle1,rec,kneel=ex('idle',0),ex('idle',1),ex('recovery',0),ex('defeat',0)
flat=cell(death,7,4)
fs=[frame(cell(atlas,26,12),24,idle0,-8)]
fs+=[Image.open(f'{S}/lift_fixed_f{i}.png').convert('RGBA') for i in range(1,5)]
fs+=[
 frame(col(7),24,idle1,-8),                     # 6 dropped
 frame(flat,30,rec,-2),                          # 7 he bends down over her
 frame(flat,30,kneel,-22),                       # 8 kneeling over her, she lies flat in front of him
 frame(flat,30,kneel,-22,1),                     # 9
]
for n,f in enumerate(fs): f.save(f'{S}/exec7_f{n+1}.png')
strip=Image.new('RGBA',(128*5,96*2),(40,38,48,255))
for n,f in enumerate(fs): strip.alpha_composite(f,(128*(n%5),96*(n//5)))
strip.resize((128*5*3,96*2*3),Image.NEAREST).save(f'{S}/exec7_preview.png')
print([f.getbbox() for f in fs])
