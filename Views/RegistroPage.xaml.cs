using Registro_de_estudiantes_MVVM.ViewModels;

namespace Registro_de_estudiantes_MVVM.Views;

/// <summary>
/// Code-behind de la Vista. Sin lógica de negocio (Req. 7): solo conecta el
/// BindingContext (inyectado por el contenedor de DI de MauiProgram) y
/// dispara la carga inicial de datos al aparecer la página.
/// </summary>
public partial class RegistroPage : ContentPage
{
    private readonly RegistroViewModel _viewModel;

    public RegistroPage(RegistroViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.CargarEstudiantesCommand.ExecuteAsync(null);
    }
}
