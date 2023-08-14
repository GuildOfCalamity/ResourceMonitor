using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Monitor.Helpers;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage;

namespace Monitor;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    public static bool initialized = false;
    public static bool useDrawing = false;
	public static double newWidth = 0d;
	public static double newHeight = 0d;
    System.Drawing.Icon? ico0 = null;
    System.Drawing.Icon? ico1 = null;
    System.Drawing.Icon? ico2 = null;
    System.Drawing.Icon? ico3 = null;
    System.Drawing.Icon? ico4 = null;
    System.Drawing.Icon? ico5 = null;

    public MainWindow()
    {
        Debug.WriteLine($"{MethodBase.GetCurrentMethod()?.DeclaringType?.Name}__{MethodBase.GetCurrentMethod()?.Name} [{DateTime.Now.ToString("hh:mm:ss.fff tt")}]");

        this.InitializeComponent();
        Title = "Monitor";
        // These must come after InitializeComponent() if using a NavigationView. 
        this.ExtendsContentIntoTitleBar = true;
        this.SetTitleBar(CustomTitleBar);

        // Our mock "OnLoad" event.
        this.Activated += MainWindow_Activated;
        // For testing, may be removed.
        this.SizeChanged += MainWindow_SizeChanged;

        UsagePage.TitleUpdateEvent += UsagePage_TitleUpdateEvent;

        if (useDrawing)
        {
            ico0 = new System.Drawing.Icon(System.IO.Path.Combine(Environment.CurrentDirectory, "Assets\\Scale0.ico"));
            ico1 = new System.Drawing.Icon(System.IO.Path.Combine(Environment.CurrentDirectory, "Assets\\Scale1.ico"));
            ico2 = new System.Drawing.Icon(System.IO.Path.Combine(Environment.CurrentDirectory, "Assets\\Scale2.ico"));
            ico3 = new System.Drawing.Icon(System.IO.Path.Combine(Environment.CurrentDirectory, "Assets\\Scale3.ico"));
            ico4 = new System.Drawing.Icon(System.IO.Path.Combine(Environment.CurrentDirectory, "Assets\\Scale4.ico"));
            ico5 = new System.Drawing.Icon(System.IO.Path.Combine(Environment.CurrentDirectory, "Assets\\Scale5.ico"));
        }
    }

    /// <summary>
    /// Using this event in place of the Loaded event.
    /// Why MS have you forsaken me? Why did you remove such a basic and essential event from the Window???
    /// </summary>
    void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (!initialized)
        {
            initialized = true;
            Debug.WriteLine($"─── MainWindow activated at {App.GetStopWatch().ToTimeString()} ───");
        }
    }

    /// <summary>
    /// This usually fires twice upon init.
    /// </summary>
    void MainWindow_SizeChanged(object sender, WindowSizeChangedEventArgs args)
    {
        // We could also use the AppWin event.
        if (args.Size.Width != double.NaN) { newWidth = args.Size.Width; }
        if (args.Size.Height != double.NaN) { newHeight = args.Size.Height; }
    }

    /// <summary>
    /// https://learn.microsoft.com/en-us/answers/questions/822928/app-icon-windows-app-sdk
    /// </summary>
    void UsagePage_TitleUpdateEvent(object? sender, float amount)
    {
        if (App.GraphType == eGraphType.LAN)
            this.Title = tbTitle.Text = $"NET {amount.ToFileSize()}";
        else if (App.GraphType == eGraphType.RAM)
            this.Title = tbTitle.Text = $"RAM {amount.ToFileSize()}";
        else if (App.GraphType == eGraphType.DISK)
            this.Title = tbTitle.Text = $"DISK {amount:N0}/sec";
        else if (App.GraphType == eGraphType.CPU)
        {
            this.Title = tbTitle.Text = $"CPU {amount:N0}%";

            #region [Unpackaged Style Image Update]
            switch (amount)
            {
                case float f when f > 79:
                    imgTitle.Source = new BitmapImage(new Uri("ms-appx:///Assets/Scale5.png"));
                    if (useDrawing && ico5 != null)
                    {
                        NativeMethods.SendMessage(App.WindowHandle, NativeMethods.WM_SETICON, NativeMethods.ICON_BIG, ico5.Handle);
                        NativeMethods.SendMessage(App.WindowHandle, NativeMethods.WM_SETICON, NativeMethods.ICON_SMALL, ico5.Handle);
                        NativeMethods.SendMessage(App.WindowHandle, NativeMethods.WM_SETICON, NativeMethods.ICON_SMALL2, ico5.Handle);
                    }
                    else
                    {
                        if (App.AppWin != null) { App.SetIcon("Assets/Scale5.ico", App.AppWin); }
                    }
                    break;
                case float f when f > 59:
                    imgTitle.Source = new BitmapImage(new Uri("ms-appx:///Assets/Scale4.png"));
                    if (useDrawing && ico4 != null)
                    {
                        NativeMethods.SendMessage(App.WindowHandle, NativeMethods.WM_SETICON, NativeMethods.ICON_BIG, ico4.Handle);
                        NativeMethods.SendMessage(App.WindowHandle, NativeMethods.WM_SETICON, NativeMethods.ICON_SMALL, ico4.Handle);
                        NativeMethods.SendMessage(App.WindowHandle, NativeMethods.WM_SETICON, NativeMethods.ICON_SMALL2, ico4.Handle);
                    }
                    else
                    {
                        if (App.AppWin != null) { App.SetIcon("Assets/Scale4.ico", App.AppWin); }
                    }
                    break;
                case float f when f > 39:
                    imgTitle.Source = new BitmapImage(new Uri("ms-appx:///Assets/Scale3.png"));
                    if (useDrawing && ico3 != null)
                    {
                        NativeMethods.SendMessage(App.WindowHandle, NativeMethods.WM_SETICON, NativeMethods.ICON_BIG, ico3.Handle);
                        NativeMethods.SendMessage(App.WindowHandle, NativeMethods.WM_SETICON, NativeMethods.ICON_SMALL, ico3.Handle);
                        NativeMethods.SendMessage(App.WindowHandle, NativeMethods.WM_SETICON, NativeMethods.ICON_SMALL2, ico3.Handle);
                    }
                    else
                    {
                        if (App.AppWin != null) { App.SetIcon("Assets/Scale3.ico", App.AppWin); }
                    }
                    break;
                case float f when f > 19:
                    imgTitle.Source = new BitmapImage(new Uri("ms-appx:///Assets/Scale2.png"));
                    if (useDrawing && ico2 != null)
                    {
                        NativeMethods.SendMessage(App.WindowHandle, NativeMethods.WM_SETICON, NativeMethods.ICON_BIG, ico2.Handle);
                        NativeMethods.SendMessage(App.WindowHandle, NativeMethods.WM_SETICON, NativeMethods.ICON_SMALL, ico2.Handle);
                        NativeMethods.SendMessage(App.WindowHandle, NativeMethods.WM_SETICON, NativeMethods.ICON_SMALL2, ico2.Handle);
                    }
                    else
                    {
                        if (App.AppWin != null) { App.SetIcon("Assets/Scale2.ico", App.AppWin); }
                    }
                    break;
                case float f when f > 1:
                    imgTitle.Source = new BitmapImage(new Uri("ms-appx:///Assets/Scale1.png"));
                    if (useDrawing && ico1 != null)
                    {
                        NativeMethods.SendMessage(App.WindowHandle, NativeMethods.WM_SETICON, NativeMethods.ICON_BIG, ico1.Handle);
                        NativeMethods.SendMessage(App.WindowHandle, NativeMethods.WM_SETICON, NativeMethods.ICON_SMALL, ico1.Handle);
                        NativeMethods.SendMessage(App.WindowHandle, NativeMethods.WM_SETICON, NativeMethods.ICON_SMALL2, ico1.Handle);
                    }
                    else
                    {
                        if (App.AppWin != null) { App.SetIcon("Assets/Scale1.ico", App.AppWin); }
                    }
                    break;
                default:
                    imgTitle.Source = new BitmapImage(new Uri("ms-appx:///Assets/Scale0.png"));
                    if (ico0 != null)
                    {
                        NativeMethods.SendMessage(App.WindowHandle, NativeMethods.WM_SETICON, NativeMethods.ICON_BIG, ico0.Handle);
                        NativeMethods.SendMessage(App.WindowHandle, NativeMethods.WM_SETICON, NativeMethods.ICON_SMALL, ico0.Handle);
                        NativeMethods.SendMessage(App.WindowHandle, NativeMethods.WM_SETICON, NativeMethods.ICON_SMALL2, ico0.Handle);
                    }
                    else
                    {
                        if (App.AppWin != null) { App.SetIcon("Assets/Scale0.ico", App.AppWin); }
                    }
                    break;
            }
            #endregion

            #region [Packaged Style Image Update]
            //string path = Environment.CurrentDirectory;
            //var folder = await StorageFolder.GetFolderFromPathAsync(path);
            //StorageFile file = await folder.GetFileAsync("Assets\\splash2.png");
            //await LoadImageAsync(file);
            #endregion
        }
    }

    /// <summary>
    /// Loads image from file to bitmap and displays it in UI.
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    async Task LoadImageAsync(StorageFile file)
    {
        using (var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
        {
            var decoder = await BitmapDecoder.CreateAsync(stream);
            bitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            var imgSource = new WriteableBitmap(bitmap.PixelWidth, bitmap.PixelHeight);
            bitmap.CopyToBuffer(imgSource.PixelBuffer);
            await imgTitle.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { imgTitle.Source = imgSource; });
        }
    }
    // Bitmap holder of currently loaded image.
    private SoftwareBitmap? bitmap;
}
