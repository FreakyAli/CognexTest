using Microsoft.Maui.Controls.Compatibility.Hosting;

namespace CognexTest;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCompatibility()
			.ConfigureMauiHandlers(handlers =>
			{
#if __ANDROID__
                    // Register Cognex ScannerControl renderer for Android
                    handlers.AddCompatibilityRenderer(typeof(cmbSDKMaui.ScannerControl),
                                                      typeof(cmbSDKMaui.Android.ScannerControlRenderer));
#endif
#if __IOS__
				// Register Cognex ScannerControl renderer for iOS
				handlers.AddCompatibilityRenderer(typeof(cmbSDKMaui.ScannerControl),
												  typeof(cmbSDKMaui.iOS.ScannerControlRenderer));
#endif
			})
			.ConfigureFonts(fonts =>
			{
				// These fonts come with the default MAUI template; keep or remove as you see fit.
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		return builder.Build();
	}
}