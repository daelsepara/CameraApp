using System;
using System.Collections.Generic;
using System.Text;

public class Select
{
    public List<Ellipse> Ellipses = new List<Ellipse>();
    public List<Box> Boxes = new List<Box>();

    public bool EllipseMode = true;

    public string EllipsesString = "";

    public string BoxesString = "";

    public void Clear()
    {
        if (EllipseMode)
            Ellipses.Clear();
        else
            Boxes.Clear();

        Update();
    }

    public int Find(int x, int y)
    {
        int region = 0;

        if (EllipseMode)
        {
            for (var i = 0; i < Ellipses.Count; i++)
            {
                if (InEllipse(x, y, Ellipses[i]))
                {
                    region = i + 1;

                    break;
                }
            }
        }
        else
        {
            for (var i = 0; i < Boxes.Count; i++)
            {
                if (InBox(x, y, Boxes[i]))
                {
                    region = i + 1;

                    break;
                }
            }
        }

        return region;
    }

    public void Switch(int Region, bool enabled)
    {
        if (EllipseMode)
        {
            if (Region > 0 && Region <= Ellipses.Count)
            {
                Ellipses[Region - 1].Enabled = enabled;
            }
        }
        else
        {
            if (Region > 0 && Region <= Boxes.Count)
            {
                Boxes[Region - 1].Enabled = enabled;
            }
        }

        Update();
    }

    public bool Status(int Region)
    {
        if (EllipseMode)
        {
            if (Region > 0 && Region <= Ellipses.Count)
            {
                return Ellipses[Region - 1].Enabled;
            }
        }
        else
        {
            if (Region > 0 && Region <= Boxes.Count)
            {
                return Boxes[Region - 1].Enabled;
            }
        }

        return false;
    }

    public void Size(int Region, out int width, out int height)
    {
        width = 0;
        height = 0;

        if (EllipseMode)
        {
            if (Region > 0 && Region <= Ellipses.Count)
            {
                width = Ellipses[Region - 1].Width;
                height = Ellipses[Region - 1].Height;
            }
        }
        else
        {
            if (Region > 0 && Region <= Boxes.Count)
            {
                width = Math.Abs(Boxes[Region - 1].X1 - Boxes[Region - 1].X0);
                height = Math.Abs(Boxes[Region - 1].Y1 - Boxes[Region - 1].Y0);
            }
        }
    }

    public void Location(int Region, out int x, out int y)
    {
        x = 0;
        y = 0;

        if (EllipseMode)
        {
            if (Region > 0 && Region <= Ellipses.Count)
            {
                x = Ellipses[Region - 1].X;
                y = Ellipses[Region - 1].Y;
            }
        }
        else
        {
            if (Region > 0 && Region <= Boxes.Count)
            {
                x = Boxes[Region - 1].X0;
                y = Boxes[Region - 1].Y0;
            }
        }
    }

    public void ReSize(int Region, int width, int height)
    {
        if (EllipseMode)
        {
            if (Region > 0 && Region <= Ellipses.Count)
            {
                Ellipses[Region - 1].Width = width;
                Ellipses[Region - 1].Height = height;
            }
        }
        else
        {
            if (Region > 0 && Region <= Boxes.Count)
            {
                Boxes[Region - 1].X1 = Boxes[Region - 1].X0 + width;
                Boxes[Region - 1].Y1 = Boxes[Region - 1].Y0 + height;
            }
        }

        Update();
    }

    public void Move(int dx, int dy, int Region)
    {
        if (EllipseMode)
        {
            if (Region > 0 && Region <= Ellipses.Count)
            {
                Ellipses[Region - 1].X += dx;
                Ellipses[Region - 1].Y += dy;
            }
        }
        else
        {
            if (Region > 0 && Region <= Boxes.Count)
            {
                Boxes[Region - 1].X0 += dx;
                Boxes[Region - 1].X1 += dx;
                Boxes[Region - 1].Y0 += dy;
                Boxes[Region - 1].Y1 += dy;
            }
        }

        Update();
    }

    bool ValidEllipse(Ellipse el)
    {
        var collisions = 0;

        if (Ellipses.Count > 0)
        {
            foreach (var ellipse in Ellipses.ToArray())
            {
                if (ellipse.EllipseIntersect(el))
                    collisions++;
            }
        }

        return !(collisions > 0);
    }

    bool InEllipse(int x, int y, Ellipse ellipse)
    {
        return ellipse.InEllipse(x, y);
    }

    List<Ellipse> UpdateEllipses(int x, int y)
    {
        var list = new List<Ellipse>();

        if (Ellipses.Count > 0)
        {
            foreach (var ellipse in Ellipses)
            {
                if (!InEllipse(x, y, ellipse))
                    list.Add(ellipse);
            }
        }

        return list;
    }

    void AddEllipse(int x0, int y0, int x1, int y1)
    {
        var a = Math.Abs(x0 - x1);
        var b = Math.Abs(y0 - y1);
        var x = Math.Min(x0, x1) + (a - 1) / 2;
        var y = Math.Min(y0, y1) + (b - 1) / 2;

        if (a > 2 && b > 2)
        {
            var ellipse = new Ellipse(x, y, a, b);

            if (ValidEllipse(ellipse))
                Ellipses.Add(ellipse);
        }
    }

    bool InBox(int x, int y, Box box)
    {
        return box.InBox(x, y);
    }

    List<Box> UpdateBoxes(int x, int y)
    {
        var list = new List<Box>();

        if (Boxes.Count > 0)
        {
            foreach (var box in Boxes)
            {
                if (!InBox(x, y, box))
                    list.Add(box);
            }
        }

        return list;
    }

    bool ValidBox(Box bx)
    {
        var collisions = 0;

        if (Boxes.Count > 0)
        {
            foreach (var box in Boxes.ToArray())
            {
                if (box.BoxIntersect(bx))
                    collisions++;
            }
        }

        return !(collisions > 0);
    }

    void AddBox(int x0, int y0, int x1, int y1)
    {
        var w = Math.Abs(x0 - x1);
        var h = Math.Abs(y0 - y1);

        var bx0 = Math.Min(x0, x1);
        var by0 = Math.Min(y0, y1);
        var bx1 = Math.Max(x0, x1);
        var by1 = Math.Max(y0, y1);

        if (w > 2 && h > 2)
        {
            var box = new Box(bx0, by0, bx1, by1);

            if (ValidBox(box))
                Boxes.Add(box);
        }
    }

    public void Add(int X0, int Y0, int X1, int Y1)
    {
        if (EllipseMode)
            AddEllipse(X0, Y0, X1, Y1);
        else
            AddBox(X0, Y0, X1, Y1);

        Update();
    }

    public void Update(int X0, int Y0)
    {
        if (EllipseMode)
            Ellipses = UpdateEllipses(X0, Y0);
        else
            Boxes = UpdateBoxes(X0, Y0);

        Update();
    }

    public String GetEllipses()
    {
        var sb = new StringBuilder();

        for (var i = 0; i < Ellipses.Count; i++)
        {
            if (i > 0)
                sb.Append(";");

            var ellipse = Ellipses[i];

            sb.Append(ellipse.X);
            sb.Append("," + ellipse.Y);
            sb.Append("," + ellipse.Width);
            sb.Append("," + ellipse.Height);
            sb.Append("," + ellipse.Rotation);
            sb.Append("," + ellipse.Enabled);
        }

        return sb.ToString();
    }

    public String GetBoxes()
    {
        var sb = new StringBuilder();

        for (var i = 0; i < Boxes.Count; i++)
        {
            if (i > 0)
                sb.Append(";");

            var box = Boxes[i];

            sb.Append(box.X0);
            sb.Append("," + box.Y0);
            sb.Append("," + box.X1);
            sb.Append("," + box.Y1);
            sb.Append("," + box.Enabled);
        }

        return sb.ToString();
    }

    public override String ToString()
    {
        var sb = new StringBuilder();

        if (EllipseMode)
        {
            if (EllipsesString != null)
                sb.Append(EllipsesString);
        }
        else
        {
            if (BoxesString != null)
                sb.Append(BoxesString);
        }

        return sb.ToString();
    }

    void Update()
    {
        EllipsesString = GetEllipses();
        BoxesString = GetBoxes();
    }

    public void Parse()
    {
        if (EllipsesString != null)
        {
            var ellipses = EllipsesString.Split(';');

            if (ellipses.Length > 0)
            {
                Ellipses.Clear();

                foreach (var ellipse in ellipses)
                {
                    var dimensions = ellipse.Split(',');

                    if (dimensions.Length == 6)
                    {
                        var X = int.Parse(dimensions[0]);
                        var Y = int.Parse(dimensions[1]);
                        var Width = int.Parse(dimensions[2]);
                        var Height = int.Parse(dimensions[3]);
                        var Rotation = int.Parse(dimensions[4]);
                        var Enabled = bool.Parse(dimensions[5]);

                        Ellipses.Add(new Ellipse(X, Y, Width, Height, Rotation, Enabled));
                    }
                }
            }
        }

        if (BoxesString != null)
        {
            var boxes = BoxesString.Split(';');

            if (boxes.Length > 0)
            {
                Boxes.Clear();

                foreach (var box in boxes)
                {
                    var dimensions = box.Split(',');

                    if (dimensions.Length == 5)
                    {
                        var X0 = int.Parse(dimensions[0]);
                        var Y0 = int.Parse(dimensions[1]);
                        var X1 = int.Parse(dimensions[2]);
                        var Y1 = int.Parse(dimensions[3]);
                        var Enabled = bool.Parse(dimensions[4]);

                        Boxes.Add(new Box(X0, Y0, X1, Y1, Enabled));
                    }
                }
            }
        }
    }

    public int Count()
    {
        if (EllipseMode)
        {
            return Ellipses.Count;
        }

        return Boxes.Count;
    }

    public List<Box> BoundingBoxes()
    {
        var boundingBoxes = new List<Box>();

        if (EllipseMode)
        {
            foreach (var ellipse in Ellipses)
            {
                var a = ((ellipse.Width - 1) / 2);
                var b = ((ellipse.Height - 1) / 2);

                boundingBoxes.Add(new Box(ellipse.X - a, ellipse.Y - b, ellipse.X + a, ellipse.Y + b));
            }
        }
        else
        {
            foreach (var box in Boxes)
            {
                boundingBoxes.Add(box);
            }
        }

        return boundingBoxes;
    }
}
