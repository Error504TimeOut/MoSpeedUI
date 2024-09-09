using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Models;
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

    private async void UpdateBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
        {
            ContentMessage = String.Format(Lang.Resources.ConfirmReset, MainWindow.ConfigFolder),
            ButtonDefinitions = new List<ButtonDefinition>
            {
                new ButtonDefinition {Name = Lang.Resources.Abort, IsCancel = true},
                new ButtonDefinition { Name = Lang.Resources.Yes }
            },
            Icon = MsBox.Avalonia.Enums.Icon.Question
        });
        var res = await box.ShowAsPopupAsync(this);
        if (res == Lang.Resources.Yes)
        {
            SetupWindow sw = new SetupWindow(true);
            sw.ShowDialog(this);
        }
    }
}