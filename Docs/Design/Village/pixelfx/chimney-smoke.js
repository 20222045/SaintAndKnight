import { hex, mix } from "../../core/index.js";
import { noiseFor, looped, soft, drift } from "./_lib.js";

export const meta = {
  name: "Chimney smoke",
  desc: "A thin plume from a chimney top: pale puffs rise, swell and lean with a light breeze, thinning to nothing. Stays light so a host can tint it by time of day.",
  tags: ["fire", "smoke", "chimney", "ambient", "town", "loop"],
  size: [48, 72],
  origin: [16, 68],
  duration: 3,
  loop: true,
  fps: 12,
  bg: "dark",
  options: { color: "#d8d0d6", shade: "#9d93a0", seed: 1, wind: 1 },
};

const D = meta.duration, NL = noiseFor(D);

export function draw(p, t, o) {
  const lit = hex(o.color), dark = hex(o.shade), S = o.seed * 97, w = o.wind;
  looped(t, D, 26, S, (r) => ({ x: r.range(-0.8, 0.8), y: -r.range(0, 1.5), vx: r.range(0.5, 2) * w, vy: -r.range(17, 23), life: r.range(2.6, 3.6), s: r.range(1.6, 2.4) }), (q, age) => {
    const [x0, y] = drift(q.x, q.y, q.vx, q.vy, age, 4 * w, -2, 0.6, 0.35);
    const k = age / q.life, x = x0 + NL(t - age, 0.7, S + 5) * (1 + 2 * age);
    const r = q.s * (0.7 + k * 1.6), a = Math.min(1, k * 10) * (1 - k) * 0.75;
    soft(p, x + r * 0.15, y + r * 0.2, r, mix(lit, dark, 0.5 + k * 0.5), a * 0.6);   // shaded underside
    soft(p, x, y, r * 0.8, mix(lit, dark, k * 0.6), a);                               // body, lit from the top
  });
}
