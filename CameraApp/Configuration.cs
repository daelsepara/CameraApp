using IniParser;
using IniParser.Model;
using System;
using System.IO;

public static class Configuration
{
    static IniData AppSettings;

    static void Read<T>(ref T variable, string section, string key, T defaultValue)
    {
        try
        {
            var read = AppSettings[section][key];

            if (!string.IsNullOrEmpty(read))
            {
                variable = (T)Convert.ChangeType(read, typeof(T));
            }
            else
            {
                variable = defaultValue;
            }
        }
        catch
        {
            variable = defaultValue;
        }
    }

    public static bool Load(string settings = "Settings.ini")
    {
        if (File.Exists(Directory.GetCurrentDirectory() + "/" + settings))
        {
            var parser = new FileIniDataParser();

            AppSettings = parser.ReadFile(Directory.GetCurrentDirectory() + "/" + settings);

            // Pattern Settings
            Read(ref Pattern.Width, "pattern", "Width", 640);
            Read(ref Pattern.Height, "pattern", "Height", 480);
            Read(ref Pattern.ScaleX, "pattern", "ScaleX", 1.0);
            Read(ref Pattern.ScaleY, "pattern", "ScaleY", 1.0);
            Read(ref Pattern.OffsetX, "pattern", "OffsetX", 0);
            Read(ref Pattern.OffsetY, "pattern", "OffsetY", 0);

            Read(ref Pattern.GrayLevel, "pattern", "GrayLevel", (ushort)255);
            Read(ref Pattern.BackgroundLevel, "pattern", "BackgroundLevel", (ushort)0);

            Read(ref Pattern.MarkerSize, "pattern", "MarkerSize", 1);

            Read(ref Pattern.Cross, "pattern", "Cross", false);
            Read(ref Pattern.Plus, "pattern", "Plus", false);
            Read(ref Pattern.Grating, "pattern", "Grating", false);
            Read(ref Pattern.LockXY, "pattern", "LockXY", true);
            Read(ref Pattern.Ring, "pattern", "Ring", false);
            Read(ref Pattern.Box, "pattern", "Box", false);

            Read(ref Pattern.FillX, "pattern", "FillX", 50);
            Read(ref Pattern.FillY, "pattern", "FillY", 50);
            Read(ref Pattern.PeriodX, "pattern", "PeriodX", 10);
            Read(ref Pattern.PeriodY, "pattern", "PeriodY", 10);

            Read(ref Pattern.TL, "pattern", "TL", true);
            Read(ref Pattern.TR, "pattern", "TR", false);
            Read(ref Pattern.BL, "pattern", "BL", false);
            Read(ref Pattern.BR, "pattern", "BR", true);

            Read(ref Pattern.AspectX, "pattern", "AspectX", 1);
            Read(ref Pattern.AspectY, "pattern", "AspectY", 1);

            Read(ref Pattern.Rings, "pattern", "Rings", 1);
            Read(ref Pattern.RingDiameter, "pattern", "RingDiameter", 100);
            Read(ref Pattern.RingPeriod, "pattern", "RingPeriod", 3);
            Read(ref Pattern.FillRing, "pattern", "FillRing", false);
            Read(ref Pattern.Boxes, "pattern", "Boxes", 1);
            Read(ref Pattern.BoxWidth, "pattern", "BoxWidth", 100);
            Read(ref Pattern.BoxPeriod, "pattern", "BoxPeriod", 3);
            Read(ref Pattern.FillBox, "pattern", "FillBox", false);

            Read(ref Pattern.Top, "pattern", "Top", 0);
            Read(ref Pattern.Left, "pattern", "Left", 0);

            Read(ref Pattern.Preview, "pattern", "Preview", false);
            Read(ref Pattern.Invert, "pattern", "Invert", false);
            Read(ref Pattern.FlipX, "pattern", "FlipX", false);
            Read(ref Pattern.FlipY, "pattern", "FlipY", false);

            Read(ref GtkSelection.Selection.EllipseMode, "select", "EllipseMode", true);
            Read(ref GtkSelection.Selected, "select", "selected", 0);
            Read(ref GtkSelection.Selection.EllipsesString, "select", "Ellipses", "");
            Read(ref GtkSelection.Selection.BoxesString, "select", "Boxes", "");

            Read(ref Detect.EdgeThreshold, "detect", "EdgeThreshold", 90);
            Read(ref Detect.MinArea, "detect", "MinArea", 20);
            Read(ref Detect.MaxArea, "detect", "MaxArea", 10000);
            Read(ref Detect.HoughThreshold, "detect", "HoughThreshold", 120);
            Read(ref Detect.CircleDistance, "detect", "CircleDistance", 20);
            Read(ref Detect.MinRadius, "detect", "MinRadius", 5);
            Read(ref Detect.MaxRadius, "detect", "MaxRadius", 500);
            Read(ref Detect.dp, "detect", "dp", 2.0);
            Read(ref Detect.MarkerSize, "detect", "MarkerSize", 2);

            return true;
        }

        return false;
    }

    static void Write<T>(T variable, string section, string key)
    {
        AppSettings[section][key] = variable.ToString();
    }

    public static void Save(string settings = "Settings.ini")
    {
        if (AppSettings == null)
            AppSettings = new IniData();

        var parser = new FileIniDataParser();

        // Pattern Settings
        Write(Pattern.Width, "pattern", "Width");
        Write(Pattern.Height, "pattern", "Height");
        Write(Pattern.ScaleX, "pattern", "ScaleX");
        Write(Pattern.ScaleY, "pattern", "ScaleY");
        Write(Pattern.OffsetX, "pattern", "OffsetX");
        Write(Pattern.OffsetY, "pattern", "OffsetY");

        Write(Pattern.GrayLevel, "pattern", "GrayLevel");
        Write(Pattern.BackgroundLevel, "pattern", "BackgroundLevel");

        Write(Pattern.MarkerSize, "pattern", "MarkerSize");

        Write(Pattern.Cross, "pattern", "Cross");
        Write(Pattern.Plus, "pattern", "Plus");
        Write(Pattern.Grating, "pattern", "Grating");
        Write(Pattern.LockXY, "pattern", "LockXY");
        Write(Pattern.Ring, "pattern", "Ring");
        Write(Pattern.Box, "pattern", "Box");

        Write(Pattern.FillX, "pattern", "FillX");
        Write(Pattern.FillY, "pattern", "FillY");
        Write(Pattern.PeriodX, "pattern", "PeriodX");
        Write(Pattern.PeriodY, "pattern", "PeriodY");

        Write(Pattern.TL, "pattern", "TL");
        Write(Pattern.TR, "pattern", "TR");
        Write(Pattern.BL, "pattern", "BL");
        Write(Pattern.BR, "pattern", "BR");

        Write(Pattern.AspectX, "pattern", "AspectX");
        Write(Pattern.AspectY, "pattern", "AspectY");

        Write(Pattern.Rings, "pattern", "Rings");
        Write(Pattern.RingDiameter, "pattern", "RingDiameter");
        Write(Pattern.RingPeriod, "pattern", "RingPeriod");
        Write(Pattern.FillRing, "pattern", "FillRing");
        Write(Pattern.Boxes, "pattern", "Boxes");
        Write(Pattern.BoxWidth, "pattern", "BoxWidth");
        Write(Pattern.BoxPeriod, "pattern", "BoxPeriod");
        Write(Pattern.FillBox, "pattern", "FillBox");

        Write(Pattern.Top, "pattern", "Top");
        Write(Pattern.Left, "pattern", "Left");

        Write(Pattern.Preview, "pattern", "Preview");
        Write(Pattern.Invert, "pattern", "Invert");
        Write(Pattern.FlipX, "pattern", "FlipX");
        Write(Pattern.FlipY, "pattern", "FlipY");

        Write(GtkSelection.Selection.EllipseMode, "select", "EllipseMode");
        Write(GtkSelection.Selected, "select", "selected");
        Write(GtkSelection.Selection.EllipsesString, "select", "Ellipses");
        Write(GtkSelection.Selection.BoxesString, "select", "Boxes");

        Write(Detect.EdgeThreshold, "detect", "EdgeThreshold");
        Write(Detect.MinArea, "detect", "MinArea");
        Write(Detect.MaxArea, "detect", "MaxArea");
        Write(Detect.HoughThreshold, "detect", "HoughThreshold");
        Write(Detect.CircleDistance, "detect", "CircleDistance");
        Write(Detect.MinRadius, "detect", "MinRadius");
        Write(Detect.MaxRadius, "detect", "MaxRadius");
        Write(Detect.dp, "detect", "dp");
        Write(Detect.MarkerSize, "detect", "MarkerSize");

        parser.WriteFile(Directory.GetCurrentDirectory() + "/" + settings, AppSettings);
    }
}
