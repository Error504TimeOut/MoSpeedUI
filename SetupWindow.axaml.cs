using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;

namespace MoSpeedUI;

public partial class SetupWindow : Window
{
    public SetupWindow(bool restart = false)
    {
        InitializeComponent();
        if (restart)
        {
            if (Directory.Exists(MainWindow.ConfigFolder))
            {
                DirectoryInfo di = new DirectoryInfo(MainWindow.ConfigFolder);
                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }

                foreach (DirectoryInfo dir in di.GetDirectories())
                {
                    dir.Delete(true);
                }
            }
        }
        Prepare();
    }
    public void Prepare()
    {
            this.Width = 600;
            this.Height = 300;
            DwnldMsRBtn.IsCheckedChanged += ((sender, args) =>
            {
                if (DwnldMsRBtn.IsChecked != null)
                    if ((bool)DwnldMsRBtn.IsChecked)
                    {
                        Step2B.IsVisible = false;
                        Step2A.IsVisible = true;
                    }
                    else
                    {
                        Step2A.IsVisible = false;
                        Step2B.IsVisible = true;
                    }
            });
            DwnldMsRBtn.IsChecked = true;
    }
    async private void DwnldBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DwnldBar.IsVisible = true;
        DwnldBar.ShowProgressText = true; 
        await DownloadAsyncWithProgress("https://github.com/EgonOlsen71/basicv2/archive/refs/heads/master.zip",Path.Join(MainWindow.ConfigFolder,"mospeed.zip"),DwnldBar);
        DwnldBar.ProgressTextFormat = Lang.Resources.Unzip;
        DwnldBar.Value = 0;
        DwnldBar.IsIndeterminate = true;
        await Task.Run(() => { ZipFile.ExtractToDirectory(Path.Join(MainWindow.ConfigFolder,"mospeed.zip"),MainWindow.ConfigFolder);});
            Directory.Move(Path.Join(MainWindow.ConfigFolder, "basicv2-master"),
                Path.Join(MainWindow.ConfigFolder, "mospeed"));
        PathBox.Text = Path.Join(MainWindow.ConfigFolder, "mospeed","dist");
        var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
        {
            ContentMessage = Lang.Resources.MSDownloadSuccess + $" {MainWindow.ConfigFolder}/mospeed",
            ButtonDefinitions = new List<ButtonDefinition>
            {
                new ButtonDefinition { Name = "Ok" }
            },
            Icon = MsBox.Avalonia.Enums.Icon.Success
        });
        File.WriteAllText(MainWindow.ConfigFile, PathBox.Text);
        await box.ShowAsPopupAsync(this);
        this.Close();
        
    }
    async private Task DownloadAsyncWithProgress(string url, string outputPath, ProgressBar progress)
    {
        try
        {
            using var client = new HttpClient();
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = totalBytes != -1;
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
                // ReSharper disable once PossibleLossOfFraction
                double percentage = totalBytesRead * 100 / totalBytes;
                if (!canReportProgress) continue;
                progress.ShowProgressText = true;
                progress.Value = percentage;
                progress.ProgressTextFormat = $"{percentage}%"; }
        }
        catch (Exception e)
        {
            var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
            {
                ContentMessage = Lang.Resources.MSDownloadError + $" {e.Message}",
                ButtonDefinitions = new List<ButtonDefinition>
                {
                    new ButtonDefinition { Name = "Ok" }
                },
                Icon = MsBox.Avalonia.Enums.Icon.Error
            });
            await box.ShowAsPopupAsync(this);
        }
    }
    private async void PathSelectBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var dir = await this.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            Title = Lang.Resources.MSPickerTitle,
            AllowMultiple = false
        });
        if (dir.Count >= 1)
        {
            PathBox.Text = dir[0].Path.AbsolutePath;
        }
    }

    private async void FinishSetupBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!Directory.Exists(PathBox.Text))
        {
            var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
            {
                ContentMessage = Lang.Resources.MSPickerInvalidPath,
                ButtonDefinitions = new List<ButtonDefinition>
                {
                    new ButtonDefinition { Name = "Ok" }
                },
                Icon = MsBox.Avalonia.Enums.Icon.Error
            });
            await box.ShowAsPopupAsync(this);
        }
        else
        {
            if (!File.Exists(Path.Join(PathBox.Text, "basicv2.jar")))
            {
                var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
                {
                    ContentMessage = Lang.Resources.MSPickerNoMS,
                    ButtonDefinitions = new List<ButtonDefinition>
                    {
                        new ButtonDefinition { Name = "Ok" }
                    },
                    Icon = MsBox.Avalonia.Enums.Icon.Error
                });
                await box.ShowAsPopupAsync(this);
            }
            else
            {
                var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
                {
                    ContentMessage = Lang.Resources.MSPickerSuccess,
                    ButtonDefinitions = new List<ButtonDefinition>
                    {
                        new ButtonDefinition { Name = "Ok" }
                    },
                    Icon = MsBox.Avalonia.Enums.Icon.Success
                });
                File.WriteAllText(MainWindow.ConfigFile, PathBox.Text);
                await box.ShowAsPopupAsync(this);
                this.Close();
            }
        }
    }
}