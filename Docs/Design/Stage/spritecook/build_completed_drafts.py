"""Build the seven requested normalized draft sheets and 59 sliced sprites.

Canonical game atlases are read-only. Generated masters remain untouched.
Characters use one uniform nearest-neighbour scale and their canonical palette.
Boss lift frames retain a visible 12px suspension gap; no gameplay is changed.
"""
import argparse
import hashlib
import json
import math
import re
import shutil
import uuid
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
OUTPUT = HERE / "completed-pack"
CW, CH, GROUND = 128, 96, 92
SLIME = [(49, 58, 34), (78, 87, 48), (113, 122, 59), (154, 171, 85), (197, 206, 121)]
SPECS = [
    dict(name="4_enemy_death_cecilia_v3", character="cecilia", columns=4, rows=2, count=8, frozen=True,
         clips={"HeldCollapse": [0,1,2,3], "ExecutionerKnockdown": [4,5,6,7]}),
    dict(name="4_enemy_death_adela_v2", character="adela", columns=4, rows=2, count=8,
         clips={"HeldCollapse": [0,1,2,3], "ExecutionerKnockdown": [4,5,6,7]}),
    *[dict(name=f"2_boss_pattern_{ch}_v2", character=ch, columns=4, rows=3, count=11,
           lifts=[0,12,3,0,12,12,12,12,0,0,0],
           clips={"BossSweep": [0,1,2,3], "BossLift": [4,5,6,7], "BossEscape": [8], "BossDeath": [9,10]})
      for ch in ("cecilia", "adela")],
    *[dict(name=f"3_trap_reaction_{ch}_v2", character=ch, columns=3, rows=3, count=9,
           lifts=[2,5,2,0,0,0,0,0,0], slime=True,
           clips={"SpikeHurt": [0,1,2], "SlimeWalk": [3,4,5,6], "SlimeIdle": [7,8]})
      for ch in ("cecilia", "adela")],
]


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def components(alpha, opaque=True):
    width, height = alpha.size
    values = alpha.tobytes(); seen = bytearray(width * height); result = []
    for start, value in enumerate(values):
        if seen[start] or ((value >= 128) != opaque):
            continue
        seen[start] = 1; todo = [start]; points = []
        left = right = start % width; top = bottom = start // width
        while todo:
            point = todo.pop(); points.append(point); x, y = point % width, point // width
            left, right, top, bottom = min(left,x), max(right,x), min(top,y), max(bottom,y)
            for nx, ny in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
                if 0 <= nx < width and 0 <= ny < height:
                    neighbor = ny * width + nx
                    if not seen[neighbor] and ((values[neighbor] >= 128) == opaque):
                        seen[neighbor] = 1; todo.append(neighbor)
        result.append(dict(area=len(points), bounds=(left,top,right+1,bottom+1), pixels=points))
    return sorted(result, key=lambda part: part["area"], reverse=True)


def extract(raw, row_counts):
    count = sum(row_counts)
    parts = components(raw.getchannel("A"))
    assert len(parts) >= count
    bodies = parts[:count]
    if len(parts) > count:
        assert parts[count]["area"] < min(body["area"] for body in bodies) * .15, "Ambiguous disconnected sprite; inspect source."
    bodies.sort(key=lambda body: body["bounds"][1])
    ordered = []; start = 0
    for row_count in row_counts:
        ordered.extend(sorted(bodies[start:start+row_count], key=lambda body: body["bounds"][0]))
        start += row_count
    images = []
    for body in ordered:
        rectangle = body["bounds"]
        image = raw.crop(rectangle)
        mask = Image.new("L", image.size)
        for point in body["pixels"]:
            mask.putpixel((point % raw.width - rectangle[0], point // raw.width - rectangle[1]), 255)
        image.putalpha(mask)
        images.append(image)
    return images, [list(body["bounds"]) for body in ordered]


def palette_image(colors):
    image = Image.new("P", (1, 1))
    image.putpalette([value for color in colors + [colors[-1]] * (256-len(colors)) for value in color])
    return image


def recolor(image, colors):
    alpha = image.getchannel("A")
    result = image.convert("RGB").quantize(palette=palette_image(colors), dither=Image.Dither.NONE).convert("RGBA")
    result.putalpha(alpha)
    return result


def matte(image, color=(40,40,48,255)):
    result = Image.new("RGBA", image.size, color)
    result.alpha_composite(image)
    return result


def canonical(character):
    path = ROOT / f"Assets/SaintKnightDay06/Resources/SaintKnightDay06/{character}_motion.png"
    image = Image.open(path).convert("RGBA")
    expected = "9ab32982cb56" if character == "cecilia" else "f87dcdf742cc"
    assert sha(path).startswith(expected), "Canonical character changed; review design before rebuilding."
    colors = sorted({color[:3] for color in image.get_flattened_data() if color[3]})
    assert len(colors) == 32
    return colors, 55 if character == "cecilia" else 62


def character_sheet(spec, destination):
    name = spec["name"]
    colors, body_height = canonical(spec["character"])
    if spec.get("frozen"):
        # Preserve the already-reviewed Cecilia v3 PNG/JSON byte-for-byte.
        for suffix in (".png", ".json"):
            shutil.copyfile(HERE / "identity-fixed-v3" / (name+suffix), destination / (name+suffix))
        sheet = Image.open(destination / (name+".png")).convert("RGBA")
        frames = [sheet.crop((i%4*CW,i//4*CH,(i%4+1)*CW,(i//4+1)*CH)) for i in range(8)]
        return frames, dict(name=name, count=8, frozen=True, outputSha256=sha(destination/(name+".png")))
    source = HERE / (name + ".png")
    raw = Image.open(source).convert("RGBA")
    row_counts = [min(spec["columns"], spec["count"]-row*spec["columns"]) for row in range(spec["rows"])]
    images, rectangles = extract(raw, row_counts)
    scale = body_height / images[0].height
    if spec.get("slime"):
        colors = sorted(set(colors + SLIME))
    frames, records = [], []
    for index, image in enumerate(images):
        resized = image.resize((round(image.width*scale), round(image.height*scale)), Image.Resampling.NEAREST)
        resized = recolor(resized, colors)
        lift = spec.get("lifts", [0]*spec["count"])[index]
        x, y = (CW-resized.width)//2, GROUND-lift-resized.height+1
        assert x >= 2 and x+resized.width <= CW-2 and y >= 1, (name,index,"clipped body")
        frame = Image.new("RGBA", (CW,CH)); frame.alpha_composite(resized,(x,y))
        bounds = frame.getbbox()
        assert bounds and bounds[3] == GROUND-lift+1
        assert set(frame.getchannel("A").get_flattened_data()) <= {0,255}
        assert all(color[:3] in colors for color in frame.get_flattened_data() if color[3])
        frames.append(frame)
        records.append(dict(index=index,sourceRect=rectangles[index],bounds=list(bounds),footLift=lift))
    assert len({frame.tobytes() for frame in frames}) == spec["count"]
    assert frames[0].getbbox()[3]-frames[0].getbbox()[1] == body_height
    sheet = Image.new("RGBA", (spec["columns"]*CW,spec["rows"]*CH))
    for index,frame in enumerate(frames):
        sheet.alpha_composite(frame,(index%spec["columns"]*CW,index//spec["columns"]*CH))
    sheet.save(destination/(name+".png"))
    metadata = dict(schemaVersion=1,character=spec["character"],frameWidth=CW,frameHeight=CH,
                    columns=spec["columns"],rows=spec["rows"],usedFrames=spec["count"],ppu=48,
                    pivotX=.5,pivotY=.26625,groundPixelY=GROUND,standingHeight=body_height,
                    footLiftPixels=spec.get("lifts",[0]*spec["count"]),clips=spec["clips"])
    (destination/(name+".json")).write_text(json.dumps(metadata,indent=2)+"\n",encoding="utf-8")
    return frames,dict(name=name,count=len(frames),sourceSize=list(raw.size),outputSize=list(sheet.size),
                       sourceSha256=sha(source),outputSha256=sha(destination/(name+".png")),
                       uniformScale=scale,paletteColors=len(colors),frames=records)


def inner_hole(image):
    parts = components(image.getchannel("A"),opaque=False)
    holes = [part for part in parts if part["bounds"][0]>0 and part["bounds"][1]>0 and
             part["bounds"][2]<image.width and part["bounds"][3]<image.height]
    assert holes and holes[0]["area"] > image.width*image.height*.12, "Ornament has no clean inner opening."
    return holes[0]["bounds"]


def ui_sheet(destination):
    name = "5_hp_orb_ornaments_v2"
    raw = Image.open(HERE/(name+".png")).convert("RGBA")
    images,rectangles = extract(raw,[3])
    opaque = [color[:3] for image in images for color in image.get_flattened_data() if color[3]]
    sample = Image.new("RGB",(1024,math.ceil(len(opaque)/1024)))
    sample.putdata(opaque+[opaque[-1]]*(sample.width*sample.height-len(opaque)))
    reduced = sample.quantize(colors=32,dither=Image.Dither.NONE).convert("RGB")
    colors = sorted(set(reduced.get_flattened_data()))
    frames=[]; holes=[]
    orb = recolor(images[0].resize((40,40),Image.Resampling.NEAREST),colors)
    frame=Image.new("RGBA",(CW,CH));frame.alpha_composite(orb,(44,29));frames.append(frame)
    for image in images[1:]:
        left,top,right,bottom = inner_hole(image);holes.append([left,top,right,bottom])
        sx,sy=44/(right-left),44/(bottom-top)
        resized=recolor(image.resize((round(image.width*sx),round(image.height*sy)),Image.Resampling.NEAREST),colors)
        x=round(64-(left+right)/2*sx);y=round(49-(top+bottom)/2*sy)
        assert x>=1 and y>=1 and x+resized.width<CW and y+resized.height<CH, "Ornament clips at standard cell size."
        frame=Image.new("RGBA",(CW,CH));frame.alpha_composite(resized,(x,y))
        # One identical 44px aperture gives the 40px glass a two-pixel gap.
        pixels=frame.load()
        for yy in range(CH):
            for xx in range(CW):
                if (xx-63.5)**2+(yy-48.5)**2 <= 22**2:
                    pixels[xx,yy]=(0,0,0,0)
        frames.append(frame)
    overlaps=[ImageChops.multiply(frames[0].getchannel("A"),frame.getchannel("A")).getbbox() for frame in frames[1:]]
    assert overlaps==[None,None], "Glass rim overlaps ornament."
    sheet=Image.new("RGBA",(CW*3,CH))
    for index,frame in enumerate(frames):sheet.alpha_composite(frame,(index*CW,0))
    sheet.save(destination/(name+".png"))
    metadata=dict(schemaVersion=1,frameWidth=CW,frameHeight=CH,columns=3,rows=1,usedFrames=3,ppu=48,
                  pivotX=.5,pivotY=.5,components=["glass","cecilia_ornament","adela_ornament"],
                  commonCenter=[64,49],glassDiameter=40,apertureDiameter=44,overlapPixels=0)
    (destination/(name+".json")).write_text(json.dumps(metadata,indent=2)+"\n",encoding="utf-8")
    preview=Image.new("RGBA",(CW*2,CH),(40,40,48,255))
    for index in range(2):
        # Preview-only colour backing shows how the clear glass can be layered.
        draw=ImageDraw.Draw(preview);cx=64+index*CW
        draw.ellipse((cx-19,30,cx+19,68),fill=(75,156,217,255))
        preview.alpha_composite(frames[0],(index*CW,0));preview.alpha_composite(frames[index+1],(index*CW,0))
    preview.resize((CW*8,CH*4),Image.Resampling.NEAREST).save(destination/"hp_layer_preview.png")
    return frames,dict(name=name,count=3,sourceRects=rectangles,innerHoles=holes,overlapPixels=0,
                       outputSha256=sha(destination/(name+".png")))


def sprite_meta(path,single):
    metadata=Path(str(path)+".meta")
    if metadata.exists():return
    template=(ROOT/"Assets/SaintKnightSpriteCook/Drafts/4_enemy_death_cecilia_v1.png.meta").read_text(encoding="utf-8")
    template=re.sub(r"^guid: .+$","guid: "+uuid.uuid4().hex,template,count=1,flags=re.M)
    if single:
        template=template.replace("spriteMode: 0","spriteMode: 1").replace("textureType: 0","textureType: 8").replace("alphaIsTransparency: 0","alphaIsTransparency: 1")
        pivot="0.5" if "hp_orb" in str(path) else "0.26625"
        template=template.replace("  spritePixelsToUnits: 48","  spriteMeshType: 0\n  alignment: 9\n  spritePivot: {x: 0.5, y: "+pivot+"}\n  spritePixelsToUnits: 48")
    metadata.write_text(template,encoding="utf-8")


def copy_checked(source,destination):
    if destination.exists() and source.read_bytes()!=destination.read_bytes():
        raise RuntimeError("Different existing asset requires backup: "+str(destination))
    destination.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,destination)


def build(destination,sync=False):
    destination.mkdir(parents=True,exist_ok=True);all_frames={};audits=[]
    for spec in SPECS:
        frames,audit=character_sheet(spec,destination);all_frames[spec["name"]]=frames;audits.append(audit)
    frames,audit=ui_sheet(destination);all_frames[audit["name"]]=frames;audits.append(audit)
    total=sum(len(frames) for frames in all_frames.values());assert total==59 and len(audits)==7
    for name,frames in all_frames.items():
        slices=destination/"Sliced"/name;slices.mkdir(parents=True,exist_ok=True)
        for index,frame in enumerate(frames):frame.save(slices/(f"{index:02d}.png"))
        sheet=Image.open(destination/(name+".png")).convert("RGBA")
        matte(sheet).resize((sheet.width*3,sheet.height*3),Image.Resampling.NEAREST).save(destination/(name+"_contact.png"))
        if sync:
            draft=ROOT/"Assets/SaintKnightSpriteCook/Drafts"
            for suffix in (".png",".json"):copy_checked(destination/(name+suffix),draft/(name+suffix))
            sprite_meta(draft/(name+".png"),False)
            for index in range(len(frames)):
                path=draft/"Sliced"/name/(f"{index:02d}.png")
                copy_checked(slices/path.name,path);sprite_meta(path,True)
    animations=[]
    for index in range(4):
        preview=Image.new("RGBA",(CW*3,CH*2),(40,40,48,255))
        draw=ImageDraw.Draw(preview)
        for row,ch in enumerate(("cecilia","adela")):
            names=["4_enemy_death_cecilia_v3" if ch=="cecilia" else "4_enemy_death_adela_v2",f"2_boss_pattern_{ch}_v2",f"3_trap_reaction_{ch}_v2"]
            for col,(name,frame_index) in enumerate(zip(names,(index,index+4,index+3))):
                draw.line((col*CW,row*CH+93,(col+1)*CW-1,row*CH+93),fill=(78,78,88,255))
                preview.alpha_composite(all_frames[name][frame_index],(col*CW,row*CH))
        animations.append(preview.resize((CW*9,CH*6),Image.Resampling.NEAREST).convert("RGB"))
    animations[0].save(destination/"completed_motion_overview.gif",save_all=True,append_images=animations[1:],duration=220,loop=0,disposal=2)
    comparison=Image.new("RGBA",(CW*4,CH*2),(40,40,48,255))
    for row,ch in enumerate(("cecilia","adela")):
        original=Image.open(ROOT/f"Assets/SaintKnightDay06/Resources/SaintKnightDay06/{ch}_motion.png").convert("RGBA")
        comparison.alpha_composite(original.crop((0,0,CW,CH)),(0,row*CH))
        names=["4_enemy_death_cecilia_v3" if ch=="cecilia" else "4_enemy_death_adela_v2",
               f"2_boss_pattern_{ch}_v2",f"3_trap_reaction_{ch}_v2"]
        for col,name in enumerate(names,1):comparison.alpha_composite(all_frames[name][0],(col*CW,row*CH))
    comparison.resize((CW*12,CH*6),Image.Resampling.NEAREST).save(destination/"canonical_design_comparison.png")
    result=dict(sheetCount=7,characterFrames=56,uiComponents=3,totalSlicedSprites=59,
                gameIntegrated=False,sheets=audits)
    (destination/"validation.json").write_text(json.dumps(result,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({key:result[key] for key in ("sheetCount","characterFrames","uiComponents","totalSlicedSprites","gameIntegrated")},indent=2))


if __name__=="__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--out",type=Path,default=OUTPUT)
    parser.add_argument("--sync-drafts",action="store_true")
    args=parser.parse_args();build(args.out,args.sync_drafts)
