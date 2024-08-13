using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace MoSpeedUI;

public partial class MainWindow : Window
{
    public static FilePickerFileType Basic { get; } = new("BASIC Files")
    {
        Patterns = new[] { "*.bas"},
        AppleUniformTypeIdentifiers = new[] { "public.text" },
        MimeTypes = new[] { "text/*" }
    };
    private CompileConfig CompileConfig = new CompileConfig();
    public MainWindow()
    {
        InitializeComponent();
        MoSpeedLogo.MaxWidth = (int)(this.ClientSize.Width / 3);
        //ParentPanel.MaxWidth = (int)(this.ClientSize.Width / 2);
        ControlPanel.MinWidth = (int)(this.ClientSize.Width * 0.75);
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
                FileTypeFilter = new[] {Basic, FilePickerFileTypes.TextPlain}
                
            });
            if (files.Count >= 1)
            {
                CompileConfig.Files = files.ToList();
            }
        };
    }
    
    private void DragEnterHandler(object? sender, DragEventArgs e)
    {
        Console.WriteLine("DragEnter");
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
        Console.WriteLine("DragExit");
        DragBox.Background = Brushes.Gainsboro;
        DragBox.BorderBrush = Brushes.Black;
        DragText.Text = Lang.Resources.DragBox;
    }
    private void DropHandler(object? sender, DragEventArgs e)
    {
        Console.WriteLine("Drop");
        DragBox.Background = Brushes.Gainsboro;
        DragBox.BorderBrush = Brushes.Black;
        DragText.Text = Lang.Resources.DragBox;
        Environment.Exit(0);
    }
}