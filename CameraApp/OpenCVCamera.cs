using Emgu.CV;
using Emgu.CV.CvEnum;
using Gdk;
using System;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;

class OpenCVCamera : VirtualCamera, IDisposable
{
    VideoCapture camera;
    int width;
    int height;
    Mat frame = new Mat();
    Mat nframe = new Mat();

    bool FlipH;
    bool FlipV;

    Stopwatch stopWatch = new Stopwatch();
    bool streaming;

    public OpenCVCamera()
    {
        InitalizeCamera(0);
    }

    public OpenCVCamera(int deviceNumber)
    {
        InitalizeCamera(deviceNumber);
    }

    public OpenCVCamera(string videoFile)
    {
        try
        {
            camera = new VideoCapture(videoFile);
            type = "opencv";
            name = "OpenCV Camera (Video File)";

            SetProperties();
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception {0}", e.Message);
        }

        streaming = false;
    }

    void InitalizeCamera(int deviceNumber)
    {
        try
        {
            camera = new VideoCapture(deviceNumber);
            type = "opencv";
            name = "OpenCV Camera";

            SetProperties();
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception {0}", e.Message);
        }

        streaming = false;
    }

    void SetProperties()
    {
        // set to HD resolution
        //camera.SetCaptureProperty(CapProp.FrameHeight, 720);
        //camera.SetCaptureProperty(CapProp.FrameWidth, 1280);

        camera.Read(frame);

        // ..then recover actual resolution
        height = frame.Height;
        width = frame.Width;

        Console.WriteLine("OpenCVCamera: Initialized");
    }

    void OnNewFrame(object sender, EventArgs eventArgs)
    {
        try
        {
            if (!stopWatch.IsRunning || stopWatch.ElapsedMilliseconds > 33)
            {
                camera.Read(frame);

                if (FlipH)
                    CvInvoke.Flip(frame, frame, FlipType.Horizontal);

                if (FlipV)
                    CvInvoke.Flip(frame, frame, FlipType.Vertical);

                stopWatch.Restart();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception {0}", e.Message);
        }

        CollectGarbage();
    }

    override public string Label()
    {
        return name;
    }

    override public string Type()
    {
        return type;
    }

    override public bool IsOpen()
    {
        return camera != null && camera.IsOpened;
    }

    override public void Open()
    {
        if (camera == null) return;
    }

    override public void Close()
    {
        if (camera != null && !camera.IsOpened)
            camera.Stop();
    }

    override public int Width()
    {
        return camera != null ? width : 0;
    }

    override public int Height()
    {
        return camera != null ? height : 0;
    }

    override public Bitmap Bitmap()
    {
        try
        {
            return frame.Bitmap;
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: {0}", e.Message);
            return null;
        }
    }

    override public Pixbuf Pixbuf()
    {
        if (frame == null)
            return null;

        try
        {
            CvInvoke.CvtColor(frame, nframe, ColorConversion.Bgr2Rgb);
        }
        catch (Exception e)
        {
            CollectGarbage();

            Console.WriteLine("Error: {0}", e.Message);

            return CopyToPixbuf(frame, false);
        }

        CollectGarbage();

        return CopyToPixbuf(nframe, false);
    }

    override public Mat Mat()
    {
        if (frame == null)
            return null;

        return frame;
    }

    override public void Dispose()
    {
        if (camera == null)
            return;

        if (camera.IsOpened && streaming)
        {
            //Stop camera and wait for 3 seconds
            Stop();

            stopWatch.Reset();

            while (stopWatch.ElapsedMilliseconds < 3000) { }
        }

        Throw(frame, camera);

        camera = null;

        CollectGarbage();

        Console.WriteLine("OpenCV: Disposed");
    }

    override public void Start()
    {
        if (camera == null || !camera.IsOpened) return;

        streaming = true;

        camera.ImageGrabbed += OnNewFrame;

        camera.Start();

        Console.WriteLine("Camera Started");

    }

    override public void Stop()
    {
        if (camera == null || !camera.IsOpened) return;

        stopWatch.Reset();

        streaming = false;

        camera.Stop();

        camera.ImageGrabbed -= OnNewFrame;

        Console.WriteLine("Camera Stopped");
    }

    override public bool IsStreaming()
    {
        return streaming;
    }

    public void Restart()
    {
        if (camera == null || !camera.IsOpened || streaming) return;

        camera.SetCaptureProperty(CapProp.PosFrames, 0);
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

    public void FlipX(bool flip = false)
    {
        FlipH = flip;
    }

    public void FlipY(bool flip = false)
    {
        FlipV = flip;
    }

    void Throw(params IDisposable[] trash)
    {
        foreach (var item in trash)
        {
            if (item != null)
                item.Dispose();
        }
    }

    void CollectGarbage()
    {
        System.GC.Collect();
    }
}
