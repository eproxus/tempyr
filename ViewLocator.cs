using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Tempyr.ViewModels;
using Tempyr.Views;

namespace Tempyr;

/// <summary>
/// Given a view model, returns the corresponding view if possible.
/// Uses an explicit map instead of reflection so trimming doesn't remove views.
/// </summary>
public class ViewLocator : IDataTemplate
{
    private static readonly Dictionary<Type, Func<Control>> Map = new()
    {
        [typeof(InstalledModsViewModel)] = () => new InstalledModsView(),
        [typeof(SettingsViewModel)]      = () => new SettingsView(),
    };

    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        if (Map.TryGetValue(param.GetType(), out var factory))
            return factory();

        return new TextBlock { Text = "Not Found: " + param.GetType().FullName };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
