using Gdk;
using GLib;
using Gtk;
using System;
using System.IO;
using System.Threading;

public partial class MainWindow : Gtk.Window
{
    bool IsSelecting;
    bool IsDragging;

    int X0, Y0, X1, Y1;
    int prevX, prevY;
    Pixbuf OriginalImage;

    VirtualCamera camera;
    int camWidth;
    int camHeight;

    bool editEnabled;

    Gtk.Window patternWindow;

    FileChooserDialog ImageSaver;
    FileChooserDialog ImageLoader;
    FileChooserDialog ClassifierChooser;

    bool ControlsActive;

    bool CommandControl;

    Mutex Rendering = new Mutex();

    OpenCV opencv = new OpenCV();

    Container prevParent;

    public MainWindow() : base(Gtk.WindowType.Toplevel)
    {
        Build();

        DisableControls();

        LoadSettings();

        InitializeCamera();
        InitializePattern();
        InitializeDetector();
        InitializeSelectionMode();
        InitializeSelection();
        InitializeChooser();

        EnableControls();

        Idle.Add(new IdleHandler(OnIdleUpdate));

        prevParent = cameraLayout;
    }

    void LoadSettings()
    {
        if (Configuration.Load("Settings.ini"))
        {
            settingsStatus.LabelProp = "Settings loaded.";
            detectSettingsLabel.LabelProp = "Settings loaded.";
        }
    }

    void SaveSettings()
    {
        Configuration.Save("Settings.ini");

        settingsStatus.LabelProp = "Settings saved.";
        detectSettingsLabel.LabelProp = "Settings saved.";
    }

    void DisableControls()
    {
        ControlsActive = false;
    }

    void EnableControls()
    {
        ControlsActive = true;
    }

    void InitializeCamera()
    {
        camWidth = cameraImage.WidthRequest;
        camHeight = cameraImage.HeightRequest;

        camera = new OpenCVCamera(1);

        if (!camera.IsOpen())
        {
            camera.Dispose();

            camera = new OpenCVCamera(0);
        }

        Title = "GTK-OpenCV Tech Demo";

        if (camera.IsOpen())
        {
            camWidth = camera.Width();
            camHeight = camera.Height();

            Title = Title + " [" + camera.Label() + "(" + camWidth + " x " + camHeight + ")]";

            grabButton.Sensitive = true;
            stopButton.Sensitive = false;
        }

        OriginalImage = new Pixbuf(Colorspace.Rgb, false, 8, camWidth > 0 ? camWidth : cameraImage.WidthRequest, camHeight > 0 ? camHeight : cameraImage.HeightRequest);
        OriginalImage.Fill(0);

        cameraImage.Pixbuf = OriginalImage.ScaleSimple(cameraImage.WidthRequest, cameraImage.HeightRequest, InterpType.Bilinear);

        grabButton.Sensitive = true;
        stopButton.Sensitive = false;

        cameraFlipX.Active = false;
        cameraFlipY.Active = false;
    }

    void InitializePattern()
    {
        patternWindow = new Gtk.Window(Gtk.WindowType.Toplevel);

        SetPatternSettings();
    }

    void SetPatternSettings()
    {
        Pattern.Resize(Pattern.Width, Pattern.Height);

        InitializePatternWindow();

        CopyScales();
        CopyOffsets();
        CopyLevels();

        // Pattern Settings
        MarkerSize.Value = Pattern.MarkerSize;

        Cross.Active = Pattern.Cross;
        Plus.Active = Pattern.Plus;
        Grating.Active = Pattern.Grating;
        Ring.Active = Pattern.Ring;
        Box.Active = Pattern.Box;

        // Grating Controls
        PeriodX.Value = Pattern.PeriodX;
        PeriodY.Value = Pattern.PeriodY;
        FillX.Value = Pattern.FillX;
        FillY.Value = Pattern.FillY;
        TL.Active = Pattern.TL;
        TR.Active = Pattern.TR;
        BL.Active = Pattern.BL;
        BR.Active = Pattern.BR;

        if (Pattern.LockXY)
        {
            PeriodX.Sensitive = true;
            PeriodY.Sensitive = false;
            FillX.Sensitive = true;
            FillY.Sensitive = false;
        }

        gratingXYLock.Active = Pattern.LockXY;

        ringDiameterScale.Value = Pattern.RingDiameter;
        ringDiameterNumeric.Value = Pattern.RingDiameter;
        Rings.Value = Pattern.Rings;
        RingPeriod.Value = Pattern.RingPeriod;
        FillRing.Active = Pattern.FillRing;

        boxWidthScale.Value = Pattern.BoxWidth;
        boxWidthNumeric.Value = Pattern.BoxWidth;
        Boxes.Value = Pattern.Boxes;
        BoxPeriod.Value = Pattern.BoxPeriod;
        FillBox.Active = Pattern.FillBox;

        Invert.Active = Pattern.Invert;
        patternPreviewMode.Active = Pattern.Preview;
        FlipX.Active = Pattern.FlipX;
        FlipY.Active = Pattern.FlipY;
    }

    void InitializePatternWindow()
    {
        patternWindow.Resize(Pattern.Width, Pattern.Height);
        patternWindow.Decorated = false;

        var screens = Screen.NMonitors;

        if (screens > 1)
        {
            var geometry = Screen.GetMonitorGeometry(1);

            Pattern.Top = geometry.Top;
            Pattern.Left = geometry.Left;
            Pattern.Width = geometry.Width;
            Pattern.Height = geometry.Height;

            patternWindow.Show();
        }
        else
        {
            patternWindow.Hide();
        }

        MovePatternWindow();

        patternWidth.Value = Pattern.Width;
        patternHeight.Value = Pattern.Height;
        patternTop.Value = Pattern.Top;
        patternLeft.Value = Pattern.Left;
    }

    void MovePatternWindow()
    {
        patternWindow.Move(Pattern.Left, Pattern.Top);
    }

    void CopyLevels()
    {
        grayLevelScale.Value = Pattern.GrayLevel;
        grayLevelNumeric.Value = Pattern.GrayLevel;
        backgroundLevelScale.Value = Pattern.BackgroundLevel;
        backgroundLevelNumeric.Value = Pattern.BackgroundLevel;
    }

    void CopyScales()
    {
        patternScaleX.Value = Pattern.ScaleX;
        patternScaleY.Value = Pattern.ScaleY;
        patternScaleXNumeric.Value = Pattern.ScaleX;
        patternScaleYNumeric.Value = Pattern.ScaleY;
    }

    void ResetPatternScales()
    {
        Pattern.ScaleX = 1.0;
        Pattern.ScaleY = 1.0;

        CopyScales();
    }

    void CopyOffsets()
    {
        patternOffsetX.Value = Pattern.OffsetX;
        patternOffsetY.Value = Pattern.OffsetY;
        patternOffsetXNumeric.Value = Pattern.OffsetX;
        patternOffsetYNumeric.Value = Pattern.OffsetY;
    }

    void ResetPatternOffsets()
    {
        Pattern.OffsetY = 0;
        Pattern.OffsetX = 0;

        CopyOffsets();
    }

    void InitializeDetector()
    {
        EdgeThreshold.Value = Detect.EdgeThreshold;
        LinkingThreshold.Value = Detect.LinkingThreshold;
        HoughThreshold.Value = Detect.HoughThreshold;
        MinRadius.Value = Detect.MinRadius;
        MaxRadius.Value = Detect.MaxRadius;
        CircleDistance.Value = Detect.CircleDistance;
        MinArea.Value = Detect.MinArea;
        MaxArea.Value = Detect.MaxArea;
        dp.Value = Detect.dp;
        minSize.Value = Detect.minSize;
        minNeighbors.Value = Detect.minNeighbors;
        scaleFactor.Value = Detect.scaleFactor;

        MarkerSize.Value = Detect.MarkerSize;
        SelectedColor.Color = GtkSelection.SelectedColor;
        MarkerColor.Color = GtkSelection.MarkerColor;

        DownUpSample.Active = opencv.DownUpSample;
        InvertGray.Active = opencv.Invert;
        SubtractBackground.Active = opencv.SubtractBackground;
        Normalize.Active = opencv.Normalize;
        GaussianFilter.Active = opencv.Blur;

        GaussianFilterWidth.Value = opencv.sx;
        GaussianFilterHeight.Value = opencv.sy;
        GaussianFilterStdevX.Value = opencv.sigmaX;
        GaussianFilterStdevY.Value = opencv.sigmaY;
    }

    void InitializeSelection()
    {
        HideEditRegionLayout(GtkSelection.Selected);

        GtkSelection.Selection.Parse();

        InitializeSelected();
    }

    void InitializeSelected()
    {
        DisableEditSignals();

        if (GtkSelection.Selected > 0)
        {
            SetupEditRegion(GtkSelection.Selected);
            SetupEditRegionLocations(GtkSelection.Selected);
            editRegionLayout.Show();
        }

        EnableEditSignals();
    }

    void InitializeSelectionMode()
    {
        if (GtkSelection.Selection.EllipseMode)
        {
            ellipseBoxToggle.Active = false;
            ellipseBoxToggle.Label = "Ellipse Mode";
        }
        else
        {
            ellipseBoxToggle.Active = true;
            ellipseBoxToggle.Label = "Box Mode";
        }
    }

    protected FileFilter AddFilter(string name, params string[] patterns)
    {
        var filter = new FileFilter() { Name = name };

        foreach (var pattern in patterns)
            filter.AddPattern(pattern);

        return filter;
    }

    void InitializeChooser()
    {
        ImageSaver = new FileChooserDialog(
            "Save Image",
            this,
            FileChooserAction.Save,
            "Cancel", ResponseType.Cancel,
            "Save", ResponseType.Accept
        );

        ImageSaver.AddFilter(AddFilter("png", "*.png"));

        ImageLoader = new FileChooserDialog(
            "Load Pattern",
            this,
            FileChooserAction.Open,
            "Cancel", ResponseType.Cancel,
            "Open", ResponseType.Accept
        );

        ImageLoader.AddFilter(AddFilter("Image files (png/jpg/jpeg/tif/tiff/bmp/gif/ico/xpm/icns/pgm)", "*.png", "*.jpg", "*.jpeg", "*.tif", "*.tiff", "*.bmp", "*.gif", "*.ico", "*.xpm", "*.icns", "*.pgm"));

        ClassifierChooser = new FileChooserDialog(
            "Choose the Classifier to open",
            this,
            FileChooserAction.Open,
            "Open", ResponseType.Accept,
            "Cancel", ResponseType.Cancel
        );

        ClassifierChooser.AddFilter(AddFilter("Classifiers (xml)", "*.xml"));
    }

    void Redraw(Gtk.Image background)
    {
        if (background == null)
            return;

        var dest = background.GdkWindow;
        var gc = new Gdk.GC(dest);

        dest.DrawPixbuf(gc, background.Pixbuf, 0, 0, 0, 0, background.WidthRequest, background.HeightRequest, RgbDither.None, 0, 0);

        if (IsSelecting)
            GtkSelection.Draw(gc, dest, X0, Y0, X1, Y1);
    }

    protected void DestroyCamera()
    {
        if (camera != null)
            camera.Dispose();
    }

    void QuitApplication()
    {
        DestroyCamera();

        if (OriginalImage != null)
            OriginalImage.Dispose();

        if (opencv != null)
            opencv.Dispose();

        Application.Quit();
    }

    protected void OnDeleteEvent(object o, DeleteEventArgs a)
    {
        QuitApplication();

        a.RetVal = true;
    }

    protected void OnCameraImageEventButtonPressEvent(object o, ButtonPressEventArgs args)
    {
        X0 = Convert.ToInt32(args.Event.X);
        Y0 = Convert.ToInt32(args.Event.Y);

        X1 = X0;
        Y1 = Y0;

        if (args.Event.Button == 3)
        {
            IsSelecting = false;
            IsDragging = false;

            HideEditRegionLayout();

            GtkSelection.Selection.Update(X0, Y0);
        }
        else
        {
            if (args.Event.Button == 1)
            {
                GtkSelection.Selected = GtkSelection.Selection.Find(X0, Y0);

                if (GtkSelection.Selected > 0)
                {
                    IsDragging = true;

                    InitializeSelected();

                    prevX = X0;
                    prevY = Y0;
                }
                else
                {
                    HideEditRegionLayout();

                    IsSelecting = true;
                }
            }
        }
    }

    protected void OnCameraImageEventButtonReleaseEvent(object o, ButtonReleaseEventArgs args)
    {
        if (IsSelecting)
        {
            IsSelecting = false;

            GtkSelection.Selection.Add(X0, Y0, X1, Y1);
        }

        if (IsDragging)
        {
            IsDragging = false;
        }
    }

    protected void OnCameraImageEventMotionNotifyEvent(object o, MotionNotifyEventArgs args)
    {
        if (!IsSelecting && !IsDragging) return;

        X1 = Convert.ToInt32(args.Event.X);
        Y1 = Convert.ToInt32(args.Event.Y);

        if (IsDragging)
            Move();
    }

    protected void Move()
    {
        var dx = X1 - prevX;
        var dy = Y1 - prevY;

        prevX = X1;
        prevY = Y1;

        GtkSelection.Selection.Move(dx, dy, GtkSelection.Selected);

        SetupEditRegionLocations(GtkSelection.Selected);
    }

    protected void SetupEditRegion(int Region)
    {
        if (Region > 0)
        {
            GtkSelection.Selection.Size(Region, out int width, out int height);
            regionEnabled.Active = GtkSelection.Selection.Status(Region);
            SetupEditRegionLocations(GtkSelection.Selected);

            widthScale.Value = width;
            heightScale.Value = height;
            widthScaleNumeric.Value = width;
            heightScaleNumeric.Value = height;
        }
    }

    protected void SetupEditRegionLocations(int Region)
    {
        if (Region > 0)
        {
            GtkSelection.Selection.Location(Region, out int x, out int y);

            dxScale.Value = x;
            dxScaleNumeric.Value = x;
            dyScale.Value = y;
            dyScaleNumeric.Value = y;
        }
    }

    protected void DisableEditSignals()
    {
        editEnabled = false;
    }

    protected void EnableEditSignals()
    {
        editEnabled = true;
    }

    protected void RenderImage()
    {
        if (OriginalImage != null)
        {
            using (Pixbuf pb = GtkSelection.Render(OriginalImage.ScaleSimple(cameraImage.WidthRequest, cameraImage.HeightRequest, InterpType.Bilinear), opencv, GtkSelection.MarkerColor, GtkSelection.Selected, GtkSelection.SelectedColor, false, true))
            {
                if (cameraImage.Pixbuf != null && pb != null)
                {
                    pb.CopyArea(0, 0, cameraImage.Pixbuf.Width, cameraImage.Pixbuf.Height, cameraImage.Pixbuf, 0, 0);
                }
            }
        }

        CollectGarbage();
    }

    protected void UpdatePattern()
    {
        Pattern.Update();

        RenderPattern(Pattern.Pixbuf);

        CollectGarbage();
    }

    protected void RenderPattern(Pixbuf pixbuf)
    {
        if (pixbuf != null && patternWindow.Visible)
            pixbuf.RenderToDrawable(patternWindow.GdkWindow, new Gdk.GC(patternWindow.GdkWindow), 0, 0, 0, 0, patternWindow.WidthRequest, patternWindow.HeightRequest, RgbDither.None, 0, 0);
    }

    bool OnIdleUpdate()
    {
        if (camera == null)
            return false;

        Rendering.WaitOne();

        if (camera.IsOpen() && camera.IsStreaming())
        {
            if (OriginalImage != null)
            {
                OriginalImage.Dispose();
                OriginalImage = Capture.ProcessPixbuf(camera);
            }

            CollectGarbage();
        }

        RenderImage();

        UpdatePattern();

        Redraw(cameraImage);

        Rendering.ReleaseMutex();

        return true;
    }

    protected void GrabButtonClicked(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            if (camera != null && camera.IsOpen())
            {
                camera.Start();

                if (!stopButton.Sensitive) stopButton.Sensitive = true;

                grabButton.Sensitive = false;
            }
        }
    }

    protected void StopButtonClicked(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            if (camera != null && camera.IsOpen())
            {
                camera.Stop();

                if (!grabButton.Sensitive)
                    grabButton.Sensitive = true;

                stopButton.Sensitive = false;
            }
        }
    }

    protected void OnRestartButtonClicked(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            if (camera != null && camera.IsOpen() && !camera.IsStreaming())
            {
                if (camera is OpenCVCamera)
                    (camera as OpenCVCamera).Restart();
            }
        }
    }

    void HideEditRegionLayout(int selected = 0)
    {
        GtkSelection.Selected = selected;
        editRegionLayout.Hide();
    }

    protected void OnClearButtonClicked(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            GtkSelection.Selection.Clear();
            HideEditRegionLayout();
        }
    }

    protected void OnToggleEllipseBoxToggled(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            if (ellipseBoxToggle.Active)
            {
                ellipseBoxToggle.Label = "Box Mode";
                GtkSelection.Selection.EllipseMode = false;
            }
            else
            {
                ellipseBoxToggle.Label = "Ellipse Mode";
                GtkSelection.Selection.EllipseMode = true;
            }

            HideEditRegionLayout();
        }
    }

    void CollectGarbage()
    {
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
    }

    protected void PatternScaleChanged(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            Pattern.ScaleX = patternScaleX.Value;
            Pattern.ScaleY = patternScaleY.Value;

            patternScaleXNumeric.Value = Pattern.ScaleX;
            patternScaleYNumeric.Value = Pattern.ScaleY;
        }
    }

    protected void PatternScaleNumericChanged(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            Pattern.ScaleX = patternScaleXNumeric.Value;
            Pattern.ScaleY = patternScaleYNumeric.Value;

            patternScaleX.Value = Pattern.ScaleX;
            patternScaleY.Value = Pattern.ScaleY;
        }
    }

    protected void OnResetScaleButtonClicked(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            DisableControls();

            ResetPatternScales();

            EnableControls();
        }
    }

    protected void OnMainNoteBookSwitchPage(object o, SwitchPageArgs args)
    {
        if (ControlsActive)
        {
            switch (args.PageNum)
            {
                case 0:
                    ReparentCameraControls(cameraLayout);
                    break;
                case 1:
                    cameraImageEvent.Reparent(patternLayout);
                    patternLayout.Move(cameraImageEvent, 300, 20);
                    break;
                case 2:
                    ReparentCameraControls(imageDetectLayout);
                    break;
            }
        }
    }

    protected void ReparentCameraControls(Fixed parent)
    {
        // Re-parent camera picture box and toggle buttons
        cameraImageEvent.Reparent(parent);
        grabButton.Reparent(parent);
        stopButton.Reparent(parent);
        restartButton.Reparent(parent);
        clearButton.Reparent(parent);
        saveButton.Reparent(parent);
        ellipseBoxToggle.Reparent(parent);

        // Move picture box and buttons to default locations
        parent.Move(cameraImageEvent, 20, 20);
        parent.Move(grabButton, 20, 520);
        parent.Move(stopButton, 80, 520);
        parent.Move(restartButton, 140, 520);
        parent.Move(clearButton, 220, 520);
        parent.Move(saveButton, 350, 520);
        parent.Move(ellipseBoxToggle, 410, 520);

        // Reparent and layout fix for edit region panel
        prevParent.Remove(editRegionLayout);
        parent.Put(editRegionLayout, 670, 20);
        prevParent = parent;
    }

    protected void PatternOffsetChanged(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            Pattern.OffsetX = Convert.ToInt32(patternOffsetX.Value);
            Pattern.OffsetY = Convert.ToInt32(patternOffsetY.Value);

            patternOffsetXNumeric.Value = Pattern.OffsetX;
            patternOffsetYNumeric.Value = Pattern.OffsetY;
        }
    }

    protected void PatternOffsetNumericChanged(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            Pattern.OffsetX = Convert.ToInt32(patternOffsetXNumeric.Value);
            Pattern.OffsetY = Convert.ToInt32(patternOffsetYNumeric.Value);

            patternOffsetX.Value = Pattern.OffsetX;
            patternOffsetY.Value = Pattern.OffsetY;
        }
    }

    protected void OnResetOffsetButtonClicked(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            DisableControls();

            ResetPatternOffsets();

            EnableControls();
        }
    }

    protected void ScaleResizeEvent(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            if (editEnabled)
                ScaleResize();
        }
    }

    protected void ScaleResize()
    {
        if (ControlsActive)
        {
            if (GtkSelection.Selected > 0)
            {
                int width = Convert.ToInt32(widthScale.Value);
                int height = Convert.ToInt32(heightScale.Value);

                GtkSelection.Selection.ReSize(GtkSelection.Selected, width, height);

                widthScaleNumeric.Value = width;
                heightScaleNumeric.Value = height;
            }
        }
    }

    protected void NumericResizeEvent(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            if (editEnabled)
                NumericResize();
        }
    }

    protected void NumericResize()
    {
        if (ControlsActive)
        {
            if (GtkSelection.Selected > 0)
            {
                int width = Convert.ToInt32(widthScaleNumeric.Value);
                int height = Convert.ToInt32(heightScaleNumeric.Value);

                GtkSelection.Selection.ReSize(GtkSelection.Selected, width, height);

                widthScale.Value = width;
                heightScale.Value = height;
            }
        }
    }

    protected void ScaleMove()
    {
        if (ControlsActive)
        {
            if (GtkSelection.Selected > 0)
            {
                GtkSelection.Selection.Location(GtkSelection.Selected, out int x, out int y);

                GtkSelection.Selection.Move(Convert.ToInt32(dxScale.Value) - x, Convert.ToInt32(dyScale.Value) - y, GtkSelection.Selected);

                SetupEditRegionLocations(GtkSelection.Selected);
            }
        }
    }

    protected void NumericMove()
    {
        if (ControlsActive)
        {
            if (GtkSelection.Selected > 0)
            {
                GtkSelection.Selection.Location(GtkSelection.Selected, out int x, out int y);

                GtkSelection.Selection.Move(Convert.ToInt32(dxScaleNumeric.Value) - x, Convert.ToInt32(dyScaleNumeric.Value) - y, GtkSelection.Selected);

                SetupEditRegionLocations(GtkSelection.Selected);
            }
        }
    }

    protected void ScaleMoveEvent(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            if (editEnabled)
                ScaleMove();
        }
    }

    protected void NumericMoveEvent(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            if (editEnabled)
                NumericMove();
        }
    }

    protected void OnGrayLevelScaleValueChanged(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            Pattern.GrayLevel = Convert.ToByte(grayLevelScale.Value);
            grayLevelNumeric.Value = Pattern.GrayLevel;
        }
    }

    protected void OnGrayLevelNumericValueChanged(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            Pattern.GrayLevel = Convert.ToByte(grayLevelNumeric.Value);
            grayLevelScale.Value = Pattern.GrayLevel;
        }
    }

    protected void OnBackgroundLevelScaleValueChanged(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            Pattern.BackgroundLevel = Convert.ToByte(backgroundLevelScale.Value);
            backgroundLevelNumeric.Value = Pattern.BackgroundLevel;
        }
    }

    protected void OnBackgroundLevelNumericValueChanged(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            Pattern.BackgroundLevel = Convert.ToByte(backgroundLevelNumeric.Value);
            backgroundLevelScale.Value = Pattern.BackgroundLevel;
        }
    }

    protected void OnCloseEditButtonClicked(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            HideEditRegionLayout();
        }
    }

    protected void OnRegionEnabledToggled(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            if (GtkSelection.Selected > 0)
            {
                GtkSelection.Selection.Switch(GtkSelection.Selected, regionEnabled.Active);
            }
        }
    }

    protected void UpdatePatternEvent(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            Pattern.Grating = Grating.Active;
            Pattern.Cross = Cross.Active;
            Pattern.Plus = Plus.Active;
            Pattern.Ring = Ring.Active;
            Pattern.Box = Box.Active;
            Pattern.Custom = Custom.Active;
            Pattern.Preview = patternPreviewMode.Active;
            Pattern.Invert = Invert.Active;
            Pattern.FlipX = FlipX.Active;
            Pattern.FlipY = FlipY.Active;
        }
    }

    protected void UpdateRingSettings(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            Pattern.FillRing = FillRing.Active;
            Pattern.RingDiameter = Convert.ToInt32(ringDiameterScale.Value);
            Pattern.Rings = Convert.ToInt32(Rings.Value);
            Pattern.RingPeriod = Convert.ToInt32(RingPeriod.Value);

            ringDiameterNumeric.Value = Pattern.RingDiameter;
        }
    }

    protected void UpdateRingSettingsNumeric(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            Pattern.FillRing = FillRing.Active;
            Pattern.RingDiameter = Convert.ToInt32(ringDiameterNumeric.Value);

            ringDiameterScale.Value = Pattern.RingDiameter;
        }
    }

    protected void UpdateBoxSettings(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            Pattern.FillBox = FillBox.Active;
            Pattern.BoxWidth = Convert.ToInt32(boxWidthScale.Value);
            Pattern.Boxes = Convert.ToInt32(Boxes.Value);
            Pattern.BoxPeriod = Convert.ToInt32(BoxPeriod.Value);

            boxWidthNumeric.Value = Pattern.BoxWidth;
        }
    }

    protected void UpdateBoxSettingsNumeric(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            Pattern.FillBox = FillBox.Active;
            Pattern.BoxWidth = Convert.ToInt32(boxWidthNumeric.Value);

            boxWidthScale.Value = Pattern.BoxWidth;
        }
    }

    protected void UpdateAspectRatio(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            Pattern.AspectX = Convert.ToInt32(aspectX.Value);
            Pattern.AspectY = Convert.ToInt32(aspectY.Value);

            if (Pattern.AspectY > Pattern.AspectX)
            {
                Pattern.AspectY = Pattern.AspectX;
                aspectY.Value = Pattern.AspectY;
            }
        }
    }

    protected void SaveImage(FileChooserDialog dialog, string title, Pixbuf pixbuf)
    {
        dialog.Title = title;

        if (!string.IsNullOrEmpty(dialog.Filename))
        {
            var directory = System.IO.Path.GetDirectoryName(dialog.Filename);

            if (Directory.Exists(directory))
            {
                dialog.SetCurrentFolder(directory);
            }
        }

        if (dialog.Run() == Convert.ToInt32(ResponseType.Accept))
        {
            if (!string.IsNullOrEmpty(dialog.Filename))
            {
                var FileName = dialog.Filename;

                if (!FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    FileName += ".png";

                pixbuf.Save(FileName, "png");
            }
        }

        dialog.Hide();
    }

    protected void OnSaveButtonClicked(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            // Stop the camera
            StopButtonClicked(o, e);

            if (ImageSaver != null)
                SaveImage(ImageSaver, "Save Image", OriginalImage);
        }
    }

    protected void FlipCamera(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            if (camera is OpenCVCamera)
            {
                var opencvCamera = camera as OpenCVCamera;

                opencvCamera.FlipX(cameraFlipX.Active);
                opencvCamera.FlipY(cameraFlipY.Active);
            }
        }
    }

    protected void ResizePatternWindow(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            Pattern.Resize(Convert.ToInt32(patternWidth.Value), Convert.ToInt32(patternHeight.Value));

            InitializePatternWindow();
        }
    }

    protected void MarkerSizeUpdate(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            Pattern.MarkerSize = Convert.ToInt32(MarkerSize.Value);
        }
    }

    protected void PatternWindowMoved(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            Pattern.Top = Convert.ToInt32(patternTop.Value);
            Pattern.Left = Convert.ToInt32(patternLeft.Value);
            MovePatternWindow();
        }
    }

    protected void GratingSettingsChanged(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            Pattern.LockXY = gratingXYLock.Active;

            Pattern.PeriodX = Convert.ToInt32(PeriodX.Value);
            Pattern.FillX = Convert.ToInt32(FillX.Value);

            if (Pattern.LockXY)
            {
                Pattern.PeriodY = Pattern.PeriodX;
                Pattern.FillY = Pattern.FillX;
                PeriodY.Value = Pattern.PeriodY;
                FillY.Value = Pattern.FillY;
            }
            else
            {
                Pattern.PeriodY = Convert.ToInt32(PeriodY.Value);
                Pattern.FillY = Convert.ToInt32(FillY.Value);
            }

            Pattern.TL = TL.Active;
            Pattern.TR = TR.Active;
            Pattern.BL = BL.Active;
            Pattern.BR = BR.Active;
        }
    }

    protected void OnGratingXYLockToggled(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            Pattern.LockXY = gratingXYLock.Active;

            if (Pattern.LockXY)
            {
                PeriodY.Sensitive = false;
                FillY.Sensitive = false;
            }
            else
            {
                PeriodY.Sensitive = true;
                FillY.Sensitive = true;
            }
        }
    }

    protected void OnPatternNotebookSwitchPage(object o, SwitchPageArgs args)
    {
        if (ControlsActive)
        {
            if (args.PageNum == 2)
                ReparentAspect(ringLayout);

            if (args.PageNum == 3)
                ReparentAspect(boxLayout);
        }
    }

    protected void ReparentAspect(Fixed parent)
    {
        aspectLabel.Reparent(parent);
        aspectX.Reparent(parent);
        aspectY.Reparent(parent);
        aspectXLabel.Reparent(parent);
        aspectYLabel.Reparent(parent);

        parent.Move(aspectLabel, 365, 5);
        parent.Move(aspectX, 365, 25);
        parent.Move(aspectY, 365, 55);
        parent.Move(aspectXLabel, 350, 30);
        parent.Move(aspectYLabel, 350, 60);
    }

    protected void OnSavePatternButtonClicked(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            if (ImageSaver != null)
                SaveImage(ImageSaver, "Save Pattern", Pattern.Pixbuf);
        }
    }

    protected void LoadImage(FileChooserDialog dialog, string title, Pixbuf pixbuf)
    {
        dialog.Title = title;

        if (!string.IsNullOrEmpty(dialog.Filename))
        {
            var directory = System.IO.Path.GetDirectoryName(dialog.Filename);

            if (Directory.Exists(directory))
            {
                dialog.SetCurrentFolder(directory);
            }
        }

        if (dialog.Run() == Convert.ToInt32(ResponseType.Accept))
        {
            if (!string.IsNullOrEmpty(dialog.Filename))
            {
                var FileName = dialog.Filename;

                if (!FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    FileName += ".png";

                if (pixbuf != null)
                {
                    using (Pixbuf pb = new Pixbuf(FileName))
                    {
                        pixbuf.Fill(0);
                        pb.CopyArea(0, 0, pb.Width > pixbuf.Width ? pixbuf.Width : pb.Width, pb.Height > pixbuf.Height ? pixbuf.Height : pb.Height, pixbuf, 0, 0);
                    }

                    // Generate Custom Pattern Preview
                    if (CustomPattern.Pixbuf != null)
                        CustomPattern.Pixbuf.Dispose();

                    CustomPattern.Pixbuf = pixbuf.ScaleSimple(CustomPattern.WidthRequest, CustomPattern.HeightRequest, InterpType.Bilinear);
                }
            }
        }

        dialog.Hide();
    }

    protected void OnLoadPatternButtonClicked(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            if (ImageLoader != null)
                LoadImage(ImageLoader, "Load Pattern", Pattern.CustomPixbuf);
        }
    }

    protected void OnClearPatternButtonClicked(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            Pattern.ClearCustomPixbuf();

            if (CustomPattern.Pixbuf != null)
                CustomPattern.Pixbuf.Dispose();

            // Generate Custom Pattern Preview
            CustomPattern.Pixbuf = Pattern.CustomPixbuf.ScaleSimple(CustomPattern.WidthRequest, CustomPattern.HeightRequest, InterpType.Bilinear);
        }
    }

    protected void OnLoadSettingsButtonClicked(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            DisableControls();

            LoadSettings();

            SetPatternSettings();

            InitializeDetector();

            InitializeSelectionMode();

            InitializeSelection();

            EnableControls();
        }
    }

    protected void OnSaveSettingsButtonClicked(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            SaveSettings();
        }
    }

    protected void OnKeyPressEvent(object o, KeyPressEventArgs args)
    {
        var key = args.Event.Key;

        if (key == Gdk.Key.Meta_L || key == Gdk.Key.Meta_R || key == Gdk.Key.Control_L || key == Gdk.Key.Control_L)
        {
            CommandControl = true;
        }
        else
        {
            if (CommandControl && (key == Gdk.Key.Q || key == Gdk.Key.q))
            {
                QuitApplication();
            }

            if (CommandControl && (key == Gdk.Key.S || key == Gdk.Key.s))
            {
                if (ImageSaver != null && OriginalImage != null)
                {
                    // Stop the camera
                    StopButtonClicked(o, args);

                    SaveImage(ImageSaver, "Save Image", OriginalImage);
                }
            }
        }
    }

    protected void OnKeyReleaseEvent(object o, KeyReleaseEventArgs args)
    {
        CommandControl = false;
    }

    protected void UpdateDetectorSettings(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            Detect.EdgeThreshold = Convert.ToInt32(EdgeThreshold.Value);
            Detect.LinkingThreshold = Convert.ToInt32(LinkingThreshold.Value);
            Detect.HoughThreshold = Convert.ToInt32(HoughThreshold.Value);
            Detect.MinRadius = Convert.ToInt32(MinRadius.Value);
            Detect.MaxRadius = Convert.ToInt32(MaxRadius.Value);
            Detect.CircleDistance = Convert.ToInt32(CircleDistance.Value);
            Detect.MinArea = Convert.ToInt32(MinArea.Value);
            Detect.MaxArea = Convert.ToInt32(MaxArea.Value);
            Detect.dp = Convert.ToDouble(dp.Value);
            Detect.MarkerSize = Convert.ToInt32(MarkerSize.Value);
            Detect.minSize = Convert.ToInt32(minSize.Value);
            Detect.minNeighbors = Convert.ToInt32(minNeighbors.Value);
            Detect.scaleFactor = Convert.ToDouble(scaleFactor.Value);

            GtkSelection.SelectedColor = SelectedColor.Color;
            GtkSelection.MarkerColor = MarkerColor.Color;

            opencv.DownUpSample = DownUpSample.Active;
            opencv.Invert = InvertGray.Active;
            opencv.SubtractBackground = SubtractBackground.Active;
            opencv.Normalize = Normalize.Active;

            opencv.Blur = GaussianFilter.Active;
            opencv.sx = Convert.ToInt32(GaussianFilterWidth.Value);
            opencv.sy = Convert.ToInt32(GaussianFilterHeight.Value);
            opencv.sigmaX = Convert.ToDouble(GaussianFilterStdevX.Value);
            opencv.sigmaY = Convert.ToDouble(GaussianFilterStdevY.Value);
        }
    }

    protected void OnEdgeBlobsButtonClicked(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            Detect.EdgeBlobs(
                opencv,
                OriginalImage,
                GtkSelection.Selection,
                Convert.ToDouble(cameraImage.WidthRequest) / OriginalImage.Width,
                Convert.ToDouble(cameraImage.HeightRequest) / OriginalImage.Height
            );
        }
    }

    protected void OnHoughCircleButtonClicked(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            Detect.HoughCircles(
                opencv,
                OriginalImage,
                GtkSelection.Selection,
                Convert.ToDouble(cameraImage.WidthRequest) / OriginalImage.Width,
                Convert.ToDouble(cameraImage.HeightRequest) / OriginalImage.Height
            );
        }
    }

    protected void OnBlobsButtonClicked(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            Detect.Blobs(
                opencv,
                OriginalImage,
                GtkSelection.Selection,
                Convert.ToDouble(cameraImage.WidthRequest) / OriginalImage.Width,
                Convert.ToDouble(cameraImage.HeightRequest) / OriginalImage.Height
            );
        }
    }

    protected void OnSimpleBlobsButtonClicked(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            Detect.Simple(
                opencv,
                OriginalImage,
                GtkSelection.Selection,
                Convert.ToDouble(cameraImage.WidthRequest) / OriginalImage.Width,
                Convert.ToDouble(cameraImage.HeightRequest) / OriginalImage.Height
            );
        }
    }

    protected void OnFacesButtonClicked(object sender, EventArgs e)
    {
        if (ControlsActive)
        {
            Detect.Faces(
                opencv,
                OriginalImage,
                GtkSelection.Selection,
                Convert.ToDouble(cameraImage.WidthRequest) / OriginalImage.Width,
                Convert.ToDouble(cameraImage.HeightRequest) / OriginalImage.Height
            );
        }
    }

    protected void OnSaveProcessedImageButtonClicked(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            if (GtkSelection.Selection.Count() > 0)
            {
                ImageSaver.Title = "Save Processed Image";

                if (!string.IsNullOrEmpty(ImageSaver.Filename))
                {
                    var directory = System.IO.Path.GetDirectoryName(ImageSaver.Filename);

                    if (Directory.Exists(directory))
                    {
                        ImageSaver.SetCurrentFolder(directory);
                    }
                }

                if (ImageSaver.Run() == Convert.ToInt32(ResponseType.Accept))
                {
                    if (!string.IsNullOrEmpty(ImageSaver.Filename))
                    {
                        var FileName = ImageSaver.Filename;

                        if (!FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                            FileName += ".png";

                        var ScaleX = Convert.ToDouble(OriginalImage.Width) / cameraImage.WidthRequest;
                        var ScaleY = Convert.ToDouble(OriginalImage.Height) / cameraImage.HeightRequest;

                        using (Pixbuf pb = GtkSelection.Render(OriginalImage, opencv, GtkSelection.MarkerColor, GtkSelection.Selected, GtkSelection.SelectedColor, false, true, ScaleX, ScaleY))
                        {
                            if (cameraImage.Pixbuf != null && pb != null)
                            {
                                pb.Save(FileName, "png");
                            }
                        }
                    }
                }

                ImageSaver.Hide();
            }
        }
    }

    protected void OnSaveBlobsButtonClicked(object o, EventArgs e)
    {
        if (ControlsActive)
        {
            ImageSaver.Title = "Choose directory and base name for the blobs";

            if (!string.IsNullOrEmpty(ImageSaver.Filename))
            {
                var directory = System.IO.Path.GetDirectoryName(ImageSaver.Filename);

                if (Directory.Exists(directory))
                {
                    ImageSaver.SetCurrentFolder(directory);
                }
            }

            if (ImageSaver.Run() == Convert.ToInt32(ResponseType.Accept))
            {
                var blobs = GtkSelection.Selection.BoundingBoxes();

                if (!string.IsNullOrEmpty(ImageSaver.Filename) && blobs.Count > 0)
                {
                    StopButtonClicked(o, e);

                    var basefile = System.IO.Path.GetFileNameWithoutExtension(ImageSaver.Filename);

                    var index = 1;

                    foreach (var rectangle in blobs)
                    {
                        var ScaleX = Convert.ToDouble(OriginalImage.Width) / cameraImage.WidthRequest;
                        var ScaleY = Convert.ToDouble(OriginalImage.Height) / cameraImage.HeightRequest;

                        var width = Convert.ToInt32(Math.Abs(rectangle.X1 - rectangle.X0) * ScaleX);
                        var height = Convert.ToInt32(Math.Abs(rectangle.Y1 - rectangle.Y0) * ScaleY);
                        var top = Convert.ToInt32(Math.Min(rectangle.Y0, rectangle.Y1) * ScaleY);
                        var left = Convert.ToInt32(Math.Min(rectangle.X0, rectangle.X1) * ScaleX);

                        using (var area = new Pixbuf(Colorspace.Rgb, false, 8, width, height))
                        {
                            if (OriginalImage != null)
                            {
                                OriginalImage.CopyArea(left, top, width, height, area, 0, 0);
                                area.Save(string.Format("{0}/{1}-{2}.png", ImageSaver.CurrentFolder, basefile, index.ToString("D4")), "png");
                            }
                        }

                        index++;
                    }
                }
            }

            ImageSaver.Hide();
        }
    }

    protected void OnSelectClassifierButtonClicked(object sender, EventArgs e)
    {
        ClassifierChooser.Title = "Select Classifier";

        if (!string.IsNullOrEmpty(ClassifierChooser.Filename))
        {
            var directory = System.IO.Path.GetDirectoryName(ClassifierChooser.Filename);

            if (Directory.Exists(directory))
            {
                ClassifierChooser.SetCurrentFolder(directory);
            }
        }

        if (ClassifierChooser.Run() == Convert.ToInt32(ResponseType.Accept))
        {
            if (!string.IsNullOrEmpty(ClassifierChooser.Filename))
            {
                Detect.Classifier = ClassifierChooser.Filename;
            }
        }

        ClassifierChooser.Hide();
    }
}
