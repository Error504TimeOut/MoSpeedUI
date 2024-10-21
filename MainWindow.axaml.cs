using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Models;
using SkiaSharp;

namespace MoSpeedUI;

public partial class MainWindow : Window
{
    public static FilePickerFileType Basic { get; } = new(Lang.Resources.FilePickerDef)
    {
        Patterns = new[] { "*.bas", "*.prg"},
        AppleUniformTypeIdentifiers = new[] { "public.text" },
        MimeTypes = new[] { "text/*" }
    };

    public static readonly string ConfigFolder = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"MoSpeedUI.config");
    public static readonly string ConfigFile = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"MoSpeedUI.config","config.xml");
    public static readonly CompileConfig CompileConfig = new();
    public static Configuration AppConfiguration = new();
    
    public MainWindow()
    {
        InitializeComponent();
        this.Width = 800;
        this.Height = 600;
        MoSpeedLogo.MaxWidth = (int)(this.Width / 3);
        //ParentPanel.MaxWidth = (int)(this.ClientSize.Width / 2);
        FileListScroller.MaxHeight = (int)(this.ClientSize.Height * 0.4);
        ControlPanel.MaxWidth = (int)(this.ClientSize.Width * 0.75);
        ControlPanel.Width = (int)(this.ClientSize.Width * 0.75);
        this.Resized += (_, e) =>
        {
            FileListScroller.MaxHeight = (int)(this.ClientSize.Height * 0.4);
            MoSpeedLogo.MaxWidth = (int)(this.Width / 3);
            ControlPanel.MaxWidth = (int)(e.ClientSize.Width * 0.75);
            ControlPanel.Width = ControlPanel.MaxWidth;
        };
        this.Opened += async (sender, args) =>
        {
            if (!Directory.Exists(ConfigFolder))
            {
                Directory.CreateDirectory(ConfigFolder);
                await SetupRoutine();
                ReadConfig();
                return;
            }
            if (!File.Exists(ConfigFile))
            {
                await SetupRoutine(true);
                ReadConfig();
            }
            else
            {
                ReadConfig();
            }

            ApplyConfig();
        };
        DragBox.Cursor = new Cursor(StandardCursorType.Hand);
        DragDrop.SetAllowDrop(this, true);
        DragDrop.SetAllowDrop(DragBox,true);
        this.AddHandler(DragDrop.DragEnterEvent, DragEnterHandler);
        this.AddHandler(DragDrop.DragLeaveEvent, DragExitHandler);
        this.AddHandler(DragDrop.DropEvent, DropHandler);
        DragBox.PointerPressed += async (s, e) =>
        {
            var files = await this.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                Title = Lang.Resources.FilePickerTitle,
                AllowMultiple = true,
                FileTypeFilter = new[] {Basic, FilePickerFileTypes.TextPlain, FilePickerFileTypes.All }
                
            });
            if (files.Count >= 1)
            {
                CompileConfig.Files.AddRange(files.ToList());
                CompileConfig.Files = CompileConfig.Files.Distinct(new FilePathCompare()).ToList();
                RefreshFileList();
            }
            else
            {
                return;
            }
            foreach (var file in CompileConfig.Files)
            {
                Console.WriteLine(file.Path);
            }
        };
        PlatformSelect.SelectionChanged += (_, _) =>
        {
            var panels = PlatformConf.Children.Where(x => x.GetType() == typeof(Panel)).ToList();
            foreach (var panel in panels)
            {
                panel.IsVisible = false;
            }
            panels[PlatformSelect.SelectedIndex].IsVisible = true;
            if (PlatformSelect.SelectedIndex >= 2)
            {
                ProgramStartAdd.IsEnabled = false;
                VarsStartAdd.IsEnabled = false;
                StringMemEndAdd.IsEnabled = false;
                RuntimeStartAdd.IsEnabled = false;
                MemHoleGrid.IsEnabled = false;
                MemHoleBtn.IsEnabled = false;
            }
            else
            {
                ProgramStartAdd.IsEnabled = true;
                VarsStartAdd.IsEnabled = true;
                StringMemEndAdd.IsEnabled = true;
                RuntimeStartAdd.IsEnabled = true;
                MemHoleGrid.IsEnabled = true;
                MemHoleBtn.IsEnabled = true;
            }
        };
        AboutLink.Cursor = new Cursor(StandardCursorType.Hand);
        PlatformSelect.SelectedIndex = 0;
    }

    private void ApplyConfig()
    {
        if (AppConfiguration.LogoDecoration)
        {
            DateTime dt = DateTime.Today;
            if (dt.Month == 6)
            {
                MoSpeedLogo.Source =
                    new Bitmap(AssetLoader.Open(new Uri("avares://MoSpeedUI/Assets/Images/mospeed_pride.png")));
            }
            else if (dt.Month == 12)
            {
                MoSpeedLogo.Source =
                    new Bitmap(AssetLoader.Open(new Uri("avares://MoSpeedUI/Assets/Images/mospeed_christmas.png")));
            }
            else if (dt.Month == 10 && Enumerable.Range(20, 31).Contains(dt.Day))
            {
                MoSpeedLogo.Source =
                    new Bitmap(AssetLoader.Open(new Uri("avares://MoSpeedUI/Assets/Images/mospeed_halloween.png")));
            }
        }
    }
    private void ReadConfig()
    {
        try
        {
            Configuration propConfig = new();
            bool redoConfig = false;
            XmlSerializer ser = new XmlSerializer(typeof(Configuration));
            StreamReader r = new StreamReader(ConfigFile);
            AppConfiguration = (Configuration)ser.Deserialize(r)!;
            r.Close();
            foreach(PropertyDescriptor descriptor in TypeDescriptor.GetProperties(AppConfiguration))
            {
                string name = descriptor.Name;
                object? value = descriptor.GetValue(AppConfiguration);
                Console.WriteLine("{0}={1}", name, value);
                if (Equals(value, descriptor.GetValue(propConfig)))
                {
                    Console.WriteLine("{0} not configured, will be saved with default value {1}", name, value);
                    redoConfig = true;
                }
            }
            if (redoConfig)
            {
                SetupWindow.RegenerateConfig(AppConfiguration);
            } 
        }
        catch (Exception e)
        {
            Console.WriteLine(e + "//"+e.Message);
            var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
            {
                ContentMessage = String.Format(Lang.Resources.ConfigReadError, ConfigFolder, e.Message),
                ButtonDefinitions = new List<ButtonDefinition>
                {
                    new ButtonDefinition { Name = "Ok" }
                },
                Icon = MsBox.Avalonia.Enums.Icon.Success
            });
            box.ShowAsPopupAsync(this);
            SetupRoutine(true);
        }
    }
    private void RefreshFileList()
    {
        FileListPanel.Children.Clear();
        foreach (var file in CompileConfig.Files)
        {
            var textBlock = new TextBlock()
                { Text = String.Format("• {0}", file.Name)};
            var border = new Border()
            {
                Background = Brushes.Gainsboro, BorderBrush = Brushes.Black, Padding = new Thickness(10),
                BorderThickness = new Thickness(2), CornerRadius = new CornerRadius(10d), Margin = new Thickness(4d),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            border.Child = textBlock;
            border.PointerPressed += (s, e) =>
            {
                if (e.GetCurrentPoint(s as Control).Properties.IsRightButtonPressed)
                {
                    var txtBlock = s as Border;
                    if (txtBlock != null)
                    {
                        CompileConfig.Files.RemoveAt(FileListPanel.Children.IndexOf(txtBlock));
                        RefreshFileList();
                    }
                }
            };
            FileListPanel.Children.Add(border);
        }
    }
    private void DragEnterHandler(object? sender, DragEventArgs e)
    {
        //Console.WriteLine("DragEnter");
        if (e.Data.Contains(DataFormats.Files))
        {
            DragBox.Background = Brushes.Lime;
            DragBox.BorderBrush = Brushes.Green;
            e.DragEffects = DragDropEffects.Copy;
            DragText.Text = Lang.Resources.DragEnter;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
            DragText.Text = Lang.Resources.DragInvalid;
        }
    }
    private void DragExitHandler(object? sender, DragEventArgs e)
    {   
        //Console.WriteLine("DragExit");
        DragBox.Background = Brushes.Gainsboro;
        DragBox.BorderBrush = Brushes.Black;
        DragText.Text = Lang.Resources.DragBox;
    }
    private void DropHandler(object? sender, DragEventArgs e)
    {
        //Console.WriteLine("Drop");
        DragBox.Background = Brushes.Gainsboro;
        DragBox.BorderBrush = Brushes.Black;
        DragText.Text = Lang.Resources.DragBox;
        var files = e.Data.GetFiles();
        foreach (var file in files)
        {
            if (file is IStorageFile item)
            {
                CompileConfig.Files.Add(item);
                CompileConfig.Files = CompileConfig.Files.Distinct(new FilePathCompare()).ToList();
                RefreshFileList();
            }
        }
    }

    private void MemHoleBtn_OnClick(object? sender, RoutedEventArgs e)
    { 
        MemHoleGrid.RowDefinitions.Add(new RowDefinition(){Height = GridLength.Star});
        Console.WriteLine(MemHoleGrid.RowDefinitions.Count-1);
        var startBox = (new TextBox(){Name = "MemHoleStart"+(MemHoleGrid.RowDefinitions.Count-1), Watermark = Lang.Resources.DecimalOrHex, Margin = new Thickness(0,4,0,0)});
        var endBox = (new TextBox(){Name = "MemHoleEnd"+(MemHoleGrid.RowDefinitions.Count-1), Watermark = Lang.Resources.DecimalOrHex, Margin = new Thickness(0,4,0,0)});
        var textBox = (new TextBlock() { Text = Lang.Resources.To, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4,4,4,0)});
        Grid.SetRow(startBox, MemHoleGrid.RowDefinitions.Count - 1);
        Grid.SetRow(endBox, MemHoleGrid.RowDefinitions.Count - 1);
        Grid.SetRow(textBox, MemHoleGrid.RowDefinitions.Count - 1);
        Grid.SetColumn(startBox, 0);
        Grid.SetColumn(textBox, 1);
        Grid.SetColumn(endBox, 2);
        startBox.Name = "MemHoleStart"+(MemHoleGrid.RowDefinitions.Count - 1);
        endBox.Name = "MemHoleEnd"+(MemHoleGrid.RowDefinitions.Count - 1);
        MemHoleGrid.Children.AddRange(new List<Control> { startBox,endBox,textBox });
    }

    private async void CompileBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!Directory.Exists(ConfigFolder))
        {
            Directory.CreateDirectory(ConfigFolder);
            SetupRoutine();
            return;
        }
        else
        {
            if (!File.Exists(ConfigFile))
            {
                SetupRoutine();
                return;
            }
        }
        CompileBtn.Content = Lang.Resources.CollectingInfo;
        CompileBtn.IsEnabled = false;
        this.Cursor = new Cursor(StandardCursorType.Wait);
        CollectInfo();
        /*if (await CheckForJava())
        {
            Console.WriteLine("Java found.");
            CollectInfo();
        }*/

        if (CompileConfig.Files.Count == 0)
        {
            var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
            {
                ContentMessage = String.Format(Lang.Resources.NoFiles),
                ButtonDefinitions = new List<ButtonDefinition>
                {
                    new() { Name = "Ok"},
                },
                Icon = MsBox.Avalonia.Enums.Icon.Warning
            });
            CompileBtn.Content = Lang.Resources.Compile;
            await box.ShowAsPopupAsync(this);
            return;
        }
        CompileBtn.Content = Lang.Resources.Compiling;
        foreach (var file in CompileConfig.Files)
        {
            CompileConfig.CurrentFile = file;
            CompilerWindow cmpW = new();
            await cmpW.ShowDialog(this);
        }
        this.Cursor = new Cursor(StandardCursorType.Arrow);
        CompileBtn.Content = Lang.Resources.Compile;
        CompileBtn.IsEnabled = true;
    }

    /*private async Task<bool> CheckForJava()
    {
        if (AppConfiguration.SkipJavaCheck)
        {
            return true;
        }
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
                return true;
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
            return false;
        }
        catch (WrongJavaVersion e)
        {
            var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
            {
                ContentMessage = String.Format(Lang.Resources.JavaWrongVersion,e.Message),
                ButtonDefinitions = new List<ButtonDefinition>
                {
                    new() { Name = Lang.Resources.Ignore},
                    new() { Name = Lang.Resources.Abort, IsCancel = true },
                    new() { Name = Lang.Resources.Accept }
                },
                Icon = MsBox.Avalonia.Enums.Icon.Warning
            });
            var res = await box.ShowAsPopupAsync(this);
            if (res == Lang.Resources.Abort)
            {
                return false;
            }
            if (res == Lang.Resources.Ignore)
            {
                AppConfiguration.SkipJavaCheck = true;
                SetupWindow.RegenerateConfig(AppConfiguration);
                return true;
            }
            return true;
        }
        catch (Exception e)
        {
            var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
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
            return false;
        }
    }*/
    private void CollectInfo()
    { 
        CompileConfig.TargetPlatform = (() =>
        {
            switch (PlatformSelect.SelectionBoxItem.ToString())
            {
                case "C64":
                    return "c64";
                case "VIC 20/VC 20":
                    return "vc20";
                case "JavaScript":
                    return "js";
                case "Powershell":
                    return "ps";
                case "Python":
                    return "py";
                default:
                    return "c64";
            }
        });
        CompileConfig.Vc20Conf = (() =>
        {
            switch (Vc20ConfBox.SelectedIndex)
            {
                case 0:
                    return 0;
                case 1:
                    return 3;
                case 2:
                    return 8;
                default:
                    return 8;
            }
        });
        CompileConfig.C64Conf = (bool)C64ConfBox.IsChecked!;
        if (PlatformSelect.SelectedIndex < 2)
        {
            Console.WriteLine("collecting mem info");
            CompileConfig.ProgramStartAdd = ProgramStartAdd.Text;
            CompileConfig.VariableStartAdd = VarsStartAdd.Text;
            CompileConfig.StringMemEndAdd = StringMemEndAdd.Text;
            CompileConfig.RuntimeStartAdd = RuntimeStartAdd.Text;
            CompileConfig.MemHoles = GenerateMemHolesString();
            Console.WriteLine(CompileConfig.MemHoles);
        }
        Console.WriteLine("collecting compile options");
        CompileConfig.CompactLevel = CompressLvl.SelectedIndex;
        switch (SrcCdePrsc.SelectedIndex)
        {
            case 0:
                CompileConfig.LowerSrc = "false";
                CompileConfig.FlipSrcCase = "false";
                break;
            case 1:
                CompileConfig.LowerSrc = "true";
                CompileConfig.FlipSrcCase = "false";
                break;
            case 2:
                CompileConfig.LowerSrc = "false";
                CompileConfig.FlipSrcCase = "true";
                break;
            default:
                CompileConfig.LowerSrc = "false";
                CompileConfig.FlipSrcCase = "false";
                break;
        }

        if(LoopHandling.SelectedIndex == 0)
        {
            CompileConfig.RemLoops = "true";
        }
        else if(LoopHandling.SelectedIndex == 1)
        {
            CompileConfig.RemLoops = "false";
        }
        else
        {
            CompileConfig.RemLoops = "false";
        }
        if ((bool)LinkerOpt.IsChecked)
        {
            CompileConfig.SplitOutput = "true";
        }
        else
        {
            CompileConfig.SplitOutput = "false";
        }
    }

    private string GenerateMemHolesString()
    {
        int memholesCount = MemHoleGrid.Children.Count(x => x.GetType() == typeof(TextBlock));
        List<string> memholes = [];
        for (int i = 0; i <= memholesCount -1; i++)
        {
            var startBox = (TextBox)MemHoleGrid.Children.Where(x => x.Name == $"MemHoleStart{i}").ToList()[0];
            var endBox = (TextBox)MemHoleGrid.Children.Where(x => x.Name == $"MemHoleEnd{i}").ToList()[0];
            // TODO: Make more efficient, line under this does not work. Current solution inefficient
            //memholes.Add($"{MemHoleGrid.FindControl<TextBox>($"MemHoleStart{i}")?.Text}-{MemHoleGrid.FindControl<TextBox>($"MemHoleEnd{i}")?.Text}");
            // ReSharper disable once AccessToModifiedClosure
            if (!String.IsNullOrWhiteSpace(startBox.Text) && !String.IsNullOrWhiteSpace(endBox.Text))
            {
                memholes.Add($"{startBox.Text}-{endBox.Text}");
            }
        }
        return String.Join(",",memholes);
    }
    private async Task SetupRoutine(bool restart = false)
    {
        SetupWindow setupWindow = new(restart);
        await setupWindow.ShowDialog(this);
    }

    private void AdvSettings_OnClick(object? sender, RoutedEventArgs e)
    {
        AdvancedSettings advSet = new();
        advSet.ShowDialog(this);
    }

    private void AboutLink_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        AboutWindow aW = new();
        aW.ShowDialog(this);
    }

    private void SettingsLink_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
    }
}