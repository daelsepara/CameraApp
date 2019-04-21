using Emgu.CV;
using Gdk;
using System.Drawing;

abstract public class VirtualCamera
{
    protected string type = "virtual";
    protected string name = "virtual camera";

    abstract public string Label();

    abstract public string Type();

    abstract public bool IsOpen();

    abstract public void Open();

    abstract public void Close();

    abstract public int Width();

    abstract public int Height();

    abstract public Bitmap Bitmap();

    abstract public Pixbuf Pixbuf();

    abstract public Mat Mat();

    abstract public void Dispose();

    abstract public void Start();

    abstract public void Stop();

    abstract public bool IsStreaming();
}
