using Emgu.CV;
using Emgu.CV.Structure;
using Gdk;

public static class Face
{
    public static void Detetct(OpenCV cv, Pixbuf pixbuf, Select selection)
    {
        if (pixbuf != null)
        {
            using (var mat = cv.ToMat(pixbuf))
            {
                var _cascadeClassifier = new CascadeClassifier("haarcascade_frontalface_alt.xml");

                var img = mat.ToImage<Bgr, byte>();
                var grayFrame = cv.ConvertToGray(img);

                var faces = _cascadeClassifier.DetectMultiScale(grayFrame, 1.1, 4, new System.Drawing.Size(pixbuf.Width / 8, pixbuf.Height / 8));

                if (faces.Length > 0)
                {
                    foreach (var face in faces)
                    {
                        selection.Add(face.Left, face.Top, face.Right, face.Bottom);
                    }
                }

                cv.Throw(grayFrame, img);
            }
        }
    }
}
