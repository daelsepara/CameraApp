using System;

public class Box
{
    public int X0, Y0, X1, Y1;

    public bool Enabled;

    public Box(int x0, int y0, int x1, int y1)
    {
        Initialize(x0, y0, x1, y1, true);
    }

    public Box(int x0, int y0, int x1, int y1, bool enabled)
    {
        Initialize(x0, y0, x1, y1, enabled);
    }

    void Initialize(int x0, int y0, int x1, int y1, bool enabled)
    {
        X0 = Math.Min(x0, x1);
        Y0 = Math.Min(y0, y1);
        X1 = Math.Max(x0, x1);
        Y1 = Math.Max(y0, y1);

        Enabled = enabled;
    }

    public bool InBox(int x, int y)
    {
        bool indsideX = x >= Math.Min(X0, X1) && x <= Math.Max(X0, X1);
        bool insideY = y >= Math.Min(Y0, Y1) && y <= Math.Max(Y0, Y1);

        return indsideX && insideY;
    }

    public bool BoxIntersect(Box box)
    {
        bool xoverlap = Math.Min(X0, X1) < Math.Max(box.X0, box.X1) && Math.Max(X0, X1) > Math.Min(box.X0, box.X1);
        bool yoverlap = Math.Min(Y0, Y1) < Math.Max(box.Y0, box.Y1) && Math.Max(Y0, Y1) > Math.Min(box.Y0, box.Y1);

        return xoverlap && yoverlap;
    }
}
