using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;

using Windows.Networking.Connectivity;

using Microcharts;
using SkiaSharp;

using Monitor.Helpers;
using System.Drawing.Printing;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Monitor;

/// <summary>
/// TODO: Add selection for different chart types.
/// </summary>
public sealed partial class UsagePage : Page
{
    bool _initialized = false;
    int _maxPoints = 21;
    DispatcherTimer? _timer;

    #region [Performance Counters]
    /*
     *   [Note on Performance Counters]
     *   
     *   There are about 2 giga-zillion categories and sub-categories/instances, I am only showing a few
     *   extremely common counters in this utility. One could go mad investigating them all. Your categories 
     *   may differ from the example due to the version of OS and what features you have enabled/disabled.
     *   You can check out the local method "DumpPerformanceCounterCategories()" to see more.
     */

    // Only possible due to our System.Diagnostics.PerformanceCounter NuGet, sadly dotNET Core does not offer the PerformanceCounter (due to x-plat).
    System.Diagnostics.PerformanceCounter? perfCPU = null;
    System.Diagnostics.PerformanceCounter? perfMemory = null;
    System.Diagnostics.PerformanceCounter? perfLogicalDisk = null;
    System.Diagnostics.PerformanceCounter? perfDiskRead = null;
    System.Diagnostics.PerformanceCounter? perfDiskWrite = null;
    System.Diagnostics.PerformanceCounter? perfFileSysRead = null;
    System.Diagnostics.PerformanceCounter? perfFileSysWrite = null;
    System.Diagnostics.PerformanceCounter? perfTCPv4Read = null;
    System.Diagnostics.PerformanceCounter? perfTCPv4Write = null;
    System.Diagnostics.PerformanceCounter? perfIPv4Read = null;
    System.Diagnostics.PerformanceCounter? perfIPv4Write = null;
    System.Diagnostics.PerformanceCounter? perfEvents = null;
    System.Diagnostics.PerformanceCounter? perfSystem = null;

    // This is simpler than trying to determine the NIC name/instance via perf counter.
    ConnectionProfile? netProfile = null;

    #endregion

    #region [Microcharts]
    Thickness marginNet = new(-6, 14, -6, -40);
    ConcurrentQueue<float> usageQueueNet = new ConcurrentQueue<float>();
    public List<ChartEntry> _entriesNet = new();
    public Chart NetChart
    {
        get
        {
            var chart = new LineChart
            {
                Entries = _entriesNet,
                LabelOrientation = Microcharts.Orientation.Horizontal,
                ValueLabelOrientation = Microcharts.Orientation.Horizontal,
                LabelTextSize = 9,
                EnableYFadeOutGradient = true,
                Typeface = SKTypeface.FromFile(System.IO.Path.Combine(Environment.CurrentDirectory, "Assets\\Fonts\\DashDigital7.ttf")),
                IsAnimated = false,
                //Margin = -10,
                //AnimationDuration = TimeSpan.FromMilliseconds(250)
            };

            return chart;
        }
    }

    Thickness marginCPU = new(-6, 14, -6, -40);
    ConcurrentQueue<float> usageQueueCPU = new ConcurrentQueue<float>();
    public List<ChartEntry> _entriesCPU = new();
    public Chart CPUChart
    {
        get
        {
            var chart = new LineChart
            {
                Entries = _entriesCPU,
                LabelOrientation = Microcharts.Orientation.Horizontal,
                ValueLabelOrientation = Microcharts.Orientation.Horizontal,
                LabelTextSize = 10,  
                EnableYFadeOutGradient = true, 
                Typeface = SKTypeface.FromFile(System.IO.Path.Combine(Environment.CurrentDirectory, "Assets\\Fonts\\DashDigital7.ttf")),
                IsAnimated = false,
                //Margin = -10,
                //AnimationDuration = TimeSpan.FromMilliseconds(250)
            };

            return chart;
        }
    }

    Thickness marginMem = new(-6, 14, -6, -40);
    ConcurrentQueue<float> usageQueueMem = new ConcurrentQueue<float>();
    public List<ChartEntry> _entriesMem = new();
    public Chart MemChart
    {
        get
        {
            var chart = new LineChart
            {
                Entries = _entriesMem,
                LabelOrientation = Microcharts.Orientation.Horizontal,
                ValueLabelOrientation = Microcharts.Orientation.Horizontal,
                LabelTextSize = 9,
                EnableYFadeOutGradient = true,
                Typeface = SKTypeface.FromFile(System.IO.Path.Combine(Environment.CurrentDirectory, "Assets\\Fonts\\DashDigital7.ttf")),
                IsAnimated = false,
                //Margin = -10,
                //AnimationDuration = TimeSpan.FromMilliseconds(250)
            };

            return chart;
        }
    }

    Thickness marginDisk = new(-6, 14, -6, -40);
    ConcurrentQueue<float> usageQueueDisk = new ConcurrentQueue<float>();
    public List<ChartEntry> _entriesDisk = new();
    public Chart DiskChart
    {
        get
        {
            var chart = new LineChart
            {
                Entries = _entriesDisk,
                LabelOrientation = Microcharts.Orientation.Horizontal,
                ValueLabelOrientation = Microcharts.Orientation.Horizontal,
                LabelTextSize = 9,
                EnableYFadeOutGradient = true,
                Typeface = SKTypeface.FromFile(System.IO.Path.Combine(Environment.CurrentDirectory, "Assets\\Fonts\\DashDigital7.ttf")),
                IsAnimated = false,
                //Margin = -10,
                //AnimationDuration = TimeSpan.FromMilliseconds(250)
            };

            return chart;
        }
    }

    Thickness marginFS = new(-6, 14, -6, -40);
    ConcurrentQueue<float> usageQueueFS = new ConcurrentQueue<float>();
    public List<ChartEntry> _entriesFS = new();
    public Chart FileSysChart
    {
        get
        {
            var chart = new LineChart
            {
                Entries = _entriesFS,
                LabelOrientation = Microcharts.Orientation.Horizontal,
                ValueLabelOrientation = Microcharts.Orientation.Horizontal,
                LabelTextSize = 9,
                EnableYFadeOutGradient = true,
                Typeface = SKTypeface.FromFile(System.IO.Path.Combine(Environment.CurrentDirectory, "Assets\\Fonts\\DashDigital7.ttf")),
                IsAnimated = false,
                //Margin = -10,
                //AnimationDuration = TimeSpan.FromMilliseconds(250)
            };

            return chart;
        }
    }

    Thickness marginSys = new(-6, 14, -6, -40);
    ConcurrentQueue<float> usageQueueSys = new ConcurrentQueue<float>();
    public List<ChartEntry> _entriesSys = new();
    public Chart SystemChart
    {
        get
        {
            var chart = new LineChart
            {
                Entries = _entriesSys,
                LabelOrientation = Microcharts.Orientation.Horizontal,
                ValueLabelOrientation = Microcharts.Orientation.Horizontal,
                LabelTextSize = 9,
                EnableYFadeOutGradient = true,
                Typeface = SKTypeface.FromFile(System.IO.Path.Combine(Environment.CurrentDirectory, "Assets\\Fonts\\DashDigital7.ttf")),
                IsAnimated = false,
                //Margin = -10,
                //AnimationDuration = TimeSpan.FromMilliseconds(250)
            };

            return chart;
        }
    }

    #region [Other Styles]
    public Chart LineChart
    {
        get
        {
            var chart = new LineChart
            {
                Entries = _entriesCPU,
                LabelOrientation = Microcharts.Orientation.Horizontal,
                ValueLabelOrientation = Microcharts.Orientation.Horizontal,
                IsAnimated = false
            };

            return chart;
        }
    }
    public Chart BarChart
    {
        get
        {
            var chart = new BarChart
            {
                Entries = _entriesCPU,

                LabelOrientation = Microcharts.Orientation.Horizontal,
                ValueLabelOrientation = Microcharts.Orientation.Horizontal,

                IsAnimated = false
            };

            return chart;
        }
    }
    public Chart RadarChart
    {
        get
        {
            var chart = new RadarChart
            {
                Entries = _entriesCPU,

                IsAnimated = false
            };

            return chart;
        }
    }
    public Chart RadialGaugeChart
    {
        get
        {
            var chart = new RadialGaugeChart
            {
                Entries = _entriesCPU,

                IsAnimated = false
            };

            return chart;
        }
    }
    #endregion

    #endregion


    /// <summary>
    /// An event that the MainWindow can subscribe to.
    /// </summary>
    public static event EventHandler<float>? TitleUpdateEvent;

	/// <summary>
	/// Constructor
	/// </summary>
	public UsagePage()
    {
        Debug.WriteLine($"{MethodBase.GetCurrentMethod()?.DeclaringType?.Name}__{MethodBase.GetCurrentMethod()?.Name} [{DateTime.Now.ToString("hh:mm:ss.fff tt")}]");

		netProfile = NetworkInformation.GetInternetConnectionProfile();

		this.InitializeComponent();

        // Ensure that the Page is only created once, and cached during navigation.
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled;

        //var fPath = System.IO.Path.Combine(Environment.CurrentDirectory, "Assets\\Fonts\\DashDigital7.ttf");
        //SKTypeface? _tf = SKTypeface.FromFile(fPath);

        this.Tapped += UsagePage_Tapped;
        this.Loaded += UsagePage_Loaded;
        this.Unloaded += UsagePage_Unloaded;
        this.ActualThemeChanged += UsagePage_ActualThemeChanged;
    }

	#region [Events]
	async void UsagePage_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
	{
        //_timer?.Stop();

        await App.ShowDialogBox(
            "Notice", 
            "Set window as topmost?", 
            "Yes", 
            "No",
            () => 
            {   
                // Use native call...
                App.ChangeTopmost(true);

                // Use Win32 API...
                //Task.Run(async () => { await NativeMethods.RetryTopMost(App.WindowHandle); });
            },
            () => 
            {   
                // Use native call...
                App.ChangeTopmost(false);

                // Use Win32 API...
                //Task.Run(async () => { await NativeMethods.UndoTopMost(App.WindowHandle); });
            });

        //_timer?.Start();
	}

	void UsagePage_Loaded(object sender, RoutedEventArgs e)
    {
		Debug.WriteLine($"─── {nameof(UsagePage)} started at {App.GetStopWatch().ToTimeString()} ───");

		if (_initialized)
            _initialized = true;

        //var entrails = Extensions.ReflectFieldInfo(typeof(Microcharts.Chart));

        #region [Init Counters]
        popup.IsOpen = true;
        // These can take seconds to initialize as the system allocates the
        // diagnostics controller resources, depending on quantity of counters.
        Task.Run(() =>
        {
            switch (App.GraphType)
            {
                case eGraphType.CPU:
                    perfCPU = new System.Diagnostics.PerformanceCounter("Processor Information", "% Processor Time", "_Total", true);
                    break;
                case eGraphType.DISK:
		            perfDiskRead = new System.Diagnostics.PerformanceCounter("PhysicalDisk", "Disk Reads/sec", "_Total", true);
                    perfDiskWrite = new System.Diagnostics.PerformanceCounter("PhysicalDisk", "Disk Writes/sec", "_Total", true);
		            //perfLogicalDisk = new System.Diagnostics.PerformanceCounter("LogicalDisk", "% Disk Time", "_Total", true);
                    break;
                case eGraphType.RAM:
                    perfMemory = new System.Diagnostics.PerformanceCounter("Memory", "Available MBytes", true);
                    break;
                case eGraphType.LAN:
                    // We're now using the native Windows.Networking.Connectivity.ConnectionProfile, but if you want pure perfcntr action then these will also work...
                    //perfTCPv4Read = new System.Diagnostics.PerformanceCounter("TCPv4", "Segments Received/sec", true);
                    //perfTCPv4Write = new System.Diagnostics.PerformanceCounter("TCPv4", "Segments Sent/sec", true);
                    //perfIPv4Read = new System.Diagnostics.PerformanceCounter("TCPv4", "Datagrams Received/sec", true);
                    //perfIPv4Write = new System.Diagnostics.PerformanceCounter("TCPv4", "Datagrams Sent/sec", true);
                    break;
                case eGraphType.FS:
                    perfFileSysWrite = new System.Diagnostics.PerformanceCounter("FileSystem Disk Activity", "FileSystem Bytes Written", "_Total", true);
                    perfFileSysRead = new System.Diagnostics.PerformanceCounter("FileSystem Disk Activity", "FileSystem Bytes Read", "_Total", true);
                    break;
                case eGraphType.SYS: 
                    // A good indicator for how busy a system is overall.
                    perfSystem = new System.Diagnostics.PerformanceCounter("System", "System Calls/sec", true);
                    //perfEvents = new System.Diagnostics.PerformanceCounter("Event Log", "Events/sec", true);
                    break;
                default:
                    break;
            }
        }).ContinueWith(async t =>
        {
            await Task.Delay(1000);
            popup.DispatcherQueue.TryEnqueue(() => { popup.IsOpen = false; });
        });
        #endregion

        #region [Show or Hide Graph Controls]
        if (App.GraphType == eGraphType.LAN) // Network
        {
            microChartNet.Opacity = 1;
            microChartDisk.Opacity = microChartSystem.Opacity = microChartFileSys.Opacity = microChartCPU.Opacity = microChartMem.Opacity = 0;
        }
        else if (App.GraphType == eGraphType.RAM) // Memory
        {
            microChartMem.Opacity = 1;
            microChartDisk.Opacity = microChartSystem.Opacity = microChartFileSys.Opacity = microChartCPU.Opacity = microChartNet.Opacity = 0;
        }
        else if (App.GraphType == eGraphType.CPU) // CPU
        {
            microChartCPU.Opacity = 1;
            microChartDisk.Opacity = microChartSystem.Opacity = microChartFileSys.Opacity = microChartMem.Opacity = microChartNet.Opacity = 0;
        }
        else if (App.GraphType == eGraphType.DISK) // Disk
        {
            microChartDisk.Opacity = 1;
            microChartMem.Opacity = microChartSystem.Opacity = microChartFileSys.Opacity = microChartCPU.Opacity = microChartNet.Opacity = 0;
        }
        else if (App.GraphType == eGraphType.FS) // File System
        {
            microChartFileSys.Opacity = 1;
            microChartMem.Opacity = microChartSystem.Opacity = microChartDisk.Opacity = microChartCPU.Opacity = microChartNet.Opacity = 0;
        }
        else if (App.GraphType == eGraphType.FS) // Calls per second
        {
            microChartSystem.Opacity = 1;
            microChartMem.Opacity = microChartFileSys.Opacity = microChartDisk.Opacity = microChartCPU.Opacity = microChartNet.Opacity = 0;
        }
        else // debug
        {
            microChartDisk.Opacity = microChartSystem.Opacity = microChartFileSys.Opacity = microChartMem.Opacity = microChartNet.Opacity = microChartCPU.Opacity = 1;
        }
        #endregion

        #region [Graph Updating Timer]
        double freq = 3.0;
        _timer = new DispatcherTimer();
        if (App.AppSettings != null && App.AppSettings.Config.Frequency >= 0.5 && App.AppSettings.Config.Frequency < 61.0)
            freq = App.AppSettings.Config.Frequency;
        _timer.Interval = TimeSpan.FromSeconds(freq);
        _timer.Tick += (_, _) =>
        {
            try
            {
                if (DispatcherQueue != null && !App.IsClosing)
                {
                    // There may be calls in the future that exceed the
                    // timer window, so stop the timer just to be safe.
                    _timer.Stop();

                    if (App.GraphType == eGraphType.LAN && netProfile != null)
                        usageQueueNet.Enqueue(GetNetwork());
                    else if (App.GraphType == eGraphType.RAM && perfMemory != null)
                        usageQueueMem.Enqueue(GetMemory());
                    else if (App.GraphType == eGraphType.CPU && perfCPU != null)
                        usageQueueCPU.Enqueue(GetCPU());
                    else if (App.GraphType == eGraphType.DISK && perfDiskRead != null && perfDiskWrite != null)
                        usageQueueDisk.Enqueue(GetDisk());
                    else if (App.GraphType == eGraphType.FS && perfFileSysRead != null && perfFileSysWrite != null)
                        usageQueueFS.Enqueue(GetFileSystem());
                    else if (App.GraphType == eGraphType.SYS && perfSystem != null)
                        usageQueueSys.Enqueue(GetSystem());

                    rootGrid.DispatcherQueue.TryEnqueue(() => { UpdateGraphSeries(); });

					_timer.Start();
				}
				else if (rootGrid.DispatcherQueue == null)
                {
                    _timer.Stop();
                }
            }
            catch (Exception)
            {
                Debug.WriteLine($"Application may be in the process of closing.");
            }
        };
        _timer.Start();
        #endregion
		
        Debug.WriteLine($"─── {nameof(UsagePage)} finished at {App.GetStopWatch().ToTimeString()} ───");
    }

	void UsagePage_Unloaded(object sender, RoutedEventArgs e)
	{
        if (_timer != null)
			_timer.Stop();
    }

    void UsagePage_ActualThemeChanged(FrameworkElement sender, object args)
    {
        RequestedTheme = RequestedTheme == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark;
        
        if (App.AppSettings != null)
            App.AppSettings.Config.Theme = $"{RequestedTheme}";

        UpdateControls();
    }

    void UpdateControls(bool useAppDictionary = true)
    {
        if (useAppDictionary)
        {
            var themeDictionary = Resources.ThemeDictionaries[ActualTheme.ToString()] as ResourceDictionary;
            if (themeDictionary != null)
                rootGrid.Background = themeDictionary["PrimaryBrush"] as SolidColorBrush;
        }
        else // use a local page resource
        {
            rootGrid.Background = Resources["SomeGridBrushName"] as SolidColorBrush;
        }
    }
    #endregion

    #region [Methods]
    /// <summary>
    /// Should not be called more than once per second.
    /// </summary>
    void UpdateGraphSeries(bool randomColor = false)
    {
        bool noText = false;

        try
        {
            // Any data yet?
            if (App.GraphType == eGraphType.CPU && usageQueueCPU.Count == 0)
                return;
            else if (App.GraphType == eGraphType.RAM && usageQueueMem.Count == 0)
                return;
            else if (App.GraphType == eGraphType.LAN && usageQueueNet.Count == 0)
                return;
            else if (App.GraphType == eGraphType.DISK && usageQueueDisk.Count == 0)
                return;
            else if (App.GraphType == eGraphType.FS && usageQueueFS.Count == 0)
                return;
            else if (App.GraphType == eGraphType.SYS && usageQueueSys.Count == 0)
                return;

            // Maximize graph space if user has made a tiny window.
            if (MainWindow.newHeight > 0 && MainWindow.newHeight < 301) { noText = true; }
            // Some rudimentary scaling. TODO: Re-adjust graph margins?
            if (MainWindow.newWidth > 0 && MainWindow.newWidth > 2500) { _maxPoints      = (noText) ? 45 : 40; }
            else if (MainWindow.newWidth > 0 && MainWindow.newWidth > 2200) { _maxPoints = (noText) ? 40 : 35; }
            else if (MainWindow.newWidth > 0 && MainWindow.newWidth > 1900) { _maxPoints = (noText) ? 37 : 32; }
            else if (MainWindow.newWidth > 0 && MainWindow.newWidth > 1500) { _maxPoints = (noText) ? 34 : 29; }
            else if (MainWindow.newWidth > 0 && MainWindow.newWidth > 1200) { _maxPoints = (noText) ? 31 : 26; }
            else if (MainWindow.newWidth > 0 && MainWindow.newWidth > 1000) { _maxPoints = (noText) ? 28 : 23; }
            else if (MainWindow.newWidth > 0 && MainWindow.newWidth > 800) { _maxPoints =  (noText) ? 24 : 19; }
            else if (MainWindow.newWidth > 0 && MainWindow.newWidth > 600) { _maxPoints =  (noText) ? 19 : 14; }
            else if (MainWindow.newWidth > 0 && MainWindow.newWidth > 400) { _maxPoints =  (noText) ? 13 : 8; }
            else if (MainWindow.newWidth > 0 && MainWindow.newWidth > 200) { _maxPoints =  (noText) ? 8 :  4; }

            // Adjust graph margin if tiny window detected.
            if (noText)
            {
                // Will look wrong at first, but the graph area will re-stretch once all text has exited.
                if (App.GraphType == eGraphType.DISK && microChartDisk.Margin == marginDisk)
                    microChartDisk.Margin = new Thickness(-2, 14, -2, -10);
                else if (App.GraphType == eGraphType.CPU && microChartCPU.Margin == marginCPU)
                    microChartCPU.Margin = new Thickness(-2, 14, -2, -10);
                else if (App.GraphType == eGraphType.RAM && microChartMem.Margin == marginMem)
                    microChartMem.Margin = new Thickness(-2, 14, -2, -10);
                else if (App.GraphType == eGraphType.LAN && microChartNet.Margin == marginNet)
                    microChartNet.Margin = new Thickness(-2, 14, -2, -10);
                else if (App.GraphType == eGraphType.FS && microChartFileSys.Margin == marginFS)
                    microChartFileSys.Margin = new Thickness(-2, 14, -2, -10);
                else if (App.GraphType == eGraphType.SYS && microChartSystem.Margin == marginSys)
                    microChartSystem.Margin = new Thickness(-2, 14, -2, -10);
            }
            else
            {
                if (App.GraphType == eGraphType.DISK && microChartDisk.Margin != marginDisk)
                    microChartDisk.Margin = marginDisk;
                else if (App.GraphType == eGraphType.CPU && microChartCPU.Margin != marginCPU)
                    microChartCPU.Margin = marginCPU;
                else if (App.GraphType == eGraphType.RAM && microChartMem.Margin != marginMem)
                    microChartMem.Margin = marginMem;
                else if (App.GraphType == eGraphType.LAN && microChartNet.Margin != marginNet)
                    microChartNet.Margin = marginNet;
                else if (App.GraphType == eGraphType.FS && microChartFileSys.Margin != marginFS)
                    microChartFileSys.Margin = marginFS;
                else if (App.GraphType == eGraphType.SYS && microChartSystem.Margin != marginSys)
                    microChartSystem.Margin = marginSys;
            }

            // var clr = GetRandomSKColor();
            var clr = Extensions.GetRandomColorString();

            if (App.GraphType == eGraphType.LAN)
            {
                if (usageQueueNet.TryDequeue(out float tcp))
                {

                    _entriesNet.Add(
                    new ChartEntry(tcp)
                    {
                        Label = noText ? "" : ".",
                        TextColor = SKColor.Parse(clr),
                        ValueLabel = noText ? "" : $"{tcp.ToFileSize()}",
                        ValueLabelColor = SKColor.Parse(clr),
                        Color = SKColor.Parse(clr)
                    });

                    while (_entriesNet.Count > _maxPoints)
                    {   // Rolling style
                        _entriesNet.RemoveAt(0);
                    }

                    // Re-paint
                    microChartNet.Invalidate();

                    // Signal the main window for title update.
                    TitleUpdateEvent?.Invoke(this, tcp);
                }
            }
            else if (App.GraphType == eGraphType.DISK)
            {
                if (usageQueueDisk.TryDequeue(out float dsk))
                {

                    _entriesDisk.Add(
                    new ChartEntry(dsk)
                    {
                        Label = noText ? "" : ".",
                        TextColor = SKColor.Parse(clr),
                        ValueLabel = noText ? "" : $"{dsk:N0}/s",
                        ValueLabelColor = SKColor.Parse(clr),
                        Color = SKColor.Parse(clr)
                    });

                    while (_entriesDisk.Count > _maxPoints)
                    {   // Rolling style
                        _entriesDisk.RemoveAt(0);
                    }

                    // Re-paint
                    microChartDisk.Invalidate();

                    // Signal the main window for title update.
                    TitleUpdateEvent?.Invoke(this, dsk);
                }
            }
            else if (App.GraphType == eGraphType.RAM)
            {
                if (usageQueueMem.TryDequeue(out float mem))
                {

                    _entriesMem.Add(
                    new ChartEntry(mem)
                    {
                        Label = noText ? "" : ".",
                        TextColor = SKColor.Parse(clr),
                        ValueLabel = noText ? "" : $"{mem.ToFileSize()}",
                        ValueLabelColor = SKColor.Parse(clr),
                        Color = SKColor.Parse(clr)
                    });

                    while (_entriesMem.Count > _maxPoints)
                    {   // Rolling style
                        _entriesMem.RemoveAt(0);
                    }

                    // Re-paint
                    microChartMem.Invalidate();

                    // Signal the main window for title update.
                    TitleUpdateEvent?.Invoke(this, mem);
                }
            }
            else if (App.GraphType == eGraphType.CPU)
            {
                if (usageQueueCPU.TryDequeue(out float cpu))
                {
                    _entriesCPU.Add(
                    new ChartEntry(cpu)
                    {
                        Label = noText ? "" : ".",
                        TextColor = SKColor.Parse(clr),
                        ValueLabel = noText ? "" : $"{cpu:N0}%",
                        ValueLabelColor = GetSKLineColor(cpu),
                        Color = GetSKLineColor(cpu)
                    });

                    while (_entriesCPU.Count > _maxPoints)
                    {   // Rolling style
                        _entriesCPU.RemoveAt(0);

                        // We could clear all points and only keep last 5 to repopulate graph.
                        //var keep = _entriesCPU.TakeLastFive().ToList(); // convert to list or our _entriesCPU.Clear() with cause a problem when the IEnumerable gets evaluated later
                        //_entriesCPU.Clear();
                        //foreach (var item in keep) { _entriesCPU.Add(item); }
                    }

                    // Re-paint
                    microChartCPU.Invalidate();

                    // Signal the main window for title update.
                    TitleUpdateEvent?.Invoke(this, cpu);
                }
            }
            else if (App.GraphType == eGraphType.FS)
            {
                if (usageQueueFS.TryDequeue(out float fs))
                {

                    _entriesFS.Add(
                    new ChartEntry(fs)
                    {
                        Label = noText ? "" : ".",
                        TextColor = SKColor.Parse(clr),
                        ValueLabel = noText ? "" : $"{fs.ToFileSize()}",
                        ValueLabelColor = SKColor.Parse(clr),
                        Color = SKColor.Parse(clr)
                    });

                    while (_entriesFS.Count > _maxPoints)
                    {   // Rolling style
                        _entriesFS.RemoveAt(0);
                    }

                    // Re-paint
                    microChartFileSys.Invalidate();

                    // Signal the main window for title update.
                    TitleUpdateEvent?.Invoke(this, fs);
                }
            }
            else if (App.GraphType == eGraphType.SYS)
            {
                if (usageQueueSys.TryDequeue(out float sys))
                {

                    _entriesSys.Add(
                    new ChartEntry(sys)
                    {
                        Label = noText ? "" : ".",
                        TextColor = SKColor.Parse(clr),
                        ValueLabel = noText ? "" : $"{sys.ToAbbreviatedSize()}",
                        ValueLabelColor = SKColor.Parse(clr),
                        Color = SKColor.Parse(clr)
                    });

                    while (_entriesSys.Count > _maxPoints)
                    {   // Rolling style
                        _entriesSys.RemoveAt(0);
                    }

                    // Re-paint
                    microChartSystem.Invalidate();

                    // Signal the main window for title update.
                    TitleUpdateEvent?.Invoke(this, sys);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"UpdateGraphSeries: {ex.Message}");
        }
    }

    /// <summary>
    /// For CPU pressure.
    /// </summary>
    SKColor GetSKLineColor(float newValue)
    {
        switch (newValue)
        {
            case float f when f > 89:
                return SKColors.OrangeRed;
            case float f when f > 79:
                return SKColors.Orange;
            case float f when f > 49:
                return SKColors.Yellow;
            case float f when f > 29:
                return SKColors.YellowGreen;
            case float f when f > 19:
                return SKColors.SpringGreen;
            case float f when f > 9:
                return SKColors.MediumAquamarine;
            default:
                return SKColors.DodgerBlue;
        }
    }

    /// <summary>
    /// Returns a random selection from <see cref="SkiaSharp.SKColors"/>.
    /// </summary>
    /// <returns><see cref="SkiaSharp.SKColor"/></returns>
    static SkiaSharp.SKColor GetRandomSKColor()
    {
        try
        {
            var colorType = typeof(SkiaSharp.SKColors);
            //var clrs = colorType.GetFields();

            var colors = colorType.GetFields()
                .Where(p => p.FieldType.Name == nameof(SKColor) && p.IsStatic && p.IsPublic)
                .Select(p => (SKColor?)p.GetValue(null) ?? SKColor.Parse(Extensions.GetRandomColorString()))
                .ToList();

            if (colors.Count > 0)
            {
                var randomIndex = Random.Shared.Next(colors.Count);
                var randomColor = colors[randomIndex];
                if (randomColor.Alpha != (byte)0)
                    return randomColor;
                else
                    return SKColors.LightGray;
            }
            else
            {
                return SKColors.LightGray;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GetRandomSKColor: {ex.Message}");
            return SKColors.Red;
        }
    }

	/// <summary>
	/// CPU Performance
	/// </summary>
	float GetCPU()
	{
        if (perfCPU  == null)
            return 0;

        float newValue = perfCPU.NextValue();
        return newValue;
	}

    /// <summary>
    /// Memory Performance
    /// </summary>
    float GetMemory()
    {
        if (perfMemory == null)
            return 0;

        return perfMemory.NextValue();
    }

    /// <summary>
    /// Disk Performance
    /// </summary>
    float GetDisk()
    {
        return GetDiskRead() + GetDiskWrite();
    }

    /// <summary>
    /// Disk Read Performance
    /// </summary>
    float GetDiskRead()
    {
        if (perfDiskRead == null) 
            return 0;

        return perfDiskRead.NextValue();
    }

    /// <summary>
    /// Disk Write Performance
    /// </summary>
    float GetDiskWrite()
    {
        if (perfDiskWrite == null)
            return 0;
        
        return perfDiskWrite.NextValue();
    }

    /// <summary>
    /// File System Performance
    /// </summary>
    float GetFileSystem()
    {
        return GetFileSysRead() + GetFileSysWrite();
    }

    /// <summary>
    /// File system bytes read
    /// </summary>
    float GetFileSysRead()
    {
        if (perfFileSysRead == null) return 0;
        return perfFileSysRead.NextValue();
    }

    /// <summary>
    /// File system bytes written
    /// </summary>
    float GetFileSysWrite()
    {
        if (perfFileSysWrite == null) return 0;
        return perfFileSysWrite.NextValue();
    }

    /// <summary>
    /// System calls per second
    /// </summary>
    float GetSystem()
    {
        if (perfSystem == null)
            return 0;

        return perfSystem.NextValue();
    }

    /// <summary>
    /// LAN Performance using <see cref="ConnectionProfile"/>
    /// </summary>
	float GetNetwork()
	{
        try
        {
            if (netProfile == null)
                return 0f;

		    var networkUsageStates = new NetworkUsageStates(TriStates.DoNotCare, TriStates.DoNotCare);
            var names = netProfile.GetNetworkNames();
            foreach(var name in names)
            {
				Debug.WriteLine($"Network Name: {name}");
			}

			//var netUsageList = netProfile?.GetNetworkUsageAsync(DateTimeOffset.Now - TimeSpan.FromMinutes(1), DateTimeOffset.Now, DataUsageGranularity.Total, networkUsageStates).GetAwaiter().GetResult();
			var netUsageList = netProfile.GetNetworkUsageAsync(DateTimeOffset.Now - TimeSpan.FromMinutes(1), DateTimeOffset.Now, DataUsageGranularity.Total, networkUsageStates).AsTask().Result;
			float totalBytesSent = 0;
            float totalBytesReceived = 0;
            if (netUsageList != null)
            {
                foreach (var netUsage in netUsageList)
                {
                    totalBytesSent += (float)netUsage.BytesSent;
                    totalBytesReceived += (float)netUsage.BytesReceived;
                }
                // Show the network I/O amounts.
                Debug.WriteLine($"BytesSent: {totalBytesSent:N0},  BytesReceived: {totalBytesReceived:N0}");
            }

            return totalBytesSent + totalBytesReceived;

		}
        catch (Exception ex)
        {
			Debug.WriteLine($"GetNetworkUsage: {ex.Message}");
			return 0f;
		}
	}

    /// <summary>
    /// LAN Performance using <see cref="PerformanceCounter"/>
    /// </summary>
    float GetTCPv4()
    {
		if (perfTCPv4Read == null) return 0;
		if (perfTCPv4Write == null) return 0;
        return perfTCPv4Read.NextValue() + perfTCPv4Write.NextValue();
	}

	/// <summary>
	/// Retrieves a PerformanceCounter result based on its CounterType.
	/// </summary>
	/// <param name="pCounter"><see cref="PerformanceCounter"/></param>
	public static string GetCounterValue(PerformanceCounter pCounter)
    {
        string retval = "";

        switch (pCounter.CounterType)
        {
            case PerformanceCounterType.NumberOfItems32:
                retval = pCounter.RawValue.ToString();
                break;
            case PerformanceCounterType.NumberOfItems64:
                retval = pCounter.RawValue.ToString();
                break;
            case PerformanceCounterType.RateOfCountsPerSecond32:
                retval = pCounter.NextValue().ToString();
                break;
            case PerformanceCounterType.RateOfCountsPerSecond64:
                retval = pCounter.NextValue().ToString();
                break;
            case PerformanceCounterType.AverageTimer32:
                retval = pCounter.NextValue().ToString();
                break;
            default:
                Debug.WriteLine($"Counter type undefined: '{pCounter.CounterType}'");
                retval = pCounter.NextValue().ToString();
                break;
        }

        return retval;
    }

    /// <summary>
    /// Shows a list of the root performance counter categories.
    /// Categories will be different based on your OS and what features you have enabled/disabled.
    /// </summary>
    void DumpPerformanceCounterCategories()
    {
        var performanceCounterCategories = PerformanceCounterCategory.GetCategories();
        foreach (PerformanceCounterCategory pcc in performanceCounterCategories)
        {
            Debug.WriteLine($"Category name: {pcc.CategoryName} ({pcc.CategoryType})");
        }
        /*
           Category name: .NET CLR Data
           Category name: .NET CLR Exceptions
           Category name: .NET CLR Interop
           Category name: .NET CLR Jit
           Category name: .NET CLR Loading
           Category name: .NET CLR LocksAndThreads
           Category name: .NET CLR Memory
           Category name: .NET CLR Networking
           Category name: .NET CLR Networking 4.0.0.0
           Category name: .NET CLR Remoting
           Category name: .NET CLR Security
           Category name: .NET Data Provider for Oracle
           Category name: .NET Data Provider for SqlServer
           Category name: .NET Memory Cache 4.0
           Category name: Active Server Pages
           Category name: APP_POOL_WAS
           Category name: AppV Client Streamed Data Percentage
           Category name: ASP.NET
           Category name: ASP.NET Applications
           Category name: ASP.NET Apps v4.0.30319
           Category name: ASP.NET State Service
           Category name: ASP.NET v4.0.30319
           Category name: ASP.NET, Version 2.0.50727
           Category name: ASP.NET-Anwendungen, Version 2.0.50727
           Category name: Authorization Manager Applications
           Category name: Battery Status
           Category name: BitLocker
           Category name: BITS Net Utilization
           Category name: Bluetooth Device
           Category name: Bluetooth Radio
           Category name: BranchCache
           Category name: Browser
           Category name: Cache
           Category name: CCM Endpoint
           Category name: CCM Message Queue
           Category name: Client Side Caching
           Category name: Database
           Category name: Database ==> Databases
           Category name: Database ==> Instances
           Category name: Database ==> TableClasses
           Category name: Distributed Routing Table
           Category name: Distributed Transaction Coordinator
           Category name: DNS64 Global
           Category name: Energy Meter
           Category name: Event Log
           Category name: Event Tracing for Windows
           Category name: Event Tracing for Windows Session
           Category name: Fax Service
           Category name: FileSystem Disk Activity
           Category name: Generic IKEv1, AuthIP, and IKEv2
           Category name: GPU Adapter Memory
           Category name: GPU Engine
           Category name: GPU Local Adapter Memory
           Category name: GPU Non Local Adapter Memory
           Category name: GPU Process Memory
           Category name: HTTP Service
           Category name: HTTP Service Request Queues
           Category name: HTTP Service Url Groups
           Category name: Hyper-V Dynamic Memory Integration Service
           Category name: Hyper-V Hypervisor
           Category name: Hyper-V Hypervisor Logical Processor
           Category name: Hyper-V Hypervisor Root Partition
           Category name: Hyper-V Hypervisor Root Virtual Processor
           Category name: Hyper-V Virtual Machine Bus Pipes
           Category name: Hyper-V VM Vid Partition
           Category name: ICMP
           Category name: ICMPv6
           Category name: Internet Information Services Global
           Category name: IPHTTPS Global
           Category name: IPHTTPS Session
           Category name: IPsec AuthIP IPv4
           Category name: IPsec AuthIP IPv6
           Category name: IPsec Connections
           Category name: IPsec Driver
           Category name: IPsec IKEv1 IPv4
           Category name: IPsec IKEv1 IPv6
           Category name: IPsec IKEv2 IPv4
           Category name: IPsec IKEv2 IPv6
           Category name: IPv4
           Category name: IPv6
           Category name: Job Object Details
           Category name: LogicalDisk
           Category name: Memory
           Category name: Microsoft Winsock BSP
           Category name: MSDTC Bridge 3.0.0.0
           Category name: MSDTC Bridge 4.0.0.0
           Category name: MSMQ Incoming Multicast Session
           Category name: MSMQ Outgoing HTTP Session
           Category name: MSMQ Outgoing Multicast Session
           Category name: MSMQ Queue
           Category name: MSMQ Service
           Category name: MSMQ Session
           Category name: NBT Connection
           Category name: Netlogon
           Category name: Network Adapter
           Category name: Network Interface
           Category name: Network QoS Policy
           Category name: No name
           Category name: NUMA Node Memory
           Category name: Objects
           Category name: Offline Files
           Category name: Pacer Flow
           Category name: Pacer Pipe
           Category name: PacketDirect EC Utilization
           Category name: PacketDirect Queue Depth
           Category name: PacketDirect Receive Counters
           Category name: PacketDirect Receive Filters
           Category name: PacketDirect Transmit Counters
           Category name: Paging File
           Category name: Peer Name Resolution Protocol
           Category name: Per Processor Network Activity Cycles
           Category name: Per Processor Network Interface Card Activity
           Category name: Physical Network Interface Card Activity
           Category name: PhysicalDisk
           Category name: Power Meter
           Category name: PowerShell Workflow
           Category name: Print Queue
           Category name: Process
           Category name: Processor
           Category name: Processor Information
           Category name: Processor Performance
           Category name: RAS
           Category name: RAS Port
           Category name: RAS Total
           Category name: ReadyBoost Cache
           Category name: Redirector
           Category name: ReFS
           Category name: RemoteFX Graphics
           Category name: RemoteFX Network
           Category name: Search Gatherer
           Category name: Search Gatherer Projects
           Category name: Search Indexer
           Category name: Security Per-Process Statistics
           Category name: Security System-Wide Statistics
           Category name: Server
           Category name: Server Work Queues
           Category name: ServiceModelEndpoint 3.0.0.0
           Category name: ServiceModelEndpoint 4.0.0.0
           Category name: ServiceModelOperation 3.0.0.0
           Category name: ServiceModelOperation 4.0.0.0
           Category name: ServiceModelService 3.0.0.0
           Category name: ServiceModelService 4.0.0.0
           Category name: SMB Client Shares
           Category name: SMB Direct Connection
           Category name: SMB Server
           Category name: SMB Server Sessions
           Category name: SMB Server Shares
           Category name: SMSvcHost 3.0.0.0
           Category name: SMSvcHost 4.0.0.0
           Category name: SQL Server 2017 XTP Cursors
           Category name: SQL Server 2017 XTP Databases
           Category name: SQL Server 2017 XTP Garbage Collection
           Category name: SQL Server 2017 XTP IO Governor
           Category name: SQL Server 2017 XTP Phantom Processor
           Category name: SQL Server 2017 XTP Storage
           Category name: SQL Server 2017 XTP Transaction Log
           Category name: SQL Server 2017 XTP Transactions
           Category name: SQLAgent:Alerts
           Category name: SQLAgent:Jobs
           Category name: SQLAgent:JobSteps
           Category name: SQLAgent:Statistics
           Category name: SQLAgent:SystemJobs
           Category name: SQLServer:Access Methods
           Category name: SQLServer:Advanced Analytics
           Category name: SQLServer:Availability Group
           Category name: SQLServer:Availability Replica
           Category name: SQLServer:Backup Device
           Category name: SQLServer:Batch Resp Statistics
           Category name: SQLServer:Broker Activation
           Category name: SQLServer:Broker Statistics
           Category name: SQLServer:Broker TO Statistics
           Category name: SQLServer:Broker/DBM Transport
           Category name: SQLServer:Buffer Manager
           Category name: SQLServer:Buffer Node
           Category name: SQLServer:Catalog Metadata
           Category name: SQLServer:CLR
           Category name: SQLServer:Columnstore
           Category name: SQLServer:Cursor Manager by Type
           Category name: SQLServer:Cursor Manager Total
           Category name: SQLServer:Database Mirroring
           Category name: SQLServer:Database Replica
           Category name: SQLServer:Databases
           Category name: SQLServer:Deprecated Features
           Category name: SQLServer:Exec Statistics
           Category name: SQLServer:External Scripts
           Category name: SQLServer:FileTable
           Category name: SQLServer:General Statistics
           Category name: SQLServer:HTTP Storage
           Category name: SQLServer:Latches
           Category name: SQLServer:Locks
           Category name: SQLServer:LogPool FreePool
           Category name: SQLServer:Memory Broker Clerks
           Category name: SQLServer:Memory Manager
           Category name: SQLServer:Memory Node
           Category name: SQLServer:Plan Cache
           Category name: SQLServer:Query Store
           Category name: SQLServer:Replication Agents
           Category name: SQLServer:Replication Dist.
           Category name: SQLServer:Replication Logreader
           Category name: SQLServer:Replication Merge
           Category name: SQLServer:Replication Snapshot
           Category name: SQLServer:Resource Pool Stats
           Category name: SQLServer:SQL Errors
           Category name: SQLServer:SQL Statistics
           Category name: SQLServer:Transactions
           Category name: SQLServer:User Settable
           Category name: SQLServer:Wait Statistics
           Category name: SQLServer:Workload Group Stats
           Category name: Storage Management WSP Spaces Runtime
           Category name: Storage Spaces Drt
           Category name: Storage Spaces Tier
           Category name: Storage Spaces Virtual Disk
           Category name: Storage Spaces Write Cache
           Category name: Synchronization
           Category name: SynchronizationNuma
           Category name: System
           Category name: TCPIP Performance Diagnostics
           Category name: TCPIP Performance Diagnostics (Per-CPU)
           Category name: TCPv4
           Category name: TCPv6
           Category name: Telephony
           Category name: Teredo Client
           Category name: Teredo Relay
           Category name: Teredo Server
           Category name: Terminal Services
           Category name: Terminal Services Session
           Category name: Thermal Zone Information
           Category name: Thread
           Category name: UDPv4
           Category name: UDPv6
           Category name: User Input Delay per Process
           Category name: User Input Delay per Session
           Category name: W3SVC_W3WP
           Category name: WAS_W3WP
           Category name: Web Service
           Category name: Web Service Cache
           Category name: WF (System.Workflow) 4.0.0.0
           Category name: WFP // Windows Vista and Windows Server 2008 introduced the Windows Filtering Platform (WFP). WFP provides APIs to non-Microsoft independent software vendors (ISVs) to create packet processing filters. Examples include firewall and antivirus software.
           Category name: WFP Classify
           Category name: WFP Reauthorization
           Category name: WFPv4
           Category name: WFPv6
           Category name: Windows Media Player Metadata
           Category name: Windows Time Service
           Category name: Windows Workflow Foundation
           Category name: WinNAT
           Category name: WinNAT ICMP
           Category name: WinNAT Instance
           Category name: WinNAT TCP
           Category name: WinNAT UDP
           Category name: WMI Objects
           Category name: WorkflowServiceHost 4.0.0.0
           Category name: WSMan Quota Statistics
           Category name: XHCI CommonBuffer
           Category name: XHCI Interrupter
           Category name: XHCI TransferRing
         */
    }
    #endregion

}
