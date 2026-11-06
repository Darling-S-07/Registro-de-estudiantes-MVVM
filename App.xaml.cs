using Microsoft.Extensions.DependencyInjection;
using Registro_de_estudiantes_MVVM.Views;

namespace Registro_de_estudiantes_MVVM
{
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;

        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;

            string? temaEnv = Environment.GetEnvironmentVariable("REGISTRO_TEMA");
            if (temaEnv == "Dark")
            {
                UserAppTheme = AppTheme.Dark;
            }
            else if (temaEnv == "Light")
            {
                UserAppTheme = AppTheme.Light;
            }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // La página y su ViewModel se resuelven desde el contenedor de
            // inyección de dependencias registrado en MauiProgram.cs.
            var page = _serviceProvider.GetRequiredService<RegistroPage>();

            var navegacion = new NavigationPage(page);
            navegacion.SetAppThemeColor(NavigationPage.BarBackgroundColorProperty,
                (Color)Resources["Primary"],
                (Color)Resources["SurfaceDark"]);
            navegacion.SetAppThemeColor(NavigationPage.BarTextColorProperty,
                Colors.White,
                (Color)Resources["SecondaryDarkText"]);

            return new Window(navegacion);
        }
    }
}
