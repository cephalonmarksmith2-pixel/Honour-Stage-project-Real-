using DiaryApp.Models;               // so NoteStorage can be found
using Microsoft.Extensions.Logging;

namespace DiaryApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();   // starts the builder (this line must come FIRST)

		builder
			.UseMauiApp<App>()                   // tells the builder which class is the app's root
			.ConfigureFonts(fonts =>             // registers font files and gives them nicknames
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});                                  // the chain ends at this semicolon

		// Our own registrations: separate statements, after the chain, before Build()
		builder.Services.AddSingleton(new NoteStorage(Path.Combine(FileSystem.AppDataDirectory, "Notes")));
		builder.Services.AddTransient<Notes>();

#if DEBUG
		builder.Logging.AddDebug();              // in debug builds only, send log messages to the debug output
#endif

		return builder.Build();                  // builds the finished app; no more registering after this
	}
}