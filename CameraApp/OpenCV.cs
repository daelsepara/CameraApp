using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Dpm;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.Cvb;
using Gdk;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

public class OpenCV : ImageProcessing, IDisposable
{
    Mat nframe = new Mat();
    SimpleBlobDetector _simpleBlobDetector;
    CvBlobDetector _blobDetector = new CvBlobDetector();

    List<CircleF> recentCircles = new List<CircleF>();
    List<System.Drawing.Rectangle> recentRectangles = new List<System.Drawing.Rectangle>();

    public bool SubtractBackground;
    public bool Invert;
    public bool DownUpSample = true;
    public bool Blur;
    public bool Normalize;

    public int sx = 3;
    public int sy = 3;
    public double sigmaX = 1.0;
    public double sigmaY = 1.0;

    public FlipType FlipX = FlipType.Horizontal;
    public FlipType FlipY = FlipType.Vertical;

    /// <summary>
    /// Define a color in OpenCV BGR format
    /// </summary>
    /// <param name="b">Blue channel value</param>
    /// <param name="g">Green channel value</param>
    /// <param name="r">Red channel value</param>
    /// <returns>Emgv.CV.Structure.Bgr</returns>
    public static Bgr Bgr(int b, int g, int r)
    {
        return new Bgr(b, g, r);
    }

    /// <summary>
    /// Convert Gdk Color to OpenCV BGR format
    /// </summary>
    /// <param name="color">Gdk RGB Color</param>
    /// <returns>Emgv.CV.Structure.Bgr</returns>
    public MCvScalar BGR(Gdk.Color color)
    {
        return new MCvScalar(color.Blue >> 8, color.Green >> 8, color.Red >> 8);
    }

    /// <summary>
    /// Wrap void functions in a try-catch clause
    /// </summary>
    /// <param name="action">Void function</param>
    /// <returns>void</returns>
    public static void TryCatch(Action action)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: {0}", e.Message);
        }
    }

    /// <summary>
    /// Convert an OpenCV matrix from BGR to RGB
    /// </summary>
    /// <param name="src">Source OpenCV matrix</param>
    /// <returns>Emgu.CV.Mat</returns>
    public Mat Bgr2Rbg(Mat src)
    {
        if (src == null)
            return null;

        var dest = new Mat();

        TryCatch(delegate
        {
            CvInvoke.CvtColor(src, dest, ColorConversion.Bgr2Rgb);
        });

        return dest;
    }

    /// <summary>
    /// Convert an OpenCV matrix from BGR to Gray
    /// </summary>
    /// <param name="src">Source OpenCV matrix</param>
    /// <returns>Emgu.CV.Mat</returns>
    public Mat Bgr2Gray(Mat src)
    {
        if (src == null)
            return null;

        var dest = new Mat();

        TryCatch(delegate
        {
            CvInvoke.CvtColor(src, dest, ColorConversion.Bgr2Gray);
        });

        return dest;
    }

    /// <summary>
    /// Convert an OpenCV matrix from RGB to BGR
    /// </summary>
    /// <param name="src">Source OpenCV matrix</param>
    /// <returns>Emgu.CV.Mat</returns>
    public Mat Rgb2Bgr(Mat src)
    {
        var dest = new Mat();

        TryCatch(delegate
        {
            CvInvoke.CvtColor(src, dest, ColorConversion.Rgb2Bgr);
        });

        return dest;
    }

    /// <summary>
    /// Convert an OpenCV matrix from RGB to Gray
    /// </summary>
    /// <param name="src">Source OpenCV matrix</param>
    /// <returns>Emgu.CV.Mat</returns>
    public Mat Rgb2Gray(Mat src)
    {
        if (src == null)
            return null;

        var dest = new Mat();

        TryCatch(delegate
        {
            CvInvoke.CvtColor(src, dest, ColorConversion.Rgb2Gray);
        });

        return dest;
    }

#if _LINUX || _WIN32

    /// <summary>
    /// Convert an OpenCV matrix to Bitmap
    /// </summary>
    /// <param name="src">Source OpenCV matrix</param>
    /// <returns>System.Drawing.Bitmap</returns>
    public Bitmap ToBitmap(Mat src)
    {
        if (src == null)
            return null;

        return src.Bitmap;
    }

    /// <summary>
    /// Convert a Gdk Pixbuf to Bitmap
    /// </summary>
    /// <param name="src">Source Gdk Pixbuf</param>
    /// <returns>System.Drawing.Bitmap</returns>
    public Bitmap ToBitmap(Pixbuf src)
    {
        if (src == null)
            return null;

        var stream = new MemoryStream(src.SaveToBuffer("bmp"))
        {
            Position = 0
        };

        return new Bitmap(stream);
    }

    /// <summary>
    /// Convert a Bitmap to a Gdk Pixbuf
    /// </summary>
    /// <param name="src">Source Bitmap</param>
    /// <returns>Gdk.Pixbuf</returns>
    public Pixbuf ToPixbuf(Bitmap src)
    {
        Contract.Ensures(Contract.Result<Pixbuf>() != null);
        if (src == null)
            return null;

        using (var stream = new MemoryStream())
        {
            src.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);

            stream.Position = 0;

            return new Pixbuf(stream);
        }
    }
#endif

    /// <summary>
    /// Convert an OpenCV matrix to a Gdk Pixbuf with optional alpha channel.
    /// Performs color conversion from BGR to RGB
    /// </summary>
    /// <param name="src">Source OpenCV matrix</param>
    /// <param name="alpha">Add alpha channel (optional)</param>
    /// <returns>Gdk.Pixbuf</returns>
    public Pixbuf ToPixbuf(Mat src, bool alpha)
    {
        if (src == null)
            return null;

        using (var dest = Bgr2Rbg(src))
        {
            if (dest != null)
                return CopyToPixbuf(dest, alpha);

            return CopyToPixbuf(src, alpha);
        }
    }

    /// <summary>
    /// Convert an OpenCV matrix to a Gdk Pixbuf with optional alpha channel.
    /// Does not perform any color conversion
    /// </summary>
    /// <param name="src">Source OpenCV matrix</param>
    /// <param name="alpha">Add alpha channel (optional)</param>
    /// <returns>Gdk.Pixbuf</returns>
    public Pixbuf ToPixbufNoCC(Mat src, bool alpha)
    {
        if (src == null)
            return null;

        return CopyToPixbuf(src, alpha);
    }

    /// <summary>
    /// Convert an OpenCV matrix to a Gdk Pixbuf with no alpha channel.
    /// Performs color conversion from BGR to RGB
    /// </summary>
    /// <param name="src">Source OpenCV matrix</param>
    /// <returns>Gdk.Pixbuf</returns>
    public Pixbuf ToPixbuf(Mat src)
    {
        return ToPixbuf(src, false);
    }

    /// <summary>
    /// Convert an OpenCV matrix to a Gdk Pixbuf with no alpha channel.
    /// Does not perform any color conversion
    /// </summary>
    /// <param name="src">Source OpenCV matrix</param>
    /// <returns>Gdk.Pixbuf</returns>
    public Pixbuf ToPixbufNoCC(Mat src)
    {
        return ToPixbufNoCC(src, false);
    }

#if _LINUX || _WIN32
    /// <summary>
    /// Convert a Bitmap to an OpenCV matrix
    /// Does not perform any color conversion
    /// </summary>
    /// <param name="src">Source Bitmap</param>
    /// <returns>OpenCV Matrix</returns>
    public Mat ToMat(Bitmap src)
    {
        if (src == null)
            return null;

        nframe = new Image<Emgu.CV.Structure.Rgb, byte>(src).Mat;

        return (nframe != null && nframe.GetData() != null) ? Rgb2Bgr(nframe) : null;
    }
#endif

    /// <summary>
    /// Load an image into an OpenCV matrix
    /// </summary>
    /// <param name="src">Source Pixbuf</param>
    /// <returns>OpenCV Matrix</returns>
    public Mat ToMat(String src)
    {
        if (string.IsNullOrEmpty(src))
            return null;

        return new Mat(src);
    }

    /// <summary>
    /// Convert a Gdk Pixbuf to an OpenCV matrix
    /// Performs color conversion from RGB to BGR
    /// </summary>
    /// <param name="src">Source Pixbuf</param>
    /// <returns>OpenCV Matrix</returns>
    public Mat ToMat(Pixbuf src)
    {
        if (src == null)
            return null;

        return Rgb2Bgr(new Mat(src.Height, src.Width, DepthType.Cv8U, src.NChannels, src.Pixels, 0));
    }

    public Pixbuf CopyToPixbuf(Mat src, bool alpha)
    {
        if (src == null)
            return null;

#if _LINUX
        var data = src.GetRawData();
#else
        var data = src.GetData();
#endif

        var pb = new Pixbuf(Colorspace.Rgb, alpha, 8, src.Cols, src.Rows);

        Marshal.Copy(data, 0, pb.Pixels, data.Length);

        return pb;
    }

    /// <summary>
    /// Detects circles on an OpenCV matrix using Hough Circles Transform.
    /// Performs color conversion from BGR to RGB
    /// </summary>
    /// <param name="src">Source OpenCV matrix</param>
    /// <param name="dp">Inverse ratio of the accumulator resolution to the image resolution. For example, if dp=1 , the accumulator has the same resolution as the input image. If dp=2 , the accumulator has half as big width and heigh</param>
    /// <param name="minDist">Minimum distance between the centers of the detected circles. If the parameter is too small, multiple neighbor circles may be falsely detected in addition to a true one. If it is too large, some circles may be missed.</param>
    /// <param name="cannyThreshold">Threshold for the hysteresis procedure</param>
    /// <param name="circleAccumulatorThreshold">accumulator threshold for the circle centers at the detection stage. The smaller it is, the more false circles may be detected. Circles, corresponding to the larger accumulator values, will be returned first.</param>
    /// <param name="minRadius">Minimum circle radius</param>
    /// <param name="maxRadius">Maximum circle radius</param>
    /// <param name="markerColor">marker color in OpenCV BGR format</param>
    /// <param name="markerSize">marker size</param>
    /// <returns>OpenCV matrix with detected circles highlighted</returns>
    public Mat DetectCirclesMat(Mat src, double dp, double minDist, double cannyThreshold, double circleAccumulatorThreshold, int minRadius, int maxRadius, Bgr markerColor, int markerSize)
    {
        if (src == null)
            return null;

        try
        {
            #region convert the image to grayscale
            var img = src.ToImage<Bgr, byte>();
            var uimage = DownUpSample ? NoiseFilter(img) : ConvertToGray(img);
            #endregion

            #region invert gray colors
            if (Invert)
                GrayInvert(uimage);
            #endregion

            #region normalize
            if (Normalize)
                NormalizeGray(uimage);
            #endregion

            #region apply Gaussian blur
            var smoothedFrame = Blur ? GaussianBlur(uimage, sx, sy, sigmaX, sigmaY) : uimage.Clone();
            #endregion

            CircleF[] circles;

            #region edge detection and Hough circle transform
            if (SubtractBackground)
            {
                #region detect background
                var foregroundMask = BackgroundSubtract(smoothedFrame);
                #endregion

                circles = CvInvoke.HoughCircles(foregroundMask, HoughType.Gradient, dp, minDist, cannyThreshold, circleAccumulatorThreshold, minRadius, maxRadius);

                #region cleanup
                Throw(foregroundMask);
                #endregion
            }
            else
            {
                circles = CvInvoke.HoughCircles(smoothedFrame, HoughType.Gradient, dp, minDist, cannyThreshold, circleAccumulatorThreshold, minRadius, maxRadius);
            }
            #endregion

            #region draw circles
            recentCircles.Clear();

            if (circles.Length > 0)
            {
                foreach (CircleF circle in circles)
                {

                    img.Draw(circle, markerColor, markerSize);

                    recentCircles.Add(circle);
                }
            }
            #endregion

            CvInvoke.CvtColor(img.Mat, nframe, ColorConversion.Bgr2Rgb);

            #region cleanup
            Throw(img, uimage, smoothedFrame);

            CollectGarbage();
            #endregion
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: {0}", e.Message);

            #region cleanup
            CollectGarbage();
            #endregion

            return src;
        }

        return nframe;
    }

#if _LINUX || _WIN32
    /// <summary>
    /// Detects circles on an OpenCV matrix using Hough Circles Transform.
    /// Performs color conversion from BGR to RGB
    /// </summary>
    /// <param name="src">Source OpenCV matrix</param>
    /// <param name="dp">Inverse ratio of the accumulator resolution to the image resolution. For example, if dp=1 , the accumulator has the same resolution as the input image. If dp=2 , the accumulator has half as big width and heigh</param>
    /// <param name="minDist">Minimum distance between the centers of the detected circles. If the parameter is too small, multiple neighbor circles may be falsely detected in addition to a true one. If it is too large, some circles may be missed.</param>
    /// <param name="cannyThreshold">Threshold for the hysteresis procedure</param>
    /// <param name="circleAccumulatorThreshold">accumulator threshold for the circle centers at the detection stage. The smaller it is, the more false circles may be detected. Circles, corresponding to the larger accumulator values, will be returned first.</param>
    /// <param name="minRadius">Minimum circle radius</param>
    /// <param name="maxRadius">Maximum circle radius</param>
    /// <param name="markerColor">marker color in OpenCV BGR format</param>
    /// <param name="markerSize">marker size</param>
    /// <returns>Gdk Pixbuf with detected circles highlighted</returns>
    public Bitmap DetectCirclesBMP(Mat src, double dp, double minDist, double cannyThreshold, double circleAccumulatorThreshold, int minRadius, int maxRadius, Bgr markerColor, int markerSize)
    {
        return ToBitmap(DetectCirclesMat(src, dp, minDist, cannyThreshold, circleAccumulatorThreshold, minRadius, maxRadius, markerColor, markerSize));
    }
#endif

    /// <summary>
    /// Detects circles on an OpenCV matrix using Hough Circles Transform.
    /// Performs color conversion from BGR to RGB
    /// </summary>
    /// <param name="src">Source OpenCV matrix</param>
    /// <param name="dp">Inverse ratio of the accumulator resolution to the image resolution. For example, if dp=1 , the accumulator has the same resolution as the input image. If dp=2 , the accumulator has half as big width and heigh</param>
    /// <param name="minDist">Minimum distance between the centers of the detected circles. If the parameter is too small, multiple neighbor circles may be falsely detected in addition to a true one. If it is too large, some circles may be missed.</param>
    /// <param name="cannyThreshold">Threshold for the hysteresis procedure</param>
    /// <param name="circleAccumulatorThreshold">accumulator threshold for the circle centers at the detection stage. The smaller it is, the more false circles may be detected. Circles, corresponding to the larger accumulator values, will be returned first.</param>
    /// <param name="minRadius">Minimum circle radius</param>
    /// <param name="maxRadius">Maximum circle radius</param>
    /// <param name="markerColor">marker color in OpenCV BGR format</param>
    /// <param name="markerSize">marker size</param>
    /// <returns>Gdk Pixbuf with detected circles highlighted</returns>
    public Pixbuf DetectCircles(Mat src, double dp, double minDist, double cannyThreshold, double circleAccumulatorThreshold, int minRadius, int maxRadius, Bgr markerColor, int markerSize)
    {
        return ToPixbufNoCC(DetectCirclesMat(src, dp, minDist, cannyThreshold, circleAccumulatorThreshold, minRadius, maxRadius, markerColor, markerSize));
    }

    /// <summary>
    /// Detects blobs using Canny edge detection and contour detection.
    /// The smallest value between threshold1 and threshold2 is used for edge linking. The largest value is used to find initial segments of strong edges.
    /// Performs color conversion from BGR to RGB
    /// </summary>
    /// <param name="src">Source OpenCV matrix</param>
    /// <param name="cannyThreshold">Threshold to find initial segments of strong edges</param>
    /// <param name="cannyThresholdLinking">Threshold for edge linking</param>
    /// <param name="minArea">Threshold for minimum area</param>
    /// <param name="maxArea">Threshold for maximum area</param>
    /// <param name="markerColor">marker color in OpenCV BGR format</param>
    /// <param name="markerSize">marker size</param>
    /// <returns>OpenCV matrix with blobs highlighted inside bounding boxes</returns>
    public Mat DetectBlobsMat(Mat src, double cannyThreshold, double cannyThresholdLinking, double minArea, double maxArea, Bgr markerColor, int markerSize)
    {
        if (src == null)
            return null;

        try
        {
            #region convert to gray scale
            var img = src.ToImage<Bgr, byte>();
            var uimage = DownUpSample ? NoiseFilter(img) : ConvertToGray(img);
            #endregion

            #region invert gray colors
            if (Invert)
                GrayInvert(uimage);
            #endregion

            #region normalize
            if (Normalize)
                NormalizeGray(uimage);
            #endregion

            #region apply Gaussian blur
            var smoothedFrame = Blur ? GaussianBlur(uimage, sx, sy, sigmaX, sigmaY) : uimage.Clone();
            #endregion

            #region edge detection
            Mat cannyEdges = new Mat();

            if (SubtractBackground)
            {
                #region detect background
                var foregroundMask = BackgroundSubtract(smoothedFrame);
                #endregion

                CvInvoke.Canny(foregroundMask, cannyEdges, cannyThreshold, cannyThresholdLinking);

                #region cleanup
                Throw(foregroundMask);
                #endregion
            }
            else
            {
                CvInvoke.Canny(smoothedFrame, cannyEdges, cannyThreshold, cannyThresholdLinking);
            }
            #endregion

            #region find contours
            var contours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(cannyEdges, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
            #endregion

            #region plot contours
            if (contours != null && contours.Size > 0)
            {
                recentRectangles.Clear();

                for (int i = 0; i < contours.Size; i++)
                {
                    #region polygon approximation
                    var contour = contours[i];
                    var approxContour = new VectorOfPoint();
                    var length = CvInvoke.ArcLength(contour, true) * 0.015;

                    CvInvoke.ApproxPolyDP(contour, approxContour, length, true);
                    #endregion

                    #region filter by area
                    var area = CvInvoke.ContourArea(approxContour, false);

                    if (area >= minArea && area < maxArea) //only consider contours with areas greather than this
                    {
                        var rectangle = CvInvoke.BoundingRectangle(approxContour);

                        img.Draw(rectangle, markerColor, markerSize);

                        recentRectangles.Add(rectangle);
                    }
                    #endregion
                }

                CvInvoke.CvtColor(img.Mat, nframe, ColorConversion.Bgr2Rgb);
            }
            else
            {
                CvInvoke.CvtColor(src, nframe, ColorConversion.Bgr2Rgb);
            }
            #endregion

            #region cleanup
            Throw(cannyEdges, contours, smoothedFrame, img, uimage);

            CollectGarbage();
            #endregion
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: {0}", e.Message);

            #region cleanup
            CollectGarbage();
            #endregion

            return src;
        }

        return nframe;
    }

    /// <summary>
    /// Detects blobs using Canny edge detection and contour detection.
    /// The smallest value between threshold1 and threshold2 is used for edge linking. The largest value is used to find initial segments of strong edges.
    /// Performs color conversion from BGR to RGB
    /// </summary>
    /// <param name="src">Source OpenCV matrix</param>
    /// <param name="cannyThreshold">Threshold to find initial segments of strong edges</param>
    /// <param name="cannyThresholdLinking">Threshold for edge linking</param>
    /// <param name="markerColor">marker color in OpenCV BGR format</param>
    /// <param name="markerSize">marker size</param>
    /// <returns>GDk Pixbuf with blobs highlighted inside bounding boxes</returns>
    public Pixbuf DetectBlobs(Mat src, double cannyThreshold, double cannyThresholdLinking, double minArea, double maxArea, Bgr markerColor, int markerSize)
    {
        return ToPixbufNoCC(DetectBlobsMat(src, cannyThreshold, cannyThresholdLinking, minArea, maxArea, markerColor, markerSize));
    }

#if _LINUX || _WIN32
    /// <summary>
    /// Detects blobs using Canny edge detection and contour detection.
    /// The smallest value between threshold1 and threshold2 is used for edge linking. The largest value is used to find initial segments of strong edges.
    /// Performs color conversion from BGR to RGB
    /// </summary>
    /// <param name="src">Source OpenCV matrix</param>
    /// <param name="cannyThreshold">Threshold to find initial segments of strong edges</param>
    /// <param name="cannyThresholdLinking">Threshold for edge linking</param>
    /// <param name="markerColor">marker color in OpenCV BGR format</param>
    /// <param name="markerSize">marker size</param>
    /// <returns>Bitmap with blobs highlighted inside bounding boxes</returns>
    public Bitmap DetectBlobsBMP(Mat src, double cannyThreshold, double cannyThresholdLinking, double minArea, double maxArea, Bgr markerColor, int markerSize)
    {
        return ToBitmap(DetectBlobsMat(src, cannyThreshold, cannyThresholdLinking, minArea, maxArea, markerColor, markerSize));
    }
#endif

    /// <summary>
    /// Detects blobs using Gaussian blurring filter and background subttraction.
    /// Performs color conversion from BGR to RGB
    /// </summary>
    /// <param name="src">Source OpenCV matrix</param>
    /// <param name="minArea">blob minimum area</param>
    /// <param name="maxArea">blob maximum area</param>
    /// <param name="markerColor">marker color in OpenCV BGR format</param>
    /// <param name="markerSize">marker size</param>
    /// <returns>OpenCV matrix with blobs highlighted inside bounding boxes</returns>
    public Mat BlobDetectorMat(Mat src, int minArea, int maxArea, Bgr markerColor, int markerSize)
    {
        if (src == null)
            return null;

        try
        {
            #region convert to grayscale
            var img = src.ToImage<Bgr, byte>();
            var uimage = DownUpSample ? NoiseFilter(img) : ConvertToGray(img);
            #endregion

            #region invert gray colors
            if (Invert)
                GrayInvert(uimage);
            #endregion

            #region normalize
            if (Normalize)
                NormalizeGray(uimage);
            #endregion

            #region apply Gaussian blur
            var smoothedFrame = Blur ? GaussianBlur(uimage, sx, sy, sigmaX, sigmaY) : uimage.Clone();
            #endregion

            #region detect blobs
            var blobs = new CvBlobs();

            if (SubtractBackground)
            {
                #region detect background
                var foregroundMask = BackgroundSubtract(smoothedFrame);
                #endregion

                _blobDetector.Detect(foregroundMask.ToImage<Gray, byte>(), blobs);

                #region cleanup
                Throw(foregroundMask);
                #endregion
            }
            else
            {
                _blobDetector.Detect(smoothedFrame.ToImage<Gray, byte>(), blobs);
            }

            blobs.FilterByArea(minArea, maxArea);
            #endregion

            recentRectangles.Clear();

            foreach (var pair in blobs)
            {
                CvInvoke.Rectangle(img, pair.Value.BoundingBox, markerColor.MCvScalar, markerSize, LineType.AntiAlias);

                recentRectangles.Add(pair.Value.BoundingBox);
            }

            CvInvoke.CvtColor(img.Mat, nframe, ColorConversion.Bgr2Rgb);

            #region cleanup
            Throw(img, uimage, blobs, smoothedFrame);

            CollectGarbage();
            #endregion
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: {0}", e.Message);

            #region cleanup
            CollectGarbage();
            #endregion

            return src;
        }

        return nframe;
    }

#if _LINUX || _WIN32
    /// <summary>
    /// Detects blobs using Gaussian blurring filter and background subttraction.
    /// Performs color conversion from BGR to RGB
    /// </summary>
    /// <param name="src">Source OpenCV matrix</param>
    /// <param name="minArea">blob minimum area</param>
    /// <param name="maxArea">blob maximum area</param>
    /// <param name="markerColor">marker color in OpenCV BGR format</param>
    /// <param name="markerSize">marker size</param>
    /// <returns>Bitmap with blobs highlighted inside bounding boxes</returns>
    public Bitmap BlobDetectorBMP(Mat src, int minArea, int maxArea, Bgr markerColor, int markerSize)
    {
        return ToBitmap(BlobDetectorMat(src, minArea, maxArea, markerColor, markerSize));
    }
#endif

    /// <summary>
    /// Detects blobs using Gaussian blurring filter and background subttraction.
    /// Performs color conversion from BGR to RGB
    /// </summary>
    /// <param name="src">Source OpenCV matrix</param>
    /// <param name="minArea">blob minimum area</param>
    /// <param name="maxArea">blob maximum area</param>
    /// <param name="markerColor">marker color in OpenCV BGR format</param>
    /// <param name="markerSize">marker size</param>
    /// <returns>Gdk Pixbuf with blobs highlighted inside bounding boxes</returns>
    public Pixbuf BlobDetector(Mat src, int minArea, int maxArea, Bgr markerColor, int markerSize)
    {
        return ToPixbufNoCC(BlobDetectorMat(src, minArea, maxArea, markerColor, markerSize));
    }

    /// <summary>
    /// Initializes Simple Blob Detector
    /// 
    /// Parameters (Structure) see: http://docs.opencv.org/3.2.0/d8/da7/structcv_1_1SimpleBlobDetector_1_1Params.html
    /// 
    /// uchar blobColor
    /// bool filterByArea
    /// bool filterByCircularity
    /// bool filterByColor
    /// bool filterByConvexity
    /// bool filterByInertia
    /// float maxArea
    /// float maxCircularity
    /// float maxConvexity
    /// float maxInertiaRatio
    /// float maxThreshold
    /// float minArea
    /// float minCircularity
    /// float minConvexity
    /// float minDistBetweenBlobs
    /// float minInertiaRatio
    /// size_t minRepeatability
    /// float minThreshold
    /// float thresholdStep
    /// </summary>
    /// <param name="parameters">SimpleBlobDetector::Parameters</param>
    public void InitSimpleBlobDetector(SimpleBlobDetectorParams parameters)
    {
        _simpleBlobDetector = new SimpleBlobDetector(parameters);
    }

    /// <summary>
    /// This implements a simple algorithm for extracting blobs from an image:
    /// 
    /// * Convert the source image to binary images by applying thresholding with several thresholds from minThreshold(inclusive) to maxThreshold(exclusive) with distance thresholdStep between neighboring thresholds.
    /// 
    /// * Extract connected components from every binary image by findContours and calculate their centers.
    /// 
    /// * Group centers from several binary images by their coordinates.Close centers form one group that corresponds to one blob, which is controlled by the minDistBetweenBlobs parameter.
    /// 
    /// * From the groups, estimate final centers of blobs and their radiuses and return as locations and sizes of keypoints.
    /// 
    /// This performs several filtrations of returned blobs.You should set filterBy* to true/false to turn on/off corresponding filtration.Available filtrations:
    /// 
    /// * By color.This filter compares the intensity of a binary image at the center of a blob to blobColor. If they differ, the blob is filtered out. Use blobColor = 0 to extract dark blobs and blobColor = 255 to extract light blobs.
    /// 
    /// * By area. Extracted blobs have an area between minArea (inclusive) and maxArea (exclusive).
    /// 
    /// * By circularity. Extracted blobs have circularity ( 4 * pi  * Area / (perimeter * perimeter)) between minCircularity(inclusive) and maxCircularity(exclusive).
    /// 
    /// * By ratio of the minimum inertia to maximum inertia.Extracted blobs have this ratio between minInertiaRatio (inclusive) and maxInertiaRatio (exclusive).
    /// 
    /// * By convexity. Extracted blobs have convexity (area / area of blob convex hull) between minConvexity (inclusive) and maxConvexity(exclusive).
    /// 
    /// * Default values of parameters are tuned to extract dark circular blobs.
    /// 
    /// see: http://docs.opencv.org/3.2.0/d0/d7a/classcv_1_1SimpleBlobDetector.html
    /// </summary>
    /// <param name="src">OpenCV Source matrix</param>
    /// <param name="markerColor">Marker color</param>
    /// <returns>OpenCV matrix with blobs highlighted</returns>
    public Mat SimpleBlobDetectionMat(Mat src, Bgr markerColor)
    {
        if (src == null || _simpleBlobDetector == null)
            return null;

        MKeyPoint[] keypoints;

        try
        {
            #region convert to grayscale
            var img = src.ToImage<Bgr, byte>();
            var uimage = DownUpSample ? NoiseFilter(img) : ConvertToGray(img);
            #endregion

            #region invert gray colors
            if (Invert)
                GrayInvert(uimage);
            #endregion

            #region normalize
            if (Normalize)
                NormalizeGray(uimage);
            #endregion

            #region apply Gaussian blur
            var smoothedFrame = Blur ? GaussianBlur(uimage, sx, sy, sigmaX, sigmaY) : uimage.Clone();
            #endregion

            #region edge detection
            if (SubtractBackground)
            {

                #region detect background
                var foregroundMask = BackgroundSubtract(smoothedFrame);
                #endregion

                #region detect keypoints
                keypoints = _simpleBlobDetector.Detect(foregroundMask);
                #endregion

                #region cleanup
                Throw(foregroundMask);
                #endregion
            }
            else
            {
                #region detect keypoints
                keypoints = _simpleBlobDetector.Detect(smoothedFrame);
                #endregion
            }
            #endregion

            #region draw keypoints
            Features2DToolbox.DrawKeypoints(img, new VectorOfKeyPoint(keypoints), img, markerColor, Features2DToolbox.KeypointDrawType.DrawRichKeypoints);
            #endregion

            CvInvoke.CvtColor(img.Mat, nframe, ColorConversion.Bgr2Rgb);

            #region cleanup
            Throw(smoothedFrame, img, uimage);

            CollectGarbage();
            #endregion
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: {0}", e.Message);

            #region cleanup
            CollectGarbage();
            #endregion

            return src;
        }

        return nframe;
    }

    /// <summary>
    /// This implements a simple algorithm for extracting blobs from an image
    /// 
    /// see: http://docs.opencv.org/3.2.0/d0/d7a/classcv_1_1SimpleBlobDetector.html
    /// <param name="src">OpenCV Source matrix</param>
    /// <param name="markerColor">Marker color</param>
    /// <returns>OpenCV matrix with blobs highlighted</returns>
    /// </summary>
    public Pixbuf SimpleBlobDetection(Mat src, Bgr markerColor)
    {
        return ToPixbufNoCC(SimpleBlobDetectionMat(src, markerColor));
    }

#if _LINUX || _WIN32
    /// <summary>
    /// This implements a simple algorithm for extracting blobs from an image
    /// 
    /// see: http://docs.opencv.org/3.2.0/d0/d7a/classcv_1_1SimpleBlobDetector.html
    /// <param name="src">OpenCV Source matrix</param>
    /// <param name="markerColor">Marker color</param>
    /// <returns>Bitmap with blobs highlighted</returns>
    /// </summary>
    public Bitmap SimpleBlobDetectionBMP(Mat src, Bgr markerColor)
    {
        return ToBitmap(SimpleBlobDetectionMat(src, markerColor));
    }
#endif

    /// <summary>
    /// Reduces noise by downsampling and then upsampling
    /// 
    /// See http://docs.opencv.org/3.2.0/d4/d1f/tutorial_pyramids.html
    /// </summary>
    /// <param name="img">OpenCV image</param>
    public Mat NoiseFilter(Image<Bgr, byte> img)
    {
        var uimage = new Mat();

        #region convert to gray
        CvInvoke.CvtColor(img, uimage, ColorConversion.Bgr2Gray);
        #endregion

        #region use image pyr to remove noise
        var pyrDown = new Mat();

        // Downsampling
        CvInvoke.PyrDown(uimage, pyrDown);

        // Upsampling
        CvInvoke.PyrUp(pyrDown, uimage);

        #endregion

        #region cleanup
        Throw(pyrDown);

        CollectGarbage();
        #endregion

        return uimage;
    }

    /// <summary>
    /// Convert to gray scale matrix
    /// 
    /// See http://docs.opencv.org/3.2.0/d4/d1f/tutorial_pyramids.html
    /// </summary>
    /// <param name="img">OpenCV BGR image</param>
    /// <returns>OpenCV matrix</returns>
    public Mat ConvertToGray(Image<Bgr, byte> img)
    {
        var uimage = new Mat();

        CvInvoke.CvtColor(img, uimage, ColorConversion.Bgr2Gray);

        return uimage;
    }

    /// <summary>
    /// Subtract background from frame
    /// 
    /// See http://docs.opencv.org/3.2.0/db/d5c/tutorial_py_bg_subtraction.html
    /// </summary>
    /// <param name="frame">Source frame</param>
    /// <returns>OpenCV matrix with background subtracted</returns>
    protected Mat BackgroundSubtract(IOutputArray frame)
    {
        #region background subtraction
        var foregroundMask = new Mat();

        var _fgDetector = new BackgroundSubtractorMOG2();
        _fgDetector.Apply(frame, foregroundMask);
        #endregion

        #region cleanup
        Throw(_fgDetector);

        CollectGarbage();
        #endregion

        return foregroundMask;
    }

    /// <summary>
    /// Apply Gaussian blur to source frame
    /// 
    /// See http://docs.opencv.org/3.2.0/dc/dd3/tutorial_gausian_median_blur_bilateral_filter.html
    /// </summary>
    /// <param name="frame">Source frame</param>
    /// <param name="sx">Filter dimensions (x)</param>
    /// <param name="sy">Filter dimensions (y)</param>
    /// <param name="sigmaX">Standard deviation (x)</param>
    /// <param name="sigmaY">Standard deviation (y)</param>
    /// <returns>OpenCV matrix with Gaussian filter applied</returns>
    protected Mat GaussianBlur(Mat frame, int sx, int sy, double sigmaX, double sigmaY)
    {
        var smoothedFrame = new Mat();

        CvInvoke.GaussianBlur(frame, smoothedFrame, new System.Drawing.Size(sx, sy), sigmaX, sigmaY, BorderType.Default);

        return smoothedFrame;
    }

    /// <summary>
    /// Bitwise-Invert source frame 
    /// 
    /// See http://docs.opencv.org/3.2.0/d2/de8/group__core__array.html
    /// </summary>
    /// <param name="frame">OpenCV matrix</param>
    protected void GrayInvert(Mat frame)
    {
        CvInvoke.BitwiseNot(frame, frame);
    }

    /// <summary>
    /// Normalize matrix containing gray scale values
    /// 
    /// See http://docs.opencv.org/3.2.0/d8/dbc/tutorial_histogram_calculation.html
    /// </summary>
    /// <param name="frame">OpenCV matrix</param>
    protected void NormalizeGray(Mat frame)
    {
        var mat = new Mat();

        CvInvoke.Normalize(frame, mat, 0.0, 1.0, NormType.MinMax);

        frame.SetTo(new MCvScalar(255.0));

        CvInvoke.Multiply(mat, frame, frame);

        Throw(mat);
    }

    /// <summary>
    /// Most recent list of detected circles (using DetectCirclesMat)
    /// 
    /// </summary>
    /// <returns>Return list of circles</returns>
    public List<CircleF> Circles()
    {
        return recentCircles;
    }

    /// <summary>
    /// Most recent list of detected blobs (using BlobDetectorMat or DelectBlobsMat)
    /// 
    /// </summary>
    /// <returns>Return list of rectangles</returns>
    public List<System.Drawing.Rectangle> Blobs()
    {
        return recentRectangles;
    }

    /// <summary>
    /// Draw ellipses on an OpenCV matrix
    /// 
    /// see http://docs.opencv.org/3.2.0/dc/da5/tutorial_py_drawing_functions.html 
    /// </summary>
    /// <param name="src">OpenCV matrix</param>
    /// <param name="ellipses">List of ellipse coordinates</param>
    /// <param name="markerSize">marker size</param>
    /// <param name="markerColor">marker color in OpenCV BGR format</param>
    /// <param name="selected">index (+1) of selected ellipse</param>
    /// <param name="selectedColor">color of selected ellipse</param>
    /// <param name="filled">flag to draw filled-boxes</param>
    /// <param name="enabled">flag to override ellipse Enabled property</param>
    /// <param name="ScaleX">X-axis scale</param>
    /// <param name="ScaleY">Y-axis scale</param>
    /// <returns>OpenCV matrix with ellipses</returns>
    public Mat DrawEllipse(Mat src, List<Ellipse> ellipses, int markerSize, Gdk.Color markerColor, int selected, Gdk.Color selectedColor, bool filled = false, bool enabled = true, double ScaleX = 1.0, double ScaleY = 1.0)
    {
        var dest = src.Clone();

        try
        {
            if (ellipses.Count > 0)
            {
                for (var i = 0; i < ellipses.Count; i++)
                {
                    var ellipse = ellipses[i];
                    var a = Convert.ToInt32(ScaleX * (ellipse.Width - 1) / 2);
                    var b = Convert.ToInt32(ScaleY * (ellipse.Height - 1) / 2);
                    var center = new System.Drawing.Point(Convert.ToInt32(ScaleX * ellipse.X), Convert.ToInt32(ScaleY * ellipse.Y));
                    var size = new System.Drawing.Size(a, b);

                    var drawColor = (i + 1 == selected) ? BGR(selectedColor) : BGR(markerColor);

                    if (ellipse.Enabled || enabled)
                        CvInvoke.Ellipse(dest, center, size, ellipse.Rotation, 0, 360, drawColor, !filled ? markerSize : -1);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: {0}", e.Message);

            #region cleanup
            CollectGarbage();
            #endregion

            return src;
        }

        CollectGarbage();

        return dest;
    }

    /// <summary>
    /// Draw ellipses on an Pixbuf using OpenCV
    /// 
    /// see http://docs.opencv.org/3.2.0/dc/da5/tutorial_py_drawing_functions.html 
    /// </summary>
    /// <param name="src">Gdk Pixbuf</param>
    /// <param name="ellipses">List of ellipse coordinates</param>
    /// <param name="markerSize">marker size</param>
    /// <param name="markerColor">marker color in Gtk RGB format</param>
    /// <param name="selected">index (+1) of selected ellipse</param>
    /// <param name="selectedColor">color of selected ellipse</param>
    /// <param name="filled">flag to draw filled-boxes</param>
    /// <param name="enabled">flag to override ellipse Enabled property</param>
    /// <param name="ScaleX">X-axis scale</param>
    /// <param name="ScaleY">Y-axis scale</param>
    /// <returns>Gdk Pixbuf with ellipses</returns>
    public Pixbuf DrawEllipse(Pixbuf src, List<Ellipse> ellipses, int markerSize, Gdk.Color markerColor, int selected, Gdk.Color selectedColor, bool filled = false, bool enabled = true, double ScaleX = 1.0, double ScaleY = 1.0)
    {
        using (var mat = ToMat(src))
        {
            using (var ellipse = DrawEllipse(mat, ellipses, markerSize, markerColor, selected, selectedColor, filled, enabled, ScaleX, ScaleY))
            {
                return ToPixbuf(ellipse);
            }
        }
    }

    /// <summary>
    /// Draw boxes on an OpenCV matrix
    /// 
    /// see http://docs.opencv.org/3.2.0/dc/da5/tutorial_py_drawing_functions.html 
    /// </summary>
    /// <param name="src">OpenCV matrix</param>
    /// <param name="boxes">List of box coordinates</param>
    /// <param name="markerSize">marker size</param>
    /// <param name="markerColor">marker color in Gtk RGB format</param>
    /// <param name="selected">index (+1) of selected box</param>
    /// <param name="selectedColor">color of selected box</param>
    /// <param name="filled">flag to draw filled-boxes</param>
    /// <param name="enabled">flag to override ellipse Enabled property</param>
    /// <param name="ScaleX">X-axis scale</param>
    /// <param name="ScaleY">Y-axis scale</param>
    /// <returns>OpenCV matrix with boxes</returns>
    public Mat DrawBox(Mat src, List<Box> boxes, int markerSize, Gdk.Color markerColor, int selected, Gdk.Color selectedColor, bool filled = false, bool enabled = true, double ScaleX = 1.0, double ScaleY = 1.0)
    {
        var dest = src.Clone();

        try
        {
            if (boxes.Count > 0)
            {
                for (var i = 0; i < boxes.Count; i++)
                {
                    var box = boxes[i];

                    var x = Convert.ToInt32(Math.Min(box.X0, box.X1) * ScaleX);
                    var y = Convert.ToInt32(Math.Min(box.Y0, box.Y1) * ScaleY);
                    var w = Convert.ToInt32(Math.Abs(box.X1 - box.X0) * ScaleX);
                    var h = Convert.ToInt32(Math.Abs(box.Y1 - box.Y0) * ScaleY);

                    var rect = new System.Drawing.Rectangle(x, y, w, h);

                    var drawColor = (i + 1 == selected) ? BGR(selectedColor) : BGR(markerColor);

                    if (box.Enabled || enabled)
                        CvInvoke.Rectangle(dest, rect, drawColor, !filled ? markerSize : -1, LineType.AntiAlias);
                }
            }

            CollectGarbage();
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: {0}", e.Message);

            #region cleanup
            CollectGarbage();
            #endregion

            return src;
        }

        return dest;
    }

    /// <summary>
    /// Draw boxes on an Pixbuf using OpenCV
    /// 
    /// see http://docs.opencv.org/3.2.0/dc/da5/tutorial_py_drawing_functions.html 
    /// </summary>
    /// <param name="src">Gdk Pixbuf</param>
    /// <param name="boxes">List of box coordinates</param>
    /// <param name="markerSize">marker size</param>
    /// <param name="markerColor">marker color in Gtk RGB format</param>
    /// <param name="selected">index (+1) of selected box</param>
    /// <param name="selectedColor">color of selected box</param>
    /// <param name="filled">flag to draw filled-boxes</param>
    /// <param name="enabled">flag to override ellipse Enabled property</param>
    /// <param name="ScaleX">X-axis scale</param>
    /// <param name="ScaleY">Y-axis scale</param>
    /// <returns>Gdk Pixbuf with ellipses</returns>
    public Pixbuf DrawBox(Pixbuf src, List<Box> boxes, int markerSize, Gdk.Color markerColor, int selected, Gdk.Color selectedColor, bool filled = false, bool enabled = true, double ScaleX = 1.0, double ScaleY = 1.0)
    {
        using (var mat = ToMat(src))
        {
            using (var box = DrawBox(mat, boxes, markerSize, markerColor, selected, selectedColor, filled, enabled, ScaleX, ScaleY))
            {
                return ToPixbuf(box);
            }
        }
    }

    /// <summary>
    /// Draw cross and/or plus on an OpenCV Matrix
    /// 
    /// </summary>
    /// <param name="src">OpenCV matrix</param>
    /// <param name="cross">flag to draw cross 'x'</param>
    /// <param name="plus">flag to draw plus '+'</param>
    /// <param name="markerSize">line width</param>
    /// <param name="markerColor">marker color in Gtk RGB format</param>
    /// <returns>OpenCV matrix with cross and/or plus</returns>
    public Mat DrawCrossPlus(Mat src, bool cross, bool plus, int markerSize, Gdk.Color markerColor)
    {
        var dest = src.Clone();

        try
        {
            if (cross)
            {
                CvInvoke.Line(dest, new System.Drawing.Point(0, 0), new System.Drawing.Point(src.Width, src.Height), BGR(markerColor), markerSize);
                CvInvoke.Line(dest, new System.Drawing.Point(0, src.Height), new System.Drawing.Point(src.Width, 0), BGR(markerColor), markerSize);
            }

            if (plus)
            {
                CvInvoke.Line(dest, new System.Drawing.Point(src.Width / 2, 0), new System.Drawing.Point(src.Width / 2, src.Height), BGR(markerColor), markerSize);
                CvInvoke.Line(dest, new System.Drawing.Point(0, src.Height / 2), new System.Drawing.Point(src.Width, src.Height / 2), BGR(markerColor), markerSize);
            }

            CollectGarbage();
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: {0}", e.Message);

            #region cleanup
            CollectGarbage();
            #endregion

            return src;
        }

        return dest;
    }

    /// <summary>
    /// Draw cross and/or plus on a Gdk Pixbuf
    /// 
    /// </summary>
    /// <param name="src">OpenCV matrix</param>
    /// <param name="cross">flag to draw cross 'x'</param>
    /// <param name="plus">flag to draw plus '+'</param>
    /// <param name="markerSize">line width</param>
    /// <param name="markerColor">marker color in Gtk RGB format</param>
    /// <returns>Gdk Pixbuf with cross and/or plus</returns>
    public Pixbuf DrawCrossPlus(Pixbuf src, bool cross, bool plus, int markerSize, Gdk.Color markerColor)
    {
        using (var mat = ToMat(src))
        {
            using (var box = DrawCrossPlus(mat, cross, plus, markerSize, markerColor))
            {
                return ToPixbuf(box);
            }
        }
    }

    /// <summary>
    /// Draw grating on an OpenCV Matrix
    /// 
    /// </summary>
    /// <param name="src">OpenCV matrix</param>
    /// <param name="periodX">grating period in X</param>
    /// <param name="periodY">grating period in Y</param>
    /// <param name="fillX">grating X fill factor</param>
    /// <param name="fillY">grating Y fill factor</param>
    /// <param name="TL">flag if top-left box is drawn</param>
    /// <param name="TR">flag if top-right box is drawn</param>
    /// <param name="BL">flag if bottom-left box is drawn</param>
    /// <param name="BR">flag if bottom-right box is drawn</param>
    /// <param name="markerColor">marker color in Gtk RGB format</param>
    /// <returns>OpenCV matrix with grating</returns>
    public Mat DrawGrating(Mat src, int periodX, int periodY, int fillX, int fillY, bool TL, bool TR, bool BL, bool BR, Gdk.Color markerColor)
    {
        var dest = src.Clone();

        try
        {
            for (var y = 0; y < src.Height; y += periodY)
            {
                for (var x = 0; x < src.Width; x += periodX)
                {
                    if (TL)
                        CvInvoke.Rectangle(dest, new System.Drawing.Rectangle(x, y, periodX * fillX / 100, periodY * fillY / 100), BGR(markerColor), -1, LineType.AntiAlias);

                    if (TR)
                        CvInvoke.Rectangle(dest, new System.Drawing.Rectangle(x + periodX * fillX / 100, y, periodX * (100 - fillX) / 100, periodY * fillY / 100), BGR(markerColor), -1, LineType.AntiAlias);

                    if (BL)
                        CvInvoke.Rectangle(dest, new System.Drawing.Rectangle(x, y + periodY * fillY / 100, periodX * fillX / 100, periodY * (100 - fillY) / 100), BGR(markerColor), -1, LineType.AntiAlias);

                    if (BR)
                        CvInvoke.Rectangle(dest, new System.Drawing.Rectangle(x + periodX * fillX / 100, y + periodY * fillY / 100, periodX * (100 - fillX) / 100, periodY * (100 - fillY) / 100), BGR(markerColor), -1, LineType.AntiAlias);
                }
            }

            CollectGarbage();
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: {0}", e.Message);

            #region cleanup
            CollectGarbage();
            #endregion

            return src;
        }

        return dest;
    }

    /// <summary>
    /// Draw grating on a Gdk Pixbuf
    /// 
    /// </summary>
    /// <param name="src">OpenCV matrix</param>
    /// <param name="periodX">grating period in X</param>
    /// <param name="periodY">grating period in Y</param>
    /// <param name="fillX">grating X fill factor</param>
    /// <param name="fillY">grating Y fill factor</param>
    /// <param name="TL">flag if top-left box is drawn</param>
    /// <param name="TR">flag if top-right box is drawn</param>
    /// <param name="BL">flag if bottom-left box is drawn</param>
    /// <param name="BR">flag if bottom-right box is drawn</param>
    /// <param name="markerColor">marker color in Gtk RGB format</param>
    /// <returns>Gtk Pixbuf with grating</returns>
    public Pixbuf DrawGrating(Pixbuf src, int periodX, int periodY, int fillX, int fillY, bool TL, bool TR, bool BL, bool BR, Gdk.Color markerColor)
    {
        using (var mat = ToMat(src))
        {
            using (var box = DrawGrating(mat, periodX, periodY, fillX, fillY, TL, TR, BL, BR, markerColor))
            {
                return ToPixbuf(box);
            }
        }
    }

    /// <summary>
    /// Draw concentric rings on an OpenCV matrix
    /// 
    /// see http://docs.opencv.org/3.2.0/dc/da5/tutorial_py_drawing_functions.html 
    /// </summary>
    /// <param name="src">OpenCV matrix</param>
    /// <param name="diameter">Ring diameter</param>
    /// <param name="aspectRatio">Ring aspect ratio</param>
    /// <param name="markerSize">marker size</param>
    /// <param name="markerColor">color in Gdk RGB format</param>
    /// <param name="fill">flag to draw filled-rings</param>
    /// <param name="rings">number of rings to draw</param>
    /// <param name="period">gap between rings</param>
    /// <returns>OpenCV matrix with concentric rings</returns>
    public Mat DrawRing(Mat src, int diameter, double aspectRatio, int markerSize, Gdk.Color markerColor, bool fill, int rings = 1, int period = 0)
    {
        var dest = src.Clone();

        try
        {
            var ringDiameter = diameter;

            for (var ring = 0; ring < rings; ring++)
            {
                var minor = Convert.ToInt32(ringDiameter / aspectRatio);
                var center = new System.Drawing.Point(dest.Width / 2, dest.Height / 2);
                var size = new System.Drawing.Size(ringDiameter / 2, minor / 2);

                CvInvoke.Ellipse(dest, center, size, 0, 0, 360, BGR(markerColor), !fill ? markerSize : -1);

                ringDiameter += period;
            }

            CollectGarbage();
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: {0}", e.Message);

            #region cleanup
            CollectGarbage();
            #endregion

            return src;
        }

        return dest;
    }

    /// <summary>
    /// Draw concentric rings on Gdk pixbuf
    /// 
    /// see http://docs.opencv.org/3.2.0/dc/da5/tutorial_py_drawing_functions.html 
    /// </summary>
    /// <param name="src">OpenCV matrix</param>
    /// <param name="diameter">Ring diameter</param>
    /// <param name="aspectRatio">Ring aspect ratio</param>
    /// <param name="markerSize">marker size</param>
    /// <param name="markerColor">color in Gdk RGB format</param>
    /// <param name="fill">flag to draw filled-rings</param>
    /// <param name="rings">number of rings to draw</param>
    /// <param name="period">gap between rings</param>
    /// <returns>Gdk Pixbuf with concentric rings</returns>
    public Pixbuf DrawRing(Pixbuf src, int diameter, double aspectRatio, int markerSize, Gdk.Color markerColor, bool fill, int rings = 1, int period = 0)
    {
        using (var mat = ToMat(src))
        {
            using (var ring = DrawRing(mat, diameter, aspectRatio, markerSize, markerColor, fill, rings, period))
            {
                return ToPixbuf(ring);
            }
        }
    }

    /// <summary>
    /// Draw concentric boxes on an OpenCV matrix
    /// 
    /// see http://docs.opencv.org/3.2.0/dc/da5/tutorial_py_drawing_functions.html 
    /// </summary>
    /// <param name="src">OpenCV matrix</param>
    /// <param name="width">Box width</param>
    /// <param name="aspectRatio">box aspect ratio</param>
    /// <param name="markerSize">marker size</param>
    /// <param name="markerColor">color in Gdk RGB format</param>
    /// <param name="fill">flag to draw filled-boxes</param>
    /// <param name="boxes">number of boxes to draw</param>
    /// <param name="period">gap between boxes</param>
    /// <returns>OpenCV matrix with concentric boxes</returns>
    public Mat DrawBox(Mat src, int width, double aspectRatio, int markerSize, Gdk.Color markerColor, bool fill, int boxes = 1, int period = 0)
    {
        var dest = src.Clone();

        try
        {
            var boxWidth = width;

            for (var box = 0; box < boxes; box++)
            {
                var height = Convert.ToInt32(boxWidth / aspectRatio);

                var x = (dest.Width - boxWidth) / 2;
                var y = (dest.Height - height) / 2;

                var rect = new System.Drawing.Rectangle(x, y, boxWidth, height);

                CvInvoke.Rectangle(dest, rect, BGR(markerColor), !fill ? markerSize : -1, LineType.AntiAlias);

                boxWidth += period;
            }

            CollectGarbage();
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: {0}", e.Message);

            #region cleanup
            CollectGarbage();
            #endregion

            return src;
        }

        return dest;
    }

    /// <summary>
    /// Draw concentric boxes on a Gdk Pixbuf
    /// 
    /// see http://docs.opencv.org/3.2.0/dc/da5/tutorial_py_drawing_functions.html 
    /// </summary>
    /// <param name="src">OpenCV matrix</param>
    /// <param name="width">Box width</param>
    /// <param name="aspectRatio">box aspect ratio</param>
    /// <param name="markerSize">marker size</param>
    /// <param name="markerColor">color in Gdk RGB format</param>
    /// <param name="fill">flag to draw filled-boxes</param>
    /// <param name="boxes">number of boxes to draw</param>
    /// <param name="period">gap between boxes</param>
    /// <returns>Gdk Pixbuf with concentric boxes</returns>
    public Pixbuf DrawBox(Pixbuf src, int width, double aspectRatio, int markerSize, Gdk.Color markerColor, bool fill, int boxes = 1, int period = 0)
    {
        using (var mat = ToMat(src))
        {
            using (var ring = DrawBox(mat, width, aspectRatio, markerSize, markerColor, fill, boxes, period))
            {
                return ToPixbuf(ring);
            }
        }
    }

    /// <summary>
    /// Flip OpenCV matrix
    /// 
    /// see http://docs.opencv.org/3.2.0/d2/de8/group__core__array.html
    ///</summary>
    /// <param name="src">OpenCV source matrix</param>
    /// <param name="flipCode">flip direction</param>
    /// <returns>Flipped source frame</returns>
    public Mat Flip(Mat src, FlipType flipCode)
    {
        try
        {
            var flipped = new Mat();

            CvInvoke.Flip(src, flipped, flipCode);
            CvInvoke.CvtColor(flipped, nframe, ColorConversion.Bgr2Rgb);

            CollectGarbage();
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: {0}", e.Message);

            #region cleanup
            CollectGarbage();
            #endregion

            return src;
        }

        return nframe;
    }

    /// <summary>
    /// Flip Gdk Pixbuf
    /// 
    /// see http://docs.opencv.org/3.2.0/d2/de8/group__core__array.html
    /// </summary>
    /// <param name="src">Gdk pixbuf</param>
    /// <param name="flipCode">flip direction</param>
    /// <returns>Flipped source frame</returns>
    public Pixbuf Flip(Pixbuf src, FlipType flipCode)
    {
        return ToPixbuf(Flip(ToMat(src), flipCode));
    }

    /// <summary>
    /// Multi-parameter object disposal function
    /// 
    /// <param name="trash">disposable items</param>
    /// </summary>
    public void Throw(params IDisposable[] trash)
    {
        foreach (var item in trash)
        {
            if (item != null)
                item.Dispose();
        }
    }

    /// <summary>
    /// Force garbage colleciton
    /// 
    /// </summary>
    void CollectGarbage()
    {
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
    }

    /// <summary>
    /// This class needs to implement Dispose()
    /// 
    /// </summary>
    public void Dispose()
    {
        Throw(nframe, _simpleBlobDetector, _blobDetector);

        CollectGarbage();
    }

    /// <summary>
    /// Detects blobs using Canny edge detection and contour detection.
    /// The smallest value between threshold1 and threshold2 is used for edge linking. The largest value is used to find initial segments of strong edges.
    /// 
    /// Saves coordinates of detected blobs in provided list
    /// </summary>
    /// <param name="src">Source OpenCV matrix</param>
    /// <param name="cannyThreshold">Threshold to find initial segments of strong edges</param>
    /// <param name="cannyThresholdLinking">Threshold for edge linking</param>
    /// <param name="minArea">Threshold for minimum area</param>
    /// <param name="maxArea">Threshold for maximum area</param>
    /// <param name="selection">List of regions</param>
    /// <param name="ScaleX">X-axis scaling</param>
    /// <param name="ScaleY">Y-axis scaling</param>
    public void DetectBlobsMat(Mat src, double cannyThreshold, double cannyThresholdLinking, double minArea, double maxArea, Select selection, double ScaleX = 1.0, double ScaleY = 1.0)
    {
        selection.Clear();

        try
        {
            #region convert to gray scale
            var img = src.ToImage<Bgr, byte>();
            var uimage = DownUpSample ? NoiseFilter(img) : ConvertToGray(img);
            #endregion

            #region invert gray colors
            if (Invert)
                GrayInvert(uimage);
            #endregion

            #region normalize
            if (Normalize)
                NormalizeGray(uimage);
            #endregion

            #region apply Gaussian blur
            var smoothedFrame = Blur ? GaussianBlur(uimage, sx, sy, sigmaX, sigmaY) : uimage.Clone();
            #endregion

            #region edge detection
            Mat cannyEdges = new Mat();

            if (SubtractBackground)
            {
                #region detect background
                var foregroundMask = BackgroundSubtract(smoothedFrame);
                #endregion

                CvInvoke.Canny(foregroundMask, cannyEdges, cannyThreshold, cannyThresholdLinking);

                #region cleanup
                Throw(foregroundMask);
                #endregion
            }
            else
            {
                CvInvoke.Canny(smoothedFrame, cannyEdges, cannyThreshold, cannyThresholdLinking);
            }
            #endregion

            #region find contours
            var contours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(cannyEdges, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
            #endregion

            #region plot contours
            if (contours != null && contours.Size > 0)
            {
                for (int i = 0; i < contours.Size; i++)
                {
                    #region polygon approximation
                    var contour = contours[i];
                    var approxContour = new VectorOfPoint();
                    var length = CvInvoke.ArcLength(contour, true) * 0.015;

                    CvInvoke.ApproxPolyDP(contour, approxContour, length, true);
                    #endregion

                    #region filter by area
                    var area = CvInvoke.ContourArea(approxContour, false);

                    if (area >= minArea && area < maxArea) //only consider contours with areas greather than this
                    {
                        var rectangle = CvInvoke.BoundingRectangle(approxContour);

                        var X0 = Convert.ToInt32(ScaleX * rectangle.X);
                        var Y0 = Convert.ToInt32(ScaleY * rectangle.Y);
                        var X1 = Convert.ToInt32(ScaleX * (rectangle.X + rectangle.Width - 1));
                        var Y1 = Convert.ToInt32(ScaleY * (rectangle.Y + rectangle.Height - 1));

                        selection.Add(X0, Y0, X1, Y1);
                    }
                    #endregion
                }
            }
            #endregion

            #region cleanup
            Throw(cannyEdges, contours, smoothedFrame, img, uimage);

            CollectGarbage();
            #endregion
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: {0}", e.Message);

            #region cleanup
            CollectGarbage();
            #endregion
        }
    }

    /// <summary>
    /// Detects blobs using Gaussian blurring filter and background subttraction.
    /// 
    /// Saves coordinates of detected blobs in provided list
    /// </summary>
    /// <param name="src">Source OpenCV matrix</param>
    /// <param name="minArea">Threshold for minimum area</param>
    /// <param name="maxArea">Threshold for maximum area</param>
    /// <param name="selection">List of regions</param>
    /// <param name="ScaleX">X-axis scaling</param>
    /// <param name="ScaleY">Y-axis scaling</param>
    public void BlobDetectorMat(Mat src, int minArea, int maxArea, Select selection, double ScaleX = 1.0, double ScaleY = 1.0)
    {
        if (src == null)
            return;

        selection.Clear();

        try
        {
            #region convert to grayscale
            var img = src.ToImage<Bgr, byte>();
            var uimage = DownUpSample ? NoiseFilter(img) : ConvertToGray(img);
            #endregion

            #region invert gray colors
            if (Invert)
                GrayInvert(uimage);
            #endregion

            #region normalize
            if (Normalize)
                NormalizeGray(uimage);
            #endregion

            #region apply Gaussian blur
            var smoothedFrame = Blur ? GaussianBlur(uimage, sx, sy, sigmaX, sigmaY) : uimage.Clone();
            #endregion

            #region detect blobs
            var blobs = new CvBlobs();

            if (SubtractBackground)
            {
                #region detect background
                var foregroundMask = BackgroundSubtract(smoothedFrame);
                #endregion

                _blobDetector.Detect(foregroundMask.ToImage<Gray, byte>(), blobs);

                #region cleanup
                Throw(foregroundMask);
                #endregion
            }
            else
            {
                _blobDetector.Detect(smoothedFrame.ToImage<Gray, byte>(), blobs);
            }

            blobs.FilterByArea(minArea, maxArea);
            #endregion

            foreach (var pair in blobs)
            {
                var rectangle = pair.Value.BoundingBox;

                var X0 = Convert.ToInt32(ScaleX * rectangle.X);
                var Y0 = Convert.ToInt32(ScaleY * rectangle.Y);
                var X1 = Convert.ToInt32(ScaleX * (rectangle.X + rectangle.Width - 1));
                var Y1 = Convert.ToInt32(ScaleY * (rectangle.Y + rectangle.Height - 1));

                selection.Add(X0, Y0, X1, Y1);
            }

            #region cleanup
            Throw(img, uimage, blobs, smoothedFrame);

            CollectGarbage();
            #endregion
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: {0}", e.Message);

            #region cleanup
            CollectGarbage();
            #endregion
        }
    }

    /// <summary>
    /// This implements a simple algorithm for extracting blobs from an image:
    /// 
    /// * Convert the source image to binary images by applying thresholding with several thresholds from minThreshold(inclusive) to maxThreshold(exclusive) with distance thresholdStep between neighboring thresholds.
    /// 
    /// * Extract connected components from every binary image by findContours and calculate their centers.
    /// 
    /// * Group centers from several binary images by their coordinates.Close centers form one group that corresponds to one blob, which is controlled by the minDistBetweenBlobs parameter.
    /// 
    /// * From the groups, estimate final centers of blobs and their radiuses and return as locations and sizes of keypoints.
    /// 
    /// This performs several filtrations of returned blobs.You should set filterBy* to true/false to turn on/off corresponding filtration.Available filtrations:
    /// 
    /// * By color.This filter compares the intensity of a binary image at the center of a blob to blobColor. If they differ, the blob is filtered out. Use blobColor = 0 to extract dark blobs and blobColor = 255 to extract light blobs.
    /// 
    /// * By area. Extracted blobs have an area between minArea (inclusive) and maxArea (exclusive).
    /// 
    /// * By circularity. Extracted blobs have circularity ( 4 * pi  * Area / (perimeter * perimeter)) between minCircularity(inclusive) and maxCircularity(exclusive).
    /// 
    /// * By ratio of the minimum inertia to maximum inertia.Extracted blobs have this ratio between minInertiaRatio (inclusive) and maxInertiaRatio (exclusive).
    /// 
    /// * By convexity. Extracted blobs have convexity (area / area of blob convex hull) between minConvexity (inclusive) and maxConvexity(exclusive).
    /// 
    /// * Default values of parameters are tuned to extract dark circular blobs.
    /// 
    /// see: http://docs.opencv.org/3.2.0/d0/d7a/classcv_1_1SimpleBlobDetector.html
    /// 
    /// Saves coordinates of detected blobs in provided list
    /// </summary>
    /// <param name="src">OpenCV Source matrix</param>
    /// <param name="selection">List of regions</param>
    /// <param name="ScaleX">X-axis scaling</param>
    /// <param name="ScaleY">Y-axis scaling</param>
    public void SimpleBlobDetectionMat(Mat src, Select selection, double ScaleX = 1.0, double ScaleY = 1.0)
    {
        if (src == null || _simpleBlobDetector == null)
            return;

        selection.Clear();

        MKeyPoint[] keypoints;

        try
        {
            #region convert to grayscale
            var img = src.ToImage<Bgr, byte>();
            var uimage = DownUpSample ? NoiseFilter(img) : ConvertToGray(img);
            #endregion

            #region invert gray colors
            if (Invert)
                GrayInvert(uimage);
            #endregion

            #region normalize
            if (Normalize)
                NormalizeGray(uimage);
            #endregion

            #region apply Gaussian blur
            var smoothedFrame = Blur ? GaussianBlur(uimage, sx, sy, sigmaX, sigmaY) : uimage.Clone();
            #endregion

            #region edge detection
            if (SubtractBackground)
            {
                #region detect background
                var foregroundMask = BackgroundSubtract(smoothedFrame);
                #endregion

                #region detect keypoints
                keypoints = _simpleBlobDetector.Detect(foregroundMask);
                #endregion

                #region cleanup
                Throw(foregroundMask);
                #endregion
            }
            else
            {
                #region detect keypoints
                keypoints = _simpleBlobDetector.Detect(smoothedFrame);
                #endregion
            }
            #endregion

            #region draw keypoints

            foreach (var keypoint in keypoints)
            {
                var X0 = Convert.ToInt32(ScaleX * (keypoint.Point.X - keypoint.Size));
                var Y0 = Convert.ToInt32(ScaleX * (keypoint.Point.Y - keypoint.Size));
                var X1 = Convert.ToInt32(ScaleX * (keypoint.Point.X + keypoint.Size));
                var Y1 = Convert.ToInt32(ScaleY * (keypoint.Point.Y + keypoint.Size));

                selection.Add(X0, Y0, X1, Y1);
            }

            #endregion

            #region cleanup
            Throw(smoothedFrame, img, uimage);

            CollectGarbage();
            #endregion
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: {0}", e.Message);

            #region cleanup
            CollectGarbage();
            #endregion
        }
    }

    /// <summary>
    /// Detects circles on an OpenCV matrix using Hough Circles Transform.
    /// 
    /// Saves coordinates of detected blobs in provided list
    /// </summary>
    /// <param name="src">Source OpenCV matrix</param>
    /// <param name="dp">Inverse ratio of the accumulator resolution to the image resolution. For example, if dp=1 , the accumulator has the same resolution as the input image. If dp=2 , the accumulator has half as big width and heigh</param>
    /// <param name="minDist">Minimum distance between the centers of the detected circles. If the parameter is too small, multiple neighbor circles may be falsely detected in addition to a true one. If it is too large, some circles may be missed.</param>
    /// <param name="cannyThreshold">Threshold for the hysteresis procedure</param>
    /// <param name="circleAccumulatorThreshold">accumulator threshold for the circle centers at the detection stage. The smaller it is, the more false circles may be detected. Circles, corresponding to the larger accumulator values, will be returned first.</param>
    /// <param name="minRadius">Minimum circle radius</param>
    /// <param name="maxRadius">Maximum circle radius</param>
    /// <param name="selection">List of regions</param>
    /// <param name="ScaleX">X-axis scaling</param>
    /// <param name="ScaleY">Y-axis scaling</param>
    public void DetectCirclesMat(Mat src, double dp, double minDist, double cannyThreshold, double circleAccumulatorThreshold, int minRadius, int maxRadius, Select selection, double ScaleX = 1.0, double ScaleY = 1.0)
    {
        if (src == null)
            return;

        selection.Clear();

        try
        {
            #region convert the image to grayscale
            var img = src.ToImage<Bgr, byte>();
            var uimage = DownUpSample ? NoiseFilter(img) : ConvertToGray(img);
            #endregion

            #region invert gray colors
            if (Invert)
                GrayInvert(uimage);
            #endregion

            #region normalize
            if (Normalize)
                NormalizeGray(uimage);
            #endregion

            #region apply Gaussian blur
            var smoothedFrame = Blur ? GaussianBlur(uimage, sx, sy, sigmaX, sigmaY) : uimage.Clone();
            #endregion

            CircleF[] circles;

            #region edge detection and Hough circle transform
            if (SubtractBackground)
            {
                #region detect background
                var foregroundMask = BackgroundSubtract(smoothedFrame);
                #endregion

                circles = CvInvoke.HoughCircles(foregroundMask, HoughType.Gradient, dp, minDist, cannyThreshold, circleAccumulatorThreshold, minRadius, maxRadius);

                #region cleanup
                Throw(foregroundMask);
                #endregion
            }
            else
            {
                circles = CvInvoke.HoughCircles(smoothedFrame, HoughType.Gradient, dp, minDist, cannyThreshold, circleAccumulatorThreshold, minRadius, maxRadius);
            }
            #endregion

            #region draw circles

            if (circles.Length > 0)
            {
                foreach (CircleF circle in circles)
                {
                    var X0 = Convert.ToInt32(ScaleX * (circle.Center.X - circle.Radius));
                    var Y0 = Convert.ToInt32(ScaleY * (circle.Center.Y - circle.Radius));
                    var X1 = Convert.ToInt32(ScaleX * (circle.Center.X + circle.Radius));
                    var Y1 = Convert.ToInt32(ScaleY * (circle.Center.Y + circle.Radius));

                    selection.Add(X0, Y0, X1, Y1);
                }
            }
            #endregion

            #region cleanup
            Throw(img, uimage, smoothedFrame);

            CollectGarbage();
            #endregion
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: {0}", e.Message);

            #region cleanup
            CollectGarbage();
            #endregion
        }
    }

    /// <summary>
    /// Detect objects based on HAAR Cascade filters
    /// 
    /// Saves coordinates of detected blobs in provided list
    /// </summary>
    /// 
    /// <param name="src">Source OpenCV matrix</param>
    /// <param name="Classifier">File path of HAAR Cascade classifier (XML format)</param>
    /// <param name="scaleFactor">Amount of image size reduction at each image scale</param>
    /// <param name="minSize">Minimum object size to consider</param>
    /// <param name="minNeighbors">Minimum number of neighboring rectangles for an object to be considered as detected</param>
    /// <param name="selection">List of regions</param>
    /// <param name="ScaleX">X-axis scaling</param>
    /// <param name="ScaleY">Y-axis scaling</param>
    public void DetectHaarMat(Mat src, string Classifier, double scaleFactor, int minSize, int minNeighbors, Select selection, double ScaleX, double ScaleY)
    {
        if (src == null)
            return;

        selection.Clear();

        if (File.Exists(Classifier) && scaleFactor > 1.0)
        {
            try
            {
                #region convert the image to grayscale
                var img = src.ToImage<Bgr, byte>();
                var uimage = DownUpSample ? NoiseFilter(img) : ConvertToGray(img);
                #endregion

                #region invert gray colors
                if (Invert)
                    GrayInvert(uimage);
                #endregion

                #region normalize
                if (Normalize)
                    NormalizeGray(uimage);
                #endregion

                #region apply Gaussian blur
                var smoothedFrame = Blur ? GaussianBlur(uimage, sx, sy, sigmaX, sigmaY) : uimage.Clone();
                #endregion

                var _cascadeClassifier = new CascadeClassifier(Classifier);
                var objects = _cascadeClassifier.DetectMultiScale(smoothedFrame, scaleFactor, minNeighbors, new System.Drawing.Size(minSize, minSize));

                if (objects.Length > 0)
                {
                    foreach (var obj in objects)
                    {
                        var X0 = Convert.ToInt32(ScaleX * obj.X);
                        var Y0 = Convert.ToInt32(ScaleY * obj.Y);
                        var X1 = Convert.ToInt32(ScaleX * (obj.X + obj.Width - 1));
                        var Y1 = Convert.ToInt32(ScaleY * (obj.Y + obj.Height - 1));

                        selection.Add(X0, Y0, X1, Y1);
                    }
                }

                #region cleanup
                Throw(img, uimage, smoothedFrame);

                CollectGarbage();
                #endregion
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);

                #region cleanup
                CollectGarbage();
                #endregion
            }
        }
    }

    /// <summary>
    /// Detect objects based on Deformable Parts Model
    /// 
    /// Saves coordinates of detected blobs in provided list
    /// </summary>
    /// 
    /// <param name="src">Source OpenCV matrix</param>
    /// <param name="DeformablePartsModelFile">File path of Deformable Parts Model (XML format)</param>
    /// <param name="DeformablePartsModelThreshold">Detection threshold</param>
    /// <param name="selection">List of regions</param>
    /// <param name="Clear">Refresh region list</param>
    /// <param name="ScaleX">X-axis scaling</param>
    /// <param name="ScaleY">Y-axis scaling</param>
    public void DeformablePartsModel(Mat src, string DeformablePartsModelFile, double DeformablePartsModelThreshold, Select selection, bool Clear, double ScaleX, double ScaleY)
    {
        if (src == null)
            return;

        if (File.Exists(DeformablePartsModelFile))
        {
            try
            {
                #region convert the image to grayscale
                var img = src.ToImage<Bgr, byte>();
                var uimage = ConvertToGray(img);
                #endregion

                var detector = new DpmDetector(new string[] { DeformablePartsModelFile });

                if (!detector.IsEmpty)
                {
                    var objects = detector.Detect(uimage);

                    if (Clear)
                        selection.Clear();

                    if (objects.Length > 0)
                    {
                        foreach (var obj in objects)
                        {
                            if (obj.Score >= DeformablePartsModelThreshold)
                            {
                                var X0 = Convert.ToInt32(ScaleX * (obj.Rect.Left));
                                var Y0 = Convert.ToInt32(ScaleY * (obj.Rect.Top));
                                var X1 = Convert.ToInt32(ScaleX * (obj.Rect.Left + obj.Rect.Width - 1));
                                var Y1 = Convert.ToInt32(ScaleY * (obj.Rect.Top + obj.Rect.Height - 1));

                                selection.Add(X0, Y0, X1, Y1, Convert.ToDouble(obj.Score), detector.ClassNames[obj.ClassId]);
                            }
                        }
                    }
                }

                #region cleanup
                Throw(img, uimage);

                CollectGarbage();
                #endregion
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);

                #region cleanup
                CollectGarbage();
                #endregion
            }
        }
    }
}
