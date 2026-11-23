const P = require(process.argv[2] + "/core/pixel.js");
const s = P.load("idle0.png");
const rig = new P.Rig(s)
  .bone("body", null, 24, 44, 24, 14)
  .bone("head", "body", 24, 14, 24, 1)
  .bone("arm", "body", 15, 22, 6, 50)
  .bind({ joint: 2, paint: { arm: [[0, 34], [14, 34], [14, 59], [0, 59]], head: [[16, 0], [33, 0], [33, 13], [16, 13]] } });
const own = P.ruler(rig.ownerMap(), 6, { step: 4 });
rig.drawBones(own, 0, 0, {}, { scale: 6 });
const frames = [];
for (let i = 0; i < 8; i++) {
  const t = i / 8 * Math.PI * 2;
  const pose = { body: { a: 0.06 * Math.sin(t), dy: Math.round(Math.sin(t)) }, head: -0.12 * Math.sin(t + 0.6), arm: 0.25 * Math.sin(t + 1.2) };
  const b = new P.Buf(64, 64); rig.draw(b, 10, 2, pose); frames.push(b);
}
P.save(P.sheet([{ buf: own, label: "OWNERS" }, ...frames.map((f, i) => ({ buf: f, label: "F" + i, scale: 4 }))], { cols: 5 }), "rig_sheet.png");
require("fs").writeFileSync("sway.gif", P.gif(frames, { delay: 110, scale: 4 }));
