using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using CommunityToolkit.Mvvm.ComponentModel; // properties
using CommunityToolkit.Mvvm.Input;          // commands

namespace Monitor;

/// <summary>
/// A view model is not utilized in this simple application, but I've added it for future use.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    static DispatcherQueue? Dispatcher = null;

    [ObservableProperty]
    int refreshRate = 3;

    [ObservableProperty]
    bool isBusy = false;

    [ObservableProperty]
    string message = "...";

	public MainViewModel() 
    {
        Debug.WriteLine($"{MethodBase.GetCurrentMethod()?.DeclaringType?.Name}__{MethodBase.GetCurrentMethod()?.Name} [{DateTime.Now.ToString("hh:mm:ss.fff tt")}]");
    }

    public MainViewModel(DispatcherQueue dispatcher) : this()
    {
        // For fancy UI updating.
        Dispatcher = dispatcher;
    }
}
