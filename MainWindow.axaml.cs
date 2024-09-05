using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;

namespace MoSpeedUI;

public partial class MainWindow : Window
{
    public static FilePickerFileType Basic { get; } = new(Lang.Resources.FilePickerDef)
    {
        Patterns = new[] { "*.bas", "*.prg"},
        AppleUniformTypeIdentifiers = new[] { "public.text" },
        MimeTypes = new[] { "text/*" }
    };

    public static readonly string ConfigFolder = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"MoSpeedUI");
    public static readonly string ConfigFile = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"MoSpeedUI","path");
    private readonly CompileConfig _compileConfig = new();
    
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
                _compileConfig.Files.AddRange(files.ToList());
                _compileConfig.Files = _compileConfig.Files.Distinct(new FilePathCompare()).ToList();
                RefreshFileList();
            }
            else
            {
                return;
            }
            foreach (var file in _compileConfig.Files)
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
        PlatformSelect.SelectedIndex = 0;
        //MemHoleGrid.RowDefinitions = new RowDefinitions("*,*");
    }

    private void RefreshFileList()
    {
        FileListPanel.Children.Clear();
        foreach (var file in _compileConfig.Files)
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
                        _compileConfig.Files.RemoveAt(FileListPanel.Children.IndexOf(txtBlock));
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
                _compileConfig.Files.Add(item);
                _compileConfig.Files = _compileConfig.Files.Distinct(new FilePathCompare()).ToList();
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

    private void CompileBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!Directory.Exists(ConfigFolder))
        {
            Directory.CreateDirectory(ConfigFolder);
            File.Create(ConfigFile);
            SetupRoutine();
        }
        else
        {
            if (!File.Exists(ConfigFile))
            {
                SetupRoutine();
            }
        }
        CompileBtn.Content = Lang.Resources.CollectingInfo;
        CompileBtn.IsEnabled = false;
        GetTopLevel(CompileBtn).Cursor = new Cursor(StandardCursorType.Wait);
        CollectInfo();
    }

    private void CollectInfo()
    {


        _compileConfig.TargetPlatform = (() =>
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
        _compileConfig.Vc20Conf = (() =>
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
        _compileConfig.C64Conf = (bool)C64ConfBox.IsChecked!;
        if (PlatformSelect.SelectedIndex < 2)
        {
            Console.WriteLine("collecting mem info");
            _compileConfig.ProgramStartAdd = ProgramStartAdd.Text;
            _compileConfig.VariableStartAdd = VarsStartAdd.Text;
            _compileConfig.StringMemEndAdd = StringMemEndAdd.Text;
            _compileConfig.RuntimeStartAdd = RuntimeStartAdd.Text;
            _compileConfig.MemHoles = GenerateMemHolesString();
            Console.WriteLine(_compileConfig.MemHoles);
        }
        Console.WriteLine("collecting compile options");
        _compileConfig.CompactLevel = CompressLvl.SelectedIndex;
        switch (SrcCdePrsc.SelectedIndex)
        {
            case 0:
                _compileConfig.LowerSrc = "false";
                _compileConfig.FlipSrcCase = "false";
                break;
            case 1:
                _compileConfig.LowerSrc = "true";
                _compileConfig.FlipSrcCase = "false";
                break;
            case 2:
                _compileConfig.LowerSrc = "false";
                _compileConfig.FlipSrcCase = "true";
                break;
            default:
                _compileConfig.LowerSrc = "false";
                _compileConfig.FlipSrcCase = "false";
                break;
        }

        if(LoopHandling.SelectedIndex == 0)
        {
            _compileConfig.RemLoops = "true";
        }
        else if(LoopHandling.SelectedIndex == 1)
        {
            _compileConfig.RemLoops = "false";
        }
        else
        {
            _compileConfig.RemLoops = "false";
        }
        if ((bool)LinkerOpt.IsChecked)
        {
            _compileConfig.SplitOutput = "true";
        }
        else
        {
            _compileConfig.SplitOutput = "false";
        }
    }

    private string GenerateMemHolesString()
    {
        int memholesCount = MemHoleGrid.Children.Count(x => x.GetType() == typeof(TextBlock));
        Console.WriteLine(memholesCount);
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
    private void SetupRoutine()
    {
        SetupWindow setupWindow = new();
        setupWindow.ShowDialog(this);
    }
}