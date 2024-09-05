using System;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Dialogs.Internal;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;

namespace MoSpeedUI;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        Height = 600;
        Width = 800;
        Logo.Width = this.ClientSize.Width / 2;
        this.Resized += ((sender, args) =>
        {
            Logo.Width = (args.ClientSize.Width / 2);
        });
        LicenseBox.Text = new StreamReader(AssetLoader.Open(new Uri("avares://MoSpeedUI/Assets/Text/LicenseText.txt"))).ReadToEnd();
    }

    private void OpenGitBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        Process p = new();
        p.StartInfo.FileName = "https://github.com/Error504TimeOut/MoSpeedUI";
        p.StartInfo.UseShellExecute = true;
        p.Start();
    }

    private void ReportBugBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        Process p = new();
        p.StartInfo.FileName = "https://github.com/Error504TimeOut/MoSpeedUI/issues";
        p.StartInfo.UseShellExecute = true;
        p.Start();
    }

    private void ClsBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}