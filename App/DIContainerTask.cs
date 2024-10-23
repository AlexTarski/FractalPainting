using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FractalPainting.App.Fractals;
using FractalPainting.Infrastructure.Common;
using FractalPainting.Infrastructure.UiActions;
using FractalPainting.UI;
using Ninject;

namespace FractalPainting.App;

public static class DIContainerTask
{
	public static MainWindow CreateMainWindow()
	{
		// Example: ConfigureContainer()...
		return new MainWindow();
	}

	public static StandardKernel ConfigureContainer()
	{
		var container = new StandardKernel();

		// Example
		// container.Bind<TService>().To<TImplementation>();

		return container;
	}
}

public static class Services
{
	private static readonly SettingsManager settingsManager;
	private static readonly AvaloniaImageController ImageController;
	private static readonly Palette palette;
	private static readonly AppSettings appSettings;
	private static Window MainWindow { get; set; }

	static Services()
	{
		palette = new Palette();
		ImageController = new AvaloniaImageController();
		settingsManager = new SettingsManager(new XmlObjectSerializer(), new FileBlobStorage());
		appSettings = settingsManager.Load();
	}

	public static SettingsManager GetSettingsManager()
	{
		return settingsManager;
	}

	public static AvaloniaImageController GetImageController()
	{
		return ImageController;
	}

	public static Palette GetPalette()
	{
		return palette;
	}

	public static ImageSettings GetImageSettings()
	{
		return appSettings.ImageSettings;
	}

	public static AppSettings GetAppSettings()
	{
		return appSettings;
	}
	
	public static Window GetMainWindow()
	{
		return MainWindow;
	}

	public static void SetMainWindow(Window window)
	{
		MainWindow = window;
	}
}

public class DragonFractalAction : IUiAction
{
	public MenuCategory Category => MenuCategory.Fractals;
	public string Name => "Дракон";
	public event EventHandler? CanExecuteChanged;

	public DragonFractalAction()
	{ }

	public bool CanExecute(object? parameter)
	{
		return true;
	}

	public async void Execute(object? parameter)
	{
		var dragonSettings = CreateRandomSettings();
		// редактируем настройки:
		await new SettingsForm(dragonSettings).ShowDialog(Services.GetMainWindow());
		// создаём painter с такими настройками
		var painter = new DragonPainter(Services.GetImageController(), dragonSettings);
		painter.Paint();
	}

	private static DragonSettings CreateRandomSettings()
	{
		return new DragonSettingsGenerator(new Random()).Generate();
	}
}

public class KochFractalAction : IUiAction
{
	public MenuCategory Category => MenuCategory.Fractals;
	public string Name => "Кривая Коха";
	public event EventHandler? CanExecuteChanged;

	public KochFractalAction()
	{ }

	public bool CanExecute(object? parameter)
	{
		return true;
	}

	public void Execute(object? parameter)
	{
		var painter = new KochPainter(Services.GetImageController(), Services.GetPalette());
		painter.Paint();
	}
}

public class DragonPainter
{
	private readonly IImageController imageController;
	private readonly DragonSettings settings;

	public DragonPainter(IImageController imageController, DragonSettings settings)
	{
		this.imageController = imageController;
		this.settings = settings;
	}

	public void Paint()
	{
		using var ctx = imageController.CreateDrawingContext();
		var imageSize = imageController.GetImageSize();
		var size = Math.Min(imageSize.Width, imageSize.Height) / 2.1f;

		var backgroundBrush = new SolidColorBrush(Colors.Black);
		var primaryBrush = new SolidColorBrush(Colors.Yellow);
		
		ctx.FillRectangle(backgroundBrush,
			new Rect(0, 0, imageSize.Width, imageSize.Height));

		var r = new Random();
		var cosa = (float)Math.Cos(settings.Angle1);
		var sina = (float)Math.Sin(settings.Angle1);
		var cosb = (float)Math.Cos(settings.Angle2);
		var sinb = (float)Math.Sin(settings.Angle2);
		var shiftX = settings.ShiftX * size * 0.8f;
		var shiftY = settings.ShiftY * size * 0.8f;
		var scale = settings.Scale;
		var p = new Point(0, 0);
		foreach (var i in Enumerable.Range(0, settings.IterationsCount))
		{
			ctx.FillRectangle(primaryBrush,
				new Rect(imageSize.Width / 3f + p.X, imageSize.Height / 2f + p.Y, 1, 1));

			if (r.Next(0, 2) == 0)
				p = new Point(scale * (p.X * cosa - p.Y * sina), scale * (p.X * sina + p.Y * cosa));
			else
				p = new Point(scale * (p.X * cosb - p.Y * sinb) + (float)shiftX,
					scale * (p.X * sinb + p.Y * cosb) + (float)shiftY);
		}

		imageController.UpdateUi();
	}
}