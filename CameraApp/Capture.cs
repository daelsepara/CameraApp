using Emgu.CV.CvEnum;
using Gdk;
#if _LINUX || _WIN32
using System.Drawing.Imaging;
#endif
using System.IO;

public static class Capture
{

#if _LINUX || _WIN32
    public static Pixbuf ProcessPixbufBMP(VirtualCamera camera, OpenCV cv)
    {
        return camera != null ? cv.ToPixbuf(cv.ToBitmap(camera.Pixbuf())) : null;
    }
#endif

    public static Pixbuf ProcessPixbuf(VirtualCamera camera)
    {
        return camera?.Pixbuf();
    }

    public static Pixbuf ProcessMatPixbuf(VirtualCamera camera, OpenCV cv)
    {
        return camera != null ? cv.ToPixbuf(cv.ToMat(camera.Pixbuf())) : null;
    }

    public static Pixbuf Flip(VirtualCamera camera, OpenCV cv, FlipType flipCode)
    {
        return camera != null ? cv.Flip(camera.Pixbuf(), flipCode) : null;
    }

    public static Pixbuf FlippedHorizontal(VirtualCamera camera, OpenCV cv)
    {
        return Flip(camera, cv, FlipType.Horizontal);
    }

    public static Pixbuf FlippedVertical(VirtualCamera camera, OpenCV cv)
    {
        return Flip(camera, cv, FlipType.Vertical);
    }

    public static Pixbuf FlippedAll(VirtualCamera camera, OpenCV cv)
    {
        return Flip(camera, cv, FlipType.None);
    }

#if _LINUX || _WIN32
    public static Pixbuf ProcessBitmap(VirtualCamera camera)
    {
        if (camera != null)
        {
            var bm = camera.Bitmap();

            if (bm != null)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    bm.Save(stream, ImageFormat.Bmp);

                    stream.Position = 0;

                    return new Pixbuf(stream);
                }
            }
        }

        return null;
    }
#endif

    public static Pixbuf ProcessMat(VirtualCamera camera, OpenCV cv)
    {
        return camera != null ? cv.ToPixbuf(camera.Mat()) : null;
    }
}
