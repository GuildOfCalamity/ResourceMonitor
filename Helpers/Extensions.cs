using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Globalization;
using System.Buffers;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Collections;

using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml;

using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI;

namespace Monitor;

public static class Extensions
{
	#region [WinUI]
	/// <summary>
	/// Converts a <see cref="System.Numerics.Vector2"/> structure (x,y) 
	/// to <see cref="System.Numerics.Vector3"/> structure (x, y, 0).
	/// </summary>
	/// <param name="v"><see cref="System.Numerics.Vector2"/></param>
	/// <returns><see cref="System.Numerics.Vector3"/></returns>
	public static System.Numerics.Vector3 ToVector3(this System.Numerics.Vector2 v)
	{
		return new System.Numerics.Vector3(v, 0);
	}

	public static bool IsXamlRootAvailable(bool UWP = false)
	{
		if (UWP)
			return Windows.Foundation.Metadata.ApiInformation.IsPropertyPresent("Windows.UI.Xaml.UIElement", "XamlRoot");
		else
			return Windows.Foundation.Metadata.ApiInformation.IsPropertyPresent("Microsoft.UI.Xaml.UIElement", "XamlRoot");
	}

	/// <summary>
	/// Helper function to calculate an element's rectangle in root-relative coordinates.
	/// </summary>
	public static Windows.Foundation.Rect GetElementRect(this Microsoft.UI.Xaml.FrameworkElement element)
	{
		try
		{
			Microsoft.UI.Xaml.Media.GeneralTransform transform = element.TransformToVisual(null);
			Windows.Foundation.Point point = transform.TransformPoint(new Windows.Foundation.Point());
			return new Windows.Foundation.Rect(point, new Windows.Foundation.Size(element.ActualWidth, element.ActualHeight));
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"GetElementRect: {ex.Message}", nameof(Extensions));
			return new Windows.Foundation.Rect(0, 0, 0, 0);
		}
	}

	public static IconElement? GetIcon(string imagePath, string imageExt = ".png")
	{
		IconElement? result = null;

		try
		{
			result = imagePath.ToLowerInvariant().EndsWith(imageExt) ?
						(IconElement)new BitmapIcon() { UriSource = new Uri(imagePath, UriKind.RelativeOrAbsolute), ShowAsMonochrome = false } :
						(IconElement)new FontIcon() { Glyph = imagePath };
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"{MethodBase.GetCurrentMethod()?.Name}: {ex.Message}", $"{nameof(Extensions)}");
		}

		return result;
	}

	public static FontIcon GenerateFontIcon(Windows.UI.Color brush, string glyph = "\uF127", int width = 10, int height = 10)
	{
		return new FontIcon()
		{
			Glyph = glyph,
			FontSize = 1.5,
			Width = (double)width,
			Height = (double)height,
			Foreground = new SolidColorBrush(brush),
		};
	}

	public static async Task<byte[]> AsPng(this UIElement control)
    {
        // Get XAML Visual in BGRA8 format
        var rtb = new RenderTargetBitmap();
        await rtb.RenderAsync(control, (int)control.ActualSize.X, (int)control.ActualSize.Y);

        // Encode as PNG
        var pixelBuffer = (await rtb.GetPixelsAsync()).ToArray();
        IRandomAccessStream mraStream = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, mraStream);
        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            (uint)rtb.PixelWidth,
            (uint)rtb.PixelHeight,
            184,
            184,
            pixelBuffer);
        await encoder.FlushAsync();

        // Transform to byte array
        var bytes = new byte[mraStream.Size];
        await mraStream.ReadAsync(bytes.AsBuffer(), (uint)mraStream.Size, InputStreamOptions.None);

        return bytes;
    }

	/// <summary>
	/// This is a redundant call from App.xaml.cs, but is here if you need it.
	/// </summary>
	/// <param name="window"><see cref="Microsoft.UI.Xaml.Window"/></param>
	/// <returns><see cref="Microsoft.UI.Windowing.AppWindow"/></returns>
	public static Microsoft.UI.Windowing.AppWindow GetAppWindow(this Microsoft.UI.Xaml.Window window)
    {
        System.IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        Microsoft.UI.WindowId wndId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        return Microsoft.UI.Windowing.AppWindow.GetFromWindowId(wndId);
    }

    /// <summary>
    /// I created this to show what controls are members of <see cref="Microsoft.UI.Xaml.FrameworkElement"/>.
    /// </summary>
    public static void FindControlsInheritingFromFrameworkElement()
    {
        var controlAssembly = typeof(Microsoft.UI.Xaml.Controls.Control).GetTypeInfo().Assembly;
        var controlTypes = controlAssembly.GetTypes()
            .Where(type => type.Namespace == "Microsoft.UI.Xaml.Controls" &&
            typeof(Microsoft.UI.Xaml.FrameworkElement).IsAssignableFrom(type));

        foreach (var controlType in controlTypes)
        {
            Debug.WriteLine($"[FrameworkElement] {controlType.FullName}");
        }
    }

	/// <summary>
	/// Calculates the linear interpolated Color based on the given Color values.
	/// </summary>
	/// <param name="colorFrom">Source Color.</param>
	/// <param name="colorTo">Target Color.</param>
	/// <param name="amount">Weightage given to the target color.</param>
	/// <returns>Linear Interpolated Color.</returns>
	public static Windows.UI.Color Lerp(this Windows.UI.Color colorFrom, Windows.UI.Color colorTo, float amount)
	{
		// Convert colorFrom components to lerp-able floats
		float sa = colorFrom.A,
			sr = colorFrom.R,
			sg = colorFrom.G,
			sb = colorFrom.B;

		// Convert colorTo components to lerp-able floats
		float ea = colorTo.A,
			er = colorTo.R,
			eg = colorTo.G,
			eb = colorTo.B;

		// lerp the colors to get the difference
		byte a = (byte)Math.Max(0, Math.Min(255, sa.Lerp(ea, amount))),
			r = (byte)Math.Max(0, Math.Min(255, sr.Lerp(er, amount))),
			g = (byte)Math.Max(0, Math.Min(255, sg.Lerp(eg, amount))),
			b = (byte)Math.Max(0, Math.Min(255, sb.Lerp(eb, amount)));

		// return the new color
		return Windows.UI.Color.FromArgb(a, r, g, b);
	}

	/// <summary>
	/// Darkens the color by the given percentage using lerp.
	/// </summary>
	/// <param name="color">Source color.</param>
	/// <param name="amount">Percentage to darken. Value should be between 0 and 1.</param>
	/// <returns>Color</returns>
	public static Windows.UI.Color DarkerBy(this Windows.UI.Color color, float amount)
	{
		return color.Lerp(Colors.Black, amount);
	}

	/// <summary>
	/// Lightens the color by the given percentage using lerp.
	/// </summary>
	/// <param name="color">Source color.</param>
	/// <param name="amount">Percentage to lighten. Value should be between 0 and 1.</param>
	/// <returns>Color</returns>
	public static Windows.UI.Color LighterBy(this Windows.UI.Color color, float amount)
	{
		return color.Lerp(Colors.White, amount);
	}

	/// <summary>
	/// Returns a random selection from <see cref="Microsoft.UI.Colors"/>.
	/// </summary>
	/// <returns><see cref="Windows.UI.Color"/></returns>
	public static Windows.UI.Color GetRandomMicrosoftUIColor()
    {
        try
        {
            var colorType = typeof(Microsoft.UI.Colors);
            var colors = colorType.GetProperties()
                .Where(p => p.PropertyType == typeof(Windows.UI.Color) && p.GetMethod.IsStatic && p.GetMethod.IsPublic)
                .Select(p => (Windows.UI.Color)p.GetValue(null))
                .ToList();

            if (colors.Count > 0)
            {
                var randomIndex = Random.Shared.Next(colors.Count);
                var randomColor = colors[randomIndex];
                return randomColor;
            }
            else
            {
                return Microsoft.UI.Colors.Gray;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GetRandomColor: {ex.Message}");
            return Microsoft.UI.Colors.Red;
        }
    }

    /// <summary>
    /// Creates a Color object from the hex color code and returns the result.
    /// </summary>
    /// <param name="hexColorCode"></param>
    /// <returns></returns>
    public static Windows.UI.Color? GetColorFromHexString(string hexColorCode)
    {
        if (string.IsNullOrEmpty(hexColorCode)) { return null; }

        try
        {
            byte a = 255; byte r = 0; byte g = 0; byte b = 0;

            if (hexColorCode.Length == 9)
            {
                hexColorCode = hexColorCode.Substring(1, 8);
            }

            if (hexColorCode.Length == 8)
            {
                a = Convert.ToByte(hexColorCode.Substring(0, 2), 16);
                hexColorCode = hexColorCode.Substring(2, 6);
            }

            if (hexColorCode.Length == 6)
            {
                r = Convert.ToByte(hexColorCode.Substring(0, 2), 16);
                g = Convert.ToByte(hexColorCode.Substring(2, 2), 16);
                b = Convert.ToByte(hexColorCode.Substring(4, 2), 16);
            }

            return Windows.UI.Color.FromArgb(a, r, g, b);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod()?.Name}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Generates a 6 digit color string and may include the # sign.
    /// The 0 and 1 options have been removed so dark colors such as #000000/#111111 are not possible.
    /// </summary>
    public static string GetRandomColorString(bool includePound = true)
    {
        StringBuilder sb = new StringBuilder();
        const string pwChars = "2346789ABCDEF";
        char[] charArray = pwChars.Distinct().ToArray();

        var result = new char[7];
        var rng = new Random();

        if (includePound)
            sb.Append("#");

        for (int x = 0; x < 6; x++)
            sb.Append(pwChars[rng.Next() % pwChars.Length]);

        return sb.ToString();
    }

    /// <summary>
    /// Get OS version by way of <see cref="Windows.System.Profile.AnalyticsInfo"/>.
    /// </summary>
    /// <returns><see cref="Version"/></returns>
    public static Version GetWindowsVersionUsingAnalyticsInfo()
    {
        try
        {
            ulong version = ulong.Parse(Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamilyVersion);
            var Major = (ushort)((version & 0xFFFF000000000000L) >> 48);
            var Minor = (ushort)((version & 0x0000FFFF00000000L) >> 32);
            var Build = (ushort)((version & 0x00000000FFFF0000L) >> 16);
            var Revision = (ushort)(version & 0x000000000000FFFFL);

            return new Version(Major, Minor, Build, Revision);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GetWindowsVersionUsingAnalyticsInfo: {ex.Message}", $"{nameof(Extensions)}");
            return new Version(); // 0.0
        }
    }
    #endregion

    #region [Enumerables]
    /// <summary>
    /// Uses an operator for the current and previous item.
    /// Needs only a single iteration to process pairs and produce an output.
    /// </summary>
    /// <example>
    /// var avg = collection.Pairwise((a, b) => (b.DateTime - a.DateTime)).Average(ts => ts.TotalMinutes);
    /// </example>
    public static IEnumerable<TResult> Pairwise<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TSource, TResult> resultSelector)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        if (resultSelector == null)
            throw new ArgumentNullException(nameof(resultSelector));

        return _(); IEnumerable<TResult> _()
        {
            using var e = source.GetEnumerator();

            if (!e.MoveNext())
                yield break;

            var previous = e.Current;
            while (e.MoveNext())
            {
                yield return resultSelector(previous, e.Current);
                previous = e.Current;
            }
        }
    }

    /// <summary>
    /// IEnumerable file reader.
    /// </summary>
    public static IEnumerable<string> ReadFileLines(string path)
    {
        string line = string.Empty;

        if (!File.Exists(path))
            yield return line;
        else
        {
            using (TextReader reader = File.OpenText(path))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }
    }

    /// <summary>
    /// IAsyncEnumerable file reader.
    /// </summary>
    public static async IAsyncEnumerable<string> ReadFileLinesAsync(string path)
    {
        string line = string.Empty;

        if (!File.Exists(path))
            yield return line;
        else
        {
            using (TextReader reader = File.OpenText(path))
            {
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    yield return line;
                }
            }
        }
    }

    /// <summary>
    /// File writer for <see cref="IEnumerable{string}"/> parameters.
    /// </summary>
    public static bool WriteFileLines(string path, IEnumerable<string> lines)
    {
        using (TextWriter writer = File.CreateText(path))
        {
            foreach (var line in lines)
            {
                writer.WriteLine(line);
            }
        }

        return true;
    }

    /// <summary>
    /// De-dupe file reader using a <see cref="HashSet{string}"/>.
    /// </summary>
    public static HashSet<string> ReadLines(string path)
    {
        if (!File.Exists(path))
            return new();

        return new HashSet<string>(File.ReadAllLines(path), StringComparer.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// De-dupe file writer using a <see cref="HashSet{string}"/>.
    /// </summary>
    public static bool WriteLines(string path, IEnumerable<string> lines)
    {
        var output = new HashSet<string>(lines, StringComparer.InvariantCultureIgnoreCase);

        using (TextWriter writer = File.CreateText(path))
        {
            foreach (var line in output)
            {
                writer.WriteLine(line);
            }
        }
        return true;
    }

    public static IEnumerable<T> JoinLists<T>(this IEnumerable<T> list1, IEnumerable<T> list2)
    {
        var joined = new[] { list1, list2 }.Where(x => x != null).SelectMany(x => x);
        return joined ?? Enumerable.Empty<T>();
    }
    public static IEnumerable<T> JoinLists<T>(this IEnumerable<T> list1, IEnumerable<T> list2, IEnumerable<T> list3)
    {
        var joined = new[] { list1, list2, list3 }.Where(x => x != null).SelectMany(x => x);
        return joined ?? Enumerable.Empty<T>();
    }
    public static IEnumerable<T> JoinMany<T>(params IEnumerable<T>[] array)
    {
        var final = array.Where(x => x != null).SelectMany(x => x);
        return final ?? Enumerable.Empty<T>();
    }

    public static Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> items)
    {
        if (items is null)
            throw new ArgumentNullException(nameof(items));

        return Implementation(items);

        static async Task<List<T>> Implementation(IAsyncEnumerable<T> items)
        {
            var rv = new List<T>();
            await foreach (var item in items)
            {
                rv.Add(item);
            }
            return rv;
        }
    }

    public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T>? toAdd)
    {
        if (collection is null)
            throw new ArgumentNullException(nameof(collection));

        if (toAdd != null)
        {
            foreach (var item in toAdd)
                collection.Add(item);
        }
    }

    public static void RemoveFirst<T>(this IList<T> collection, Func<T, bool> predicate)
    {
        if (collection is null)
            throw new ArgumentNullException(nameof(collection));

        if (predicate is null)
            throw new ArgumentNullException(nameof(predicate));

        for (int i = 0; i < collection.Count; i++)
        {
            if (predicate(collection[i]))
            {
                collection.RemoveAt(i);
                break;
            }
        }
    }

    /// <summary>
    /// Splits a <see cref="Dictionary{TKey, TValue}"/> into two equal halves.
    /// </summary>
    /// <param name="dictionary"><see cref="Dictionary{TKey, TValue}"/></param>
    /// <returns>tuple</returns>
    public static (Dictionary<string, string> firstHalf, Dictionary<string, string> secondHalf) SplitDictionary(this Dictionary<string, string> dictionary)
    {
        int count = dictionary.Count;

        if (count <= 1)
        {
            // Return the entire dictionary as the first half and an empty dictionary as the second half.
            return (dictionary, new Dictionary<string, string>());
        }

        int halfCount = count / 2;
        var firstHalf = dictionary.Take(halfCount).ToDictionary(kv => kv.Key, kv => kv.Value);
        var secondHalf = dictionary.Skip(halfCount).ToDictionary(kv => kv.Key, kv => kv.Value);

        // Adjust the second half if the count is odd.
        if (count % 2 != 0)
            secondHalf = dictionary.Skip(halfCount + 1).ToDictionary(kv => kv.Key, kv => kv.Value);

        return (firstHalf, secondHalf);
    }

    #pragma warning disable 8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
    /// <summary>
    /// Helper for <see cref="System.Collections.Generic.SortedList{TKey, TValue}"/>
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="sortedList"></param>
    /// <returns><see cref="Dictionary{TKey, TValue}"/></returns>
    public static Dictionary<TKey, TValue> ConvertToDictionary<TKey, TValue>(this System.Collections.Generic.SortedList<TKey, TValue> sortedList)
    {
        Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();
        foreach (KeyValuePair<TKey, TValue> pair in sortedList)
        { 
            dictionary.Add(pair.Key, pair.Value); 
        }
        return dictionary;
    }

    /// <summary>
    /// Helper for <see cref="System.Collections.SortedList"/>
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="sortedList"></param>
    /// <returns><see cref="Dictionary{TKey, TValue}"/></returns>
    public static Dictionary<TKey, TValue> ConvertToDictionary<TKey, TValue>(this System.Collections.SortedList sortedList)
    {
        Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();
        foreach (DictionaryEntry pair in sortedList) 
        { 
            dictionary.Add((TKey)pair.Key, (TValue)pair.Value); 
        }
        return dictionary;
    }

    /// <summary>
    /// Helper for <see cref="System.Collections.Hashtable"/>
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="hashList"></param>
    /// <returns><see cref="Dictionary{TKey, TValue}"/></returns>
    public static Dictionary<TKey, TValue> ConvertToDictionary<TKey, TValue>(this System.Collections.Hashtable hashList)
    {
        Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();
        foreach (DictionaryEntry pair in hashList) 
        { 
            dictionary.Add((TKey)pair.Key, (TValue)pair.Value); 
        }
        return dictionary;
    }
    #pragma warning restore 8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.

    public static void ForEach<T>(this IEnumerable<T> ie, Action<T> action)
    {
        foreach (var i in ie)
        {
            try { action(i); }
            catch (Exception ex) { Debug.WriteLine($"{ex.GetType()}: {ex.Message}"); }
        }
    }

    public static T Retry<T>(this Func<T> operation, int attempts)
    {
        while (true)
        {
            try
            {
                attempts--;
                return operation();
            }
            catch (Exception ex) when (attempts > 0)
            {
                Debug.WriteLine($"{MethodBase.GetCurrentMethod()?.Name}: {ex.Message}", $"{nameof(Extensions)}");
                Thread.Sleep(2000);
            }
        }
    }
    #endregion

    #region [ValueTask]
    /*
     [A note about the code below and the Task<T> vs ValueTask<T> concerns.]

     A method may return an instance of this value type when it's likely that the result of its operation 
     will be available synchronously, and when it's expected to be invoked so frequently that the cost of 
     allocating a new Task<TResult> for each call will be prohibitive.

     There are tradeoffs to using a ValueTask<TResult> instead of a Task<TResult>. For example, while a 
     ValueTask<TResult> can help avoid an allocation in the case where the successful result is available 
     synchronously, it also contains multiple fields, whereas a Task<TResult> as a reference type is a 
     single field. This means that returning a ValueTask<TResult> from a method results in copying more 
     data. It also means, that if a method that returns a ValueTask<TResult> is awaited within an async 
     method, the state machine for that async method will be larger, because it must store a struct 
     containing multiple fields instead of a single reference.

     For uses other than consuming the result of an asynchronous operation using await, ValueTask<TResult> 
     can lead to a more convoluted programming model that requires more allocations. For example, consider 
     a method that could return either a Task<TResult> with a cached task as a common result or a ValueTask<TResult>. 
     If the consumer of the result wants to use it as a Task<TResult> in a method like WhenAll or WhenAny, the 
     ValueTask<TResult> must first be converted to a Task<TResult> using AsTask, leading to an allocation that 
     would have been avoided if a cached Task<TResult> had been used in the first place.

     As such, the default choice for any asynchronous method should be to return a Task or Task<TResult>. 
     Only if performance analysis proves it worthwhile should a ValueTask<TResult> be used instead of a Task<TResult>. 
     The non generic version of ValueTask is not recommended for most scenarios. The CompletedTask property should 
     be used to hand back a successfully completed singleton in the case where a method returning a Task completes 
     synchronously and successfully.
     */

    /// <summary>
    /// https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/
    /// https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1?view=net-6.0
    /// </summary>
    /// <remarks>
    /// Code taken from https://stackoverflow.com/questions/45689327/task-whenall-for-valuetask
    /// </remarks>
    public static async ValueTask<T[]> WhenAll<T>(params ValueTask<T>[] tasks)
    {
        ArgumentNullException.ThrowIfNull(tasks);

        if (tasks.Length == 0)
            return Array.Empty<T>();

        // We don't allocate the list if no task throws
        List<Exception>? exceptions = null;

        var results = new T[tasks.Length];
        for (var i = 0; i < tasks.Length; i++)
        {
            try
            {
                results[i] = await tasks[i].ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exceptions ??= new(tasks.Length);
                exceptions.Add(ex);
            }
        }

        return exceptions is null ? results : throw new AggregateException(exceptions);
    }

    /// <summary>
    /// https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/
    /// https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1?view=net-6.0
    /// </summary>
    /// <remarks>
    /// This method has not been tested.
    /// </remarks>
    public static Task WhenAny(this IEnumerable<ValueTask> tasks)
    {
        return Task.WhenAny(tasks.Select(v => v.AsTask()));
    }

    /// <summary>
    /// https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/
    /// https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1?view=net-6.0
    /// </summary>
    /// <remarks>
    /// This method has not been tested.
    /// </remarks>
    public static Task WhenAny(this List<ValueTask> tasks)
    {
        return Task.WhenAny(tasks.Select(v => v.AsTask()));
    }

    /// <summary>
    /// https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/
    /// https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1?view=net-6.0
    /// </summary>
    /// <remarks>
    /// This method has been tested.
    /// It doesn't make much sense to convert the ValueTasks into Tasks
    /// just so we can use Task.WhenAny, but it's here if you want it.
    /// </remarks>
    public static async ValueTask<ValueTask<TResult>> WhenAny<TResult>(List<ValueTask<TResult>> tasks)
    {
        try
        {
            Task<TResult>? intermediate = await Task.WhenAny(tasks.Select((v) => v.AsTask())).ConfigureAwait(false);
            return ValueTask.FromResult<TResult>(intermediate.Result);
        }
        catch (Exception ex)
        {
            return ValueTask.FromException<TResult>(ex);
        }
    }

    /// <summary>
    /// https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/
    /// https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1?view=net-6.0
    /// </summary>
    /// <remarks>
    /// This method has been tested.
    /// It doesn't make much sense to convert the ValueTasks into Tasks
    /// just so we can use Task.WhenAny, but it's here if you want it.
    /// </remarks>
    public static async ValueTask<ValueTask<TResult>> WhenAnyAwaitSubTasks<TResult>(List<ValueTask<TResult>> tasks)
    {
        try
        {
            Task<TResult>? intermediate = await Task.WhenAny(tasks.Select(async (v) => await v.AsTask())).ConfigureAwait(false);
            return ValueTask.FromResult<TResult>(intermediate.Result);
        }
        catch (Exception ex)
        {
            return ValueTask.FromException<TResult>(ex);
        }
    }

    /// <summary>
    /// https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/
    /// https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1?view=net-6.0
    /// </summary>
    /// <remarks>
    /// This method has not been tested.
    /// It doesn't make much sense to convert the ValueTasks into Tasks
    /// just so we can use Task.WhenAny, but it's here if you want it.
    /// </remarks>
    public static async ValueTask<ValueTask<TResult>> WhenAnyExperimental<TResult>(List<ValueTask<TResult>> tasks)
    {
        ValueTask<TResult> result = default;

        Task<TResult>? intermediate = await Task.WhenAny(tasks.Select(async (v) => await v.AsTask())).ConfigureAwait(false);
        await intermediate.ContinueWith((t) =>
        {
            result = ValueTask.FromResult<TResult>(t.Result);

        }, default, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default);

        return result;
    }
    #endregion

    #region [Tasks]
    /// <summary>
    /// Task.Factory.StartNew (() => { throw null; }).IgnoreExceptions();
    /// </summary>
    public static void IgnoreExceptions(this Task task)
    {
        task.ContinueWith(t =>
        {
            var ignore = t.Exception;
            foreach (Exception ex in ignore?.Flatten()?.InnerExceptions)
                Debug.WriteLine($"[{ex.GetType()}]: {ex.Message}");
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    /// <summary>
    /// Chainable task helper.
    /// var result = await SomeLongAsyncFunction().WithTimeout(TimeSpan.FromSeconds(2));
    /// </summary>
    /// <typeparam name="TResult">the type of task result</typeparam>
    /// <returns><see cref="Task"/>TResult</returns>
    public async static Task<TResult> WithTimeout<TResult>(this Task<TResult> task, TimeSpan timeout)
    {
        Task winner = await (Task.WhenAny(task, Task.Delay(timeout)));

        if (winner != task)
            throw new TimeoutException();

        return await task;   // Unwrap result/re-throw
    }

    /// <summary>
    /// Task extension to add a timeout.
    /// </summary>
    /// <returns>The task with timeout.</returns>
    /// <param name="task">Task.</param>
    /// <param name="timeoutInMilliseconds">Timeout duration in Milliseconds.</param>
    /// <typeparam name="T">The 1st type parameter.</typeparam>
    public async static Task<T> WithTimeout<T>(this Task<T> task, int timeoutInMilliseconds)
    {
        var retTask = await Task.WhenAny(task, Task.Delay(timeoutInMilliseconds))
            .ConfigureAwait(false);

        #pragma warning disable CS8603 // Possible null reference return.
        return retTask is Task<T> ? task.Result : default;
        #pragma warning restore CS8603 // Possible null reference return.
    }

    /// <summary>
    /// Chainable task helper.
    /// var result = await SomeLongAsyncFunction().WithCancellation(cts.Token);
    /// </summary>
    /// <typeparam name="TResult">the type of task result</typeparam>
    /// <returns><see cref="Task"/>TResult</returns>
    public static Task<TResult> WithCancellation<TResult>(this Task<TResult> task, CancellationToken cancelToken)
    {
        var tcs = new TaskCompletionSource<TResult>();
        var reg = cancelToken.Register(() => tcs.TrySetCanceled());
        task.ContinueWith(ant =>
        {
            reg.Dispose();
            if (ant.IsCanceled)
                tcs.TrySetCanceled();
            else if (ant.IsFaulted)
                tcs.TrySetException(ant.Exception.InnerException);
            else
                tcs.TrySetResult(ant.Result);
        });
        return tcs.Task;  // Return the TaskCompletionSource result
    }

    public static Task<T> WithAllExceptions<T>(this Task<T> task)
    {
        TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();

        task.ContinueWith(ignored =>
        {
            switch (task.Status)
            {
                case TaskStatus.Canceled:
                    Debug.WriteLine($"[TaskStatus.Canceled]");
                    tcs.SetCanceled();
                    break;
                case TaskStatus.RanToCompletion:
                    tcs.SetResult(task.Result);
                    //Debug.WriteLine($"[TaskStatus.RanToCompletion({task.Result})]");
                    break;
                case TaskStatus.Faulted:
                    // SetException will automatically wrap the original AggregateException
                    // in another one. The new wrapper will be removed in TaskAwaiter, leaving
                    // the original intact.
                    Debug.WriteLine($"[TaskStatus.Faulted: {task.Exception.Message}]");
                    tcs.SetException(task.Exception);
                    break;
                default:
                    Debug.WriteLine($"[TaskStatus: Continuation called illegally.]");
                    tcs.SetException(new InvalidOperationException("Continuation called illegally."));
                    break;
            }
        });

        return tcs.Task;
    }

    #pragma warning disable RECS0165 // Asynchronous methods should return a Task instead of void
    /// <summary>
    /// Attempts to await on the task and catches exception
    /// </summary>
    /// <param name="task">Task to execute</param>
    /// <param name="onException">What to do when method has an exception</param>
    /// <param name="continueOnCapturedContext">If the context should be captured.</param>
    public static async void SafeFireAndForget(this Task task, Action<Exception>? onException = null, bool continueOnCapturedContext = false)
    #pragma warning restore RECS0165 // Asynchronous methods should return a Task instead of void
    {
        try
        {
            await task.ConfigureAwait(continueOnCapturedContext);
        }
        catch (Exception ex) when (onException != null)
        {
            onException.Invoke(ex);
        }
        catch (Exception ex) when (onException == null)
        {
            Debug.WriteLine($"SafeFireAndForget: {ex.Message}");
        }
    }
    #endregion

    #region [Date and Time]
    public static string GenerateTimeStamp(bool includeMS = false)
    {
        StringBuilder str = new StringBuilder();
        str.Append(DateTime.Now.Year);
        str.Append(DateTime.Now.Month);
        str.Append(DateTime.Now.Day);
        str.Append(DateTime.Now.Hour);
        str.Append(DateTime.Now.Minute);
        str.Append(DateTime.Now.Second);
        if (includeMS)
            str.Append(DateTime.Now.Millisecond);
        return str.ToString();
    }

    /// <summary>
    /// Helper method that takes a string as input and returns a DateTime object.
    /// This method can handle date formats such as "04/30", "0430", "04/2030", 
    /// "042030", "42030", "4/2030" and uses the current year as the year value
    /// for the returned DateTime object.
    /// </summary>
    /// <param name="dateString">the month and year string to parse</param>
    /// <returns><see cref="DateTime"/></returns>
    /// <example>
    /// CardData.CreditData.ExpirationDate = response.ExpiryDate.ExtractExpiration();
    /// </example>
    public static DateTime ExtractExpiration(this string dateString)
    {
        if (string.IsNullOrEmpty(dateString))
            return DateTime.Now;

        try
        {
            string yearPrefix = DateTime.Now.Year.ToString().Substring(0, 2);
            string yearSuffix = "00";

            if (dateString.Contains(@"\"))
                dateString = dateString.Replace(@"\", "/");

            if (dateString.Length == 5 && !dateString.Contains("/"))  // Myyyy
            {
                yearSuffix = dateString.Substring(dateString.Length - 2, 2);
                dateString = dateString.PadLeft(6, '0');
            }
            else if (dateString.Length == 3 && !dateString.Contains("/"))  // Myy
            {
                yearSuffix = dateString.Substring(dateString.Length - 2, 2);
                dateString = dateString.PadLeft(4, '0');
            }
            else if (dateString.Length > 4)  // MM/yy
                yearSuffix = dateString.Substring(dateString.Length - 2, 2);
            else if (dateString.Length > 3)  // MMyy
                yearSuffix = dateString.Substring(dateString.Length - 2, 2);
            else if (dateString.Length > 2)  // Myy
                yearSuffix = dateString.Substring(dateString.Length - 2, 2);
            else if (dateString.Length > 1)  // should not happen
                yearSuffix = dateString;

            if (!int.TryParse($"{yearPrefix}{yearSuffix}", out int yearBase))
                yearBase = DateTime.Now.Year;

            DateTime result;
            if (DateTime.TryParseExact(dateString, "MM/yy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out result))
            {
                return new DateTime(yearBase, result.Month, DateTime.DaysInMonth(yearBase, result.Month));
            }
            else if (DateTime.TryParseExact(dateString, "MMyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out result))
            {
                return new DateTime(yearBase, result.Month, DateTime.DaysInMonth(yearBase, result.Month));
            }
            else if (DateTime.TryParseExact(dateString, "M/yy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out result))
            {
                return new DateTime(yearBase, result.Month, DateTime.DaysInMonth(yearBase, result.Month));
            }
            else if (DateTime.TryParseExact(dateString, "Myy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out result))
            {
                return new DateTime(yearBase, result.Month, DateTime.DaysInMonth(yearBase, result.Month));
            }
            else if (DateTime.TryParseExact(dateString, "MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out result))
            {
                return new DateTime(result.Year, result.Month, DateTime.DaysInMonth(DateTime.Now.Year, result.Month));
            }
            else if (DateTime.TryParseExact(dateString, "MMyyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out result))
            {
                return new DateTime(result.Year, result.Month, DateTime.DaysInMonth(DateTime.Now.Year, result.Month));
            }
            else if (DateTime.TryParseExact(dateString, "M/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out result))
            {
                return new DateTime(result.Year, result.Month, DateTime.DaysInMonth(DateTime.Now.Year, result.Month));
            }
            else if (DateTime.TryParseExact(dateString, "Myyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out result))
            {
                return new DateTime(result.Year, result.Month, DateTime.DaysInMonth(DateTime.Now.Year, result.Month));
            }
            else if (DateTime.TryParseExact(dateString, "yy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out result))
            {
                return new DateTime(yearBase, 12, DateTime.DaysInMonth(yearBase, 12));
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ExtractExpiration: Invalid date format.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExtractExpiration: {ex.Message}");
        }

        return DateTime.Now;
    }

    public static DateTime StartOfMonth(this DateTime dateTime)
    {
        return dateTime.Date.AddDays(-1 * (dateTime.Day - 1));
    }

    public static DateTime EndOfMonth(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, DateTime.DaysInMonth(dateTime.Year, dateTime.Month));
    }

	/// <summary>
	/// Convert a <see cref="DateTime"/> object into an ISO 8601 formatted string.
	/// </summary>
	/// <param name="dateTime"><see cref="DateTime"/></param>
	/// <returns>ISO 8601 formatted string</returns>
	public static string ToJsonFriendlyFormat(this DateTime dateTime)
	{
		return dateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
	}

	/// <summary>
	/// 1/1/2023 7:00:00 AM Local to a DateTimeOffset value of 1/1/2023 7:00:00 AM -07:00
	/// </summary>
	/// <param name="dateTime"><see cref="DateTime"/></param>
	/// <returns><see cref="DateTimeOffset"/></returns>
	public static DateTimeOffset ToLocalTimeOffset(this DateTime dateTime)
	{
		dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Local);
		DateTimeOffset localTime = dateTime;
		return localTime;
	}

	/// <summary>
	/// 1/1/2023 7:00:00 AM Utc to a DateTimeOffset value of 1/1/2023 7:00:00 AM +00:00
	/// </summary>
	/// <param name="dateTime"><see cref="DateTime"/></param>
	/// <returns><see cref="DateTimeOffset"/></returns>
	public static DateTimeOffset ToDateTimeOffset(this DateTime dateTime)
	{
		dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
		DateTimeOffset utcTime = dateTime;
		return utcTime;
	}

	/// <summary>
	/// 1/1/2023 7:00:00 AM Unspecified to a DateTime value of 1/1/2023 7:00:00 AM -05:00
	/// </summary>
	/// <param name="dateTime"><see cref="DateTime"/></param>
	/// <param name="timeZone"></param>
	/// <returns><see cref="DateTimeOffset"/></returns>
	public static DateTimeOffset ToDateTimeOffset(this DateTime dateTime, string timeZone = "Eastern Standard Time")
	{
		try
		{
			DateTimeOffset dto = new DateTimeOffset(dateTime, TimeZoneInfo.FindSystemTimeZoneById(timeZone).GetUtcOffset(dateTime));
			Console.WriteLine("Converted {0} {1} to a DateTime value of {2}", dateTime, dateTime.Kind, dto);
			return dto;
		}
		catch (TimeZoneNotFoundException) // Handle exception if time zone is not defined in registry
		{
			Debug.WriteLine("Unable to identify target time zone for conversion.", $"{nameof(Extensions)}");
			return ToDateTimeOffset(dateTime);
		}
	}

	/// <summary>
	/// Convert time to local UTC DateTimeOffset value
	/// </summary>
	/// <param name="dateTime"><see cref="DateTime"/></param>
	/// <returns><see cref="DateTimeOffset"/></returns>
	public static DateTimeOffset ToLocalDateTimeOffset(this DateTime dateTime)
	{
		return new DateTimeOffset(dateTime, TimeZoneInfo.Local.GetUtcOffset(dateTime));
	}

	/// <summary>
	/// Converts DateTimeOffset values to DateTime values.
	/// Based on its offset, it determines whether the DateTimeOffset 
	/// value is a UTC time, a local time, or some other time and defines 
	/// the returned date and time value's Kind property accordingly.
	/// </summary>
	/// <param name="dateTime"><see cref="DateTimeOffset"/></param>
	/// <returns><see cref="DateTime"/></returns>
	public static DateTime ToDateTime(this DateTimeOffset dateTime)
	{
		if (dateTime.Offset.Equals(TimeSpan.Zero))
			return dateTime.UtcDateTime;
		else if (dateTime.Offset.Equals(TimeZoneInfo.Local.GetUtcOffset(dateTime.DateTime)))
			return DateTime.SpecifyKind(dateTime.DateTime, DateTimeKind.Local);
		else
			return dateTime.DateTime;
	}

	/// <summary>
	/// Checks to see if a date is between two dates.
	/// </summary>
	public static bool Between(this DateTime dt, DateTime rangeBeg, DateTime rangeEnd)
	{
		return dt.Ticks >= rangeBeg.Ticks && dt.Ticks <= rangeEnd.Ticks;
	}

	/// <summary>
	/// Returns a range of <see cref="DateTime"/> objects matching the criteria provided.
	/// </summary>
	/// <example>
	/// IEnumerable{DateTime} dateRange = DateTime.Now.GetDateRangeTo(DateTime.Now.AddDays(80));
	/// </example>
	/// <param name="self"><see cref="DateTime"/></param>
	/// <param name="toDate"><see cref="DateTime"/></param>
	/// <returns><see cref="IEnumerable{DateTime}"/></returns>
	public static IEnumerable<DateTime> GetDateRangeTo(this DateTime self, DateTime toDate)
	{
		var range = Enumerable.Range(0, new TimeSpan(toDate.Ticks - self.Ticks).Days);

		return from p in range select self.Date.AddDays(p);
	}

	/// <summary>
	/// Accounts for once date1 is past date2.
	/// </summary>
	public static bool WithinOneDayOrPast(this DateTime date1, DateTime date2)
	{
		DateTime first = DateTime.Parse($"{date1}");
		if (first < date2) // Account for past-due amounts.
		{
			return true;
		}
		else
		{
			TimeSpan difference = first - date2;
			return Math.Abs(difference.TotalDays) <= 1.0;
		}
	}

	/// <summary>
	/// Only accounts for date1 being within range of date2.
	/// </summary>
	public static bool WithinOneDay(this DateTime date1, DateTime date2)
	{
		TimeSpan difference = DateTime.Parse($"{date1}") - date2;
		return Math.Abs(difference.TotalDays) <= 1.0;
	}

	/// <summary>
	/// Only accounts for date1 being within range of date2 by some amount.
	/// </summary>
	public static bool WithinAmountOfDays(this DateTime date1, DateTime date2, double days)
	{
		TimeSpan difference = DateTime.Parse($"{date1}") - date2;
		return Math.Abs(difference.TotalDays) <= days;
	}

	/// <summary>
	/// Display a readable sentence as to when that time happened.
	/// e.g. "5 minutes ago" or "in 2 days"
	/// </summary>
	/// <param name="value"><see cref="DateTime"/>the past/future time to compare from now</param>
	/// <returns>human friendly format</returns>
	public static string ToReadableTime(this DateTime value, bool useUTC = false)
	{
		TimeSpan ts;
		if (useUTC) { ts = new TimeSpan(DateTime.UtcNow.Ticks - value.Ticks); }
		else { ts = new TimeSpan(DateTime.Now.Ticks - value.Ticks); }

		double delta = ts.TotalSeconds;
		if (delta < 0) // in the future
		{
			delta = Math.Abs(delta);
			if (delta < 60) { return Math.Abs(ts.Seconds) == 1 ? "in one second" : "in " + Math.Abs(ts.Seconds) + " seconds"; }
			if (delta < 120) { return "in a minute"; }
			if (delta < 3000) { return "in " + Math.Abs(ts.Minutes) + " minutes"; } // 50 * 60
			if (delta < 5400) { return "in an hour"; } // 90 * 60
			if (delta < 86400) { return "in " + Math.Abs(ts.Hours) + " hours"; } // 24 * 60 * 60
			if (delta < 172800) { return "tomorrow"; } // 48 * 60 * 60
			if (delta < 2592000) { return "in " + Math.Abs(ts.Days) + " days"; } // 30 * 24 * 60 * 60
			if (delta < 31104000) // 12 * 30 * 24 * 60 * 60
			{
				int months = Convert.ToInt32(Math.Floor((double)Math.Abs(ts.Days) / 30));
				return months <= 1 ? "in one month" : "in " + months + " months";
			}
			int years = Convert.ToInt32(Math.Floor((double)Math.Abs(ts.Days) / 365));
			return years <= 1 ? "in one year" : "in " + years + " years";
		}
		else // in the past
		{
			if (delta < 60) { return ts.Seconds == 1 ? "one second ago" : ts.Seconds + " seconds ago"; }
			if (delta < 120) { return "a minute ago"; }
			if (delta < 3000) { return ts.Minutes + " minutes ago"; } // 50 * 60
			if (delta < 5400) { return "an hour ago"; } // 90 * 60
			if (delta < 86400) { return ts.Hours + " hours ago"; } // 24 * 60 * 60
			if (delta < 172800) { return "yesterday"; } // 48 * 60 * 60
			if (delta < 2592000) { return ts.Days + " days ago"; } // 30 * 24 * 60 * 60
			if (delta < 31104000) // 12 * 30 * 24 * 60 * 60
			{
				int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
				return months <= 1 ? "one month ago" : months + " months ago";
			}
			int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
			return years <= 1 ? "one year ago" : years + " years ago";
		}
	}


	/// <summary>
	/// Converts <see cref="TimeSpan"/> objects to a simple human-readable string.
	/// e.g. 420 milliseconds, 3.1 seconds, 2 minutes, 4.231 hours, etc.
	/// </summary>
	/// <param name="span"><see cref="TimeSpan"/></param>
	/// <param name="significantDigits">number of right side digits in output (precision)</param>
	/// <returns>human-friendly string</returns>
	public static string ToTimeString(this TimeSpan span, int significantDigits = 3)
	{
		var format = $"G{significantDigits}";
		return span.TotalMilliseconds < 1000 ? span.TotalMilliseconds.ToString(format) + " milliseconds"
				: (span.TotalSeconds < 60 ? span.TotalSeconds.ToString(format) + " seconds"
				: (span.TotalMinutes < 60 ? span.TotalMinutes.ToString(format) + " minutes"
				: (span.TotalHours < 24 ? span.TotalHours.ToString(format) + " hours"
				: span.TotalDays.ToString(format) + " days")));
	}

	/// <summary>
	/// Converts long file size into typical browser file size.
	/// </summary>
	public static string ToFileSize(this ulong size)
	{
		if (size < 1024) { return (size).ToString("F0") + " Bytes"; }
		if (size < Math.Pow(1024, 2)) { return (size / 1024).ToString("F0") + "KB"; }
		if (size < Math.Pow(1024, 3)) { return (size / Math.Pow(1024, 2)).ToString("F0") + "MB"; }
		if (size < Math.Pow(1024, 4)) { return (size / Math.Pow(1024, 3)).ToString("F0") + "GB"; }
		if (size < Math.Pow(1024, 5)) { return (size / Math.Pow(1024, 4)).ToString("F0") + "TB"; }
		if (size < Math.Pow(1024, 6)) { return (size / Math.Pow(1024, 5)).ToString("F0") + "PB"; }
		return (size / Math.Pow(1024, 6)).ToString("F0") + "EB";
	}

	/// <summary>
	/// Converts long file size into typical browser file size.
	/// </summary>
	public static string ToFileSize(this float size)
	{
		if (size < 1024) { return (size).ToString("F0") + " Bytes"; }
		if (size < Math.Pow(1024, 2)) { return (size / 1024).ToString("F0") + "KB"; }
		if (size < Math.Pow(1024, 3)) { return (size / Math.Pow(1024, 2)).ToString("F0") + "MB"; }
		if (size < Math.Pow(1024, 4)) { return (size / Math.Pow(1024, 3)).ToString("F0") + "GB"; }
		if (size < Math.Pow(1024, 5)) { return (size / Math.Pow(1024, 4)).ToString("F0") + "TB"; }
		if (size < Math.Pow(1024, 6)) { return (size / Math.Pow(1024, 5)).ToString("F0") + "PB"; }
		return (size / Math.Pow(1024, 6)).ToString("F0") + "EB";
	}
	#endregion

	#region [IEnumerables]
	public static IEnumerable<T> TakeLastFive<T>(this IEnumerable<T> source)
    {
        if (source.Count() < 5)
            return source;

        return source.Skip(Math.Max(0, source.Count() - 5));
    }

    public static IEnumerable<T> TakeFirstFive<T>(this IEnumerable<T> source)
    {
        if (source.Count() < 5)
            return source;

        return source.Take(5);
    }

    /// <summary>
    /// NOTE: You must call this on a UI thread.
    /// IEnumerable{Button} kids = GetChildren(rootStackPanel).Where(ctrl => ctrl is Button).Cast{Button}();
    /// </summary>
    public static IEnumerable<UIElement> GetChildren(this UIElement parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            if (VisualTreeHelper.GetChild(parent, i) is UIElement child)
            {
                yield return child;
            }
        }
    }
    #endregion

    #region [WriteableBitmap]
    public static async Task SaveAsync(this WriteableBitmap writeableBitmap, StorageFile outputFile)
    {
        var encoderId = GetEncoderId(outputFile.Name);

        try
        {
            Stream stream = writeableBitmap.PixelBuffer.AsStream();
            byte[] pixels = new byte[(uint)stream.Length];
            await stream.ReadAsync(pixels, 0, pixels.Length);

            using (var writeStream = await outputFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await BitmapEncoder.CreateAsync(encoderId, writeStream);
                encoder.SetPixelData(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Premultiplied,
                    (uint)writeableBitmap.PixelWidth,
                    (uint)writeableBitmap.PixelHeight,
                    96,
                    96,
                    pixels);

                await encoder.FlushAsync();

                using (var outputStream = writeStream.GetOutputStreamAt(0))
                {
                    await outputStream.FlushAsync();
                }
            }
        }
        catch (Exception ex)
        {
            // Your exception handling here..
            throw;
        }
    }

    public static async Task<WriteableBitmap> LoadAsync(this WriteableBitmap writeableBitmap, StorageFile storageFile)
    {
        var wb = writeableBitmap;

        using (var stream = await storageFile.OpenReadAsync())
        {
            await wb.SetSourceAsync(stream);
        }

        return wb;
    }

    static Guid GetEncoderId(string fileName)
    {
        Guid encoderId;

        var ext = Path.GetExtension(fileName);

        if (new[] { ".bmp", ".dib" }.Contains(ext))
            encoderId = BitmapEncoder.BmpEncoderId;
        else if (new[] { ".tiff", ".tif" }.Contains(ext))
            encoderId = BitmapEncoder.TiffEncoderId;
        else if (new[] { ".gif" }.Contains(ext))
            encoderId = BitmapEncoder.GifEncoderId;
        else if (new[] { ".jpg", ".jpeg", ".jpe", ".jfif", ".jif" }.Contains(ext))
            encoderId = BitmapEncoder.JpegEncoderId;
        else if (new[] { ".hdp", ".jxr", ".wdp" }.Contains(ext))
            encoderId = BitmapEncoder.JpegXREncoderId;
        else //if (new [] {".png"}.Contains(ext))
            encoderId = BitmapEncoder.PngEncoderId;

        return encoderId;
    }
	#endregion

	#region [CropBitmap]
	/// <summary>
	/// Get a cropped bitmap from a image file.
	/// </summary>
	/// <param name="originalImageFile">The original image file.</param>
	/// <param name="startPoint">The start point of the region to be cropped.</param>
	/// <param name="corpSize">The size of the region to be cropped.</param>
	/// <returns>The cropped image.</returns>
	public async static Task<WriteableBitmap> GetCroppedBitmapAsync(StorageFile originalImageFile,
		Windows.Foundation.Point startPoint,
		Windows.Foundation.Size corpSize,
		double scale)
	{
		if (double.IsNaN(scale) || double.IsInfinity(scale))
			scale = 1;

		// Convert start point and size to integer.
		uint startPointX = (uint)Math.Floor(startPoint.X * scale);
		uint startPointY = (uint)Math.Floor(startPoint.Y * scale);
		uint height = (uint)Math.Floor(corpSize.Height * scale);
		uint width = (uint)Math.Floor(corpSize.Width * scale);

		using (IRandomAccessStream stream = await originalImageFile.OpenReadAsync())
		{

			// Create a decoder from the stream. With the decoder, we can get 
			// the properties of the image.
			BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

			// The scaledSize of original image.
			uint scaledWidth = (uint)Math.Floor(decoder.PixelWidth * scale);
			uint scaledHeight = (uint)Math.Floor(decoder.PixelHeight * scale);


			// Refine the start point and the size. 
			if (startPointX + width > scaledWidth)
				startPointX = scaledWidth - width;

			if (startPointY + height > scaledHeight)
				startPointY = scaledHeight - height;

			// Get the cropped pixels.
			byte[] pixels = await GetPixelDataScaled(decoder,
				startPointX, startPointY,
				width, height,
				scaledWidth, scaledHeight);

			// Stream the bytes into a WriteableBitmap
			WriteableBitmap cropBmp = new WriteableBitmap((int)width, (int)height);
			Stream pixStream = cropBmp.PixelBuffer.AsStream();
			pixStream.Write(pixels, 0, (int)(width * height * 4));

			return cropBmp;
		}
	}

	/// <summary>
	/// Use BitmapTransform to define the region to crop, and then get the pixel data in the region
	/// </summary>
	public async static Task<byte[]> GetPixelData(BitmapDecoder decoder, uint startPointX, uint startPointY, uint width, uint height)
	{
		return await GetPixelDataScaled(decoder, startPointX, startPointY, width, height, decoder.PixelWidth, decoder.PixelHeight);
	}

	/// <summary>
	/// Use BitmapTransform to define the region to crop, and then get the pixel data in the region.
	/// If you want to get the pixel data of a scaled image, set the scaledWidth and scaledHeight
	/// of the scaled image.
	/// </summary>
	public async static Task<byte[]> GetPixelDataScaled(BitmapDecoder decoder, uint startPointX, uint startPointY, uint width, uint height, uint scaledWidth, uint scaledHeight)
	{
		BitmapTransform transform = new BitmapTransform();
		BitmapBounds bounds = new BitmapBounds();
		bounds.X = startPointX;
		bounds.Y = startPointY;
		bounds.Height = height;
		bounds.Width = width;
		transform.Bounds = bounds;
		transform.ScaledWidth = scaledWidth;
		transform.ScaledHeight = scaledHeight;

        try
        {
            // Get the cropped pixels within the bounds of transform.
            PixelDataProvider pix = await decoder.GetPixelDataAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Straight,
                transform,
                ExifOrientationMode.IgnoreExifOrientation,
                ColorManagementMode.ColorManageToSRgb);

            byte[] pixels = pix.DetachPixelData();

            return pixels;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GetPixelDataScaled: {ex.Message}");
            return new byte[] { 0 };
        }
	}
	#endregion

    #region [Byte Conversions]
    public static string ByteToString(params byte[] data)
    {
        StringBuilder sBuilder = new StringBuilder();

        foreach (byte b in data)
            sBuilder.AppendFormat("{0:X2} ", b);

        return sBuilder.ToString();
    }

    public static byte[] ByteStringToByte(this string bytes, char delim = ' ')
    {
        List<byte> bs = new List<byte>();

        foreach (string b in bytes.Split(delim))
        {
            if (b.Trim().Equals(""))
                continue;
            bs.Add(byte.Parse(b.Trim(), System.Globalization.NumberStyles.HexNumber));
        }

        return bs.ToArray();
    }
    #endregion

    #region [Resources]
    /// <summary>
    /// Use this if you only have a root resource dictionary.
    /// var rdBrush = Extensions.GetResource{SolidColorBrush}("PrimaryBrush");
    /// </summary>
    public static T? GetResource<T>(string resourceName) where T : class
    {
        try
        {
            if (Application.Current.Resources.TryGetValue($"{resourceName}", out object value))
                return (T)value;
            else
                return default(T);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GetResource: {ex.Message}", $"{nameof(Extensions)}");
            return null;
        }
    }

    /// <summary>
    /// Use this if you have merged theme resource dictionaries.
    /// var darkBrush = Extensions.GetThemeResource{SolidColorBrush}("PrimaryBrush", ElementTheme.Dark);
    /// var lightBrush = Extensions.GetThemeResource{SolidColorBrush}("PrimaryBrush", ElementTheme.Light);
    /// </summary>
    public static T? GetThemeResource<T>(string resourceName, ElementTheme? theme) where T : class
    {
        try
        {
            if (theme == null) { theme = ElementTheme.Default; }

            var dictionaries = Application.Current.Resources.MergedDictionaries;
            foreach (var item in dictionaries)
            {
                // Do we have any themes in this resource dictionary?
                if (item.ThemeDictionaries.Count > 0)
                {
                    if (theme == ElementTheme.Dark)
                    {
                        if (item.ThemeDictionaries.TryGetValue("Dark", out var drd))
                        {
                            ResourceDictionary? dark = drd as ResourceDictionary;
                            if (dark != null)
                            {
                                Debug.WriteLine($"Found dark theme resource dictionary");
                                if (dark.TryGetValue($"{resourceName}", out var tmp))
                                    return (T)tmp;
                                else
                                    Debug.WriteLine($"Could not find '{resourceName}'");
                            }
                        }
                        else { Debug.WriteLine($"{nameof(ElementTheme.Dark)} theme was not found"); }
                    }
                    else if (theme == ElementTheme.Light)
                    {
                        if (item.ThemeDictionaries.TryGetValue("Light", out var lrd))
                        {
                            ResourceDictionary? light = lrd as ResourceDictionary;
                            if (light != null)
                            {
                                Debug.WriteLine($"Found light theme resource dictionary");
                                if (light.TryGetValue($"{resourceName}", out var tmp))
                                    return (T)tmp;
                                else
                                    Debug.WriteLine($"Could not find '{resourceName}'");
                            }
                        }
                        else { Debug.WriteLine($"{nameof(ElementTheme.Light)} theme was not found"); }
                    }
                    else if (theme == ElementTheme.Default)
                    {
                        if (item.ThemeDictionaries.TryGetValue("Default", out var drd))
                        {
                            ResourceDictionary? dflt = drd as ResourceDictionary;
                            if (dflt != null)
                            {
                                Debug.WriteLine($"Found default theme resource dictionary");
                                if (dflt.TryGetValue($"{resourceName}", out var tmp))
                                    return (T)tmp;
                                else
                                    Debug.WriteLine($"Could not find '{resourceName}'");
                            }
                        }
                        else { Debug.WriteLine($"{nameof(ElementTheme.Default)} theme was not found"); }
                    }
                    else
                        Debug.WriteLine($"No theme to match");
                }
                else
                    Debug.WriteLine($"No theme dictionaries found");
            }

            return default(T);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GetThemeResource: {ex.Message}", $"{nameof(Extensions)}");
            return null;
        }
    }
    #endregion

    #region [Strings]
    public static bool HasAlpha(this string str)
    {
        if (string.IsNullOrEmpty(str)) { return false; }
        return str.Any(x => char.IsLetter(x));
    }
    public static bool HasAlphaRegex(this string str)
    {
        return Regex.IsMatch(str ?? "", @"[+a-zA-Z]+");
    }

    public static bool HasNumeric(this string str)
    {
        if (string.IsNullOrEmpty(str)) { return false; }
        return str.Any(x => char.IsNumber(x));
    }
    public static bool HasNumericRegex(this string str)
    {
        return Regex.IsMatch(str ?? "", @"[0-9]+"); // [^\D+]
    }

    public static bool HasSpace(this string str)
    {
        if (string.IsNullOrEmpty(str)) { return false; }
        return str.Any(x => char.IsSeparator(x));
    }
    public static bool HasSpaceRegex(this string str)
    {
        return Regex.IsMatch(str ?? "", @"[\s]+");
    }

    public static bool HasPunctuation(this string str)
    {
        if (string.IsNullOrEmpty(str)) { return false; }
        return str.Any(x => char.IsPunctuation(x));
    }

    public static bool HasAlphaNumeric(this string str)
    {
        if (string.IsNullOrEmpty(str)) { return false; }
        return str.Any(x => char.IsNumber(x)) && str.Any(x => char.IsLetter(x));
    }
    public static bool HasAlphaNumericRegex(this string str)
    {
        return Regex.IsMatch(str ?? "", "[a-zA-Z0-9]+");
    }

    public static string RemoveAlphas(this string str)
    {
        return string.Concat(str?.Where(c => char.IsNumber(c) || c == '.') ?? string.Empty);
    }

    public static string RemoveNumerics(this string str)
    {
        return string.Concat(str?.Where(c => char.IsLetter(c)) ?? string.Empty);
    }

    public static string RemoveExtraSpaces(this string strText)
    {
        if (!string.IsNullOrEmpty(strText))
            strText = Regex.Replace(strText, @"\s+", " ");

        return strText;
    }

    /// <summary>
    /// ExampleTextSample => Example Text Sample
    /// </summary>
    /// <param name="input"></param>
    /// <returns>space delimited string</returns>
    public static string SeparateCamelCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        StringBuilder result = new StringBuilder();
        result.Append(input[0]);

        for (int i = 1; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]))
                result.Append(' ');

            result.Append(input[i]);
        }

        return result.ToString();
    }

    /// <summary>
    /// Helper for parsing command line arguments.
    /// </summary>
    /// <param name="inputArray"></param>
    /// <returns>string array of args excluding the 1st arg</returns>
    public static string[] IgnoreFirstTakeRest(this string[] inputArray)
    {
        if (inputArray.Length > 1)
            return inputArray.Skip(1).ToArray();
        else
            return new string[0];
    }

    /// <summary>
    /// Returns the first element from a tokenized string, e.g.
    /// Input:"{tag}"  Output:"tag"
    /// </summary>
    /// <example>
    /// var clean = ExtractFirst("{tag}", '{', '}');
    /// </example>
    public static string ExtractFirst(this string text, char start, char end)
    {
        string pattern = @"\" + start + "(.*?)" + @"\" + end; //pattern = @"\{(.*?)\}"
        Match match = Regex.Match(text, pattern);
        if (match.Success)
            return match.Groups[1].Value;
        else
            return "";
    }

    /// <summary>
    /// Returns the last element from a tokenized string, e.g.
    /// Input:"{tag}"  Output:"tag"
    /// </summary>
    /// <example>
    /// var clean = ExtractLast("{tag}", '{', '}');
    /// </example>
    public static string ExtractLast(this string text, char start, char end)
    {
        string pattern = @"\" + start + @"(.*?)\" + end; //pattern = @"\{(.*?)\}"
        MatchCollection matches = Regex.Matches(text, pattern);
        if (matches.Count > 0)
        {
            Match lastMatch = matches[matches.Count - 1];
            return lastMatch.Groups[1].Value;
        }
        else
            return "";
    }

    /// <summary>
    /// Returns all the elements from a tokenized string, e.g.
    /// Input:"{tag}"  Output:"tag"
    /// </summary>
    public static string[] ExtractAll(this string text, char start, char end)
    {
        string pattern = @"\" + start + @"(.*?)\" + end; //pattern = @"\{(.*?)\}"
        MatchCollection matches = Regex.Matches(text, pattern);
        string[] results = new string[matches.Count];
        for (int i = 0; i < matches.Count; i++)
            results[i] = matches[i].Groups[1].Value;

        return results;
    }

    /// <summary>
    /// Returns the specified occurrence of a character in a string.
    /// </summary>
    /// <returns>
    /// Index of requested occurrence if successful, -1 otherwise.
    /// </returns>
    /// <example>
    /// If you wanted to find the second index of the percent character in a string:
    /// int index = "blah%blah%blah".IndexOfNth('%', 2);
    /// </example>
    public static int IndexOfNth(this string input, char character, int position)
    {
        int index = -1;

        if (string.IsNullOrEmpty(input))
            return index;

        for (int i = 0; i < position; i++)
        {
            index = input.IndexOf(character, index + 1);
            if (index == -1) { break; }
        }

        return index;
    }
    #endregion

    #region [Miscellaneous]
    /// <summary>
    /// Formats the external caller into a usable name.
    /// Based on the project, the compiler could choose to inline this method with the caller, we do not want that behavior
    /// in this case as we want to separate the caller and the callee so we'll add the [MethodImplOptions.NoInlining] directive.
    /// </summary>
    /// <param name="resource">the method/object being requested</param>
    /// <returns>the calling assembly's name</returns>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)] // Prevent the JIT compiler from inlining the method that calls GetCallingAssembly/GetEntryAssembly.
    static string LogCaller(string resource = "")
    {
        var callerName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "";
        var callerVersion = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version ?? new Version();

        if (!string.IsNullOrEmpty(resource))
            Debug.WriteLine($"{resource} called from {callerName} (v{callerVersion})");

        return callerName;
    }

    /// <summary>
    /// Determines if the specified exception is un-recoverable.
    /// </summary>
    /// <returns>true if the process cannot be recovered from, false otherwise</returns>
    public static bool IsCritical(this Exception exception)
	{
		return (exception is OutOfMemoryException) ||
			   (exception is StackOverflowException) ||
			   (exception is AccessViolationException) ||
			   (exception is ThreadAbortException);
	}

	/// <summary>
	/// Basic key/pswd generator for unique IDs.
	/// This employs the standard MS key table which accounts
	/// for the 36 Latin letters and Arabic numerals used in
	/// most Western European languages...
	/// 24 chars are favored: 2346789 BCDFGHJKMPQRTVWXY
	/// 12 chars are avoided: 015 AEIOU LNSZ
	/// Only 2 chars are occasionally mistaken: 8 & B (depends on the font).
	/// The base of possible codes is large (about 3.2 * 10^34).
	/// </summary>
	public static string KeyGen(int pLength = 6, long pSeed = 0)
    {
        const string pwChars = "2346789BCDFGHJKMPQRTVWXY";
        if (pLength < 6)
            pLength = 6; // minimum of 6 characters

        char[] charArray = pwChars.Distinct().ToArray();

        if (pSeed == 0)
        {
            pSeed = DateTime.Now.Ticks;
            Thread.Sleep(1); // allow a tick to go by (if hammering)
        }

        var result = new char[pLength];
        var rng = new Random((int)pSeed);

        for (int x = 0; x < pLength; x++)
            result[x] = pwChars[rng.Next() % pwChars.Length];

        return (new string(result));
    }

    public static string PrettyXml(this string xml)
    {
        try
        {
            var stringBuilder = new StringBuilder();
            var element = System.Xml.Linq.XElement.Parse(xml);
            var settings = new System.Xml.XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            settings.Indent = true;
            settings.NewLineOnAttributes = true;
            // XmlWriter offers a StringBuilder as an output.
            using (var xmlWriter = System.Xml.XmlWriter.Create(stringBuilder, settings))
            {
                element.Save(xmlWriter);
            }

            return stringBuilder.ToString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PrettyXml: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Debugging helper method.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns>type name then base type for the object</returns>
    public static string NameOf(this object obj)
    {
        return $"{obj.GetType().Name} => {obj.GetType().BaseType?.Name}";
    }

    public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
    {
        return val.CompareTo(min) < 0 ? min : (val.CompareTo(max) > 0 ? max : val);
    }

	/// <summary>
	/// Scale a range of numbers. [baseMin to baseMax] will become [limitMin to limitMax]
	/// </summary>
	public static double Scale(this double valueIn, double baseMin, double baseMax, double limitMin, double limitMax) => ((limitMax - limitMin) * (valueIn - baseMin) / (baseMax - baseMin)) + limitMin;
	public static float Scale(this float valueIn, float baseMin, float baseMax, float limitMin, float limitMax) => ((limitMax - limitMin) * (valueIn - baseMin) / (baseMax - baseMin)) + limitMin;
	public static int Scale(this int valueIn, int baseMin, int baseMax, int limitMin, int limitMax) => ((limitMax - limitMin) * (valueIn - baseMin) / (baseMax - baseMin)) + limitMin;

	/// <summary>
	/// LERP a range of numbers.
	/// </summary>
	public static double Lerp(this double start, double end, double amount = 0.5D) => start + (end - start) * amount;
	public static float Lerp(this float start, float end, float amount = 0.5F) => start + (end - start) * amount;

	/// <summary>
	/// Returns the field names and their types for a specific class.
	/// </summary>
	/// <param name="myType"></param>
	/// <example>
	/// var dict = ReflectFieldInfo(typeof(MainPage));
	/// </example>
	public static Dictionary<string, Type> ReflectFieldInfo(Type myType)
    {
        Dictionary<string, Type> results = new();
        FieldInfo[] myFieldInfo = myType.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
        for (int i = 0; i < myFieldInfo.Length; i++) { results[myFieldInfo[i].Name] = myFieldInfo[i].FieldType; }
        return results;
    }

    /// <summary>
    /// Tries to get a boxed <typeparamref name="T"/> value from an input <see cref="object"/> instance.
    /// </summary>
    /// <typeparam name="T">The type of value to try to unbox.</typeparam>
    /// <param name="obj">The input <see cref="object"/> instance to check.</param>
    /// <param name="value">The resulting <typeparamref name="T"/> value, if <paramref name="obj"/> was in fact a boxed <typeparamref name="T"/> value.</param>
    /// <returns><see langword="true"/> if a <typeparamref name="T"/> value was retrieved correctly, <see langword="false"/> otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryUnbox<T>(this object obj, out T value) where T : struct
    {
        if (obj.GetType() == typeof(T))
        {
            value = Unsafe.Unbox<T>(obj);
            return true;
        }
        value = default;
        return false;
    }

    /// <summary>
    /// This performs no conversion, it reboxes the same value in another type.
    /// </summary>
    /// <example>
    /// object? enumTest = LogLevel.Warning.GetBoxedEnumValue();
    /// Debug.WriteLine($"{enumTest} ({enumTest.GetType()})");
    /// Output: "5 (System.Int32)"
    /// </example>
    public static object GetBoxedEnumValue(this Enum anyEnum)
    {
        Type intType = Enum.GetUnderlyingType(anyEnum.GetType());
        return Convert.ChangeType(anyEnum, intType);
    }
	#endregion
}
