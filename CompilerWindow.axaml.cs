using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace MoSpeedUI;

public partial class CompilerWindow : Window
{
    public CompilerWindow()
    {
        InitializeComponent();
        this.Width = 800;
        this.Height = 600;
        //CompileOut.Height = ClientSize.Height - 100;
        /*this.Resized += (_, e) =>
        {
            CompileOut.Height = e.ClientSize.Height / 2;
        };*/
        IStorageFile? fileout = Task.Run(SelectOutput).GetAwaiter().GetResult();
        if (fileout == null)
        {
            this.Close();
            return;
        }
        MainWindow.CompileConfig.OutputFile = fileout;
        MainWindow.CompileConfig.ArgumentList = BuildArguments();
        ArgumentList.Text = String.Format(Lang.Resources.UsingArguments, MainWindow.CompileConfig.ArgumentList);
        Compile();
        MainWindow.CompileConfig.OutputFile = null;
    }

    private async void Compile()
    {
        bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        Process mospeed = new();
        mospeed.StartInfo.WorkingDirectory = MainWindow.CompileConfig.MoSpeedPath;
        mospeed.StartInfo.FileName = "java";
        mospeed.StartInfo.UseShellExecute = false;
        mospeed.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        if (isWindows)
        {
            mospeed.StartInfo.Arguments =
                $"-cp basicv2.jar;dist/basicv2.jar com.sixtyfour.cbmnative.shell.MoSpeedCL {MainWindow.CompileConfig.ArgumentList}";
        }
        else
        {
            mospeed.StartInfo.Arguments =
                $"-cp basicv2.jar:dist/basicv2.jar com.sixtyfour.cbmnative.shell.MoSpeedCL {MainWindow.CompileConfig.ArgumentList}";
        }
        mospeed.StartInfo.RedirectStandardOutput = true;
        mospeed.EnableRaisingEvents = true;
        mospeed.OutputDataReceived += (s, e) => AppendText(e.Data);
        mospeed.Start();
        mospeed.BeginOutputReadLine();
        await mospeed.WaitForExitAsync();
        ClsBtn.IsEnabled = true;
    }
    private void AppendText(string? text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            Dispatcher.UIThread.Post(() =>
            {
                CompileOut.Text += text + Environment.NewLine;
                CompileOut.CaretIndex = int.MaxValue;
            });
        }
    }
    private async Task<IStorageFile?> SelectOutput()
    {
        int count = 1;
        IStorageFile? file = null;
        while (file == null)
        {
            if (count >= 4)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    Close();
                });
                break;
            }
            file = await this.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
            {
                Title = String.Format(Lang.Resources.SaveFilePickerTitle, MainWindow.CompileConfig.CurrentFile.Name,
                    (4 - count)),
                SuggestedFileName = $"++{MainWindow.CompileConfig.CurrentFile.Name}.prg",
                DefaultExtension = "prg",
            });
            count++;
        }
        return file;
    }
    private string BuildArguments()
    {
        CompileConfig cc = MainWindow.CompileConfig;
        return $"/target=\"{Uri.UnescapeDataString(cc.OutputFile?.Path.AbsolutePath)}\" /platform={cc.TargetPlatform?.Invoke()} " +
               $"/memconfig={cc.Vc20Conf.Invoke()} /bigram={cc.C64Conf.ToString().ToLower()} /progstart={(string.IsNullOrWhiteSpace(cc.ProgramStartAdd) ? "2072" : cc.ProgramStartAdd)} " +
               (string.IsNullOrWhiteSpace(cc.VariableStartAdd) ? "" : $" /varstart={cc.VariableStartAdd}") +
               (string.IsNullOrWhiteSpace(cc.StringMemEndAdd) ? "" : $" /varend={cc.StringMemEndAdd}") + 
               (string.IsNullOrWhiteSpace(cc.RuntimeStartAdd) ? "" : $" /runtimestart={cc.RuntimeStartAdd}") +
               (string.IsNullOrWhiteSpace(cc.MemHoles) ? "" : $" /memhole={cc.MemHoles}") +
               $" /compactlevel={cc.CompactLevel} /tolower={cc.LowerSrc} /flipcase={cc.FlipSrcCase} /loopopt={cc.RemLoops} " +
               $"/multipart={cc.SplitOutput}" + (string.IsNullOrWhiteSpace(cc.ExtendedArguments) ? "" : $" {cc.ExtendedArguments}") + 
               $" \"{Uri.UnescapeDataString(cc.CurrentFile.Path.AbsolutePath)}\"";
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }
}