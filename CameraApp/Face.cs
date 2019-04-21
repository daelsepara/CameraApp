using Emgu.CV;
using Emgu.CV.Structure;
using Gdk;
using System;

public static class Face
{
    public static void Detect(OpenCV cv, Pixbuf pixbuf, Select selection, double ScaleX, double ScaleY)
    {
        if (pixbuf != null)
        {
            using (var mat = cv.ToMat(pixbuf))
            {
                var _cascadeClassifier = new CascadeClassifier("haarcascade_frontalface_alt.xml");

                var img = mat.ToImage<Bgr, byte>();
                var grayFrame = cv.ConvertToGray(img);

                var faces = _cascadeClassifier.DetectMultiScale(grayFrame, 1.1, 4, new System.Drawing.Size(pixbuf.Width / 8, pixbuf.Height / 8));

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

                cv.Throw(grayFrame, img);
            }
        }
    }
}
