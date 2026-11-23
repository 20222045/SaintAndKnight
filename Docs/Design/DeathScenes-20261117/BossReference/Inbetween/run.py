"""Ask Codex CLI (built-in image_gen) for real in-between frames of the seven boss defeat poses.

Each call: idle pose (sheet cell 0) -> defeat pose (sheet cell i), 6 frames in a 3x2 grid,
same art style as CharacterPoses/<name>.png. Output: out/<name>_<i>.png (raw model images).
Usage: python run.py [cecilia|adela] [pose index 1-7 ...]   (default: both, all seven)
Requires `npm i -g @openai/codex` logged in; uses the user's ChatGPT/Codex usage.
"""
import shutil, subprocess, sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
SHEETS = HERE.parent / "CharacterPoses"
OUT = HERE / "out"

LOOK = {
    "cecilia": "adult woman, long wavy silver hair, white-gold veil with flower ornament, off-shoulder layered "
               "lavender-white robe with gold trim, brown lace-up boots, holding a silver censer on a chain",
    "adela": "adult woman knight, long crimson ponytail, dark plate armour with layered pauldrons and gold trim, "
             "torn crimson cape and scarf, high-heeled armoured boots, longsword and cross shield",
}
# Same order as CharacterPoses/README.md (sheet left-to-right, top-to-bottom; cell 0 = idle).
BEATS = {
    1: "floor tentacles: she staggers, drops low and ends with one hand on the floor, weight sinking",
    2: "tentacle grab: pulled forward, knees buckle, ends kneeling with one hand on forehead, other hand on front knee",
    3: "slime spit: struck in the face, raises a forearm to cover her eyes, braces on the back foot",
    4: "root cocoon: folds sideways to the ground and wraps both arms around her face",
    5: "crimson calyx: lifted off the ground, arms opening wide, hips raised, legs hanging",
    6: "rose and chains: sinks into a seated pose, one hand behind on the seat, other hand on her knee",
    7: "double ring: kneels, pulls knees and elbows close to the body, chin lifted upward",
}

PROMPT = """Use the image_gen tool. Reference: the attached pose sheet ({look}).
Generate ONE image, 1536x1024, a 3 columns x 2 rows grid of 6 animation frames read left-to-right, top-to-bottom:
frame 1 = her standing idle pose exactly as the top-left figure of the sheet; frame 6 = exactly the
{ordinal} figure of the sheet; frames 2-5 = smooth in-betweens. Motion: {beat}.
Locks: same character, same face, outfit, colours, proportions and the same detailed pixel-art style as the sheet;
same figure scale in every frame, feet on the same ground line, side view facing the same direction as the sheet;
each prop stays in the same hand. Clothing stays fully on. Flat solid #3c3c4b background, wide empty gutters,
no text, no labels. Then copy the generated PNG into the current directory as {name}_{i}.png."""
ORDINALS = ["", "second in the top row", "third in the top row", "fourth in the top row",
            "first in the bottom row", "second in the bottom row", "third in the bottom row", "fourth in the bottom row"]


def run(name, i):
    OUT.mkdir(exist_ok=True)
    target = OUT / f"{name}_{i}.png"
    if target.exists():
        return print("skip", target.name)
    shutil.copy(SHEETS / f"{name}.png", OUT / f"{name}_sheet.png")
    prompt = PROMPT.format(look=LOOK[name], ordinal=ORDINALS[i], beat=BEATS[i], name=name, i=i)
    r = subprocess.run(["codex", "exec", "--skip-git-repo-check", "-s", "workspace-write",
                        "-i", f"{name}_sheet.png", "-"], input=prompt, text=True, encoding="utf-8",
                       cwd=OUT, capture_output=True, shell=True)
    tail = (r.stdout + r.stderr)[-400:]
    print(name, i, "ok" if target.exists() else "FAILED", "" if target.exists() else tail)
    return target.exists()


if __name__ == "__main__":
    names = [a for a in sys.argv[1:] if a in LOOK] or list(LOOK)
    poses = [int(a) for a in sys.argv[1:] if a.isdigit()] or list(BEATS)
    for n in names:
        for i in poses:
            if not run(n, i):
                sys.exit("stopped (usage limit or error); rerun later, finished files are skipped")
