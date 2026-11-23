"""Compare the same game camera and frame numbers from the two Play Mode runs."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
LOGS = HERE.parents[4] / "Logs"
FONT = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 15)
SOURCES = ["BossCharacters20261119","Grab2D20261119"]


if __name__ == "__main__":
    frames=[]
    for index in range(32):
        canvas=Image.new("RGB",(520,392),"#201e28")
        draw=ImageDraw.Draw(canvas)
        for row,ch in enumerate(["cecilia","adela"]):
            for col,run in enumerate(SOURCES):
                x,y=6+col*258,6+row*194
                name=["세실리아","아델라"][row]
                draw.text((x,y),name+" · "+["이전","이번"][col],font=FONT,fill="#f2e1c7")
                with Image.open(LOGS/run/"Screens"/f"{ch}_boss_grab_motion_{index:02}.png") as pic:
                    canvas.paste(pic.convert("RGB").crop((72,44,326,208)),(x,y+23))
        frames.append(canvas)
    colours=Image.new("RGB",(130,98*32))
    for i,frame in enumerate(frames): colours.paste(frame.resize((130,98)),(0,98*i))
    palette=colours.quantize(colors=256)
    frames=[f.quantize(palette=palette,dither=Image.Dither.NONE) for f in frames]
    frames[0].save(HERE/"grab-comparison.gif",save_all=True,append_images=frames[1:],
                   loop=0,duration=[120,130]*16,disposal=1,optimize=False)
