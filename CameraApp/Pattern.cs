using Gdk;

public static class Pattern
{
    // Pattern Window Settings
    public static int Width = 640;
    public static int Height = 480;

    public static int Top;
    public static int Left;

    public static double ScaleX = 1.0;
    public static double ScaleY = 1.0;

    public static int OffsetX;
    public static int OffsetY;

    public static Pixbuf Pixbuf;
    public static Pixbuf CustomPixbuf;

    public static ushort GrayLevel = 255;
    public static ushort BackgroundLevel;

    // Preview mode (only affects selection)
    public static bool Preview;

    public static int MarkerSize = 1;

    // Pattern types
    public static bool Cross;
    public static bool Plus;
    public static bool Grating;
    public static bool Ring;
    public static bool FillRing;
    public static bool Box;
    public static bool FillBox;
    public static bool Custom;

    // Grating settings
    public static int PeriodX = 10;
    public static int PeriodY = 10;
    public static int FillX = 50;
    public static int FillY = 50;
    public static bool TL = true;
    public static bool TR;
    public static bool BL;
    public static bool BR = true;
    public static bool LockXY = true;

    public static bool Invert;
    public static bool FlipX;
    public static bool FlipY;

    // Aspect Ratio
    public static int AspectX = 1;
    public static int AspectY = 1;

    // Pattern Dimensions (Ring, Box)
    public static int RingDiameter = 200;
    public static int Rings = 1;
    public static int RingPeriod = 3;
    public static int Boxes = 1;
    public static int BoxWidth = 200;
    public static int BoxPeriod = 3;

    public static void Resize(int width, int height)
    {
        if (Pixbuf != null)
            Pixbuf.Dispose();

        Width = width;
        Height = height;

        Pixbuf = new Pixbuf(Colorspace.Rgb, false, 8, width, height);

        // Copy custom pattern when resizing if possible
        if (CustomPixbuf != null)
        {
            using (Pixbuf pb = new Pixbuf(Colorspace.Rgb, false, 8, Width, Height))
            {
                pb.Fill(0);
                CustomPixbuf.CopyArea(0, 0, pb.Width > CustomPixbuf.Width ? CustomPixbuf.Width : pb.Width, pb.Height > CustomPixbuf.Height ? CustomPixbuf.Height : pb.Height, pb, 0, 0);
                CustomPixbuf.Dispose();
                CustomPixbuf = pb.Copy();
            }
        }
        else
        {
            // Otherwise, create a new Custom Pixbuf
            ClearCustomPixbuf();
        }
    }

    public static void ClearCustomPixbuf()
    {
        CustomPixbuf = new Pixbuf(Colorspace.Rgb, false, 8, Width, Height);
        CustomPixbuf.Fill(0);
    }

    public static void Update()
    {
        var BG = (Invert ? (255 - BackgroundLevel) : BackgroundLevel);
        var FG = (Invert ? (255 - GrayLevel) : GrayLevel);

        Pixbuf.Fill(((uint)BG << 24) | ((uint)BG << 16) | ((uint)BG << 8) | 255);

        using (OpenCV opencv = new OpenCV())
        {
            var patternColor = new Color((byte)FG, (byte)FG, (byte)FG);

            if (Custom)
            {
                Copy(CustomPixbuf);
            }

            if (Cross || Plus)
            {
                using (Pixbuf pattern = opencv.DrawCrossPlus(Pixbuf, Cross, Plus, MarkerSize, patternColor))
                {
                    Render(pattern);
                }
            }

            if (Grating)
            {
                using (Pixbuf pattern = opencv.DrawGrating(Pixbuf, PeriodX, PeriodY, FillX, FillY, TL, TR, BL, BR, patternColor))
                {
                    Render(pattern);
                }
            }

            double AspectRatio = System.Convert.ToDouble(AspectX / AspectY);

            if (Ring)
            {
                using (Pixbuf pattern = opencv.DrawRing(Pixbuf, RingDiameter, AspectRatio, MarkerSize, patternColor, FillRing, Rings, RingPeriod * MarkerSize))
                {
                    Render(pattern);
                }
            }

            if (Box)
            {
                using (Pixbuf pattern = opencv.DrawBox(Pixbuf, BoxWidth, AspectRatio, MarkerSize, patternColor, FillBox, Boxes, BoxPeriod * MarkerSize))
                {
                    Render(pattern);
                }
            }

            if (!Preview)
            {
                using (Pixbuf pattern = GtkSelection.Render(Pixbuf, opencv, patternColor, 0, patternColor, true, false))
                {
                    Render(pattern);
                }
            }

            if (FlipX)
            {
                using (Pixbuf pattern = opencv.Flip(Pixbuf, opencv.FlipX))
                {
                    Copy(pattern);
                }
            }

            if (FlipY)
            {
                using (Pixbuf pattern = opencv.Flip(Pixbuf, opencv.FlipY))
                {
                    Copy(pattern);
                }
            }
        }
    }

    static void Render(Pixbuf pattern)
    {
        if (pattern != null && Pixbuf != null)
            pattern.Scale(Pixbuf, 0, 0, Pixbuf.Width, Pixbuf.Height, OffsetX, OffsetY, ScaleX, ScaleY, InterpType.Bilinear);
    }

    static void Copy(Pixbuf pattern)
    {
        if (pattern != null && Pixbuf != null)
            pattern.CopyArea(0, 0, Pixbuf.Width, Pixbuf.Height, Pixbuf, 0, 0);
    }
}
