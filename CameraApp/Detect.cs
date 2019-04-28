using Emgu.CV;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Gdk;
using System;

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
    public static double scaleFactor = 1.0;
    public static int minNeighbors = 1;
    public static int minSize = 10;

    public static string Classifier = "haarcascade_frontalface_default.xml";

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

    public static void Faces(OpenCV cv, Pixbuf pixbuf, Select selection, double ScaleX, double ScaleY)
    {
        if (pixbuf != null)
        {
            using (var mat = cv.ToMat(pixbuf))
            {
				if (System.IO.File.Exists(Classifier))
				{
					var _cascadeClassifier = new CascadeClassifier(Classifier);

					var img = mat.ToImage<Bgr, byte>();
					var grayFrame = cv.ConvertToGray(img);

					if (scaleFactor > 1.0)
					{
						var faces = _cascadeClassifier.DetectMultiScale(grayFrame, scaleFactor, minNeighbors, new System.Drawing.Size(minSize, minSize));

						selection.Clear();

						if (faces.Length > 0)
						{
							foreach (var face in faces)
							{
								var X0 = Convert.ToInt32(ScaleX * face.X);
								var Y0 = Convert.ToInt32(ScaleY * face.Y);
								var X1 = Convert.ToInt32(ScaleX * (face.X + face.Width - 1));
								var Y1 = Convert.ToInt32(ScaleY * (face.Y + face.Height - 1));

								selection.Add(X0, Y0, X1, Y1);
							}
						}
					}

					cv.Throw(grayFrame, img);
				}
            }
        }
    }
}
