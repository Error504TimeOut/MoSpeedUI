using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace MoSpeedUI;

public partial class MainWindow : Window
{
    public static FilePickerFileType Basic { get; } = new(Lang.Resources.FilePickerDef)
    {
        Patterns = new[] { "*.bas", "*.prg"},
        AppleUniformTypeIdentifiers = new[] { "public.text" },
        MimeTypes = new[] { "text/*" }
    };
    private CompileConfig CompileConfig = new();
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
        PlatformSelect.SelectionChanged += (s, _) =>
        {
            var panels = PlatformConf.Children.Where(x => x.GetType() == typeof(Panel)).ToList();
            foreach (var panel in panels)
            {
                panel.IsVisible = false;
            }

            panels[PlatformSelect.SelectedIndex].IsVisible = true;
        };
        PlatformSelect.SelectedIndex = 0;
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
}