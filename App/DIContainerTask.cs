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
using Ninject.Extensions.Factory;
using Ninject.Extensions.Conventions;
using System.ComponentModel;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FractalPainting.App;

public static class DIContainerTask
{
	public static MainWindow CreateMainWindow()
	{
		var container = ConfigureContainer();
		return container.Get<MainWindow>();
	}

	public static StandardKernel ConfigureContainer()
	{
        var container = new StandardKernel();
        container.Bind(
                c => c.FromThisAssembly().SelectAllClasses().InheritedFrom<IUiAction>().BindAllInterfaces()
                );
        container.Bind<MainWindow>().ToSelf().InSingletonScope();
        container.Bind<AppSettings>().ToMethod(context => context.Kernel.Get<SettingsManager>().Load())
            .InSingletonScope();
        container.Bind<IImageController>().ToMethod(context => context.Kernel.Get<AvaloniaImageController>())
            .InSingletonScope();
        container.Bind<ImageSettings>().ToMethod(context => context.Kernel.Get<AppSettings>().ImageSettings)
            .InSingletonScope();
        container.Bind<Palette>().ToSelf().InSingletonScope();
        container.Bind<IDragonPainterFactory>().ToFactory();
        container.Bind<IObjectSerializer>().To<XmlObjectSerializer>()
            .WhenInjectedInto<SettingsManager>().InSingletonScope();
        container.Bind<IBlobStorage>().To<FileBlobStorage>()
            .WhenInjectedInto<SettingsManager>().InSingletonScope();
        container.Bind<SettingsManager>().ToSelf().InSingletonScope();

        return container;
    }
}

public class DragonFractalAction : IUiAction
{
    private readonly IDragonPainterFactory _painterFactory;
    private Func<Window> getMainWindow;

    public MenuCategory Category => MenuCategory.Fractals;
	public string Name => "Дракон";
	public event EventHandler? CanExecuteChanged;

    public DragonFractalAction(Func<Window> getWindow, IDragonPainterFactory painterFactory)
    {
        _painterFactory = painterFactory;
		getMainWindow = getWindow;
    }

    public bool CanExecute(object? parameter)
	{
		return true;
	}

	public async void Execute(object? parameter)
	{
		var dragonSettings = CreateRandomSettings();
		// редактируем настройки:
		await new SettingsForm(dragonSettings).ShowDialog(DIContainerTask.CreateMainWindow());
		// создаём painter с такими настройками
        var painter = _painterFactory.CreateDragonPainter(dragonSettings);
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

    private readonly Lazy<KochPainter> _painter;

    public KochFractalAction(Lazy<KochPainter> painter)
    {
        _painter = painter;
    }

    public bool CanExecute(object? parameter)
	{
		return true;
	}

	public void Execute(object? parameter)
	{
		_painter.Value.Paint();
	}
}

public class DragonPainter
{
	private readonly IImageController imageController;
	private readonly DragonSettings settings;
	private readonly Palette palette;

	public DragonPainter(IImageController imageController, DragonSettings settings, Palette palette)
	{
		this.imageController = imageController;
		this.settings = settings;
		this.palette = palette;
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

public interface IDragonPainterFactory
{
    DragonPainter CreateDragonPainter(DragonSettings settings);
}