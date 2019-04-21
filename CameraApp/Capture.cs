using Emgu.CV.CvEnum;
using Gdk;
using System.Drawing.Imaging;
using System.IO;

public static class Capture
{
    public static Pixbuf ProcessPixbufBMP(VirtualCamera camera, OpenCV cv)
    {
        return camera != null ? cv.ToPixbuf(cv.ToBitmap(camera.Pixbuf())) : null;
    }

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

    public static Pixbuf ProcessMat(VirtualCamera camera, OpenCV cv)
    {
        return camera != null ? cv.ToPixbuf(camera.Mat()) : null;
    }
}
