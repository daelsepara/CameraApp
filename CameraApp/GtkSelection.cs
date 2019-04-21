using Gdk;
using System;

public static class GtkSelection
{
    public static Select Selection = new Select();

    public static Color MarkerColor = new Color(255, 0, 0);

    public static Color SelectedColor = new Color(0, 255, 255);

    public static int MarkerSize = 2;

    public static int Selected;

    public static void Draw(Gdk.GC gc, Window dest, int X0, int Y0, int X1, int Y1)
    {
        if (gc == null || dest == null)
            return;

        var w = Math.Abs(X1 - X0);
        var h = Math.Abs(Y1 - Y0);

        if (w > 2 && h > 2)
        {
            gc.RgbFgColor = SelectedColor;

            gc.SetLineAttributes(MarkerSize, LineStyle.Solid, CapStyle.Round, JoinStyle.Round);

            if (Selection.EllipseMode)
                DrawEllipse(gc, dest, X0, Y0, X1, Y1, w, h);
            else
                DrawBox(gc, dest, X0, Y0, X1, Y1, w, h);
        }
    }

    static void DrawBox(Gdk.GC gc, Window dest, int X0, int Y0, int X1, int Y1, int w, int h, bool filled = false)
    {
        dest.DrawRectangle(gc, filled, Math.Min(X0, X1), Math.Min(Y0, Y1), w, h);
    }

    static void DrawEllipse(Gdk.GC gc, Window dest, int X0, int Y0, int X1, int Y1, int a, int b, bool filled = false)
    {
        dest.DrawArc(gc, filled, Math.Min(X0, X1), Math.Min(Y0, Y1), a, b, 0, 360 * 64);
    }

    public static Pixbuf Render(Pixbuf refImage, OpenCV cv, Color color, int selected, Color selectedColor, bool filled = false, bool enabled = true, double ScaleX = 1.0, double ScaleY = 1.0)
    {
        if (Selection.EllipseMode)
        {
            if (Selection.Ellipses.Count > 0)
                return cv.DrawEllipse(refImage, Selection.Ellipses, MarkerSize, color, selected, selectedColor, filled, enabled, ScaleX, ScaleY);
        }
        else
        {
            if (Selection.Boxes.Count > 0)
                return cv.DrawBox(refImage, Selection.Boxes, MarkerSize, color, selected, selectedColor, filled, enabled, ScaleX, ScaleY);
        }

        return refImage.Copy();
    }
}
