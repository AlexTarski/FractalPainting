using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Markup.Xaml;
using FractalPainting.Infrastructure.Common;
using FractalPainting.Infrastructure.UiActions;
using FractalPainting.UI;
using ImageController = FractalPainting.Infrastructure.Common.AvaloniaImageController;

namespace FractalPainting.App;

public class ImageSettingsAction : IUiAction
{
	public MenuCategory Category => MenuCategory.Settings;
	public event EventHandler? CanExecuteChanged;
	public string Name => "Изображение...";
	
	public bool CanExecute(object? parameter)
	{
		return true;
	}

	public async void Execute(object? parameter)
	{
		var imageSettings = Services.GetImageSettings();
		await new SettingsForm(imageSettings).ShowDialog(Services.GetMainWindow());
		Services.GetImageController().RecreateImage(imageSettings);
	}
}

public class SaveImageAction : IUiAction
{
	public MenuCategory Category => MenuCategory.File;
	public event EventHandler? CanExecuteChanged;
	public string Name => "Сохранить...";

	public bool CanExecute(object? parameter)
	{
		return true;
	}

	public async void Execute(object? settings)
	{
		var topLevel = TopLevel.GetTopLevel(Services.GetMainWindow());
		if (topLevel is null) return;

		var options = new FilePickerSaveOptions
		{
			Title = "Сохранить изображение",
			SuggestedFileName = "image.bmp",
		};
		var saveFile = await topLevel.StorageProvider.SaveFilePickerAsync(options);
		if (saveFile is not null)
			Services.GetImageController().SaveImage(saveFile.Path.AbsolutePath);
	}
}

public class PaletteSettingsAction : IUiAction
{
	public MenuCategory Category => MenuCategory.Settings;
	public event EventHandler? CanExecuteChanged;
	public string Name => "Палитра...";

	public bool CanExecute(object? parameter)
	{
		return true;
	}

	public async void Execute(object? parameter)
	{
		await new SettingsForm(Services.GetPalette()).ShowDialog(Services.GetMainWindow());
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
				new SaveImageAction(),
				new DragonFractalAction(),
				new KochFractalAction(),
				new ImageSettingsAction(),
				new PaletteSettingsAction()
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