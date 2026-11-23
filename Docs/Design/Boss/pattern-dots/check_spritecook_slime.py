"""Geometry/format regressions for the SpriteCook material compositor (no Unity)."""
import json
import os
import tempfile
import unittest
from pathlib import Path

from PIL import Image, ImageChops
import make_boss_spit_from_material as builder


class SpriteCookSlimeChecks(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.temp = tempfile.TemporaryDirectory(prefix="saintknight-slime-check-")
        cls.previous_out = builder.m.OUT
        builder.m.OUT = cls.temp.name
        cls.results = {}
        mats = builder.stages()
        for ch in builder.m.CELLS:
            path = Path(builder.m.ROOT) / f"Assets/SaintKnightDay06/Resources/SaintKnightDay06/{ch}_motion.png"
            with Image.open(path) as image:
                atlas = image.convert("RGBA")
            head = builder.m.bbox(builder.m.body(atlas, builder.m.SWALLOW[ch][0]))[1]
            scale = (builder.GROUND - head + 6) / mats[5].height
            details = []
            builder.m.trap_sheet(atlas, ch, "slime", builder.make_spit_frame(mats, scale, details))
            cls.results[ch] = dict(details=details, scale=scale)

    @classmethod
    def tearDownClass(cls):
        builder.m.OUT = cls.previous_out
        cls.temp.cleanup()

    def test_sheet_layout_and_frame_contract(self):
        for ch in self.results:
            with self.subTest(character=ch):
                base = Path(self.temp.name) / f"{ch}_boss_slime"
                data = json.loads(base.with_suffix(".json").read_text())
                with Image.open(base.with_suffix(".png")) as sheet:
                    self.assertEqual(sheet.size, (1024, 288))
                    self.assertIsNone(sheet.crop((896, 192, 1024, 288)).getbbox())
                    self.assertEqual(set(sheet.getchannel("A").get_flattened_data()), {0, 255})
                self.assertEqual((data["frameWidth"], data["frameHeight"], data["usedFrames"]), (128, 96, 23))
                original = Path(builder.m.HERE) / "staging-spritecook" / (base.name + ".json")
                self.assertEqual(data, json.loads(original.read_text()))
                frames = [d["output"].tobytes() for d in self.results[ch]["details"]]
                self.assertEqual(len(set(frames)), 23)

    def test_first_drop_really_contacts_the_head(self):
        for ch, result in self.results.items():
            with self.subTest(character=ch):
                d = result["details"][0]
                overlap = ImageChops.multiply(d["layer"].getchannel("A"), d["body"].getchannel("A"))
                box = overlap.getbbox()
                self.assertIsNotNone(box)
                self.assertGreaterEqual(box[1], d["head_top"])
                self.assertLessEqual(box[3], d["head_top"] + 8)

    def test_extended_stream_is_narrow_continuous_and_varied(self):
        checked = 0
        for result in self.results.values():
            for d in result["details"]:
                if d["motion"] == "Escape" or d["material"] not in (1, 2, 3) or d["stream_end"] < 4:
                    continue
                rows = []
                for y in range(d["stream_end"]):
                    row = d["layer"].crop((0, y, builder.W, y + 1))
                    alpha = row.getchannel("A")
                    self.assertGreater(sum(v > 0 for v in alpha.get_flattened_data()), 0)
                    self.assertLessEqual(sum(v > 0 for v in alpha.get_flattened_data()), 7)
                    rows.append(row.tobytes())
                if d["stream_end"] >= 8:
                    self.assertGreater(len(set(rows)), 2)
                checked += 1
        self.assertGreater(checked, 0)

    def test_middle_material_bottom_has_drip_tips_not_a_cut_line(self):
        for ch, result in self.results.items():
            for stage in (1, 2, 3):
                with self.subTest(character=ch, stage=stage):
                    material = builder.scaled(builder.stages()[stage], result["scale"])
                    material = builder.bottom_drips(builder.taper_neck(material))
                    run = longest = 0
                    for x in range(material.width):
                        run = run + 1 if material.getpixel((x, material.height - 1))[3] else 0
                        longest = max(longest, run)
                    self.assertLessEqual(longest, 3)

    def test_late_mound_has_no_exposed_character_outside_its_outline(self):
        for ch, result in self.results.items():
            for index in (4, 5, 14, 15, 16, 17, 21, 22):
                with self.subTest(character=ch, cell=index):
                    d = result["details"][index]
                    outside = ImageChops.invert(d["envelope"])
                    out_alpha = d["output"].getchannel("A")
                    layer_alpha = d["layer"].getchannel("A")
                    extra = ImageChops.subtract(out_alpha, layer_alpha)
                    self.assertIsNone(ImageChops.multiply(extra, outside).getbbox())
                    if index >= 21:
                        self.assertEqual(d["output"].tobytes(), d["layer"].tobytes())

    def test_uncovered_body_pixels_keep_the_original_colour_and_scale(self):
        checked = 0
        for result in self.results.values():
            for d in result["details"][:4] + result["details"][18:21]:
                for y in range(builder.H):
                    for x in range(builder.W):
                        original = d["body"].getpixel((x, y))
                        if original[3] and not d["layer"].getpixel((x, y))[3]:
                            self.assertEqual(d["output"].getpixel((x, y)), original)
                            checked += 1
        self.assertGreater(checked, 1000)


if __name__ == "__main__":
    unittest.main(verbosity=2)
