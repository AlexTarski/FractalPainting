using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Markup.Xaml;
using FractalPainting.Infrastructure.Common;
using FractalPainting.Infrastructure.UiActions;
using FractalPainting.UI;
using ImageController = FractalPainting.Infrastructure.Common.AvaloniaImageController;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace FractalPainting.App;

public class ImageSettingsAction : IUiAction
{
    public MenuCategory Category => MenuCategory.Settings;
    public event EventHandler? CanExecuteChanged;
    public string Name => "Изображение...";

    private ImageSettings imageSettings;
    private IImageController imageController;
    private Func<Window> getMainWindow;

    public ImageSettingsAction(ImageSettings imageSettings, IImageController imageController, Func<Window> getMainWindow)
    {
        this.imageSettings = imageSettings;
        this.imageController = imageController;
        this.getMainWindow = getMainWindow;
    }

    public bool CanExecute(object? parameter)
    {
        return true;
    }

    public async void Execute(object? parameter)
    {
        await new SettingsForm(imageSettings).ShowDialog(getMainWindow());
        imageController.RecreateImage(imageSettings);
    }
}

public class SaveImageAction : IUiAction
{
    public MenuCategory Category => MenuCategory.File;
    public event EventHandler? CanExecuteChanged;
    public string Name => "Сохранить...";

    private Func<Window> getMainWindow;
    private IImageController imageController;

    public SaveImageAction(Func<Window> getMainWindow, IImageController imageController)
    {
        this.getMainWindow = getMainWindow;
        this.imageController = imageController;
    }

    public bool CanExecute(object? parameter)
    {
        return true;
    }

    public async void Execute(object? settings)
    {
        var topLevel = TopLevel.GetTopLevel(getMainWindow());
        if (topLevel is null) return;

        var options = new FilePickerSaveOptions
        {
            Title = "Сохранить изображение",
            SuggestedFileName = "image.bmp",
        };
        var saveFile = await topLevel.StorageProvider.SaveFilePickerAsync(options);
        if (saveFile is not null)
            imageController.SaveImage(saveFile.Path.AbsolutePath);
    }
}

public class PaletteSettingsAction : IUiAction
{
    public MenuCategory Category => MenuCategory.Settings;
    public event EventHandler? CanExecuteChanged;
    public string Name => "Палитра...";

    private Func<Window> getMainWindow;
    private Palette palette;

    public PaletteSettingsAction(Func<Window> getMainWindow, Palette palette)
    {
        this.getMainWindow = getMainWindow;
        this.palette = palette;
    }

    public bool CanExecute(object? parameter)
    {
        return true;
    }

    public async void Execute(object? parameter)
    {
        await new SettingsForm(palette).ShowDialog(getMainWindow());
    }
}

public partial class MainWindow : Window
{
    private const int MenuSize = 32;
    // Контролы из авалонии
    private Menu? menu;
    private ImageControl? image;

    public MainWindow() : this(
            new IUiAction[]
            {
                new SaveImageAction(Services.GetMainWindow, Services.GetImageController()),
                new DragonFractalAction(),
                new KochFractalAction(),
                new ImageSettingsAction(Services.GetImageSettings(), Services.GetImageController(), Services.GetMainWindow),
                new PaletteSettingsAction(Services.GetMainWindow, Services.GetPalette())
            }, Services.GetImageController())
    {
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        menu = this.FindNameScope()?.Find<Menu>("Menu");
        image = this.FindNameScope()?.Find<ImageControl>("Image");
    }

    public MainWindow(IUiAction[] actions, ImageController imageController)
    {
        InitializeComponent();
        var imageSettings = CreateSettingsManager().Load().ImageSettings;
        ClientSize = new Size(imageSettings.Width, imageSettings.Height + MenuSize);
        menu.ItemsSource = actions.ToMenuItems();
        Title = "Fractal Painter";
        Services.SetMainWindow(this);

        imageController.SetControl(image);
        imageController.RecreateImage(imageSettings);
    }

    private static SettingsManager CreateSettingsManager()
    {
        return new SettingsManager(new XmlObjectSerializer(), new FileBlobStorage());
    }
}