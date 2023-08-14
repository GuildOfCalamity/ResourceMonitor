using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;

namespace Monitor;

[SuppressUnmanagedCodeSecurity]
public static class ConsoleManager
{
    const uint ATTACH_PARENT_PROCESS = 0x0ffffffff;
    
    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(uint dwProcessId);

    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("kernel32.dll")]
    private static extern int GetConsoleOutputCP();

    public static bool HasConsole
    {
        get { return GetConsoleWindow() != IntPtr.Zero; }
    }

    /// <summary>
    /// https://learn.microsoft.com/en-us/windows/console/attachconsole
    /// </summary>
    public static bool Attach(uint pid = 0)
    {
        if (pid == default || pid == 0)
            pid = ATTACH_PARENT_PROCESS;
        
        return AttachConsole(pid);
    }

    /// <summary>
    /// https://learn.microsoft.com/en-us/windows/console/freeconsole
    /// </summary>
    public static bool Free()
    {
        return FreeConsole();
    }

    /// <summary>
    /// Creates a new console instance if the process is not attached to a console already.
    /// </summary>
    public static void Show()
    {
        if (!HasConsole)
        {
            AllocConsole();
            InvalidateOutAndError();
        }
    }

    /// <summary>
    /// If the process has a console attached to it, it will be detached and no longer visible. Writing to the System.Console is still possible, but no output will be shown.
    /// </summary>
    public static void Hide()
    {
        if (HasConsole)
        {
            SetOutAndErrorNull();
            FreeConsole();
        }
    }

    public static void Toggle()
    {
        if (HasConsole)
            Hide();
        else
            Show();
    }

    /// <summary>
    /// Uses reflection to get and set <see cref="System.Console"/> fields.
    /// </summary>
    static void InvalidateOutAndError()
    {
        Type type = typeof(System.Console);

        System.Reflection.FieldInfo? _out = type.GetField("_out", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        System.Reflection.FieldInfo? _error = type.GetField("_error", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        System.Reflection.MethodInfo? _InitializeStdOutError = type.GetMethod("InitializeStdOutError", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

        try
        {
            _out?.SetValue(null, null);
            _error?.SetValue(null, null);
            _InitializeStdOutError?.Invoke(null, new object[] { true });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"InvalidateOutAndError: {ex.Message}");
        }
    }

    static void SetOutAndErrorNull()
    {
        Console.SetOut(TextWriter.Null);
        Console.SetError(TextWriter.Null);
    }
}