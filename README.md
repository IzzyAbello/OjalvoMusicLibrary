# OjalvoMusicLibrary
Proyecto2-MyP-CS-FC-UNAM

Autor: Isidro A. de Ojalvo Abelló García

Profesor: Dr. Canek Peláez Valdés

## Resumen

`ojalvomusic` es una biblioteca musical de escritorio escrita en C#. Un
**minero** recorre un directorio (y sus subdirectorios) buscando archivos MP3,
lee sus etiquetas ID3v2.4 y guarda la información en una base de datos SQLite.
Después, una **interfaz gráfica** (Avalonia) permite:

- buscar rolas (búsqueda simple o avanzada por campos);
- ver sus datos y su portada;
- editar rolas y álbumes;
- registrar personas y grupos, con los miembros de cada grupo;
- reproducir la rola seleccionada.

Los archivos MP3 **nunca se modifican**: todos los cambios se guardan solo en
la base de datos.

El uso detallado (opciones de la línea de comandos, cada ventana de la
interfaz, mensajes y solución de problemas) está en el
[manual de usuario](MANUAL.md).

## Requisitos

- **.NET 10 SDK** (para compilar y ejecutar).
- **VLC / libVLC 3** (opcional; solo hace falta para reproducir). Sin VLC la
  aplicación funciona igual, pero con el reproductor deshabilitado.

| Sistema        | Comando                                          |
|----------------|--------------------------------------------------|
| Arch Linux     | `sudo pacman -S dotnet-sdk vlc vlc-plugins-all`  |
| Debian, Ubuntu | `sudo apt install dotnet-sdk-10.0 vlc libvlc-dev` |

## Instalación

```bash
git clone https://github.com/IzzyAbello/OjalvoMusicLibrary
cd OjalvoMusicLibrary
dotnet build
dotnet test       # opcional: corre las pruebas
```

## Ejecución

Todos los comandos se corren desde la raíz del repositorio. Lo que va después
de `--` son los parámetros del programa.

**Interfaz gráfica**

```bash
dotnet run --project MusicLib.UI                # abre la interfaz
dotnet run --project MusicLib.UI -- ./Dataset   # mina ./Dataset y abre la interfaz
```

**Solo línea de comandos** (mina y termina, sin abrir la interfaz):

```bash
dotnet run --project MusicLib.UI -- -m ./Dataset -l
```

`-l` muestra en la consola el avance y el resumen del minado. Para ver todas
las opciones:

```bash
dotnet run --project MusicLib.UI -- --help
```

**Ejecutable independiente** (sin `dotnet run`):

```bash
dotnet publish MusicLib.UI -c Release -o publish
./publish/ojalvomusic --help
```

La base de datos se crea sola la primera vez, en
`~/.local/share/OjalvoMusic/library.db` (Linux) o en
`%LOCALAPPDATA%\OjalvoMusic\library.db` (Windows).

## Estructura

```
Dataset/          MP3 de prueba (también los usan las pruebas)
MusicLib/         núcleo: modelos, base de datos, minero, log y CLI
MusicLib.UI/      ejecutable 'ojalvomusic' (interfaz Avalonia)
MusicLib.Tests/   pruebas xUnit
```

## Licencia

GPL v3. Ver [LICENSE](LICENSE).
