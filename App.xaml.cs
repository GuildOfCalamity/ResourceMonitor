using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;

using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;

using Monitor.Helpers;
using System.Threading;
using System.Drawing;

namespace Monitor;

/// <summary>
/// CPU Usage Utility
/// </summary>
public partial class App : Application
{
	#region [Properties]
	Window? m_window;
    List<Action> unsubscribeList = new List<Action>();
    static int Monitors { get; set; } = 1;
    static DisplayArea? Desktop { get; set; }
    static ValueStopwatch stopWatch { get; set; } = ValueStopwatch.StartNew();
    public static bool IsClosing { get; private set; }
    public static bool UseAcrylic { get; private set; } = false;
    public static IntPtr WindowHandle { get; private set; }
    public static AppWindow? AppWin { get; private set; }
    public static Window? MainWin { get; private set; }
    public static FrameworkElement? MainRoot { get; private set; }
    public static AppWindowPresenterKind PresenterKind { get; private set; } = AppWindowPresenterKind.Overlapped;
    public static OverlappedPresenter? Presenter { get; set; } = null;
    public static eGraphType GraphType { get; private set; } = eGraphType.CPU;
    public static SettingsManager? AppSettings { get; set; }

    // https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/#advantages-and-disadvantages-of-packaging-your-app
#if IS_UNPACKAGED // We're using a custom PropertyGroup Condition we defined in the csproj to help us with the decision.
    public static bool IsPackaged { get => false; }
#else
    public static bool IsPackaged { get => true; }
#endif
	#endregion

	#region [Window Changed Events]
	public static event Action<Windows.Graphics.PointInt32>? OnWindowMove = (position) => { };
    public static event Action<Windows.Graphics.SizeInt32>? OnWindowSizeChanged = (size) => { };
    public static event Action<string>? OnWindowClosing = (msg) => { };
    public static event Action<string>? OnWindowOrderChanged = (msg) => { };
    public static event Action<string>? OnWindowMinMax = (args) => { };
    public static event Action<string[]>? OnArgumentsReceived = (args) => { };
    #endregion

    /// <summary>
    /// Our entry point into the WinUI application.
    /// </summary>
    public App()
    {
        Debug.WriteLine($"{MethodBase.GetCurrentMethod()?.DeclaringType?.Name}__{MethodBase.GetCurrentMethod()?.Name} [{DateTime.Now.ToString("hh:mm:ss.fff tt")}]");

        #region [Exception handlers]
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
        unsubscribeList.Add(() => { AppDomain.CurrentDomain.UnhandledException -= CurrentDomainUnhandledException; });
        AppDomain.CurrentDomain.FirstChanceException += CurrentDomainFirstChanceException;
        unsubscribeList.Add(() => { AppDomain.CurrentDomain.FirstChanceException -= CurrentDomainFirstChanceException; });
        AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;
        unsubscribeList.Add(() => { AppDomain.CurrentDomain.ProcessExit -= CurrentDomainOnProcessExit; });
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        unsubscribeList.Add(() => { TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException; });
        // We'll exclude this from the unsub list in the event that an error occurs during final exit.
        this.UnhandledException += ApplicationUnhandledException;
        #endregion

        this.InitializeComponent();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        // Alternate method due to bug in WinUI.
        var cmdArgs = Environment.GetCommandLineArgs();

        // Load our config file.
        AppSettings = new SettingsManager();

        // Check for SystemBackdrop
        if (cmdArgs?.Length > 8 && !string.IsNullOrEmpty(cmdArgs[8]))
            UseAcrylic = true;

        m_window = new MainWindow();
        MainRoot = m_window.Content as FrameworkElement; // for content dialogs
        MainWin = m_window;
        m_window.Activate();

        AppWin = GetAppWindow(m_window);

        if (AppWin != null)
        {
            //AppWin.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
            //AppWin.TitleBar.IconShowOptions = IconShowOptions.ShowIconAndSystemMenu;

            // With the AppWindow we can configure other events:
            AppWin.Closing += OnWindowClosingEvent;
            unsubscribeList.Add(() => { AppWin.Closing -= OnWindowClosingEvent; });
            AppWin.Destroying += OnWindowDestroyingEvent;
            unsubscribeList.Add(() => { AppWin.Destroying -= OnWindowDestroyingEvent; });
            AppWin.Changed += OnWindowChangedEvent;
            unsubscribeList.Add(() => { AppWin.Changed -= OnWindowChangedEvent; });

            // If not using custom title bar, you can hide command buttons like so.
            if (AppWin.Presenter is OverlappedPresenter p)
            {
                //p.IsAlwaysOnTop = true;
                //p.IsMinimizable = false;
                //p.IsMaximizable = false;
                //p.IsResizable = false;
                Presenter = p;
                Debug.WriteLine($"Default PresenterKind => {Presenter.Kind}");
            }
            AppWin.SetPresenter(PresenterKind);

            SetIcon("Assets/Scale2.ico", AppWin);

            Monitors = ScreenInformation.GetMonitorCount();
            Desktop = GetDisplayArea(m_window);

			// The default AppWindowPresenterKind is Overlapped.
			if (PresenterKind == AppWindowPresenterKind.Overlapped)
            {
                if (cmdArgs?.Length < 2 && AppSettings != null && AppSettings.Config.CenterScreen == false)
                {
                    if (Desktop != null)
                    {
                        if (AppSettings.Config.PositionX > Desktop.WorkArea.Width * Monitors)
                            AppSettings.Config.PositionX = Desktop.WorkArea.Width - (AppSettings.Config.WindowWidth / 2);
                        if (AppSettings.Config.PositionY > Desktop.WorkArea.Height * Monitors)
                            AppSettings.Config.PositionY = Desktop.WorkArea.Height - (AppSettings.Config.WindowHeight / 2);
                    }
                    AppWin.MoveAndResize(new Windows.Graphics.RectInt32(AppSettings.Config.PositionX, AppSettings.Config.PositionY, AppSettings.Config.WindowWidth, AppSettings.Config.WindowHeight));
                    //AppWin.Move(new Windows.Graphics.PointInt32(200, 200));
                }
                else if (cmdArgs?.Length < 2) // standard window setup
				{
                    AppWin.Resize(new Windows.Graphics.SizeInt32(1200, 600));
                    CenterWindow(m_window);
                }
            }
        }

		#region [Processing CLI Arguments]
		if (cmdArgs?.Length > 1)
        {
            OnArgumentsReceived?.Invoke(cmdArgs);
            if (!string.IsNullOrEmpty(cmdArgs[1]) && (cmdArgs[1].Contains("cpu", StringComparison.OrdinalIgnoreCase) || cmdArgs[1].Contains("processor", StringComparison.OrdinalIgnoreCase)))
                GraphType = eGraphType.CPU;
            else if (!string.IsNullOrEmpty(cmdArgs[1]) && (cmdArgs[1].Contains("memory", StringComparison.OrdinalIgnoreCase) || cmdArgs[1].Contains("ram", StringComparison.OrdinalIgnoreCase)))
                GraphType = eGraphType.RAM;
            else if (!string.IsNullOrEmpty(cmdArgs[1]) && (cmdArgs[1].Contains("net", StringComparison.OrdinalIgnoreCase) || cmdArgs[1].Contains("lan", StringComparison.OrdinalIgnoreCase)))
                GraphType = eGraphType.LAN;
            else if (!string.IsNullOrEmpty(cmdArgs[1]) && (cmdArgs[1].Contains("disk", StringComparison.OrdinalIgnoreCase) || cmdArgs[1].Contains("ssd", StringComparison.OrdinalIgnoreCase)))
                GraphType = eGraphType.DISK;
            else if (!string.IsNullOrEmpty(cmdArgs[1]) && (cmdArgs[1].Contains("file", StringComparison.OrdinalIgnoreCase) || cmdArgs[1].Contains("fs", StringComparison.OrdinalIgnoreCase)))
                GraphType = eGraphType.FS;
            else if (!string.IsNullOrEmpty(cmdArgs[1]) && (cmdArgs[1].Contains("sys", StringComparison.OrdinalIgnoreCase) || cmdArgs[1].Contains("calls", StringComparison.OrdinalIgnoreCase)))
                GraphType = eGraphType.SYS;
            else if (!string.IsNullOrEmpty(cmdArgs[1]) && (cmdArgs[1].Contains("screen", StringComparison.OrdinalIgnoreCase) || cmdArgs[1].Contains("capture", StringComparison.OrdinalIgnoreCase)))
                CaptureScreen();

            // Check for position and size args.
            if (cmdArgs?.Length > 6 && !string.IsNullOrEmpty(cmdArgs[2]) && !string.IsNullOrEmpty(cmdArgs[3]) && !string.IsNullOrEmpty(cmdArgs[4]) && !string.IsNullOrEmpty(cmdArgs[5]) && !string.IsNullOrEmpty(cmdArgs[6]) && AppWin != null)
            {
                int m = 1;            // monitor
                int x = 0; int y = 0; // location
                int w = 0; int h = 0; // size
                if (int.TryParse(cmdArgs[2], out int arg2)) { m = arg2; } // type of graph
                if (int.TryParse(cmdArgs[3], out int arg3)) { x = arg3; } // x position
                if (int.TryParse(cmdArgs[4], out int arg4)) { y = arg4; } // y position
                if (int.TryParse(cmdArgs[5], out int arg5)) { w = arg5; } // total width
                if (int.TryParse(cmdArgs[6], out int arg6)) { h = arg6; } // total height
                // Support offsets that are slightly our of bounds.
                if (x > -30 && y > -30 && w > 0 && h > 0 && m <= Monitors && AppWin != null)
                {
					#region [Displays are considered in the horizontal plane only]
					if (Desktop != null && m > 3)      // Display 4
                        AppWin.MoveAndResize(new Windows.Graphics.RectInt32((Desktop.WorkArea.Width * 3) + x, y, w, h));
                    else if (Desktop != null && m > 2) // Display 3
                        AppWin.MoveAndResize(new Windows.Graphics.RectInt32((Desktop.WorkArea.Width * 2) + x, y, w, h));
                    else if (Desktop != null && m > 1) // Display 2
                        AppWin.MoveAndResize(new Windows.Graphics.RectInt32((Desktop.WorkArea.Width * 1) + x, y, w, h));
                    else                               // Display 1
                        AppWin.MoveAndResize(new Windows.Graphics.RectInt32(x, y, w, h));
					#endregion
				}
			}

            // Check for update frequency.
			if (AppSettings != null && cmdArgs?.Length > 7 && !string.IsNullOrEmpty(cmdArgs[7]))
				if (double.TryParse(cmdArgs[7], out double arg7)) { AppSettings.Config.Frequency = arg7; }

            #region [Extras if launched from CLI]
            //if (ConsoleManager.Attach(default))
            //    Console.WriteLine($" Starting '{GraphType}' monitor... ");
            //else
            //    Debug.WriteLine($"> No console to attach.");
            #endregion
        }
		#endregion

		#region [Changing the process priority]
		// We'll try and tell the system to run us low priority.
		Process pro = Process.GetCurrentProcess();
        if (pro != null)
            pro.PriorityClass = ProcessPriorityClass.Idle;
        #endregion

        Debug.WriteLine($"─── OnLaunched finished at {stopWatch.GetElapsedTime().ToTimeString()} ───");
    }

    public static TimeSpan GetStopWatch(bool reset = false)
    {
        var ts = stopWatch.GetElapsedTime();
        if (reset) { stopWatch = ValueStopwatch.StartNew(); }
        return ts;
    }

    /// <summary>
    /// We can signal a few different events base on the boolean flag contained
    /// in <see cref="Microsoft.UI.Windowing.AppWindowChangedEventArgs"/>.
    /// </summary>
    void OnWindowChangedEvent(AppWindow sender, AppWindowChangedEventArgs args)
    {
        /*  [AppWindowChangedEventArgs]
          ------------------------------------------------------------
            DidPositionChange....... happens on a move
            DidPresenterChange...... happens on a maximize/minimize
            DidZOrderChange......... happens on a foreground change
            DidSizeChange........... happens on size change
            DidVisibilityChange..... happens on AppWindow visibility
            ZOrderBelowWindowId..... z-order identifier
        */

        // Signal any listening events...
        if (args.DidPresenterChange) { OnWindowMinMax?.Invoke(DateTime.Now.ToString("hh:mm:ss.fff tt")); }
        if (args.DidZOrderChange) { OnWindowOrderChanged?.Invoke(DateTime.Now.ToString("hh:mm:ss.fff tt")); }
        if (args.DidSizeChange) 
        {
            if (AppSettings != null)
            {
                AppSettings.Config.WindowWidth = sender.Size.Width;
                AppSettings.Config.WindowHeight = sender.Size.Height;
            }
            OnWindowSizeChanged?.Invoke(sender.Size);
            Debug.WriteLine($"Size change: {sender.Size.Width},{sender.Size.Height}");
        }
        if (args.DidPositionChange) 
        {
            if (AppSettings != null)
            {
                AppSettings.Config.PositionX = sender.Position.X;
                AppSettings.Config.PositionY = sender.Position.Y;
            }
            OnWindowMove?.Invoke(sender.Position);
            Debug.WriteLine($"Position change: {sender.Position.X},{sender.Position.Y}");
        }
    }

    void OnWindowDestroyingEvent(AppWindow sender, object args)
    {
        if (args != null) { Debug.WriteLine($" args is of type {args.GetType()}"); }
        else { Debug.WriteLine($" args is null"); }

        // Unsubscribe from the core events.
        UnsubscribeFromEvents();
    }

    /// <summary>
    /// Uses the native <see cref="OverlappedPresenter"/> instead of Win32 API.
    /// </summary>
    /// <param name="isTop">true to make window topmost, fale otherwise</param>
    public static void ChangeTopmost(bool isTop)
    {
        if (Presenter != null)
        {
            Presenter.IsAlwaysOnTop = isTop;
        }
    }

    /// <summary>
    /// Helper for outside calling.
    /// </summary>
    /// <param name="fallback">if the namespace is null for some reason this will be used instead</param>
    /// <returns>the namespace of the type</returns>
    public static string GetCurrentNamespace(string fallback = "WinUI")
    {
        return System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType?.Namespace ?? fallback;
    }

    #region [Domain Events]
    /// <summary>
    /// https://docs.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.unhandledexception
    /// </summary>
    void ApplicationUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        Exception? ex = e.Exception;
        Debug.WriteLine($"Unhandled exception of type {ex?.GetType()}: {ex}", $"{nameof(App)}");
        e.Handled = true;
    }

    void CurrentDomainOnProcessExit(object? sender, EventArgs e)
    {
        IsClosing = true;

        if (sender is null)
            return;

        if (sender is AppDomain ad)
        {
            Debug.WriteLine($"[OnProcessExit]", $"{nameof(App)}");
            Debug.WriteLine($"DomainID: {ad.Id}", $"{nameof(App)}");
            Debug.WriteLine($"FriendlyName: {ad.FriendlyName}", $"{nameof(App)}");
            Debug.WriteLine($"BaseDirectory: {ad.BaseDirectory}", $"{nameof(App)}");
        }
    }

    void CurrentDomainFirstChanceException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
    {
        Debug.WriteLine($"First chance exception: {e.Exception}", $"{nameof(App)}");
    }

    void CurrentDomainUnhandledException(object? sender, System.UnhandledExceptionEventArgs e)
    {
        Exception? ex = e.ExceptionObject as Exception;
        Debug.WriteLine($"Thread exception of type {ex?.GetType()}: {ex}", $"{nameof(App)}");
    }

    void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Debug.WriteLine($"Unobserved task exception: {e.Exception}", $"{nameof(App)}");

        e.SetObserved(); // suppress and handle manually
    }

    void OnWindowClosingEvent(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        IsClosing = true;
        try { App.AppSettings.SaveSettings(); }
        catch (Exception) { Debug.WriteLine($"[OnWindowClosing] Settings could not be saved!"); }
        OnWindowClosing?.Invoke(DateTime.Now.ToString("hh:mm:ss.fff tt"));
        Debug.WriteLine($"[OnWindowClosing] Cancel={args.Cancel}");
    }

    /// <summary>
    /// Unsubscribe from all events in the list
    /// </summary>
    void UnsubscribeFromEvents()
    {
        foreach (Action unsubscribe in unsubscribeList) { unsubscribe(); }
    }
    #endregion

    #region [Window Helpers]
    /// <summary>
    /// This code example demonstrates how to retrieve an AppWindow from a WinUI3 window.
    /// The AppWindow class is available for any top-level HWND in your app.
    /// AppWindow is available only to desktop apps (both packaged and unpackaged), it's not available to UWP apps.
    /// https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/windowing/windowing-overview
    /// https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.windowing.appwindow.create?view=windows-app-sdk-1.3
    /// </summary>
    public Microsoft.UI.Windowing.AppWindow? GetAppWindow(object window)
    {
        // Retrieve the window handle (HWND) of the current (XAML) WinUI3 window.
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

        // For other classes to use.
        WindowHandle = hWnd;

        // Retrieve the WindowId that corresponds to hWnd.
        Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);

        // Lastly, retrieve the AppWindow for the current (XAML) WinUI3 window.
        Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

        if (appWindow != null)
        {
            // You now have an AppWindow object, and you can call its methods to manipulate the window.
            // As an example, let's change the title text of the window: appWindow.Title = "Title text updated via AppWindow!";
            //appWindow.Move(new Windows.Graphics.PointInt32(200, 100));
            appWindow?.MoveAndResize(new Windows.Graphics.RectInt32(250, 100, 1300, 800), Microsoft.UI.Windowing.DisplayArea.Primary);
        }

        return appWindow;
    }

    /// <summary>
    /// Use <see cref="Microsoft.UI.Windowing.AppWindow"/> to set the taskbar icon for WinUI application.
    /// This method has been tested with an unpackaged and packaged app.
    /// Setting the icon in the project file using the ApplicationIcon tag.
    /// </summary>
    /// <param name="iconName">the local path, including any subfolder, e.g. "Assets\Icon.ico"</param>
    /// <param name="appWindow"><see cref="Microsoft.UI.Windowing.AppWindow"/></param>
    public static void SetIcon(string iconName, Microsoft.UI.Windowing.AppWindow appWindow)
    {
        if (appWindow == null)
            return;

        try
        {
            if (IsPackaged)
                appWindow.SetIcon(System.IO.Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, iconName));
            else
                appWindow.SetIcon(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), iconName));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod()?.Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Centers a <see cref="Microsoft.UI.Xaml.Window"/> based on the <see cref="Microsoft.UI.Windowing.DisplayArea"/>.
    /// </summary>
    void CenterWindow(Window window)
    {
        try
        {
            System.IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            if (Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId) is Microsoft.UI.Windowing.AppWindow appWindow &&
                Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Nearest) is Microsoft.UI.Windowing.DisplayArea displayArea)
            {
                Windows.Graphics.PointInt32 CenteredPosition = appWindow.Position;
                CenteredPosition.X = (displayArea.WorkArea.Width - appWindow.Size.Width) / 2;
                CenteredPosition.Y = (displayArea.WorkArea.Height - appWindow.Size.Height) / 2;
                appWindow.Move(CenteredPosition);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod()?.Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// The <see cref="Microsoft.UI.Windowing.DisplayArea"/> exposes properties such as:
    /// OuterBounds     (Rect32)
    /// WorkArea.Width  (int)
    /// WorkArea.Height (int)
    /// IsPrimary       (bool)
    /// DisplayId.Value (ulong)
    /// </summary>
    /// <param name="window"></param>
    /// <returns><see cref="DisplayArea"/></returns>
    Microsoft.UI.Windowing.DisplayArea? GetDisplayArea(Window window)
    {
        try
        {
            System.IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var da = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Nearest);
            return da;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GetDisplayArea: {ex.Message}");
            return null;
        }
    }
    #endregion

    #region [Dialog Helpers]
    /// <summary>
    /// The <see cref="Windows.UI.Popups.MessageDialog"/> does not look as nice as the
    /// <see cref="Microsoft.UI.Xaml.Controls.ContentDialog"/> and is not part of the native Microsoft.UI.Xaml.Controls.
    /// The <see cref="Windows.UI.Popups.MessageDialog"/> offers the <see cref="Windows.UI.Popups.UICommandInvokedHandler"/> 
    /// callback, but this could be replaced with actions. Both can be shown asynchronously.
    /// </summary>
    /// <remarks>
    /// You'll need to call <see cref="WinRT.Interop.InitializeWithWindow.Initialize"/> when using the <see cref="Windows.UI.Popups.MessageDialog"/>,
    /// because the <see cref="Microsoft.UI.Xaml.XamlRoot"/> does not exist and an owner must be defined.
    /// </remarks>
    public static async Task ShowMessageBox(string title, string message, string primaryText, string cancelText)
    {
        // Create the dialog.
        var messageDialog = new MessageDialog($"{message}");
        messageDialog.Title = title;
        messageDialog.Commands.Add(new UICommand($"{primaryText}", new UICommandInvokedHandler(DialogDismissedHandler)));
        messageDialog.Commands.Add(new UICommand($"{cancelText}", new UICommandInvokedHandler(DialogDismissedHandler)));
        messageDialog.DefaultCommandIndex = 1;
        // We must initialize the dialog with an owner.
        WinRT.Interop.InitializeWithWindow.Initialize(messageDialog, App.WindowHandle);
        // Show the message dialog. Our DialogDismissedHandler will deal with what selection the user wants.
        await messageDialog.ShowAsync();

        // We could force the result in a separate timer...
        //DialogDismissedHandler(new UICommand("time-out"));
    }

    /// <summary>
    /// Callback for the selected option from the user.
    /// </summary>
    static void DialogDismissedHandler(IUICommand command)
    {
        Debug.WriteLine($"UICommand.Label => {command.Label}");
    }

    /// <summary>
    /// The <see cref="Microsoft.UI.Xaml.Controls.ContentDialog"/> looks much better than the
    /// <see cref="Windows.UI.Popups.MessageDialog"/> and is part of the native Microsoft.UI.Xaml.Controls.
    /// The <see cref="Microsoft.UI.Xaml.Controls.ContentDialog"/> does not offer a <see cref="Windows.UI.Popups.UICommandInvokedHandler"/>
    /// callback, but in this example was replaced with actions. Both can be shown asynchronously.
    /// </summary>
    /// <remarks>
    /// There is no need to call <see cref="WinRT.Interop.InitializeWithWindow.Initialize"/> when using the <see cref="Microsoft.UI.Xaml.Controls.ContentDialog"/>,
    /// but a <see cref="Microsoft.UI.Xaml.XamlRoot"/> must be defined since it inherits from <see cref="Microsoft.UI.Xaml.Controls.Control"/>.
    /// </remarks>
    public static async Task ShowDialogBox(string title, string message, string primaryText, string cancelText, Action? onPrimary, Action? onCancel)
    {
        //Windows.UI.Popups.IUICommand defaultCommand = new Windows.UI.Popups.UICommand("OK");

        // NOTE: Content dialogs will automatically darken the background.
        ContentDialog contentDialog = new ContentDialog()
        {
            Title = title,
            PrimaryButtonText = primaryText,
            CloseButtonText = cancelText,
            Content = new TextBlock()
            {
                Text = message,
                FontSize = (double)App.Current.Resources["FontSizeTwo"],
                FontFamily = (Microsoft.UI.Xaml.Media.FontFamily)App.Current.Resources["ScanLineFont"],
                TextWrapping = TextWrapping.Wrap
            },
            XamlRoot = App.MainRoot?.XamlRoot,
            RequestedTheme = App.MainRoot?.ActualTheme ?? ElementTheme.Default
        };

        ContentDialogResult result = await contentDialog.ShowAsync();

        switch (result)
        {
            case ContentDialogResult.Primary:
                onPrimary?.Invoke();
                break;
            //case ContentDialogResult.Secondary:
            //    onSecondary?.Invoke();
            //    break;
            case ContentDialogResult.None: // Cancel
                onCancel?.Invoke();
                break;
            default:
                Debug.WriteLine($"Dialog result not defined.");
                break;
        }
    }
    #endregion

    #region [Win32 API]
    /// <summary>
    /// This works fine, but it must be called before any other window 
    /// commands are invoked e.g. CenterWindow(), Move(), Resize(), et al.
    /// </summary>
    public static void HideWindowBorders(Window? window)
    {
        try
        {
            IntPtr hwnd = IntPtr.Zero;

            if (window == null)
                hwnd = WindowHandle;
            else
                hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

            int style = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_STYLE); //gets current style
            _ = NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_STYLE, (int)(style & ~(NativeMethods.WS_CAPTION | NativeMethods.WS_SIZEBOX))); //removes caption and the sizebox from current style
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"HideWindowBorders: {ex.Message}");
        }
    }

    public static void SetAsForeground(Window? window)
    {
        if (window == null)
            return;

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        _ = NativeMethods.SetForegroundWindow(hwnd);
    }

    public static void DisableTitleBar(Window window)
    {
        try
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            // Send the WM_SYSCOMMAND message with the SC_MOVE parameter to disable the title bar
            _ = NativeMethods.SendMessage(hwnd, NativeMethods.WM_SYSCOMMAND, (IntPtr)NativeMethods.SC_MOVE, IntPtr.Zero);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"DisableTitleBar: {ex.Message}");
        }
    }

    /// <summary>
    /// Disable windows toolbar's control box.
    /// This will also disable system menu with Alt+Space hotkey.
    /// </summary>
    public static void DisableControlBox(Window window)
    {
        try
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            _ = NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_STYLE, NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_STYLE) & ~NativeMethods.WS_SYSMENU);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"DisableControlBox: {ex.Message}");
        }
    }

    /// <summary>
    /// Set WS_EX_TOOLWINDOW to ignore the Window
    /// </summary>
    public static void SetToolWindowStyle(Window window)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        _ = NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EX_STYLE, NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EX_STYLE) | NativeMethods.WS_EX_TOOLWINDOW);
    }
    #endregion

    #region [Support Debugging]
    /// <summary>
    /// Capture entire desktop as a JPEG and save to disk.
    /// This can be used for debugging a user's system; the image could be sent back to a server for inspection.
    /// </summary>
    public static void CaptureScreen()
    {
        Bitmap bitmap;
        IntPtr handle = IntPtr.Zero;

        try
        {
            int monitors = ScreenInformation.GetMonitorCount();

            System.IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWin);
            Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            Microsoft.UI.Windowing.DisplayArea? da = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Nearest);
        
            if (da == null || MainWin == null)
                return;

            bitmap = new Bitmap(da.OuterBounds.Width * monitors, da.OuterBounds.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            // If you do not want the bottom app bar, comment the above line and uncomment the below line...
            //bitmap = new Bitmap(da.WorkArea.Width * monitors, da.WorkArea.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
            }

            handle = bitmap.GetHbitmap();

            // If setting an image control's source property...
            //imgCtrlName.Source = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            try
            {
                string root = string.Empty;
                var ts = Extensions.GenerateTimeStamp();
                var logPaths = DriveInfo.GetDrives().Where(di => (di.DriveType == DriveType.Fixed) && (di.IsReady) && (di.AvailableFreeSpace > 10000000)).Select(di => di.RootDirectory).OrderByDescending(di => di.FullName);
                if (logPaths != null)
                {
                    root = logPaths.FirstOrDefault()?.FullName ?? "C:\\";
                    bitmap.Save(System.IO.Path.Combine(root, $"{GetCurrentNamespace()}_{ts}.jpg"));
                    Debug.WriteLine($"Saved screen capture {ts}.jpg to {root}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SaveScreenImage: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"CaptureScreen: {ex.Message}");
        }
        finally
        {
            NativeMethods.DeleteObject(handle);
        }
    }
    #endregion
}
