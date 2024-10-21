using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
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
        UpdateBtn.IsEnabled = false;
        string tmpDir = Path.Join(Path.GetTempPath(), Path.GetRandomFileName());
        string basePath = MainWindow.AppConfiguration.MoSpeedPath.Replace("\\dist", "\\").Replace("/dist", "/");
        Directory.CreateDirectory(tmpDir);
        string outputPath = Path.Join(tmpDir, "mospeed.zip");
        try
        {
            using var client = new HttpClient();
            using var response =
                await client.GetAsync("https://github.com/EgonOlsen71/basicv2/archive/refs/heads/master.zip",
                    HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            await using var contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream =
                new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
            var buffer = new byte[8192];
            long totalBytesRead = 0;
            int bytesRead;
            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalBytesRead += bytesRead;
            }
            fileStream.Close();
            await Task.Run(() =>
            {
                ZipFile.ExtractToDirectory(Path.Join(tmpDir, "mospeed.zip"),
                    tmpDir, true);
            });
            try
            {
                Directory.Delete(basePath, true);   
            }
            catch{}
            Shared.CopyFilesRecursively(Path.Join(tmpDir, "basicv2-master"),
                basePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex + "//" + ex.Message);
            var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
            {
                ContentMessage = Lang.Resources.MSDownloadError + $" {ex.Message}",
                ButtonDefinitions = new List<ButtonDefinition>
                {
                    new ButtonDefinition { Name = "Ok" }
                },
                Icon = MsBox.Avalonia.Enums.Icon.Error
            });
            await box.ShowAsPopupAsync(this);
            return;
        }
        finally
        {
            UpdateBtn.IsEnabled = true;
        }
        var sBox = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
        {
            ContentMessage = Lang.Resources.MSDownloadSuccess + $" {MainWindow.AppConfiguration.MoSpeedPath}",
            ButtonDefinitions = new List<ButtonDefinition>
            {
                new ButtonDefinition { Name = "Ok" }
            },
            Icon = MsBox.Avalonia.Enums.Icon.Success
        });
        await sBox.ShowAsPopupAsync(this);
    }
}