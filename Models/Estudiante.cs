using System;
using SQLite;

namespace Registro_de_estudiantes_MVVM.Models
{
    public class Estudiante
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // [Unique] crea un índice único en SQLite: no permite dos estudiantes con la misma cuenta.
        [Unique, NotNull, MaxLength(20)]
        public string NumeroCuenta { get; set; } = string.Empty;

        [NotNull, MaxLength(150)]
        public string NombreCompleto { get; set; } = string.Empty;

        [NotNull, MaxLength(150)]
        public string Correo { get; set; } = string.Empty;

        [NotNull, MaxLength(20)]
        public string Telefono { get; set; } = string.Empty;

        [NotNull, MaxLength(100)]
        public string Carrera { get; set; } = string.Empty;

        [NotNull]
        public DateTime FechaNacimiento { get; set; }

        [NotNull, MaxLength(20)]
        public string Genero { get; set; } = string.Empty;

        [NotNull]
        public bool AceptaTerminos { get; set; }
    }
}