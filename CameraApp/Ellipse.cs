public class Ellipse
{
    /*
     * Bivariate polynomial representation
     */
    public class Bivariate
    {
        public double A, B, C, D, E, F;

        public Bivariate(double a, double b, double c, double d, double e, double f)
        {
            A = a;

            B = b;

            C = c;

            D = d;

            E = e;

            F = f;
        }
    }

    public int X, Y, Width, Height, Rotation;

    public bool Enabled;

    /*
     * Ellipse
     * 
     * x, y: origin
     * width, height: size of bounding box
     * rotation: rotation angle
     */
    public Ellipse(int x, int y, int width, int height, int rotation)
    {
        Initialize(x, y, width, height, rotation, true);
    }

    /*
     * Ellipse
     * 
     * x, y: origin
     * width, height: size of bounding box
     * rotation: rotation angle
     * enabled: flag whether visible or not
     */
    public Ellipse(int x, int y, int width, int height, int rotation, bool enabled)
    {
        Initialize(x, y, width, height, rotation, enabled);
    }

    /*
     * Ellipse
     * 
     * x, y: origin
     * width, height: size of bounding box
     * rotation: rotation angle, set to 0 to simplify computation
     */
    public Ellipse(int x, int y, int width, int height)
    {
        Initialize(x, y, width, height, 0, true);
    }

    void Initialize(int x, int y, int width, int height, int rotation, bool enabled)
    {
        X = x;

        Y = y;

        Width = width;

        Height = height;

        Rotation = rotation;

        Enabled = enabled;
    }

    /*
     * Is point (x, y) inside of the ellipse centered at (ex, ey)
     * with diameters width and height, rotated by an angle whose
     * cosine and sine are A and B?  Return true iff it's so.  See
     * www.khanacademy.org/math/trigonometry/conics_precalc/ellipses-precalc/v/conic-sections--doublero-to-ellipses
     */
    bool InEllipse(int ex, int ey, int width, int height, int A, int B, int x, int y)
    {
        x -= ex;
        y -= ey;

        var termX = 2 * (A * x + B * y) / width;  /* sqrt 1st term */
        var termY = 2 * (B * x - A * y) / height;  /* sqrt 2nd term */

        return ((termX * termX) + (termY * termY)) < 1;
    }

    /*
     * Simplified wrapper to the InEllipse(ex, ey, width, height, A, B, x, y)
     */
    public bool InEllipse(int x, int y)
    {
        return InEllipse(X, Y, Width, Height, 1, 0, x, y);
    }

    /*
     * Express the traditional KA ellipse, rotated by an angle
     * whose cosine and sine are A and B, in terms of a "bivariate"
     * polynomial that sums to zero.  See
     * http://elliotnoma.wordpress.com/2013/04/10/a-closed-form-solution-for-the-intersections-of-two-ellipses
     */
    Bivariate BivariateForm(int x, int y, int width, int height, int A, int B)
    {
        /*
         * Start by rotating the ellipse center by the OPPOSITE
         * of the desired angle.  That way when the bivariate
         * computation transforms it back, it WILL be at the
         * correct (and original) coordinates.
         */

        var a = A * x + B * y;
        var c = -B * x + A * y;

        /*
         * Now let the bivariate computation
         * rotate in the opposite direction.
         */

        B = -B;  /* A = cos(-rot); B = sin(-rot); */

        var b = (double)(width * width / 4);
        var d = (double)(height * height / 4);

        return new Bivariate(
            (A * A / b) + (B * B / d),  /* x^2 coefficient */
            (-2 * A * B / b) + (2 * A * B / d),  /* xy coeff */
            (B * B / b) + (A * A / d),  /* y^2 coeff */
            (-2 * a * A / b) - (2 * c * B / d),  /* x coeff */
            (2 * a * B / b) - (2 * c * A / d),  /* y coeff */
            (a * a / b) + (c * c / d) - 1  /* constant */
                                           /* So, ax^2 + bxy + cy^2 + dx + ey + f = 0 */
        );
    }

    /*
     * Does the quartic function described by
     * y = z4*x^4 + z3*x^3 + z2*x^2 + z1*x + z0 have *any*
     * real solutions?  See
     * http://en.wikipedia.org/wiki/Quartic_function
     * Thanks to Dr. David Goldberg for the convertion to
     * a depressed quartic!
     */
    bool RealRoot(int z4, int z3, int z2, int z1, int z0)
    {
        /* First trivial checks for z0 or z4 being zero */
        if (z0 == 0)
        {
            return true;  /* zero is a root! */
        }

        if (z4 == 0)
        {

            if (z3 != 0)
            {
                return true;  /* cubics always have roots */
            }

            if (z2 != 0)
            {

                return (z1 * z1 - 4 * z2 * z0) >= 0; /* quadratic */
            }

            return z1 != 0;  /* sloped lines have one root */
        }

        var a = z3 / z4;
        var b = z2 / z4;
        var c = z1 / z4;
        var d = z0 / z4;

        var p = (8 * b - 3 * a * a) / 8;
        var q = (a * a * a - 4 * a * b + 8 * c) / 8;
        var r = (-3 * a * a * a * a + 256 * d - 64 * c * a + 16 * a * a * b) / 256;

        /*
        *   x^4 +        p*x^2 + q*x + r
        * a*x^4 + b*x^3 + c*x^2 + d*x + e
        * so a=1  b=0  c=p  d=q  e=r
        * That is, we have a depessed quartic.
        */
        var descrim = 256 * r * r * r - 128 * p * p * r * r + 144 * p * q * q * r - 27 * q * q * q * q + 16 * p * p * p * p * r - 4 * p * p * p * q * q;
        var P = 8 * p;
        var D = 64 * r - 16 * p * p;

        return descrim < 0 || (descrim > 0 && P < 0 && D < 0) || (descrim == 0 && (D != 0 || P <= 0));
    }

    /*
     * Is the Y coordinate(s) of the intersection of two conic
     * sections real? They are in their bivariate form,
     * ax²  + bxy  + cx²  + dx  + ey  + f = 0
     * For now, a and a1 cannot be zero.
     */
    bool YIntersect(double a, double b, double c, double d, double e, double f, double aa, double bb, double cc, double dd, double ee, double ff)
    {
        /*
         * Normalize the conics by their first coefficient, a.
         * Then get the differnce of the two equations.
         */
        bb /= aa;
        b /= a;
        cc /= aa;
        c /= a;
        dd /= aa;
        d /= a;
        ee /= aa;
        e /= a;
        ff /= aa;
        f /= a;

        var deltaB = (int)(bb - b);
        var deltaC = (int)(cc - c);
        var deltaD = (int)(dd - d);
        var deltaE = (int)(ee - e);
        var deltaF = (int)(ff - f);

        /* Special case for b's and d's being equal */
        if (deltaB == 0 && deltaD == 0)
        {

            return RealRoot(0, 0, deltaC, deltaE, deltaF);
        }

        var a3 = (int)(b * cc - bb * c);
        var a2 = (int)(b * ee + d * cc - bb * e - dd * c);
        var a1 = (int)(b * ff + d * ee - bb * f - dd * e);
        var a0 = (int)(d * ff - dd * f);

        var A = deltaC * deltaC - a3 * deltaB;
        var B = 2 * deltaC * deltaE - deltaB * a2 - deltaD * a3;
        var C = deltaE * deltaE + 2 * deltaC * deltaF - deltaB * a1 - deltaD * a2;
        var D = 2 * deltaE * deltaF - deltaD * a1 - deltaB * a0;
        var E = deltaF * deltaF - deltaD * a0;

        return RealRoot(A, B, C, D, E);
    }

    /*
     * Do two conics sections el and el1 intersect? Each are in
     * bivariate form, ax^2  + bxy  + cx^2  + dx  + ey  + f = 0
     * Solve by constructing a quartic that must have a real
     * solution if they intersect.  This checks for real Y
     * intersects, then flips the parameters around to check
     * for real X intersects.
     */
    bool ConicsIntersect(Bivariate el, Bivariate el1)
    {
        /* check for real y intersects, then real x intersects */
        return YIntersect(el.A, el.B, el.C, el.D, el.E, el.F, el1.A, el1.B, el1.C, el1.D, el1.E, el1.F) && YIntersect(el.C, el.B, el.A, el.E, el.D, el.F, el1.C, el1.B, el1.A, el1.E, el1.D, el1.F);
    }

    /*
    * Do two ellipses intersect?  The first ellipse is described
    * in its "traditional" manner by the first four parameters,
    * along with a rotation parameter, rot1.  Similarly, the last
    * five parameters describe the second ellipse.  This (rather
    * large) function can be copied to another program.
    */
    public bool EllipseIntersect(int x1, int y1, int width1, int height1, int x2, int y2, int width2, int height2)
    {
        /* realtive translation makes distance test simpler... */
        x2 -= x1;
        y2 -= y1;
        x1 = 0;
        y1 = 0;

        var maxR = (((width1 > height1) ? width1 : height1) + ((width2 > height2) ? width2 : height2)) / 2;

        if (x2 * x2 + y2 * y2 > maxR * maxR)
        {
            /* The two ellipses are too far apart to care */
            return false;
        }

        var A1 = 1;
        var B1 = 0;
        var A2 = 1;
        var B2 = 0;

        /* Is the center of one inside the other? */
        if (InEllipse(x1, y1, width1, height1, A1, B1, x2, y2) || InEllipse(x2, y2, width2, height2, A2, B2, x1, y1))
        {
            return true;
        }

        /* Ok, do the hard work */
        var elps1 = BivariateForm(x1, y1, width1, height1, A1, B1);
        var elps2 = BivariateForm(x2, y2, width2, height2, A2, B2);

        /*
        * Now, ask your good friend with a PhD in Mathematics how he
        * would do it; then translate his R code.  See
        * https://docs.google.com/file/d/0B7wsEy6bpVePSEt2Ql9hY0hFdjA/
        */
        return ConicsIntersect(elps1, elps2);
    }

    /*
     * Simplified wrapper to EllipseIntersect(x1, y1, width1, height1, x2, y2, width2, height2)
     */
    public bool EllipseIntersect(Ellipse el)
    {
        return EllipseIntersect(X, Y, Width, Height, el.X, el.Y, el.Width, el.Height);
    }
}
