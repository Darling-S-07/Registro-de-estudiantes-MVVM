using SQLite;
using Registro_de_estudiantes_MVVM.Models;

namespace Registro_de_estudiantes_MVVM.Services;

/// Maneja la conexión e inicialización de la base de datos SQLite
/// y las operaciones sobre la tabla de estudiantes.
/// Se registra como Singleton en MauiProgram para que todos los ViewModels
/// compartan la misma conexión.
public class SQLiteDatabase
{
    private const SQLiteOpenFlags Flags =
        SQLiteOpenFlags.ReadWrite |
        SQLiteOpenFlags.Create |
        SQLiteOpenFlags.SharedCache;

    private readonly string _dbPath;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private SQLiteAsyncConnection? _database;

    public SQLiteDatabase(string dbPath)
    {
        _dbPath = dbPath;
    }

    // Abre la conexión y crea la tabla la primera vez que se necesita,
    // sin bloquear el hilo de la interfaz (sin .Wait()).
    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_database is not null)
            return _database;

        await _initLock.WaitAsync();
        try
        {
            if (_database is null)
            {
                var connection = new SQLiteAsyncConnection(_dbPath, Flags);
                await connection.CreateTableAsync<Estudiante>();
                _database = connection;
            }

            return _database;
        }
        finally
        {
            _initLock.Release();
        }
    }

    // Create: guarda un estudiante. Devuelve el número de filas insertadas.
    // Tras insertar, estudiante.Id contiene el ID generado.
    // Lanza SQLiteException (Result = Constraint) si el NumeroCuenta ya existe.
    public async Task<int> GuardarEstudianteAsync(Estudiante estudiante)
    {
        var db = await GetConnectionAsync();
        return await db.InsertAsync(estudiante);
    }

    // Read: todos los estudiantes, del más reciente al más antiguo.
    public async Task<List<Estudiante>> ObtenerEstudiantesAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<Estudiante>()
                       .OrderByDescending(e => e.Id)
                       .ToListAsync();
    }

    // Read by Id
    public async Task<Estudiante?> ObtenerEstudiantePorIdAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.FindAsync<Estudiante>(id);
    }

    // Validación de cuenta única: true si ya existe un estudiante con ese número de cuenta.
    public async Task<bool> ExisteNumeroCuentaAsync(string numeroCuenta)
    {
        var db = await GetConnectionAsync();
        int cantidad = await db.Table<Estudiante>()
                               .Where(e => e.NumeroCuenta == numeroCuenta)
                               .CountAsync();
        return cantidad > 0;
    }

    // Update
    public async Task<int> ActualizarEstudianteAsync(Estudiante estudiante)
    {
        var db = await GetConnectionAsync();
        return await db.UpdateAsync(estudiante);
    }

    // Delete
    public async Task<int> EliminarEstudianteAsync(Estudiante estudiante)
    {
        var db = await GetConnectionAsync();
        return await db.DeleteAsync(estudiante);
    }
}
