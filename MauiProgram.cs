using Microsoft.Extensions.Logging;
using Registro_de_estudiantes_MVVM.Services;
using Registro_de_estudiantes_MVVM.ViewModels;
using Registro_de_estudiantes_MVVM.Views;

namespace Registro_de_estudiantes_MVVM
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // ===================================================================
            // Registro de servicios, ViewModels y Views (arquitectura MVVM)
            // ===================================================================

            // Servicio de datos: Singleton para compartir una única conexión SQLite.
            builder.Services.AddSingleton(_ =>
                new SQLiteDatabase(Path.Combine(FileSystem.AppDataDirectory, "registro_estudiantes.db3")));

            // ViewModel y Page: Transient, una instancia nueva por cada navegación.
            builder.Services.AddTransient<RegistroViewModel>();
            builder.Services.AddTransient<RegistroPage>();

            return builder.Build();
        }
    }
}
