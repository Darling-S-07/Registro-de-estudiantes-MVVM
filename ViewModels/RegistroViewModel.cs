using System.Collections.ObjectModel;
using System.Net.Mail;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Registro_de_estudiantes_MVVM.Models;
using Registro_de_estudiantes_MVVM.Services;
using SQLite;

namespace Registro_de_estudiantes_MVVM.ViewModels;

/// <summary>
/// ViewModel de la pantalla de registro de estudiantes.
/// Contiene todo el estado y la lógica del formulario: el code-behind de la
/// Vista (RegistroPage.xaml.cs) no necesita conocer nada de esto, solo
/// enlazarse (bindings) a estas propiedades y comandos.
/// </summary>
public partial class RegistroViewModel : ObservableObject
{
    private static readonly string[] CarrerasDisponibles =
    {
        "Ingeniería en Sistemas",
        "Administración de Empresas",
        "Contaduría Pública",
        "Derecho",
        "Medicina",
        "Psicología",
        "Arquitectura",
        "Mercadotecnia"
    };

    private readonly SQLiteDatabase _database;

    public RegistroViewModel(SQLiteDatabase database)
    {
        _database = database;

        Carreras = CarrerasDisponibles;
        Estudiantes = new ObservableCollection<Estudiante>();

        fechaNacimiento = DateTime.Today.AddYears(-18);

        // Sincroniza el estado inicial del switch de tema con el tema activo
        // de la aplicación, sin pasar por el setter (que volvería a asignarlo).
        modoOscuro = Application.Current?.UserAppTheme == AppTheme.Dark ||
                     (Application.Current?.UserAppTheme == AppTheme.Unspecified &&
                      Application.Current?.RequestedTheme == AppTheme.Dark);
    }

    // ===================================================================
    // Catálogo de carreras y lista de estudiantes (Req. 6: ObservableCollection)
    // ===================================================================

    public IReadOnlyList<string> Carreras { get; }

    [ObservableProperty]
    private ObservableCollection<Estudiante> estudiantes;

    [ObservableProperty]
    private string tituloLista = "Estudiantes registrados";

    // ===================================================================
    // Campos del formulario (propiedades observables - CommunityToolkit.Mvvm)
    // Cada una notifica a GuardarCommand para que reevalúe su CanExecute.
    // ===================================================================

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
    private string numeroCuenta = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
    private string nombreCompleto = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
    private string correo = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
    private string telefono = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
    private string? carreraSeleccionada;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
    private DateTime fechaNacimiento;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
    private bool esMasculino;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
    private bool esFemenino;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
    private bool esOtro;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
    private bool aceptaTerminos;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
    private bool isBusy;

    private string GeneroSeleccionado =>
        EsMasculino ? "Masculino" :
        EsFemenino ? "Femenino" :
        EsOtro ? "Otro" : string.Empty;

    // ===================================================================
    // Tema claro / oscuro (Switch con UserAppTheme)
    // ===================================================================

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IconoTema))]
    [NotifyPropertyChangedFor(nameof(EstadoTema))]
    private bool modoOscuro;

    public string IconoTema => ModoOscuro ? "🌙" : "☀️";

    public string EstadoTema => ModoOscuro ? "Activado (Modo oscuro)" : "Desactivado (Modo claro)";

    partial void OnModoOscuroChanged(bool value)
    {
        if (Application.Current is null)
            return;

        Application.Current.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
    }

    [RelayCommand]
    private void AlternarTema() => ModoOscuro = !ModoOscuro;

    // ===================================================================
    // Carga de estudiantes (se invoca desde RegistroPage.OnAppearing)
    // ===================================================================

    [RelayCommand]
    private async Task CargarEstudiantesAsync()
    {
        try
        {
            var lista = await _database.ObtenerEstudiantesAsync();

            Estudiantes.Clear();
            foreach (var estudiante in lista)
                Estudiantes.Add(estudiante);

            TituloLista = $"Estudiantes registrados ({Estudiantes.Count})";
        }
        catch (Exception ex)
        {
            await MostrarAlertaAsync("Error", $"No se pudo cargar la lista de estudiantes: {ex.Message}", "OK");
        }
    }

    // ===================================================================
    // Guardar (Req. 5: CommandCanExecute)
    // ===================================================================

    [RelayCommand(CanExecute = nameof(PuedeGuardar))]
    private async Task GuardarAsync()
    {
        if (!PuedeGuardar())
            return;

        var estudiante = new Estudiante
        {
            NumeroCuenta = NumeroCuenta.Trim(),
            NombreCompleto = NombreCompleto.Trim(),
            Correo = Correo.Trim(),
            Telefono = Telefono.Trim(),
            Carrera = CarreraSeleccionada!,
            FechaNacimiento = FechaNacimiento.Date,
            Genero = GeneroSeleccionado,
            AceptaTerminos = AceptaTerminos
        };

        bool confirmar = await MostrarAlertaAsync(
            "Resumen del estudiante",
            ConstruirResumen(estudiante),
            "Guardar",
            "Cancelar");

        if (!confirmar)
            return;

        IsBusy = true;
        try
        {
            if (await _database.ExisteNumeroCuentaAsync(estudiante.NumeroCuenta))
            {
                await MostrarCuentaDuplicadaAsync(estudiante.NumeroCuenta);
                return;
            }

            await _database.GuardarEstudianteAsync(estudiante);

            await CargarEstudiantesAsync();
            Limpiar();

            await MostrarAlertaAsync("Éxito", "El estudiante se registró correctamente.", "OK");
        }
        catch (SQLiteException ex) when (ex.Result == SQLite3.Result.Constraint)
        {
            await MostrarCuentaDuplicadaAsync(estudiante.NumeroCuenta);
        }
        catch (Exception ex)
        {
            await MostrarAlertaAsync("Error", $"Ocurrió un error al guardar el estudiante: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Condición de habilitación del botón Guardar: todos los campos deben
    // estar presentes y tener un formato válido (Req. 5).
    private bool PuedeGuardar()
    {
        if (IsBusy)
            return false;

        if (string.IsNullOrWhiteSpace(NumeroCuenta) || !NumeroCuentaEsValido(NumeroCuenta.Trim()))
            return false;

        if (string.IsNullOrWhiteSpace(NombreCompleto) || !NombreCompletoEsValido(NombreCompleto.Trim()))
            return false;

        if (string.IsNullOrWhiteSpace(Correo) || !CorreoEsValido(Correo.Trim()))
            return false;

        if (string.IsNullOrWhiteSpace(Telefono) || !TelefonoEsValido(Telefono.Trim()))
            return false;

        if (string.IsNullOrWhiteSpace(CarreraSeleccionada))
            return false;

        if (FechaNacimiento.Date >= DateTime.Today || DateTime.Today.AddYears(-120) > FechaNacimiento.Date)
            return false;

        if (string.IsNullOrEmpty(GeneroSeleccionado))
            return false;

        if (!AceptaTerminos)
            return false;

        return true;
    }

    [RelayCommand]
    private void Limpiar()
    {
        NumeroCuenta = string.Empty;
        NombreCompleto = string.Empty;
        Correo = string.Empty;
        Telefono = string.Empty;
        CarreraSeleccionada = null;
        FechaNacimiento = DateTime.Today.AddYears(-18);
        EsMasculino = false;
        EsFemenino = false;
        EsOtro = false;
        AceptaTerminos = false;
    }

    private async Task MostrarCuentaDuplicadaAsync(string numeroCuenta)
    {
        await MostrarAlertaAsync(
            "Cuenta duplicada",
            $"Ya existe un estudiante con el número de cuenta {numeroCuenta}.",
            "OK");
    }

    private static string ConstruirResumen(Estudiante estudiante)
    {
        return
            $"Número de cuenta: {estudiante.NumeroCuenta}\n" +
            $"Nombre: {estudiante.NombreCompleto}\n" +
            $"Correo: {estudiante.Correo}\n" +
            $"Teléfono: {estudiante.Telefono}\n" +
            $"Carrera: {estudiante.Carrera}\n" +
            $"Fecha de nacimiento: {estudiante.FechaNacimiento:dd/MM/yyyy}\n" +
            $"Género: {estudiante.Genero}\n" +
            $"Acepta términos: {(estudiante.AceptaTerminos ? "Sí" : "No")}";
    }

    // ===================================================================
    // Validaciones de formato (sin dependencias de la Vista)
    // ===================================================================

    // Formato válido: usuario@dominio.ext, sin espacios ni texto adicional.
    private static bool CorreoEsValido(string correo)
    {
        if (correo.Contains(' '))
            return false;

        try
        {
            var direccion = new MailAddress(correo);
            return direccion.Address == correo && direccion.Host.Contains('.');
        }
        catch (FormatException)
        {
            return false;
        }
    }

    // Acepta dígitos y guiones, con al menos 8 dígitos (formato hondureño típico: 9999-9999).
    private static bool TelefonoEsValido(string telefono)
    {
        int digitos = telefono.Count(char.IsDigit);
        bool soloDigitosYGuiones = telefono.All(c => char.IsDigit(c) || c == '-');
        return soloDigitosYGuiones && digitos >= 8;
    }

    // Solo dígitos, sin letras ni símbolos (ej. 20231001).
    private static bool NumeroCuentaEsValido(string numeroCuenta)
    {
        return numeroCuenta.Length > 0 && numeroCuenta.All(char.IsDigit);
    }

    // Solo letras (incluye tildes y ñ) y espacios, con al menos dos palabras (nombre y apellido).
    private static bool NombreCompletoEsValido(string nombreCompleto)
    {
        bool soloLetrasYEspacios = nombreCompleto.All(c => char.IsLetter(c) || c == ' ');
        int palabras = nombreCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        return soloLetrasYEspacios && palabras >= 2;
    }

    // ===================================================================
    // Helper de diálogos: el ViewModel no conoce la Page, usa la ventana activa.
    // ===================================================================
    private static Task<bool> MostrarAlertaAsync(string titulo, string mensaje, string aceptar, string? cancelar = null)
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null)
            return Task.FromResult(false);

        if (cancelar is null)
        {
            return page.DisplayAlertAsync(titulo, mensaje, aceptar)
                       .ContinueWith(_ => true);
        }

        return page.DisplayAlertAsync(titulo, mensaje, aceptar, cancelar);
    }
}
