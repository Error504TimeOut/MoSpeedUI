using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.OpenGL;

namespace MoSpeedUI;

public partial class SettingsDialog : Window
{
    public SettingsDialog()
    {
        InitializeComponent();
        this.SizeToContent = SizeToContent.WidthAndHeight;
        this.DataContext = Shared.AppConfiguration;
        MSPath.Bind(TextBox.TextProperty, new Binding(nameof(Shared.AppConfiguration.MoSpeedPath)) { Mode = BindingMode.TwoWay });
        JavaPath.Bind(TextBox.TextProperty, new Binding(nameof(Shared.AppConfiguration.JavaPath)) { Mode = BindingMode.TwoWay });
        LogoDec.Bind(CheckBox.IsCheckedProperty, new Binding(nameof(Shared.AppConfiguration.LogoDecoration)) { Mode = BindingMode.TwoWay });
        this.Closing += (_, _) =>
        {
            SetupWindow.RegenerateConfig(Shared.AppConfiguration);
        };
    }
}