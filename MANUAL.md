# Manual de usuario — OjalvoMusic

Este manual explica cómo usar `ojalvomusic`: la línea de comandos, el minero
y cada ventana de la interfaz. Para instalarlo, ver el [README](README.md).

## Contenido

1. [Conceptos](#1-conceptos)
2. [Línea de comandos](#2-línea-de-comandos)
3. [El minero](#3-el-minero)
4. [La ventana principal](#4-la-ventana-principal)
5. [Buscar rolas](#5-buscar-rolas)
6. [Editar rolas y álbumes](#6-editar-rolas-y-álbumes)
7. [Reproductor](#7-reproductor)
8. [Personas](#8-personas)
9. [Grupos](#9-grupos)
10. [Teclado](#10-teclado)
11. [Dónde se guardan los datos](#11-dónde-se-guardan-los-datos)
12. [Solución de problemas](#12-solución-de-problemas)

---

## 1. Conceptos

- **Rola:** una canción, es decir, un archivo MP3. Se identifica por su
  ruta: el mismo archivo nunca se registra dos veces.
- **Intérprete:** quien canta o toca la rola. Se identifica por su nombre
  **exacto**; "Queen" y "queen" son dos intérpretes distintos. Cada
  intérprete tiene un tipo:
  - **Persona**, si existe una persona registrada con ese nombre artístico.
  - **Grupo**, si existe un grupo registrado con ese nombre.
  - **Desconocido**, el tipo con el que lo crea el minero.
- **Álbum:** se identifica por su nombre **y** el directorio donde están sus
  archivos. Dos carpetas con un álbum del mismo nombre son dos álbumes
  distintos. Las rolas sin álbum en sus etiquetas van al álbum especial
  **Unknown**.
- **Persona** y **grupo:** datos extra sobre un intérprete (nombre real,
  fechas, miembros). Se registran a mano desde la interfaz.

> **Los archivos MP3 nunca se modifican.** Editar, borrar o agregar algo solo
> cambia la base de datos.

---

## 2. Línea de comandos

```
ojalvomusic [opciones] [<ruta>]
```

Si ejecutas desde el código fuente, `ojalvomusic` se sustituye por
`dotnet run --project MusicLib.UI --`. Por ejemplo:
`dotnet run --project MusicLib.UI -- -m ./Dataset`.

### Formas de uso

| Comando                   | Qué hace                                           |
|---------------------------|----------------------------------------------------|
| `ojalvomusic`             | Abre la interfaz.                                  |
| `ojalvomusic <ruta>`      | Mina `<ruta>` y después abre la interfaz.          |
| `ojalvomusic -m <ruta>`   | Solo mina `<ruta>`; no abre la interfaz.           |
| `ojalvomusic -h`          | Muestra la ayuda y termina.                        |

### Opciones

| Opción                                       | Descripción |
|----------------------------------------------|-------------|
| `-m`, `--miner`, `--minero`, `--minar <ruta>` | Mina el directorio indicado y termina. La ruta es obligatoria y va justo después de la opción. Puede ser relativa al directorio actual o absoluta. |
| `-l`, `--log`                                | Muestra en la consola el log del programa: parámetros, avance del minado ("[Leyendo] 3/12", "[Guardando] 12/12"), resumen y errores detallados. Se combina con cualquier otra opción. |
| `-h`, `--help`                               | Muestra la ayuda y termina. Tiene prioridad sobre todo lo demás, aunque haya otros parámetros inválidos. |

Reglas:

- Solo se admite **una** ruta, ya sea suelta o con `-m`, pero no ambas.
- `-m` solo puede indicarse una vez.
- Cualquier otro parámetro que empiece con `-` es una opción desconocida.
- La ruta debe existir y ser un directorio, no un archivo.
- Las opciones pueden ir en cualquier orden: `-l -m ./Musica` es igual a
  `-m ./Musica -l`.

### Ejemplos

```bash
ojalvomusic ~/Música              # mina ~/Música y abre la interfaz
ojalvomusic -m ~/Música -l        # mina mostrando el avance y termina
ojalvomusic -l                    # abre la interfaz con log en la consola
```

### Códigos de salida

| Código | Significado |
|--------|-------------|
| `0`    | Todo salió bien. |
| `1`    | Error al ejecutar (por ejemplo, no se pudo abrir la base de datos). Se imprime `Error: …`. |
| `2`    | Parámetros inválidos. Se imprime el motivo y la sugerencia `Usa 'ojalvomusic --help' para ver las opciones.` |
| `130`  | El usuario canceló el minado con **Ctrl+C**. |

Mensajes de uso inválido:

| Mensaje | Causa |
|---------|-------|
| `'-m' requiere la ruta donde minar.` | `-m` va al final o le sigue otra opción. |
| `La opción de minado solo puede indicarse una vez.` | Se escribió `-m` dos veces. |
| `Opción desconocida: '-x'.` | La opción no existe. |
| `Solo se admite una ruta. Se recibió también '…'.` | Hay dos rutas sueltas. |
| `No se pueden tener dos rutas de minado.` | Hay una ruta suelta y otra con `-m`. |
| `La ruta '…' no existe.` | La ruta no existe. |
| `'…' es un archivo, no un directorio.` | La ruta apunta a un archivo. |

---

## 3. El minero

El minero se usa desde la línea de comandos (sección 2) o con el botón
**Minar** de la interfaz (sección 4). En ambos casos hace lo mismo:

1. **Busca** todos los archivos `.mp3` del directorio y de sus
   subdirectorios, en todos los niveles. No distingue mayúsculas en la
   extensión y salta las carpetas a las que no tiene permiso de entrar.
2. **Descarta** los archivos que ya están en la base de datos (por su ruta).
   Por eso volver a minar la misma carpeta solo agrega los archivos nuevos.
3. **Lee** las etiquetas ID3 de cada archivo, sin modificarlo.
4. **Guarda** todo en la base de datos de una vez. Si algo falla a la mitad,
   la base no queda a medias.

### De la etiqueta a la base de datos

| Dato       | Se toma de                       | Si falta |
|------------|----------------------------------|----------|
| Título     | Título (TIT2)                    | El nombre del archivo, sin `.mp3`. |
| Intérprete | Intérprete (TPE1)                | El artista del álbum (TPE2); si tampoco hay, "Intérprete desconocido". |
| Álbum      | Álbum (TALB) + carpeta del archivo | El álbum Unknown. |
| Pista      | Número de pista (TRCK)           | 0. |
| Año        | Año (TDRC)                       | 0. |
| Género     | Género (TCON)                    | "Desconocido". |

Si una etiqueta tiene varios intérpretes o géneros, se guardan separados por
comas. Los intérpretes nuevos se crean con tipo Desconocido.

### Resumen

Al terminar se muestra cuántos archivos hubo de cada tipo. En la interfaz
aparece en la barra de estado; en la consola, con `-l`.

- **Encontradas:** archivos `.mp3` hallados.
- **Insertadas:** rolas nuevas guardadas.
- **Omitidas:** ya estaban en la base de datos.
- **Fallidas:** no se pudieron leer, por ejemplo por un archivo dañado. Con
  `-l` se lista el motivo de cada una.

### Cancelar

En la consola se cancela con **Ctrl+C**; en la interfaz, con el botón
**Cancelar**. El resumen dice "Minado cancelado".

- Si se cancela mientras **lee** las etiquetas, no se guarda nada.
- Si se cancela mientras **guarda**, se conservan las rolas que ya se
  guardaron. Volver a minar agrega el resto.

---

## 4. La ventana principal

```
┌───────────────────────────────────────────────────────────────────────────────────────┐
│ [Minar] [Personas] [Grupos]  [ Buscar…      ][Buscar]  [Búsqueda avanzada] [Recargar] │
│                                                                                       │
├───────────────────────────────────────────┬───────────────────────────────────────────┤
│ #  Título   Intérprete  Álbum  Año Género │  ┌───────┐                                │
│ …  (tabla de rolas)                       │  │portada│                                │
│                                           │  └───────┘                                │
│                                           │  Título, Intérprete,                      │
│                                           │  Álbum, Pista, Año,                       │
│                                           │  Género, Archivo,                         │
│                                           │  Carpeta del álbum                        │
├───────────────────────────────────────────┤                                           │
│ [Reproducir]  Sonando: …                  │ [Editar rola]                             │
│               ━━━━━━●──────────  1:23/3:45│ [Editar álbum]                            │
├───────────────────────────────────────────┴───────────────────────────────────────────┤
│ Barra de estado                                                                       │
└───────────────────────────────────────────────────────────────────────────────────────┘
```

- **Barra superior:**
  - **Minar** elige una carpeta y la mina. Mientras mina, el botón se llama
    **Cancelar** y la barra de estado muestra el avance ("Leyendo 5/20…").
    Al terminar, la tabla se recarga.
  - **Personas** y **Grupos** abren sus ventanas (secciones 8 y 9).
  - Búsqueda simple, **Búsqueda avanzada** y **Recargar** (sección 5).
- **Tabla de rolas:** muestra pista, título, intérprete, álbum, año y
  género. Un clic en un encabezado ordena por esa columna y otro clic invierte
  el orden. Las columnas se pueden ensanchar arrastrando su borde. Se
  selecciona una rola a la vez.
- **Panel derecho:** datos de la rola seleccionada, incluidas la ruta del
  archivo y la carpeta de su álbum. La **portada** se lee del MP3; si no tiene
  o está dañada, se ve el ícono ♪.
- **Reproductor:** ver la sección 7.
- **Barra de estado:** resultados de búsquedas, avance y resumen del minado,
  y errores.

---

## 5. Buscar rolas

### Búsqueda simple

Escribe en la caja y pulsa **Buscar** o **Enter**. Se muestran las rolas cuyo
**título, intérprete, álbum o género contienen** ese texto.

- No distingue mayúsculas ni acentos: "cafe" encuentra "Café Tacvba".
- Basta con una parte: "sol" encuentra "Soledad" y "Girasol".
- Con la caja vacía se muestran todas las rolas.
- Los caracteres `%` y `_` se buscan literalmente; no son comodines.

### Búsqueda avanzada

El botón **Búsqueda avanzada** abre una ventana con campos separados:

| Campo                  | Busca rolas que… |
|------------------------|------------------|
| Título                 | contengan el texto en el título |
| Intérprete             | contengan el texto en el intérprete |
| Álbum                  | contengan el texto en el álbum |
| Género                 | contengan el texto en el género |
| Año desde / Año hasta  | tengan un año dentro de ese rango (inclusive) |

- Los campos vacíos no filtran. Los que tienen valor se combinan: la rola
  debe cumplirlos **todos** (Y lógico).
- Se puede dar solo "desde" o solo "hasta".
- Si "desde" es mayor que "hasta", aparece el error `El año "desde" no puede
  ser mayor que el año "hasta".` y la ventana sigue abierta.
- **Buscar** (o Enter) aplica la búsqueda; **Cancelar** (o Esc) cierra sin
  buscar.

### Recargar

**Recargar** borra la búsqueda y vuelve a mostrar todas las rolas, leídas de
nuevo de la base de datos.

---

## 6. Editar rolas y álbumes

Los botones están en el panel derecho y se activan al seleccionar una rola.
Al guardar, la tabla repite la última búsqueda y vuelve a seleccionar la
rola. Si la rola ya no cumple la búsqueda, la tabla queda sin selección.

### Editar rola

| Campo      | Reglas |
|------------|--------|
| Título     | Obligatorio. |
| Intérprete | Obligatorio. Si escribes un nombre que ya existe (exacto), la rola pasa a ese intérprete. Si no existe, se crea uno nuevo con tipo Desconocido. |
| Álbum      | Obligatorio. Si cambias el nombre, la rola pasa al álbum con ese nombre **en la carpeta del archivo**. Si no existe, se crea, con el año de la rola. Esto no renombra el álbum de las demás rolas; para eso está "Editar álbum". |
| Pista      | Número de 0 a 9999. |
| Año        | Número de 0 a 9999. |
| Género     | Opcional; si se deja vacío, queda "Desconocido". |

### Editar álbum

Cambia el **nombre** y el **año** del álbum de la rola seleccionada. La
ventana muestra su directorio, que no se puede cambiar, y el aviso "El cambio
aplica a todas las rolas de este álbum."

- El nombre es obligatorio.
- No puede haber dos álbumes con el mismo nombre en el mismo directorio:
  `Ya existe el álbum "…" en ese directorio.`
- El álbum **Unknown** no se puede editar; el botón aparece deshabilitado.

En ambos editores, **Guardar** (o Enter) guarda y **Cancelar** (o Esc) cierra
sin cambios. Si hay un error, se muestra en rojo y la ventana sigue abierta.

---

## 7. Reproductor

Requiere VLC instalado (ver el README). Sin VLC, la barra de estado dice "No
se encontró VLC; la reproducción no está disponible." y el botón queda
deshabilitado. El resto del programa funciona igual.

- **Reproducir** toca la rola seleccionada desde el inicio. Mientras suena,
  el botón dice **Pausa**; pulsarlo pausa, y pulsarlo otra vez reanuda.
- Si seleccionas otra rola mientras algo suena, la música **sigue sonando**.
  El botón vuelve a decir **Reproducir**, y al pulsarlo se toca la nueva
  rola.
- El texto junto al botón dice qué está cargado: "Sonando: título —
  intérprete" o "En pausa: …".
- La **barra de progreso** avanza con la rola y muestra el tiempo como
  `transcurrido / total`. Un clic en la barra salta a ese punto. También se
  puede arrastrar: el salto se hace al soltar. Funciona igual en pausa.
- Cuando la rola termina, la barra vuelve a 0 y el botón a **Reproducir**.
- Si el archivo ya no existe en el disco, la barra de estado dice "No se
  encontró el archivo '…'."

---

## 8. Personas

El botón **Personas** abre la lista de personas registradas: nombre artístico,
nombre real, nacimiento y muerte. Tiene los botones **Agregar**, **Editar**,
**Eliminar** y **Cerrar** (o Esc). Editar y Eliminar requieren seleccionar a
alguien.

### Agregar y editar

| Campo            | Reglas |
|------------------|--------|
| Nombre artístico | Obligatorio. **No se puede cambiar** una vez creada la persona. |
| Nombre real      | Opcional. |
| Nacimiento       | Opcional; formato `AAAA-MM-DD` (por ejemplo `1946-09-05`). |
| Fallecimiento    | Opcional; formato `AAAA-MM-DD`; no puede ser antes del nacimiento. |

- Mientras los datos sean inválidos, **Guardar** queda deshabilitado y un texto
  gris dice el motivo. Por ejemplo: "La fecha de nacimiento debe ser una fecha
  AAAA-MM-DD."
- No se puede agregar un nombre que ya sea una persona (`Ya existe la persona
  "…".`) ni un grupo (`"…" ya es un grupo.`).
- **Al agregar una persona**, si hay un intérprete con exactamente ese nombre,
  el intérprete pasa a tipo **Persona** y sus rolas quedan ligadas a ella. Si
  no hay, se crea el intérprete. Para ligar a alguien con sus rolas, escribe el
  nombre tal como aparece en la columna Intérprete.

### Eliminar

Pide confirmación. Al aceptar:

- la persona sale de todos los grupos en los que estaba;
- se borra la persona;
- su intérprete vuelve a tipo Desconocido.

**Sus rolas no se tocan.** En la confirmación, Enter **no** acepta; hay que
pulsar **Aceptar**. Esc cancela.

---

## 9. Grupos

El botón **Grupos** abre la lista de grupos (nombre, inicio y disolución), con
los mismos botones que Personas.

### Agregar y editar

| Campo      | Reglas |
|------------|--------|
| Nombre     | Obligatorio. **No se puede cambiar** una vez creado el grupo. |
| Inicio     | Opcional; formato `AAAA-MM-DD`. |
| Disolución | Opcional; formato `AAAA-MM-DD`; no puede ser antes del inicio. |
| Miembros   | Personas que pertenecen al grupo. |

Las reglas de Guardar y de nombres repetidos son las mismas que en Personas
(`Ya existe el grupo "…".` / `"…" ya es una persona.`). Agregar un grupo liga
igual a un intérprete con ese nombre exacto, que pasa a tipo **Grupo**.

### Miembros

La parte de abajo del formulario tiene dos listas:

```
 Miembros          Disponibles
┌───────────┐      ┌───────────┐
│ Freddie   │  ◀  │ Brian     │
│ Roger     │  ▶  │ John      │
└───────────┘      └───────────┘
```

- **◀** pasa a la persona seleccionada en "Disponibles" a los miembros.
- **▶** regresa al miembro seleccionado a "Disponibles".
- Ambas listas se mantienen en orden alfabético.
- Una persona puede estar en varios grupos.
- Los miembros deben estar registrados en **Personas**. Si alguien no aparece,
  agrégalo primero ahí.
- Los cambios se guardan **solo al pulsar Guardar**, junto con los demás
  campos. Cancelar los descarta.

### Eliminar

Pide confirmación. Se quitan sus miembros (las personas **no** se borran), se
borra el grupo y su intérprete vuelve a tipo Desconocido. Sus rolas no se
tocan.

---

## 10. Teclado

| Tecla  | Dónde | Acción |
|--------|-------|--------|
| Enter  | Caja de búsqueda | Buscar. |
| Enter  | Búsqueda avanzada, editores, formularios | Buscar / Guardar. |
| Esc    | Cualquier ventana secundaria | Cancelar o Cerrar. |
| Esc    | Confirmación de borrado | Cancelar (Enter no confirma). |

---

## 11. Dónde se guardan los datos

Todo se guarda en una base de datos SQLite que se crea sola la primera vez:

| Sistema | Ruta |
|---------|------|
| Linux   | `~/.local/share/OjalvoMusic/library.db` |
| Windows | `%LOCALAPPDATA%\OjalvoMusic\library.db` |

Para empezar de cero, cierra el programa y borra ese archivo. Los MP3 no se
ven afectados.

---

## 12. Solución de problemas

| Síntoma | Qué hacer |
|---------|-----------|
| "No se encontró VLC; la reproducción no está disponible." | Instala VLC (ver el README) y vuelve a abrir el programa. |
| El reproductor inicia, pero no se oye nada | En Arch, revisa que estén los plugins de VLC (`vlc-plugins-all`). Revisa también el volumen del sistema. |
| "No se encontró el archivo '…'." | El MP3 se movió o se borró después de minarlo. Mina de nuevo su carpeta nueva. |
| Una rola no aparece después de minar | Revisa que la extensión sea `.mp3` y que el resumen no la cuente como fallida. Usa `-l` para ver el motivo. |
| Volver a minar no actualiza una rola | Por diseño, las rutas ya registradas se omiten. Edita la rola desde la interfaz. |
| La persona o el grupo no se ligan a sus rolas | El nombre debe ser **idéntico** al intérprete, con mayúsculas, acentos y espacios incluidos. |
| La tabla no muestra lo último | Pulsa **Recargar**. |

Para cualquier otro error, ejecuta con `-l`: el log en la consola trae el
detalle completo.
