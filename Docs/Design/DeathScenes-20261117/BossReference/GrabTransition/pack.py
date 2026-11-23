"""Pack the four 2dimg2motion entrance drawings with PixelOE and Codixel."""
import argparse
import json
from pathlib import Path
import subprocess
import tempfile

import numpy as np
from PIL import Image
import torch
from pixeloe.torch.pixelize import pixelize

HERE = Path(__file__).resolve().parent
EDITOR = HERE.parents[4] / "Assets/SaintKnightSpriteCook/Editor"
# Sheet-space upper arm on the rising side, bracing wrist, planted ankle, face.
MARKS = {
    "cecilia": [
        [(365,242),(318,291),(383,567),(352,81)],
        [(979,239),(951,324),(1003,564),(989,126)],
        [(365,880),(309,944),(383,1148),(367,764)],
        [(1046,859),(945,951),(1006,1149),(989,810)]],
    "adela": [
        [(373,252),(302,318),(438,562),(351,92)],
        [(980,279),(980,353),(1050,560),(991,183)],
        [(387,944),(369,990),(442,1167),(381,852)],
        [(1069,925),(925,1074),(1075,1165),(1005,887)]],
}


def pack(name, temp, codixel):
    source = Image.open(HERE / (name+".png")).convert("RGB")
    step = 10 if name=="cecilia" else 9
    records, frames = [], []
    for i, points in enumerate(MARKS[name]):
        x, y = (i%2)*627, (i//2)*627
        rgb = np.array(source.crop((x,y,x+627,y+627)),dtype=np.int16)
        excess = rgb[:,:,1]-np.maximum(rgb[:,:,0],rgb[:,:,2])
        alpha = Image.fromarray(np.uint8(excess < 45)*255)
        # Remove spill before downsampling. Neutral edge colour cannot introduce a green halo.
        rgb[:,:,1] -= np.maximum(excess,0)
        tile = Image.fromarray(rgb.astype(np.uint8))
        tile.putalpha(alpha)
        left,top,right,bottom = alpha.getbbox()
        tile = tile.crop((left,top,right,bottom))
        size=(round(tile.width/step),round(tile.height/step))
        work = tile.resize((size[0]*2,size[1]*2),Image.Resampling.LANCZOS)
        matte=Image.new("RGBA",work.size,(69,62,73,255))
        matte.alpha_composite(work)
        tensor=torch.from_numpy(np.array(matte.convert("RGB"))).permute(2,0,1)[None].float()/255
        result=pixelize(tensor,pixel_size=2,thickness=1,mode="contrast",do_quant=False,
                        no_post_upscale=True,backend="torch")
        pixels=Image.fromarray((result[0].permute(1,2,0).clamp(0,1).numpy()*255).astype(np.uint8))
        mask=tile.getchannel("A").resize(size,Image.Resampling.BOX).point(lambda a: 255 if a>=128 else 0)
        pixels.putalpha(mask)
        def local(p):
            return (round((p[0]-x-left)*size[0]/tile.width),round((p[1]-y-top)*size[1]/tile.height))
        ankle=local(points[2])
        # Anchor the forward boot to the game's existing kneeling key and flat floor.
        dx,dy=77-ankle[0],90-size[1]
        frame=Image.new("RGBA",(128,96))
        frame.paste(pixels,(dx,dy),mask)
        path=temp/(name+str(i)+".png")
        frame.save(path)
        frames.append(str(path))
        coords=[dict(x=local(p)[0]+dx,y=local(p)[1]+dy) for p in points]
        records.append(dict(zip(("left","right","boot","face"),coords),height=size[1]))
    subprocess.run(["node",str(HERE/"atlas.cjs"),str(codixel),
                    str(EDITOR/(name+"_grab_entrance.png")),*frames],check=True)
    return records


if __name__=="__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument("codixel",type=Path,help="Codixel core/pixel.js path")
    args=parser.parse_args()
    torch.set_num_threads(2)
    with tempfile.TemporaryDirectory() as tmp:
        marks={name:pack(name,Path(tmp),args.codixel.resolve()) for name in MARKS}
    (EDITOR/"GrabEntranceMarks.json").write_text(json.dumps(marks,indent=2)+"\n",encoding="utf-8")
