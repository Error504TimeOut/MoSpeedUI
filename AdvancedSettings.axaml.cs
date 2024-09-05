using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SkiaSharp;

namespace MoSpeedUI;

public partial class AdvancedSettings : Window
{
    public AdvancedSettings()
    {
        this.Width = 800;
        Height = 250;
        InitializeComponent();
        if (!string.IsNullOrWhiteSpace(MainWindow.CompileConfig.ExtendedArguments))
        {
            AdvBox.Text = MainWindow.CompileConfig.ExtendedArguments;
        }

        this.Closing += ((sender, args) => { MainWindow.CompileConfig.ExtendedArguments = AdvBox?.Text!; });
    }

    private void Button_OnClick(object? s, RoutedEventArgs e)
    {
        MainWindow.CompileConfig.ExtendedArguments = AdvBox?.Text!;
        Close();
    }

    private void WikiBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        Process p = new();
        p.StartInfo.FileName = Lang.Resources.WikiUrl;
        p.StartInfo.UseShellExecute = true;
        p.Start();
    }
}