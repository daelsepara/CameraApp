using Emgu.CV.Features2D;
using Gdk;

public static class Detect
{
    public static int EdgeThreshold = 90;
    public static int LinkingThreshold = 60;
    public static int MinArea = 20;
    public static int MaxArea = 10000;
    public static int HoughThreshold = 120;
    public static int CircleDistance = 20;
    public static int MinRadius = 5;
    public static int MaxRadius = 500;
    public static double dp = 2.0;

    public static int MarkerSize = 2;

    public static void EdgeBlobs(OpenCV cv, Pixbuf pixbuf, Select selection, double ScaleX, double ScaleY)
    {
        using (var mat = cv.ToMat(pixbuf))
        {
            cv.DetectBlobsMat(
                mat,
                EdgeThreshold,
                LinkingThreshold,
                MinArea,
                MaxArea,
                selection,
                ScaleX,
                ScaleY
            );
        }
    }

    public static void HoughCircles(OpenCV cv, Pixbuf pixbuf, Select selection, double ScaleX, double ScaleY)
    {
        using (var mat = cv.ToMat(pixbuf))
        {
            cv.DetectCirclesMat(
                mat,
                dp,
                CircleDistance,
                EdgeThreshold,
                HoughThreshold,
                MinRadius,
                MaxRadius,
                selection,
                ScaleX,
                ScaleY
            );
        }
    }

    public static void Blobs(OpenCV cv, Pixbuf pixbuf, Select selection, double ScaleX, double ScaleY)
    {
        using (var mat = cv.ToMat(pixbuf))
        {
            cv.BlobDetectorMat(
                mat,
                MinArea,
                MaxArea,
                selection,
                ScaleX,
                ScaleY
            );
        }
    }

    public static void Simple(OpenCV cv, Pixbuf pixbuf, Select selection, double ScaleX, double ScaleY)
    {
        var parameters = new SimpleBlobDetectorParams
        {
            MinArea = MinArea,
            MaxArea = MaxArea,
            FilterByArea = true
        };

        cv.InitSimpleBlobDetector(parameters);

        using (var mat = cv.ToMat(pixbuf))
        {
            cv.SimpleBlobDetectionMat(
                mat,
                selection,
                ScaleX,
                ScaleY
            );
        }
    }
}
