const [library, output, ...inputs] = process.argv.slice(2);
const P = require(library);
const atlas = new P.Buf(128 * inputs.length, 96);
inputs.forEach((path, i) => {
    const cell = P.load(path);
    if (cell.w !== 128 || cell.h !== 96) throw new Error("Unexpected frame dimensions");
    atlas.sprite(cell, 128 * i, 0);
});
P.save(atlas, output);
