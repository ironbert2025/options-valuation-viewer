# Diseño v1.0 — Visor de valorización de opciones

5 de noviembre de 2026 · Fase DESIGN · Base: `docs/specs/spec-v1.md` y `docs/specs/proposal-v1.md` (aprobada: ScottPlot 5, `net10.0-windows`, xUnit v3)

Alcance: MVP v1.0, solo offline. Solo firmas y pseudocódigo; nada de implementación. Las marcas **(sin verificar)** señalan APIs de ScottPlot que no he comprobado y que se confirmarán en un spike al inicio de la etapa 6.

## 1. Modelo de dominio (`OptionsViewer.Core`)

Todos los tipos son inmutables (`record` / `readonly record struct`). La hora es `TimeOnly` tal como viene en el archivo, sin zona ni conversión (RNF-03). Los precios son `decimal` (ver decisión D6).

```csharp
enum OptionType { Call, Put }

readonly record struct ContractKey(OptionType Type, decimal Strike);

// Una fila del CSV (RF-01: las 12 columnas; Time vive en el Snapshot)
record ContractQuote(
    decimal Strike, decimal SpotPrice, decimal Bid, decimal Ask,
    decimal IntValue, decimal ExtValue,
    decimal Delta, decimal Gamma, decimal Theta, decimal Vega, decimal IV);

// Todas las filas con la misma Time (RF-02). Strike único dentro del snapshot.
record Snapshot(TimeOnly Time, IReadOnlyDictionary<decimal, ContractQuote> QuotesByStrike);

// Resultado de leer el archivo: todo el contenido, sin filtrar.
record SnapshotSet(OptionType Type, IReadOnlyList<Snapshot> Snapshots);   // orden estrictamente creciente por Time

// Resultado del filtro de horario (RF-02, RF-10).
record TradingSession(
    OptionType Type,
    IReadOnlyList<Snapshot> Snapshots,     // solo 09:30:00..16:00:00 inclusivos
    int DiscardedCount,                    // CA-01: 2 en los archivos de referencia
    bool NoSnapshotsInWindow);             // RF-10 / CA-12

// Resultado de la gráfica 1.
record ContractSeries(
    ContractKey Key,
    TimeOnly T0,
    decimal EntryAskPerContract,           // Ask(T0) × 100
    bool PercentAvailable,                 // false si Ask(T0) = 0  →  n/d
    IReadOnlyList<SeriesPoint> Points);    // UN punto por cada snapshot desde T0 hasta el último de la sesión

record SeriesPoint(TimeOnly Time, PricePoint? Value)   // Value == null  ⇒  HUECO
{
    int SecondsOfDay;                      // eje horizontal en tiempo real (RF-06): Time en segundos desde 00:00:00
}

record PricePoint(
    decimal AskPerContract,                // Ask × 100
    decimal BidPerContract,                // Bid × 100
    decimal PnlDollars,                    // Bid_c(t) − Ask_c(T0)
    decimal? PnlPercent);                  // null ⇒ n/d (solo cuando Ask_c(T0) = 0)
```

**Hueco (RF-06, CA-05):** un contrato ausente de un snapshot que sí existe en la sesión se representa como `SeriesPoint` con `Value = null`. El punto **se conserva** en la lista (no se omite), de modo que el hueco se puede probar en el núcleo y la interfaz lo dibuja como `NaN` (la línea se corta) y el tooltip puede decir "sin dato". El tiempo entre snapshots no genera ni elimina puntos.

**n/d (Ask(T0) = 0):** `ContractSeries.PercentAvailable = false` y todos los `PnlPercent` son `null`. El PnL en $ se calcula igual. En los CSV de referencia Ask nunca vale 0 (mínimo 0,02), así que este caso se prueba con un CSV sintético.

## 2. Contratos entre capas

### 2.1 Fuente de datos (puerto definido en Core, implementado en Infrastructure)

```csharp
interface ISnapshotSource
{
    OptionType OptionType { get; }
    string DisplayName { get; }                       // nombre del archivo, para la UI
    LoadResult Load(CancellationToken cancellationToken);
}

// Resultado tipado: o éxito o un CsvError (ver sección 5)
record LoadResult(SnapshotSet? Value, CsvError? Error) { bool IsSuccess => Error is null; }
```

- **Entrada:** el token de cancelación (el usuario abre otro archivo mientras uno carga, RF-09).
- **Salida:** `LoadResult` con `SnapshotSet` completo (sin filtrar) o un `CsvError`.
- **Errores:** los de contenido (archivo vacío, columna faltante, valor no numérico, fila malformada, orden/duplicados) viajan como `CsvError` dentro del resultado. Los de entorno (archivo inexistente, sin permiso, `IOException`) se lanzan como excepciones, y `OperationCanceledException` si se cancela.
- La única implementación v1.0 es `CsvFileSnapshotSource(string path, OptionType type)` en Infrastructure; abre con `FileAccess.Read` y `FileShare.ReadWrite` (RNF-01, aplica también offline). La fuente en vivo es v1.1 y no se diseña aquí; se prevé como una interfaz aparte que extienda esta.

### 2.2 Utilidad de nombre de archivo (Core)

```csharp
static class ChainFileName
{
    static OptionType? TryGetOptionType(string fileName);   // busca "_Call_" o "_Put_" (sin distinguir mayúsculas); null si no está
}
```

Acepta con o sin `<id>` inicial. La fecha del nombre no se usa en v1.0 (ver 8.2, A4).

### 2.3 Casos de uso (Core; clases estáticas puras, sin I/O ni estado)

```csharp
static class MarketHours
{
    static readonly TimeOnly Open  = 09:30:00;
    static readonly TimeOnly Close = 16:00:00;
    static bool IsInSession(TimeOnly t);                          // Open <= t <= Close
    static TradingSession Apply(SnapshotSet set);                 // RF-02, RF-10
}
```
- **Entrada:** `SnapshotSet`. **Salida:** `TradingSession`. **Errores:** ninguno; si no queda ningún snapshot, devuelve `NoSnapshotsInWindow = true` (aviso, no error).

```csharp
static class StrikeCatalog
{
    static IReadOnlyList<decimal> List(TradingSession session);   // RF-03
}
```
- **Salida:** strikes distintos de los snapshots de la sesión, ordenados de menor a mayor (CA-03: 16 valores). Lista vacía si la sesión está vacía. **Errores:** ninguno.

```csharp
static class ValuationSeries
{
    static SeriesResult Build(TradingSession session, ContractKey key, TimeOnly t0);   // RF-04..06
}

record SeriesResult(ContractSeries? Value, SeriesError? Error) { bool IsSuccess => Error is null; }
enum SeriesErrorKind { EmptySession, T0NotASnapshot, ContractAbsentAtT0 }
record SeriesError(SeriesErrorKind Kind, TimeOnly T0, ContractKey Key);
```
- **Entradas:** sesión filtrada, contrato (tipo y strike), T0.
- **Salida:** `ContractSeries` desde T0 hasta el último snapshot de la sesión.
- **Errores (tipados, no excepciones):** `EmptySession` (sin snapshots), `T0NotASnapshot` (T0 no coincide con la `Time` de ningún snapshot de la sesión) y `ContractAbsentAtT0` (CA-06: el contrato no existe en T0). La UI los traduce a un aviso y no dibuja.
- Un `ContractKey.Type` distinto de `session.Type` es un error de programación → `ArgumentException`.

## 3. Flujo de datos

1. El usuario pulsa **Abrir CSV** y elige un archivo (OpenFileDialog).
2. La UI obtiene el tipo con `ChainFileName.TryGetOptionType` (si es `null`, pregunta Call o Put; D5) y crea `CsvFileSnapshotSource(path, type)`.
3. La UI ejecuta `Task.Run(() => source.Load(token))`; mientras tanto deshabilita los controles de selección y muestra "Cargando…".
4. Infrastructure abre el archivo (`FileShare.ReadWrite`), lee la cabecera, mapea nombres de columna → índices (sin distinguir mayúsculas, cualquier orden, columnas extra ignoradas) y valida las 12.
5. Lee línea a línea, convierte con `InvariantCulture` (punto decimal, independiente de la configuración regional), y agrupa filas consecutivas con la misma `Time` en un `Snapshot`.
6. Devuelve `LoadResult`. Si trae `CsvError`, la UI lo muestra con su línea, limpia las gráficas y termina (CA-02).
7. Si hay éxito, la UI llama a `MarketHours.Apply` → `TradingSession`. Si `NoSnapshotsInWindow`, muestra el aviso de zona horaria (RF-10) y termina.
8. La UI muestra los conteos (snapshots dentro / descartados, CA-01), llena el combo de strikes con `StrikeCatalog.List` y el combo de T0 con las `Time` de `session.Snapshots`.
9. Al elegir strike y T0, la UI llama a `ValuationSeries.Build`. Con error, muestra el aviso (CA-06) y limpia las gráficas.
10. Con éxito, la UI convierte `ContractSeries.Points` en arrays para ScottPlot (`SecondsOfDay` en X; `NaN` donde `Value == null`) y refresca una sola vez cada gráfica.
11. Al cambiar strike, T0 o archivo se repiten los pasos 9–10 o 1–10, sin reiniciar la app (RF-09).

## 4. Reglas de cálculo

**Filtro de horario (RF-02, CA-09), inclusivo en ambos extremos:**
```
IsInSession(t):  return Open <= t AND t <= Close        // 09:30:00 <= t <= 16:00:00
Apply(set):
    kept      = [s in set.Snapshots WHERE IsInSession(s.Time)]
    discarded = set.Snapshots.Count - kept.Count
    return TradingSession(set.Type, kept, discarded, noSnapshotsInWindow = (kept.Count == 0))
```
`TimeOnly` tiene resolución sub-segundo, pero `Time` solo trae `HH:MM:SS`, así que 16:00:00 queda incluido y 16:00:01 excluido sin ambigüedad.

**Serie desde T0, con huecos (RF-04..06, CA-04..06, CA-13):**
```
Build(session, key, t0):
    if session.Snapshots is empty            → error EmptySession
    i0 = index of snapshot with Time == t0   → if none: error T0NotASnapshot
    q0 = session.Snapshots[i0].QuotesByStrike[key.Strike]   → if missing: error ContractAbsentAtT0
    entryC = q0.Ask * 100
    points = []
    for s in session.Snapshots[i0 .. end]:            // se recorre por posición de snapshot
        q = s.QuotesByStrike.TryGet(key.Strike)
        if q is null:  points.add(SeriesPoint(s.Time, null))        // HUECO
        else:          points.add(SeriesPoint(s.Time, PriceAt(q, entryC)))
    return ContractSeries(key, t0, entryC, PercentAvailable = (entryC != 0), points)
```
- **El tiempo entre snapshots nunca interviene:** no hay ninguna resta de `Time` ni umbral. Un hueco existe solo si el contrato falta en un snapshot que existe; dos snapshots consecutivos con dato quedan unidos aunque los separen 4, 60 o 181 s (RNF-10, RF-06).
- La X de cada punto es `SecondsOfDay` (tiempo real), no su índice.

**PnL por contrato (×100), en `decimal`:**
```
PriceAt(q, entryC):
    askC = q.Ask * 100
    bidC = q.Bid * 100
    pnl  = bidC - entryC
    pct  = (entryC == 0) ? null : pnl / entryC * 100        // sin redondeo en el núcleo
    return PricePoint(askC, bidC, pnl, pct)
```
Redondeo solo al mostrar (2 decimales). Ejemplo CA-04: Ask(T0) = 3,00 → 300 $; Bid = 2,89 → 289 $; PnL = −11 $; PnL% = −3,6667 → "−3,67 %".

## 5. Errores del lector CSV (RF-01, CA-02)

**Resultado tipado, no excepciones.** Motivos: (a) estos errores son el resultado esperable de leer datos externos, no situaciones excepcionales; (b) deben llevar datos estructurados (línea, columna, texto original) que la UI muestra tal cual y que las pruebas comprueban sin buscar mensajes; (c) mantiene el flujo explícito (CA-02 exige "no dibuja nada"). Las excepciones se reservan para fallos de entorno (archivo inexistente, permisos).

```csharp
enum CsvErrorKind { EmptyFile, MissingColumns, DuplicateColumn, MalformedRow, InvalidValue, OutOfOrder, DuplicateStrike }

record CsvError(
    CsvErrorKind Kind,
    int? Line,                         // número de línea física, base 1, la cabecera es la línea 1; null en EmptyFile de 0 bytes
    string? Column,                    // nombre de columna afectada, si aplica
    string? RawValue,                  // texto original que falló, si aplica
    IReadOnlyList<string> MissingColumns,
    string Message);                   // texto en español listo para mostrar
```

| Situación | `Kind` | `Line` | Detalles |
| --- | --- | --- | --- |
| Archivo de 0 bytes, o solo cabecera sin filas de datos | `EmptyFile` | `null` (0 bytes) o 1 (solo cabecera) | — |
| Falta una o más de las 12 columnas | `MissingColumns` | 1 | `MissingColumns` = nombres que faltan |
| Nombre de columna repetido en la cabecera | `DuplicateColumn` | 1 | `Column` |
| Fila con menos campos que la cabecera | `MalformedRow` | N | — |
| Valor no numérico (o `Time` que no es `HH:MM:SS`) | `InvalidValue` | N | `Column`, `RawValue` |
| `Time` menor que la del snapshot anterior | `OutOfOrder` | N | `RawValue` = hora |
| Mismo strike repetido dentro de un snapshot | `DuplicateStrike` | N | `Column` = `StrikePrice` |

Reglas del lector: se detiene en el **primer** error (ver A5 en 8.2). Las líneas en blanco se ignoran. Columnas de más y otro orden se aceptan (CA-02, segunda parte). Un `\r` final no cuenta como parte del último campo (RNF-09; `StreamReader.ReadLine` acepta CRLF y LF). Los números se interpretan con `InvariantCulture`.

## 6. Interfaz (`OptionsViewer.WinForms`)

**Controles y qué pide cada uno**

| Control | Función | Llama a |
| --- | --- | --- |
| Botón **Abrir CSV…** + `OpenFileDialog` | Elegir archivo (RF-01, RF-09) | `ChainFileName`, `CsvFileSnapshotSource.Load`, `MarketHours.Apply` |
| Etiquetas de estado | Archivo, tipo, snapshots dentro / descartados (CA-01) | `TradingSession.Snapshots.Count`, `DiscardedCount` |
| `ComboBox` **Strike** | Lista de strikes (RF-03) | `StrikeCatalog.List` |
| `ComboBox` **T0** | `Time` de todos los snapshots de la sesión (RF-04, D3) | `session.Snapshots` |
| Banner de aviso (`Label`) | RF-10, CA-06 y errores de CSV | `TradingSession.NoSnapshotsInWindow`, `SeriesError`, `CsvError` |
| `FormsPlot` superior | Ask(t), Bid(t), línea de Ask(T0) | `ValuationSeries.Build` |
| `FormsPlot` inferior | PnL en $ y en % (D4) | `ValuationSeries.Build` |

El strike y el T0 redibujan automáticamente al cambiar cualquiera de los dos, cuando ambos están elegidos (RF-09).

**Carga asíncrona (RNF-07, CA-10):** el manejador del botón es `async`; ejecuta `await Task.Run(() => source.Load(token))`, así que la continuación vuelve sola al hilo de la interfaz (no hace falta `Invoke`). Mientras carga, se deshabilitan los combos y el botón y se muestra "Cargando…". Si el usuario abre otro archivo, se cancela el `CancellationTokenSource` anterior y se ignora cualquier resultado que no sea del último. `MarketHours.Apply`, `StrikeCatalog.List` y `ValuationSeries.Build` se ejecutan en el hilo de la interfaz porque manejan como mucho unos 1.600 snapshots (a confirmar al medir en la etapa 6).

**Gráficas (ScottPlot 5)**
- Se convierte `Points` en arrays: X = `SecondsOfDay`, Y = valor o `double.NaN` donde `Value == null`. Que ScottPlot corte la línea en `NaN` **(sin verificar)**; si no lo hiciera, se parte la serie en segmentos de puntos consecutivos con dato y se añade una serie por segmento.
- Eje X fijo de 09:30:00 a 16:00:00 (34.200 a 57.600 s), con rótulos `HH:mm`; se usa un eje con fecha ficticia o un formateador de números **(sin verificar cuál ofrece la API)**. La fecha del archivo no interviene.
- Línea horizontal de Ask(T0) con un `HorizontalLine` del panel superior.
- Un refresco por gráfica por cada cambio de selección.

**Tooltip (RF-07)**
1. Se escucha el movimiento del ratón sobre cada `FormsPlot`.
2. Se convierte el píxel a coordenadas del gráfico (`Plot.GetCoordinates(pixel)`, **sin verificar el nombre exacto**) y se toma la X.
3. Se busca por búsqueda binaria, en el arreglo ordenado de `SecondsOfDay`, el punto más cercano en el eje horizontal. Se hace sobre los datos del núcleo y no con una función de la librería, para que no dependa de cómo trate los `NaN`.
4. Si el punto tiene `Value`: se muestra hora, Bid, Ask, PnL $ y PnL % (o "n/d"). Si es un hueco: "hora — sin dato" (D8).
5. Se marca el punto con un marcador y el texto se muestra en un `ToolTip` o `Label` flotante; solo se refresca la gráfica cuando cambia el punto cercano, para no redibujar en cada movimiento.

## 7. Estrategia de pruebas (xUnit v3, proyecto `OptionsViewer.Tests`)

Los CSV de `samples/` se enlazan al proyecto de pruebas (copiados a la salida, sin duplicarlos en el repo). Los CSV sintéticos se generan en memoria con un ayudante de pruebas y se pasan por un `Stream` o archivo temporal. Subcarpetas: `Infrastructure/` para el lector y `Core/` para el resto.

| CA | Prueba que lo verifica | Datos | Carpeta |
| --- | --- | --- | --- |
| CA-01 | Cargar Calls y Puts → `MarketHours.Apply`: 1.613 dentro, 2 descartados (08:24:27 y 18:24:35); la `IV` de la última fila se lee como número con CRLF | `samples/` Calls y Puts | `Infrastructure/` + `Core/` |
| CA-02 | Cabecera sin una columna → `MissingColumns`; archivo vacío → `EmptyFile`; valor no numérico → `InvalidValue` con la línea correcta; mismas columnas reordenadas y con una extra → éxito | Sintético (variantes de las primeras filas de `samples/`) | `Infrastructure/` |
| CA-03 | `StrikeCatalog.List`: 16 valores ordenados; 330 a 367,5 en Calls y 327,5 a 365 en Puts | `samples/` | `Core/` |
| CA-04 | `Build` con Calls, strike 340, T0 = 09:30:00: entrada 300, Bid 289, PnL −11, PnL% −3,67 (comparado a 2 decimales) | `samples/` Calls | `Core/` |
| CA-05 | `Build` con Calls, strike 337,5, T0 = 09:44:49: el siguiente punto con dato es 09:45:29 y entre ambos hay 9 puntos con `Value == null`. Segunda parte: dos snapshots consecutivos con dato separados 60 y 181 s quedan sin hueco | `samples/` Calls + sintético | `Core/` |
| CA-06 | `Build` con un T0 donde el contrato no existe → `ContractAbsentAtT0` y sin `ContractSeries` | `samples/` o sintético | `Core/` |
| CA-09 | `IsInSession` y `Apply` con 09:29:59 (fuera), 09:30:00 (dentro), 16:00:00 (dentro), 16:00:01 (fuera) | Sintético | `Core/` |
| CA-10 | **No automatizable sin UI.** Prueba manual en la etapa 6: cargar el archivo de referencia y hacer clic en los controles mientras carga. Complemento automatizado: la carga respeta la cancelación | `samples/` | Manual (+ `Infrastructure/`) |
| CA-11 | Se cumple con CA-01, 03, 04, 05, 09 y 13 (arriba); `dotnet test` corre sin interfaz | — | Todo el proyecto |
| CA-12 | CSV con todas las `Time` fuera de 09:30:00–16:00:00 (por ejemplo, 08:00 a 08:59) → `NoSnapshotsInWindow = true` | Sintético | `Core/` |
| CA-13 | CSV sintético con un snapshot cada 6 s hasta las 11:00 y uno por minuto después: mismos recuentos y filtro; sin huecos falsos para un contrato presente en todos; `SecondsOfDay` coincide con la hora real de cada snapshot | Sintético | `Core/` |

Pruebas adicionales: `Ask(T0) = 0` (n/d, `PercentAvailable = false`), `T0NotASnapshot`, `OutOfOrder`, `DuplicateStrike`, número decimal con punto bajo configuración regional con coma (`InvariantCulture`), y `ChainFileName` con y sin `<id>`.

## 8. Decisiones y ambigüedades que necesitan aprobación

### 8.1 Decisiones de diseño

| # | Decisión | Opciones | Recomendación | Justificación |
| --- | --- | --- | --- | --- |
| D1 | Cómo se informan los errores de contenido del CSV | A) Resultado tipado `LoadResult` con `CsvError`. B) Excepciones propias | **A** | RF-01 y CA-02 piden un error claro con la línea; un resultado tipado se prueba sin mensajes. Las excepciones quedan para I/O |
| D2 | Qué columnas se validan como numéricas y se guardan | A) Las 12 columnas, y se guardan las 12 en `ContractQuote`. B) Solo las usadas (`Time`, `StrikePrice`, `Bid`, `Ask`) | **A** | RF-01 pide validar las 12; futuras gráficas usarán Delta/IV (RNF-05). Contra: un valor basura en una columna no usada bloquea la carga |
| D3 | Control de T0 | A) `ComboBox` con todas las `Time` de la sesión; si el contrato no está, `ContractAbsentAtT0` | **A** | Compatible con RF-04 y CA-06 (ver A3). B) `DateTimePicker` libre exige además `T0NotASnapshot` |
| D4 | Disposición de las gráficas | A) Dos paneles: precio (Ask, Bid, entrada) arriba y PnL en $ abajo con el % en un eje derecho. B) Tres paneles | **A** | RF-05 pide ambos PnL en la misma gráfica 1. PnL% es solo PnL$ escalado, así que cabe en un eje secundario. Eje secundario de ScottPlot **sin verificar**; si falla, B |
| D5 | Origen del tipo Call/Put | A) Del nombre del archivo; si no se puede, se pregunta al usuario. B) Siempre lo elige el usuario | **A** | El esquema del CSV no trae el tipo; el patrón de nombre de la spec sí (`_Call_` / `_Put_`) |
| D6 | Tipo numérico para precios | A) `decimal`. B) `double` | **A** | CA-04 exige −11 $ y −3,67 % exactos; con `double`, 2,89 × 100 − 3 × 100 puede dar −11,000000000000028 |
| D7 | Orden y duplicados | A) Error `OutOfOrder` / `DuplicateStrike` con línea. B) Ordenar y tolerar | **A** | La spec dice que las filas "vienen ordenadas" pero no dice qué pasa si no; ordenar en silencio ocultaría un problema de la otra app |
| D8 | Tooltip sobre un hueco | A) Mostrar "hora — sin dato" del snapshot más cercano en X. B) Saltar al punto más cercano que tenga dato | **A** | RF-07 pide el snapshot más cercano en X y RF-06 dice que el hueco es ausencia de dato; B muestra una hora distinta de donde está el cursor |

### 8.2 Ambigüedades o contradicciones de la spec (no resueltas en silencio)

- **A1. RF-07 y huecos:** "el snapshot más cercano" no dice qué mostrar si ese snapshot no tiene el contrato. → D8.
- **A2. RF-10:** la spec no dice qué muestra la app (listas vacías, sin gráfica). Diseño: el aviso no es un error; las listas quedan vacías y no se dibuja. Hay que confirmarlo.
- **A3. RF-04 frente a CA-06:** RF-04 dice "elegir T0 entre los snapshots en los que el contrato existe" y a la vez prevé avisar si no existe; CA-06 exige poder elegir un T0 donde no existe. Parecen contradictorios si el selector solo ofrece los válidos. → D3.
- **A4. Tipo de contrato y fecha del nombre:** "Entradas" lista Tipo (Call o Put) como entrada, pero cada archivo es de un solo tipo y el CSV no lo trae (→ D5). Además el nombre tiene dos fechas (`<fecha>_<fecha>`) y la spec solo dice que "la fecha se toma del nombre"; el diseño no la usa en v1.0 porque `Time` no la necesita. Confirmar que no hace falta mostrarla.
- **A5. "Valor no numérico" y "archivo vacío":** la spec no dice qué columnas cuentan (→ D2), si una celda vacía es no numérica (diseño: sí), si `Time` mal formado cuenta (diseño: sí), si "vacío" incluye solo la cabecera (diseño: sí), ni si se informa solo el primer error o todos (diseño: el primero; CA-02 habla de "la línea afectada" en singular).
- **A6. Orden y duplicados:** la spec afirma que las filas están ordenadas pero no cubre el caso contrario ni los strikes repetidos. → D7.
- **A7. Eje fijo y zoom:** la spec fija el eje de 09:30 a 16:00; no dice si el usuario puede hacer zoom o desplazarse. El diseño lo deshabilita en v1.0 **(API sin verificar)**. Confirmar.
- **A8. Cuenta de la spec desactualizada:** en "Decisiones, supuestos y respuestas", la validación de columnas y el tooltip aparecen como "supuestos confirmados … propuestas que no se han pedido" y de nuevo como propuestas aceptadas; además RNF-06 dice que la librería "se decide en diseño", pero ya está aprobada en la propuesta. No afecta al diseño; conviene limpiar la spec.
- **A9. Límite de RF-10:** solo avisa si ningún snapshot cae en horario. Un archivo desfasado una hora que aún tenga algún snapshot en horario no se detecta. Es una limitación consciente de la spec, no se aborda aquí.
