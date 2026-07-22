using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using RemnantOverseer.ViewModels;

namespace RemnantOverseer.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        if (Design.IsDesignMode)
        {
            // This can be before or after InitializeComponent.
            var settingsService = new Services.SettingsService();
            var saveDataService = new Services.SaveDataService(settingsService);
            var itemColorService = new Services.ItemColorService(settingsService);
            Design.SetDataContext(this, new SettingsViewModel(settingsService, saveDataService, itemColorService));
        }
        InitializeComponent();
    }
}
