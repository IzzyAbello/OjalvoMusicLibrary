namespace MusicLib.Models;

/// <summary>
/// Valores de la tabla types.
/// </summary>
public enum PerformerType
{
    /// <summary>
    /// Es persona.
    /// </summary>
    Person = 0,

    /// <summary>
    /// Es grupo.
    /// </summary>
    Group = 1,

    /// <summary>
    /// No se sabe que pinga es.
    /// </summary>
    Unknown = 2
}

/// <summary>
/// Modelo del Intérprete.
/// </summary>
public class Performer
{
    /// <summary>
    /// Comodín para establecer intérprete desconocido.
    /// </summary>
    public const string UnknownName = "Unknown Performer";

    /// <summary>
    /// Identificador.
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// Tipo de intérprete.
    /// </summary>
    public PerformerType Type { get; set; } = PerformerType.Unknown;
    
    /// <summary>
    /// Nombre.
    /// </summary>
    public string Name { get; set; } = "";
}

/// <summary>
/// Modelo de Álbum.
/// </summary>
public class Album
{
    /// <summary>
    /// Álbum comodín que ya inserta el (id 0, 'Unknown').
    /// </summary>
    public const long UnknownId = 0;

    /// <summary>
    /// Identificador.
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// Ruta.
    /// </summary>
    public string Path { get; set; } = "";   // directorio del álbum
    
    /// <summary>
    /// Nombre.
    /// </summary>
    public string Name { get; set; } = "";
    
    /// <summary>
    /// Año.
    /// </summary>
    public int Year { get; set; }
}

/// <summary>
/// Modelo de la Rola.
/// </summary>
public class Rola
{
    /// <summary>
    /// Identificador.
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// Identificador del intérprete.
    /// </summary>
    public long IdPerformer { get; set; }
    
    /// <summary>
    /// Indentificador del álbum.
    /// </summary>
    public long IdAlbum { get; set; } = Album.UnknownId;
    
    /// <summary>
    /// Ruta del archivo MP3.
    /// </summary>
    public string Path { get; set; } = "";
    
    /// <summary>
    /// Título.
    /// </summary>
    public string Title { get; set; } = "";
    
    /// <summary>
    /// Órden en el álbum.
    /// </summary>
    public int Track { get; set; }
    
    /// <summary>
    /// Año.
    /// </summary>
    public int Year { get; set; }
    
    /// <summary>
    /// Género.
    /// </summary>
    public string Genre { get; set; } = "";
}

/// <summary>
/// Modelo de Persona.
/// </summary>
public class Person
{
    /// <summary>
    /// Identificador.
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// Nombre artístico.
    /// </summary>
    public string StageName { get; set; } = "";
    
    /// <summary>
    /// Nombre real.
    /// </summary>
    public string? RealName { get; set; }
    
    /// <summary>
    /// Fecha de nacimiento 'YYYY-MM-DD'
    /// </summary>
    public string? BirthDate { get; set; }
    
    /// <summary>
    /// Fecha de muerte 'YYYY-MM-DD'
    /// </summary>
    public string? DeathDate { get; set; }
}


/// <summary>
/// Modelo de Grupo.
/// </summary>
public class Group
{
    /// <summary>
    /// Identificador.
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// Nombre.
    /// </summary>
    public string Name { get; set; } = "";
    
    /// <summary>
    /// Fecha de inicio.
    /// </summary>
    public string? StartDate { get; set; }
    
    /// <summary>
    /// Fecha de disolución.
    /// </summary>
    public string? EndDate { get; set; }
}
