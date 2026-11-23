using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

// Technical export only: copies and nearest-neighbor samples pixels already authored in the inputs.
public static class SaintKnightTrapWrapPackerV2
{
    private const int CellWidth = 128, CellHeight = 96, Columns = 12, GroundBottomExclusive = 94;

    public sealed class PixelBounds
    {
        public int X, Y, Width, Height;
        public PixelBounds(int x, int y, int width, int height) { X = x; Y = y; Width = width; Height = height; }
    }

    public sealed class FrameReport
    {
        public int Cell, SourceRow, SourceColumn;
        public double Scale;
        public string Motion, Source;
        public PixelBounds SourceBounds, DestinationBounds;
    }

    public sealed class CharacterReport
    {
        public string Character;
        public int AtlasWidth, AtlasHeight, MaximumSourceHeight, MaximumSourceWidth, TargetMaximumHeight;
        public double SlimeScale, TentacleScale;
        public int SlimeMaximumSourceHeight, SlimeMaximumSourceWidth, TentacleMaximumSourceHeight, TentacleMaximumSourceWidth;
        public int PreservedPixelsVerified, ChangedCells, SemiTransparentReplacementPixels;
        public FrameReport[] Frames;
    }

    private sealed class Pixels
    {
        public readonly int Width, Height;
        public readonly int[] Data;
        public Pixels(int width, int height) { Width = width; Height = height; Data = new int[width * height]; }
        public Pixels Clone() { var copy = new Pixels(Width, Height); Array.Copy(Data, copy.Data, Data.Length); return copy; }

        public static Pixels Read(string path)
        {
            using (var image = new Bitmap(path))
            {
                var pixels = new Pixels(image.Width, image.Height);
                var locked = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    for (int y = 0; y < image.Height; y++)
                        Marshal.Copy(IntPtr.Add(locked.Scan0, y * locked.Stride), pixels.Data, y * image.Width, image.Width);
                }
                finally { image.UnlockBits(locked); }
                return pixels;
            }
        }

        public void Save(string path)
        {
            using (var image = new Bitmap(Width, Height, PixelFormat.Format32bppArgb))
            {
                var locked = image.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                try
                {
                    for (int y = 0; y < Height; y++)
                        Marshal.Copy(Data, y * Width, IntPtr.Add(locked.Scan0, y * locked.Stride), Width);
                }
                finally { image.UnlockBits(locked); }
                image.Save(path, ImageFormat.Png);
            }
        }
    }

    private sealed class SourceFrame
    {
        public Pixels Sheet;
        public PixelBounds Bounds;
        public int Row, Column;
        public string Source;
    }

    private static SourceFrame GetFrame(Pixels sheet, int row, int column, string source)
    {
        int left = column * sheet.Width / 3, right = (column + 1) * sheet.Width / 3;
        int top = row * sheet.Height / 2, bottom = (row + 1) * sheet.Height / 2;
        int x0 = right, x1 = left - 1, y0 = bottom, y1 = top - 1;
        for (int y = top; y < bottom; y++)
            for (int x = left; x < right; x++)
                if (((uint)sheet.Data[y * sheet.Width + x] >> 24) >= 128)
                {
                    x0 = Math.Min(x0, x); x1 = Math.Max(x1, x);
                    y0 = Math.Min(y0, y); y1 = Math.Max(y1, y);
                }
        if (x1 < x0) throw new InvalidDataException("No alpha >= 128 pixels in " + source + " row " + row + " column " + column);
        return new SourceFrame { Sheet = sheet, Row = row, Column = column, Source = source,
            Bounds = new PixelBounds(x0, y0, x1 - x0 + 1, y1 - y0 + 1) };
    }

    private static Pixels Resample(SourceFrame frame, double scale)
    {
        int width = Math.Max(1, (int)Math.Round(frame.Bounds.Width * scale, MidpointRounding.AwayFromZero));
        int height = Math.Max(1, (int)Math.Round(frame.Bounds.Height * scale, MidpointRounding.AwayFromZero));
        if (width > CellWidth || height > GroundBottomExclusive) throw new InvalidDataException("Frame does not fit 128 x 96.");
        int left = CellWidth / 2 - width / 2, top = GroundBottomExclusive - height;
        var output = new Pixels(CellWidth, CellHeight);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int sourceX = frame.Bounds.X + Math.Min(frame.Bounds.Width - 1, (int)((x + .5) * frame.Bounds.Width / width));
                int sourceY = frame.Bounds.Y + Math.Min(frame.Bounds.Height - 1, (int)((y + .5) * frame.Bounds.Height / height));
                int color = frame.Sheet.Data[sourceY * frame.Sheet.Width + sourceX];
                if (((uint)color >> 24) >= 128)
                    output.Data[(top + y) * CellWidth + left + x] = color | unchecked((int)0xff000000);
            }
        return output;
    }

    private static PixelBounds AlphaBounds(Pixels pixels)
    {
        int left = pixels.Width, right = -1, top = pixels.Height, bottom = -1;
        for (int y = 0; y < pixels.Height; y++)
            for (int x = 0; x < pixels.Width; x++)
                if (((uint)pixels.Data[y * pixels.Width + x] >> 24) != 0)
                {
                    left = Math.Min(left, x); right = Math.Max(right, x);
                    top = Math.Min(top, y); bottom = Math.Max(bottom, y);
                }
        if (right < 0) throw new InvalidDataException("Downsampling erased an entire frame.");
        return new PixelBounds(left, top, right - left + 1, bottom - top + 1);
    }

    private static CharacterReport PackCharacter(string name, int row, int atlasRow, int maximumHeight,
        Pixels firstSheet, Pixels tentacleSheet, string atlasPath, string outputPath, Pixels preview)
    {
        var first = new SourceFrame[3];
        for (int c = 0; c < 3; c++) first[c] = GetFrame(firstSheet, row, c, "full_body_wrap_concept.png");
        var tentacles = new SourceFrame[3];
        for (int c = 0; c < 3; c++)
            tentacles[c] = tentacleSheet != null ? GetFrame(tentacleSheet, row, c, "tentacle_struggle_sheet.png") : first[2];
        SourceFrame[] sequence = { tentacles[0], tentacles[1], tentacles[2], first[0], first[1], first[0], first[1], first[0], first[1] };
        int maxHeight = 0, maxWidth = 0;
        foreach (var frame in sequence) { maxHeight = Math.Max(maxHeight, frame.Bounds.Height); maxWidth = Math.Max(maxWidth, frame.Bounds.Width); }
        int slimeHeight = Math.Max(first[0].Bounds.Height, first[1].Bounds.Height);
        int slimeWidth = Math.Max(first[0].Bounds.Width, first[1].Bounds.Width);
        int tentacleHeight = 0, tentacleWidth = 0;
        foreach (var frame in tentacles)
        {
            tentacleHeight = Math.Max(tentacleHeight, frame.Bounds.Height);
            tentacleWidth = Math.Max(tentacleWidth, frame.Bounds.Width);
        }
        // Generated sheets can have different pixel densities. Preserve pose proportions within each sheet.
        double slimeScale = Math.Min((double)maximumHeight / slimeHeight, 124.0 / slimeWidth);
        double tentacleScale = Math.Min((double)maximumHeight / tentacleHeight, 124.0 / tentacleWidth);
        var original = Pixels.Read(atlasPath);
        if (original.Width != Columns * CellWidth || original.Height != (atlasRow + 1) * CellHeight)
            throw new InvalidDataException("Unexpected input atlas dimensions for " + name + ": " + original.Width + " x " + original.Height);
        var result = original.Clone();
        var reports = new List<FrameReport>();
        string[] motions = { "SpikeHurt", "SpikeHurt", "SpikeHurt", "SlimeWalk", "SlimeWalk", "SlimeWalk", "SlimeWalk", "SlimeIdle", "SlimeIdle" };
        for (int i = 0; i < sequence.Length; i++)
        {
            double scale = i < 3 ? tentacleScale : slimeScale;
            var frame = Resample(sequence[i], scale);
            for (int y = 0; y < CellHeight; y++)
                Array.Copy(frame.Data, y * CellWidth, result.Data, (atlasRow * CellHeight + y) * original.Width + i * CellWidth, CellWidth);
            for (int y = 0; y < CellHeight; y++)
                for (int x = 0; x < CellWidth; x++)
                {
                    int color = frame.Data[y * CellWidth + x];
                    if (((uint)color >> 24) == 0) continue;
                    for (int dy = 0; dy < 3; dy++)
                        for (int dx = 0; dx < 3; dx++)
                            preview.Data[(row * CellHeight * 3 + y * 3 + dy) * preview.Width + (i * CellWidth + x) * 3 + dx] = color;
                }
            reports.Add(new FrameReport { Cell = atlasRow * Columns + i, Motion = motions[i], Scale = scale, Source = sequence[i].Source,
                SourceRow = sequence[i].Row, SourceColumn = sequence[i].Column, SourceBounds = sequence[i].Bounds, DestinationBounds = AlphaBounds(frame) });
        }
        result.Save(outputPath);
        var reloaded = Pixels.Read(outputPath);
        int verified = 0, semiTransparent = 0;
        for (int y = 0; y < original.Height; y++)
            for (int x = 0; x < original.Width; x++)
            {
                int index = y * original.Width + x;
                bool replacement = y >= atlasRow * CellHeight && x < 9 * CellWidth;
                if (!replacement)
                {
                    if (original.Data[index] != reloaded.Data[index]) throw new InvalidDataException("Preserved pixel changed in " + name + " at " + x + "," + y);
                    verified++;
                }
                else
                {
                    uint alpha = (uint)reloaded.Data[index] >> 24;
                    if (alpha > 0 && alpha < 255) semiTransparent++;
                }
            }
        if (semiTransparent != 0) throw new InvalidDataException("Replacement frame contains semi-transparent pixels.");
        return new CharacterReport { Character = name, AtlasWidth = original.Width, AtlasHeight = original.Height,
            MaximumSourceHeight = maxHeight, MaximumSourceWidth = maxWidth, TargetMaximumHeight = maximumHeight,
            SlimeScale = slimeScale, TentacleScale = tentacleScale,
            SlimeMaximumSourceHeight = slimeHeight, SlimeMaximumSourceWidth = slimeWidth,
            TentacleMaximumSourceHeight = tentacleHeight, TentacleMaximumSourceWidth = tentacleWidth,
            PreservedPixelsVerified = verified, ChangedCells = 9, SemiTransparentReplacementPixels = semiTransparent,
            Frames = reports.ToArray() };
    }

    public static CharacterReport[] Pack(string sourcePath, string tentaclePath, string ceciliaAtlas, string adelaAtlas, string outputDirectory)
    {
        var firstSheet = Pixels.Read(sourcePath);
        var tentacleSheet = String.IsNullOrEmpty(tentaclePath) ? null : Pixels.Read(tentaclePath);
        var preview = new Pixels(9 * CellWidth * 3, 2 * CellHeight * 3);
        for (int i = 0; i < preview.Data.Length; i++) preview.Data[i] = unchecked((int)0xff282830);
        var result = new[] {
            PackCharacter("cecilia", 0, 6, 55, firstSheet, tentacleSheet, ceciliaAtlas, Path.Combine(outputDirectory, "cecilia_motion.png"), preview),
            PackCharacter("adela", 1, 7, 62, firstSheet, tentacleSheet, adelaAtlas, Path.Combine(outputDirectory, "adela_motion.png"), preview)
        };
        preview.Save(Path.Combine(outputDirectory, "trap_reactions_preview.png"));
        return result;
    }
}
