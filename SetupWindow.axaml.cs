using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Resources;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
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
    private Configuration _config = new Configuration();
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
    
    public static void RegenerateConfig(Configuration config)
    {
        XmlSerializer ser = new XmlSerializer(typeof(Configuration));
        StreamWriter w = new StreamWriter(MainWindow.ConfigFile);
        ser.Serialize(w,config);
        w.Close();
    }
    public void GenerateConfig()
    {
        XmlSerializer ser = new XmlSerializer(typeof(Configuration));
        if (File.Exists(MainWindow.ConfigFile))
        {
            StreamReader r = new StreamReader(MainWindow.ConfigFile);
            _config = (Configuration)ser.Deserialize(r)!;
            r.Close();
        }
        _config.MoSpeedPath = PathBox.Text!;
        StreamWriter w = new StreamWriter(MainWindow.ConfigFile);
        ser.Serialize(w,_config);
        w.Close();
    }
    public void Prepare()
    {
        this.Closing += (sender, args) =>
        {
            GenerateConfig();
        };
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
        FindJava();
    }

    private async void FindJava()
    {
        ParentPanel.IsEnabled = false;
        DwnldBar.IsVisible = true;
        DwnldBar.ShowProgressText = true;
        DwnldBar.ProgressTextFormat = Lang.Resources.FindingJava;
        bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        if (!isWindows)
        {
            _config.JavaPath = "java";
            ParentPanel.IsEnabled = true;
            DwnldBar.IsVisible = false;
        }
        else
        { 
            string stdout = "";
            Process process = new Process();
            process.StartInfo.FileName = "java";
            process.StartInfo.Arguments = "-fullversion";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            try
            {
                process.Start();
                await process.WaitForExitAsync();
                stdout = await process.StandardError.ReadToEndAsync();
                int javaver = int.Parse(stdout.Split('\"')[1].Split('.')[0]);
                process.Dispose();
                if (javaver >= 11)
                {
                    _config.JavaPath = "java";
                    return;
                }
                throw new WrongJavaVersion(String.Format(Lang.Resources.WrongJavaVersionException, javaver));
            }
            catch (System.ComponentModel.Win32Exception e)
            {
                var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
                {
                    ContentMessage = Lang.Resources.JavaNotFoundError + $" {e.Message}",
                    ButtonDefinitions = new List<ButtonDefinition>
                    {
                        new() { Name = "Ok" }
                    },
                    Icon = MsBox.Avalonia.Enums.Icon.Error
                });
                await box.ShowAsPopupAsync(this);
                return;
            }
            catch (WrongJavaVersion e)
            {
                var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
                {
                    ContentMessage = String.Format(Lang.Resources.JavaWrongVersion,e.Message,stdout),
                    ButtonDefinitions = new List<ButtonDefinition>
                    {
                        new() { Name = Lang.Resources.Abort, IsCancel = true },
                        new() { Name = Lang.Resources.Accept }
                    },
                    Icon = MsBox.Avalonia.Enums.Icon.Warning
                });
                var res = await box.ShowAsPopupAsync(this);
                if (res == Lang.Resources.Abort)
                {
                    return;
                }
                _config.JavaPath = "java";
                ParentPanel.IsEnabled = true;
            }
            catch (Exception e)
            {
                /*var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
                {
                    ContentMessage = String.Format(Lang.Resources.JavaGenericError,e.Message,stdout),
                    ButtonDefinitions = new List<ButtonDefinition>
                    {
                        new() { Name = "Ok" },
                        new() { Name = Lang.Resources.Ignore }
                    },
                    Icon = MsBox.Avalonia.Enums.Icon.Warning
                });
                var res = await box.ShowAsPopupAsync(this);
                if (res == Lang.Resources.Ignore)
                {
                    AppConfiguration.SkipJavaCheck = true;
                    SetupWindow.RegenerateConfig(AppConfiguration);
                    return true;
                }
                return false;*/
                process.StartInfo.FileName = "where";
                process.StartInfo.Arguments = "java.exe";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                try
                {
                    process.Start();
                    await process.WaitForExitAsync();
                    stdout = await process.StandardOutput.ReadToEndAsync();
                    File.WriteAllText(Path.Join(MainWindow.ConfigFolder,new Guid().ToString()+"stdout-where.txt"), stdout);
                    List<string> javas = stdout.Split(Environment.NewLine.ToCharArray()).ToList();
                    foreach (var java in javas)
                    {
                        Process jcheck = new Process();
                        jcheck.StartInfo.FileName = "java";
                        jcheck.StartInfo.Arguments = "-fullversion";
                        jcheck.StartInfo.UseShellExecute = false;
                        jcheck.StartInfo.RedirectStandardError = true;
                        jcheck.Start();
                        await jcheck.WaitForExitAsync();
                        stdout = await process.StandardError.ReadToEndAsync();
                        File.WriteAllText(Path.Join(MainWindow.ConfigFolder,new Guid().ToString()+"stdout-java.txt"),java+" returned: "+stdout);
                        int javaver = int.Parse(stdout.Split('\"')[1].Split('.')[0]);
                        jcheck.Dispose();
                        if (javaver >= 11)
                        {
                            ParentPanel.IsEnabled = true;
                            _config.JavaPath = java;
                            break;
                        }
                        var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
                        {
                            ContentMessage = Lang.Resources.JavaFoundWrongVerWin + $" ({MainWindow.ConfigFile}), (Java-Execs: {string.Join(",",javas)})",
                            ButtonDefinitions = new List<ButtonDefinition>
                            {
                                new() {Name = "Ok"},
                            },
                            Icon = MsBox.Avalonia.Enums.Icon.Error
                        });
                        await box.ShowAsPopupAsync(this);
                        ParentPanel.IsEnabled = false;
                    }
                }
                catch (Exception ex)
                {
                    var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
                    {
                        ContentMessage = String.Format(Lang.Resources.WindowsJavaPanic,stdout,ex.Message),
                        ButtonDefinitions = new List<ButtonDefinition>
                        {
                            new() { Name = "Ok" },
                        },
                        Icon = MsBox.Avalonia.Enums.Icon.Warning
                    });
                    await box.ShowAsPopupAsync(this);
                    ParentPanel.IsEnabled = false;
                }
            }
        }
    }
    async private void DwnldBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        PathBox.IsReadOnly = true;
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
            ContentMessage = Lang.Resources.MSDownloadSuccess + $" {Path.Join(MainWindow.ConfigFolder,"mospeed")}",
            ButtonDefinitions = new List<ButtonDefinition>
            {
                new ButtonDefinition { Name = "Ok" }
            },
            Icon = MsBox.Avalonia.Enums.Icon.Success,
        });
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
                await box.ShowAsPopupAsync(this);
                this.Close();
            }
        }
    }
}