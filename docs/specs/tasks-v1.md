# Tareas v1.0 — Visor de valorización de opciones

5 de noviembre de 2026 · Fase TASKS · Base: `docs/specs/spec-v1.md`, `docs/specs/proposal-v1.md` y `docs/specs/design-v1.md` (aprobados). Alcance: MVP v1.0, solo offline.

## Convenciones

- **Una tarea = un commit.** Los mensajes siguen el estilo del historial (inglés, imperativo, una línea). Al commitear se añade la línea de coautoría que corresponda.
- **Prueba primero:** se escribe la prueba, se ve fallar (rojo) y luego se implementa hasta ponerla en verde. Nombres de prueba: `Metodo_Condicion_Resultado`. Cada prueba de un criterio lleva `[Trait("CA", "CA-xx")]` (y `[Trait("RF", "RF-xx")]` si aplica); T-5.3 usa esos rasgos para comprobar CA-11.
- **Comando de verificación:** `dotnet test` en la raíz de la solución, salvo que la tarea diga otra cosa.
- **Filtro por criterio con MTP:** con este runner, `dotnet test --filter "CA=CA-xx"` sin pruebas coincidentes sale con código 8 (fallo).
  Por eso esas verificaciones solo valen cuando ya existan pruebas con ese rasgo.
- **Tamaños:** S (menos de 30 min) o M (30 a 90 min). No hay tareas L.
- **"Cubre"** indica lo que la tarea aporta; la tabla de trazabilidad del final dice qué tareas cierran cada requisito.

## Versiones verificadas (api.nuget.org, 5-nov-2026)

| Paquete | Versión exacta | Publicada | Nota |
| --- | --- | --- | --- |
| `ScottPlot.WinForms` | 5.1.59 | 22-jun-2026 | Última de la serie 5. Targets: `net10.0-windows7.0`, `net8.0-windows7.0`, `net462`. Depende de `ScottPlot` 5.1.59 y `SkiaSharp.Views.WindowsForms` 3.119.0 |
| `xunit.v3` | 4.0.2 | 9-oct-2026 | Última estable (4.0.0 y 4.0.1 anteriores). Target `net8.0`, utilizable desde net10. **La propuesta citaba 3.2.2; ver duda 1** |
| `xunit.runner.visualstudio` | 4.0.1 | 9-oct-2026 | Última estable. Compatibilidad con `xunit.v3` 4.0.2: sin verificar hasta T-1.5 |
| `Microsoft.NET.Test.Sdk` | 18.11.0 | 13-oct-2026 | Última estable |
| SDK | 10.0.202 | — | Instalado junto con 9.0.312 y 9.0.313 |

Sin verificar: cómo se comporta `dotnet test` con xUnit v3 en el SDK 10 (modo VSTest o Microsoft.Testing.Platform). T-1.5 lo comprueba.

---

## Etapa 1 — Esqueleto y spike de ScottPlot

### T-1.1 Fijar el SDK e ignorar archivos de .NET
- **Qué se hace:** crear `global.json` con el SDK `10.0.202` (`rollForward: latestPatch`, sin prerelease) y un `.gitignore` de .NET (`bin/`, `obj/`, `.vs/`, `*.user`, `TestResults/`) más `artifacts/` para los archivos generados.
- **Cubre:** RNF-04 (parcial: base de .NET).
- **Prueba primero:** sin prueba automática: son archivos de configuración.
- **Depende de:** —
- **Hecho cuando:** `dotnet --version` en la raíz imprime `10.0.202` y `git status` no lista carpetas `bin/` ni `obj/`.
- **Tamaño:** S
- **Commit:** `Pin SDK 10.0.202 and add .NET gitignore`

### T-1.2 Escribir CLAUDE.md con las reglas del proyecto
- **Qué se hace:** crear un `CLAUDE.md` corto (menos de una página) con: el núcleo (`OptionsViewer.Core`) no depende de WinForms ni de ScottPlot; `Time` es `TimeOnly` sin zona ni conversión; precios en `decimal`; no se supone cadencia fija entre snapshots (todo se calcula con la `Time` de cada snapshot); resultados tipados en vez de excepciones para errores de contenido; convención de pruebas y `Trait`; comando `dotnet test`; los CSV de `samples/` son de solo lectura.
- **Cubre:** RNF-03, RNF-05, RNF-10 (reglas), D1 y D6 del diseño.
- **Prueba primero:** sin prueba automática: es documentación. La regla de dependencias se protege con una prueba en T-1.5.
- **Depende de:** T-1.1
- **Hecho cuando:** el archivo existe, cabe en una página y cita las cuatro reglas pedidas.
- **Tamaño:** S
- **Commit:** `Add CLAUDE.md with project rules`

### T-1.3 Fijar versiones y opciones comunes de compilación
- **Qué se hace:** crear `Directory.Build.props` (`Nullable` activado, `ImplicitUsings`, `LangVersion` por defecto del SDK, `TreatWarningsAsErrors`) y `Directory.Packages.props` con gestión central de paquetes y las versiones exactas de la tabla de arriba.
- **Cubre:** RNF-06 (versión de ScottPlot fijada), RNF-08 (base de pruebas).
- **Prueba primero:** sin prueba automática: configuración; la comprueba la compilación de T-1.4.
- **Depende de:** T-1.1
- **Hecho cuando:** ambos archivos existen y listan `ScottPlot.WinForms` 5.1.59, `xunit.v3` 4.0.2, `xunit.runner.visualstudio` 4.0.1 y `Microsoft.NET.Test.Sdk` 18.11.0 sin rangos.
- **Tamaño:** S
- **Commit:** `Add central package versions and common build props`

### T-1.4 Crear la solución y los 4 proyectos
- **Qué se hace:** crear la solución en la raíz y, bajo `src/` y `tests/`, `OptionsViewer.Core` (`net10.0`, sin referencias), `OptionsViewer.Infrastructure` (`net10.0`, → Core), `OptionsViewer.WinForms` (`net10.0-windows`, `UseWindowsForms`, → Core, Infrastructure, `ScottPlot.WinForms`) y `OptionsViewer.Tests` (`net10.0`, → Core, Infrastructure, xUnit v3). Core e Infrastructure no llevan el sufijo `-windows`.
- **Cubre:** RNF-04, RNF-05.
- **Prueba primero:** sin prueba automática: estructura de proyectos; la guarda de dependencias se escribe en T-1.5.
- **Depende de:** T-1.3
- **Hecho cuando:** `dotnet build` termina sin errores ni advertencias y el grafo de referencias coincide con la propuesta (WinForms → Core, Infrastructure; Infrastructure → Core; Tests → Core, Infrastructure).
- **Tamaño:** M
- **Commit:** `Add solution with Core, Infrastructure, WinForms and Tests projects`

### T-1.5 Añadir prueba de humo y guarda de arquitectura
- **Qué se hace:** escribir en `OptionsViewer.Tests` una prueba de humo y una prueba que lee las referencias de ensamblados de Core e Infrastructure. Con esto se confirma que `dotnet test` descubre y ejecuta xUnit v3 en el SDK 10 con el runner fijado.
- **Cubre:** RNF-05, RNF-08.
- **Prueba primero:** `Smoke_Suite_Runs` (siempre verde, confirma el descubrimiento) y `Core_NoReferenciaWinFormsNiScottPlot` / `Infrastructure_NoReferenciaWinFormsNiScottPlot` (comprueban que ningún ensamblado referenciado empieza por `System.Windows.Forms` o `ScottPlot`).
- **Depende de:** T-1.4
- **Hecho cuando:** `dotnet test` descubre y pasa las 3 pruebas. Si `dotnet test` no ejecuta xUnit v3 con la configuración por defecto, se registra qué ajuste hizo falta (p. ej. el modo del runner en `global.json`) en `CLAUDE.md`.
- **Tamaño:** S
- **Commit:** `Add smoke test and architecture guard tests`

### T-1.6 Spike: comprobar que NaN corta la línea
- **Qué se hace:** crear `spikes/ScottPlotSpike` (proyecto desechable `net10.0-windows`, fuera de la solución) que dibuja un `Scatter` con un `double.NaN` en medio, genera la imagen sin mostrar ventana y comprueba que no hay píxeles de línea en la columna del `NaN`. Además, instancia un `FormsPlot` dentro de un `Form` real (se crea el handle del control y se fuerza un dibujo, p. ej. mostrando el formulario un instante o renderizándolo a un bitmap) y comprueba que se crea y se dibuja sin excepciones. Esto valida en tiempo de ejecución el `SkiaSharp.Views.WindowsForms` 3.119.0 (y OpenTK) que dispara el aviso NU1701 silenciado en `OptionsViewer.WinForms.csproj`. Se registra el resultado.
- **Cubre:** RF-06, RNF-06 (riesgo de la sección 6 del diseño y compatibilidad en ejecución de las dependencias de ScottPlot.WinForms con NU1701).
- **Prueba primero:** la propia comprobación del spike es la prueba: `Spike_NaN_CortaLaLinea` y `Spike_FormsPlot_SeCreaYSeDibujaEnUnFormReal` (desechables, se eliminan en T-1.9). Si `Spike_NaN_CortaLaLinea` falla, la alternativa del diseño: partir la serie en segmentos de puntos consecutivos con dato y añadir una serie por segmento; se anota en `docs/specs/spike-scottplot.md`. Si `Spike_FormsPlot_SeCreaYSeDibujaEnUnFormReal` falla (excepción al crear o dibujar el control), se registra en el mismo documento y **se detiene la ejecución y se consulta al usuario**; no se elige alternativa sin su decisión.
- **Depende de:** T-1.4
- **Hecho cuando:** el spike compila, las dos comprobaciones dan resultado (pasa o falla) y el resultado y, si fallan, la alternativa elegida (o la consulta pendiente al usuario, en el caso de `FormsPlot`) quedan escritos en `docs/specs/spike-scottplot.md`.
- **Tamaño:** M
- **Commit:** `Spike: verify ScottPlot NaN breaks the line`

### T-1.7 Spike: comprobar que existe un eje secundario
- **Qué se hace:** en el mismo proyecto de spike, añadir un eje Y derecho con una escala distinta (PnL% = PnL$ × 100 / entrada), asociar una serie a ese eje y comprobar que se dibuja con su propia escala. Registrar el nombre real de la API.
- **Cubre:** RF-05, D4 del diseño.
- **Prueba primero:** `Spike_EjeSecundario_EscalaPropia` (desechable). Si falla, la alternativa del diseño (D4, opción B): tres paneles, uno por magnitud (precio, PnL $, PnL %).
- **Depende de:** T-1.6
- **Hecho cuando:** `docs/specs/spike-scottplot.md` dice si el eje secundario existe, con la API a usar, o queda decidido el uso de tres paneles.
- **Tamaño:** M
- **Commit:** `Spike: verify ScottPlot secondary axis`

### T-1.8 Spike: comprobar el eje X fijo de 09:30 a 16:00 sin zoom
- **Qué se hace:** fijar los límites X en 34.200 y 57.600 segundos, rotular el eje como `HH:mm`, desactivar zoom y desplazamiento con el ratón, y probar la conversión de píxel a coordenadas (`GetCoordinates`) que usará el tooltip. El diseño no define alternativa si el control no se puede bloquear.
- **Cubre:** RF-06, RF-07, spec "sin zoom en la v1".
- **Prueba primero:** `Spike_EjeX_FijoSinZoom` (desechable): tras simular rueda y arrastre, los límites siguen siendo 34.200–57.600. Si falla, se anota el hallazgo y se pregunta (duda 3) antes de la etapa 6.
- **Depende de:** T-1.6
- **Hecho cuando:** `docs/specs/spike-scottplot.md` recoge cómo se fija el eje, cómo se desactiva el zoom, cómo se rotula en `HH:mm` y el nombre real de la conversión píxel→coordenadas.
- **Tamaño:** M
- **Commit:** `Spike: verify fixed X axis without zoom`

### T-1.9 Registrar conclusiones y retirar el spike
- **Qué se hace:** completar `docs/specs/spike-scottplot.md` con una tabla de resultado y la decisión (ScottPlot tal cual o alternativa) para cada comprobación, y eliminar `spikes/ScottPlotSpike`.
- **Cubre:** RNF-06 (cierre del riesgo).
- **Prueba primero:** sin prueba automática: documentación y eliminación de código desechable.
- **Depende de:** T-1.7, T-1.8
- **Hecho cuando:** `spikes/` ya no existe, `dotnet build` y `dotnet test` siguen en verde y el documento tiene las 3 filas (NaN, eje secundario, eje fijo sin zoom) con resultado y decisión.
- **Tamaño:** S
- **Commit:** `Record ScottPlot spike conclusions and remove spike`

---

## Etapa 2 — Lector CSV

### T-2.1 Declarar los tipos del núcleo para la lectura
- **Qué se hace:** en Core, declarar `OptionType`, `ContractKey`, `ContractQuote` (12 campos `decimal`), `Snapshot` (`TimeOnly Time`, `IReadOnlyDictionary<decimal, ContractQuote> QuotesByStrike`), `SnapshotSet`, `ISnapshotSource`, `LoadResult`, `CsvError` y `CsvErrorKind`, tal como están en design-v1.md §1, §2.1 y §5.
- **Cubre:** RF-01 (tipos), RNF-03 (`TimeOnly`), RNF-05, D1, D2, D6.
- **Prueba primero:** sin prueba automática: solo declaraciones de tipos; las usan las pruebas de T-2.2 a T-2.7.
- **Depende de:** T-1.5
- **Hecho cuando:** `dotnet build` en verde y `Core_NoReferenciaWinFormsNiScottPlot` sigue pasando.
- **Tamaño:** M
- **Commit:** `Add core domain types and CSV source port`

### T-2.2 Inferir Call o Put del nombre de archivo
- **Qué se hace:** `ChainFileName.TryGetOptionType(string fileName)` en Core: busca `_Call_` o `_Put_` sin distinguir mayúsculas, con o sin `<id>` inicial; devuelve `null` si no encuentra ninguno.
- **Cubre:** D5; "Archivos" de la spec (patrón de nombre).
- **Prueba primero:** `TryGetOptionType_NombreConIdYCall_DevuelveCall`, `TryGetOptionType_SinIdYPut_DevuelvePut`, `TryGetOptionType_Minusculas_Reconoce`, `TryGetOptionType_SinTipo_DevuelveNull` (usa los nombres reales de `samples/`).
- **Depende de:** T-2.1
- **Hecho cuando:** `dotnet test` pasa las 4 pruebas.
- **Tamaño:** S
- **Commit:** `Add ChainFileName option type detection`

### T-2.3 Preparar utilidades de prueba: CSV de referencia y CSV sintético
- **Qué se hace:** en Tests, enlazar `samples/*.csv` como contenido copiado a la salida (sin duplicarlos), añadir `SamplePaths` y un constructor de CSV sintéticos (`SyntheticCsv`) que arma texto con cabecera, orden de columnas, CRLF o LF y filas dados.
- **Cubre:** RNF-08.
- **Prueba primero:** `SamplePaths_ArchivosDeReferenciaExisten` y `SyntheticCsv_Construye_CabeceraYFilasConCrlf`.
- **Depende de:** T-1.5
- **Hecho cuando:** `dotnet test` pasa ambas pruebas y `git status` no muestra copias de los CSV dentro de `tests/`.
- **Tamaño:** S
- **Commit:** `Add test helpers for reference and synthetic CSV`

### T-2.4 Leer y validar la cabecera por nombre
- **Qué se hace:** en Infrastructure, un lector de cabecera interno que mapea nombre de columna → índice sin distinguir mayúsculas, acepta cualquier orden e ignora columnas de más. Devuelve `CsvError` `MissingColumns` (con los nombres que faltan) o `DuplicateColumn`, siempre con `Line = 1`.
- **Cubre:** RF-01, CA-02 (parcial).
- **Prueba primero:** `Parse_Cabecera12Columnas_MapeaTodas`, `Parse_OtroOrdenYMinusculas_Acepta`, `Parse_ColumnaDeMas_Ignora`, `Parse_FaltaAsk_MissingColumnsLinea1`, `Parse_ColumnaRepetida_DuplicateColumn`.
- **Depende de:** T-2.1, T-2.3
- **Hecho cuando:** `dotnet test` pasa las 5 pruebas.
- **Tamaño:** M
- **Commit:** `Add CSV header parsing with column validation`

### T-2.5 Convertir una fila a ContractQuote con valores validados
- **Qué se hace:** en Infrastructure, convertir una fila (texto → `TimeOnly` en `HH:MM:SS` y 12 `decimal`) con `InvariantCulture`. Errores: `InvalidValue` con `Line`, `Column` y `RawValue`; `MalformedRow` si faltan campos. Se valida cada una de las 12 columnas (D2).
- **Cubre:** RF-01, CA-02 (parcial), CA-01 (parcial: lectura de `IV` con CRLF), RNF-09, D2, D6.
- **Prueba primero:** `Parse_FilaValida_ConvierteDecimalesConPunto` (bajo cultura `es-ES`), `Parse_IVConCRFinal_LeeNumero`, `Parse_IVMenos999_LeeNumero`, `Parse_ValorNoNumerico_InvalidValueConColumnaYLinea`, `Parse_CeldaVacia_InvalidValue`, `Parse_TimeMalFormado_InvalidValue`, `Parse_MenosCampos_MalformedRow`.
- **Depende de:** T-2.4
- **Hecho cuando:** `dotnet test` pasa las 7 pruebas, incluida la ejecutada bajo `es-ES`.
- **Tamaño:** M
- **Commit:** `Add CSV row parsing with typed errors`

### T-2.6 Agrupar filas en snapshots y validar orden y duplicados
- **Qué se hace:** en Infrastructure, agrupar filas consecutivas con la misma `Time` en un `Snapshot`; error `OutOfOrder` si una `Time` es menor que la anterior y `DuplicateStrike` si un strike se repite en el mismo snapshot, ambos con número de línea. No usa ninguna diferencia de tiempo entre snapshots.
- **Cubre:** RF-02 (agrupación), RNF-10, D7.
- **Prueba primero:** `Group_FilasConMismaTime_UnSoloSnapshot`, `Group_TimesDistintas_SnapshotsSeparados`, `Group_TimeRetrocede_OutOfOrderConLinea`, `Group_StrikeRepetido_DuplicateStrikeConLinea`, `Group_SaltosDe4_60_181Segundos_NoAlteraLaAgrupacion`.
- **Depende de:** T-2.5
- **Hecho cuando:** `dotnet test` pasa las 5 pruebas.
- **Tamaño:** M
- **Commit:** `Group CSV rows into snapshots with order checks`

### T-2.7 Implementar CsvFileSnapshotSource
- **Qué se hace:** `CsvFileSnapshotSource(string path, OptionType type)` implementa `ISnapshotSource.Load(CancellationToken)`: abre con `FileAccess.Read` y `FileShare.ReadWrite`, lee línea a línea (CRLF o LF), usa T-2.4 a T-2.6 y devuelve `LoadResult`. 0 bytes o solo cabecera → `EmptyFile`. Los errores de entorno se lanzan como excepciones; la cancelación lanza `OperationCanceledException`.
- **Cubre:** RF-01, CA-02, CA-01 (parcial), RNF-01, RNF-08, RNF-09, D1.
- **Prueba primero:** `Load_ArchivoDe0Bytes_EmptyFile`, `Load_SoloCabecera_EmptyFileLinea1`, `Load_ColumnasReordenadasYExtra_Exito` (CA-02), `Load_FaltaUnaColumna_ErrorYSinValor` (CA-02), `Load_CrLfYLf_MismoResultado`, `Load_OtroProcesoEscribiendo_NoFallaNiBloquea` (RNF-01), `Load_TokenCancelado_LanzaOperationCanceled`, `Load_CallsReferencia_Devuelve1615SnapshotsSinFiltrar`.
- **Depende de:** T-2.6
- **Hecho cuando:** `dotnet test` pasa las 8 pruebas y el archivo de Calls de referencia carga con 1.615 snapshots y la `IV` de la última fila leída como número.
- **Tamaño:** M
- **Commit:** `Add CsvFileSnapshotSource with read-only shared access`

---

## Etapa 3 — Snapshots y horario

### T-3.1 Decidir si una hora está en horario
- **Qué se hace:** en Core, `MarketHours.Open` (09:30:00), `MarketHours.Close` (16:00:00) y `MarketHours.IsInSession(TimeOnly)`, con comparación inclusiva en ambos extremos y sin conversión de zona.
- **Cubre:** RF-02 (parcial), CA-09, RNF-03.
- **Prueba primero:** `IsInSession_09_29_59_Excluye`, `IsInSession_09_30_00_Incluye`, `IsInSession_16_00_00_Incluye`, `IsInSession_16_00_01_Excluye` (CA-09).
- **Depende de:** T-2.1
- **Hecho cuando:** `dotnet test --filter "CA=CA-09"` pasa las 4 pruebas.
- **Tamaño:** S
- **Commit:** `Add MarketHours session check`

### T-3.2 Filtrar el SnapshotSet y avisar si queda vacío
- **Qué se hace:** declarar `TradingSession` y `MarketHours.Apply(SnapshotSet)`: conserva los snapshots dentro de horario, cuenta los descartados y pone `NoSnapshotsInWindow = true` si no queda ninguno (aviso, no error).
- **Cubre:** RF-02, RF-10, CA-12 (núcleo), CA-09.
- **Prueba primero:** `Apply_MezclaDentroYFuera_ConservaYCuentaDescartados`, `Apply_TodosFuera_NoSnapshotsInWindow` (CA-12: horas 08:00 a 08:59), `Apply_SnapshotEn1600_Conserva`, `Apply_ConservaElOrden`.
- **Depende de:** T-3.1
- **Hecho cuando:** `dotnet test` pasa las 4 pruebas y `NoSnapshotsInWindow` es `true` solo cuando no queda ningún snapshot.
- **Tamaño:** S
- **Commit:** `Add MarketHours.Apply and TradingSession`

### T-3.3 Verificar el conteo con los CSV de referencia
- **Qué se hace:** prueba de integración (Infrastructure + Core) que carga Calls y Puts de `samples/` con `CsvFileSnapshotSource` y aplica `MarketHours.Apply`.
- **Cubre:** CA-01, RF-02, RNF-08.
- **Prueba primero:** `Referencia_Calls_1613DentroYDescarta2` y `Referencia_Puts_1613DentroYDescarta2` (los descartados son 08:24:27 y 18:24:35).
- **Depende de:** T-2.7, T-3.2
- **Hecho cuando:** `dotnet test --filter "CA=CA-01"` pasa y las dos horas descartadas son las de la spec.
- **Tamaño:** S
- **Commit:** `Test reference CSV snapshot counts (CA-01)`

---

## Etapa 4 — Strikes y serie del contrato

### T-4.1 Listar los strikes de la sesión
- **Qué se hace:** `StrikeCatalog.List(TradingSession)` en Core: strikes distintos, ordenados de menor a mayor; lista vacía si la sesión está vacía.
- **Cubre:** RF-03, CA-03.
- **Prueba primero:** `List_CallsReferencia_16StrikesDe330A367_5` y `List_PutsReferencia_16StrikesDe327_5A365` (CA-03, ordenados), `List_SesionVacia_ListaVacia`.
- **Depende de:** T-3.3
- **Hecho cuando:** `dotnet test --filter "CA=CA-03"` pasa.
- **Tamaño:** S
- **Commit:** `Add StrikeCatalog.List`

### T-4.2 Declarar los tipos de la serie
- **Qué se hace:** en Core, declarar `ContractSeries`, `SeriesPoint` (con `SecondsOfDay`), `PricePoint`, `SeriesResult`, `SeriesErrorKind` y `SeriesError` según design-v1.md §1 y §2.3. `Value == null` representa el hueco; `PnlPercent == null` representa n/d.
- **Cubre:** RF-06 (representación del hueco), RF-05 (tipos).
- **Prueba primero:** `SeriesPoint_SecondsOfDay_ConvierteLaHoraEnSegundosDesdeMedianoche` (09:30:00 → 34.200; 16:00:00 → 57.600).
- **Depende de:** T-2.1
- **Hecho cuando:** `dotnet test` pasa la prueba y `dotnet build` sin advertencias.
- **Tamaño:** S
- **Commit:** `Add valuation series types`

### T-4.3 Construir la serie: errores de entrada
- **Qué se hace:** `ValuationSeries.Build(TradingSession, ContractKey, TimeOnly)` en Core, solo la parte de validación: `EmptySession`, `T0NotASnapshot`, `ContractAbsentAtT0`; un tipo de contrato distinto del de la sesión lanza `ArgumentException`.
- **Cubre:** RF-04, CA-06.
- **Prueba primero:** `Build_SesionVacia_EmptySession`, `Build_T0QueNoEsSnapshot_T0NotASnapshot`, `Build_ContratoAusenteEnT0_ContractAbsentAtT0` (CA-06), `Build_TipoDistinto_LanzaArgumentException`.
- **Depende de:** T-4.2, T-3.2
- **Hecho cuando:** `dotnet test` pasa las 4 pruebas y en los tres casos de error `Value` es `null`.
- **Tamaño:** S
- **Commit:** `Add series builder input validation (CA-06)`

### T-4.4 Construir la serie: entrada, Bid y PnL
- **Qué se hace:** completar `Build` con el caso con datos: `EntryAskPerContract = Ask(T0) × 100`; un `PricePoint` por snapshot desde T0 con `AskPerContract`, `BidPerContract`, `PnlDollars = Bid_c − Ask_c(T0)` y `PnlPercent = PnlDollars / Ask_c(T0) × 100`, sin redondear, en `decimal`.
- **Cubre:** RF-05, CA-04, D6.
- **Prueba primero:** `Build_Calls340T0_0930_EntradaBidYPnlDelEjemplo` (CA-04: entrada 300, Bid 289, PnL −11, PnL% = −3,67 al redondear a 2 decimales), `Build_PrimerPunto_EsT0`, `Build_PuntosDesdeT0HastaElUltimoSnapshot`, `Build_ValoresSonExactosEnDecimal`.
- **Depende de:** T-4.3
- **Hecho cuando:** `dotnet test --filter "CA=CA-04"` pasa.
- **Tamaño:** M
- **Commit:** `Compute entry price and PnL in series builder (CA-04)`

### T-4.5 Construir la serie: huecos por ausencia, no por tiempo
- **Qué se hace:** en `Build`, cuando el contrato falta en un snapshot de la sesión se emite un `SeriesPoint` con `Value = null`. La regla recorre por posición de snapshot y no resta `Time` en ningún punto.
- **Cubre:** RF-06, CA-05, RNF-10.
- **Prueba primero:** `Build_Calls337_5_T0_094449_NueveHuecosYVuelveA094529` (CA-05, caso real), `Build_ContratoEnSnapshotsConsecutivosSeparados181s_SinHueco`, `Build_ContratoEnSnapshotsConsecutivosSeparados60s_SinHueco`, `Build_CambioDeCadenciaEnLaSerie_NoCreaHuecos` (sintético).
- **Depende de:** T-4.4
- **Hecho cuando:** `dotnet test --filter "CA=CA-05"` pasa y el caso real da 9 puntos con `Value == null` entre 09:44:49 y 09:45:29.
- **Tamaño:** M
- **Commit:** `Add gap handling by missing contract only (CA-05)`

### T-4.6 Manejar Ask(T0) = 0 como n/d
- **Qué se hace:** en `Build`, si `Ask(T0)` es 0, `PercentAvailable = false` y todos los `PnlPercent` son `null`; el PnL en $ se calcula igual y no hay división por cero.
- **Cubre:** RF-05, regla "n/d" de la spec.
- **Prueba primero:** `Build_AskT0Cero_PercentAvailableFalse`, `Build_AskT0Cero_PnlPercentNulosEnTodosLosPuntos`, `Build_AskT0Cero_PnlDolaresSeCalcula` (CSV sintético, porque en los de referencia Ask nunca es 0).
- **Depende de:** T-4.4
- **Hecho cuando:** `dotnet test` pasa las 3 pruebas.
- **Tamaño:** S
- **Commit:** `Handle zero entry ask as n/d`

---

## Etapa 5 — Cadencia variable y cierre de CA-11

### T-5.1 Generar CSV sintéticos con cadencia configurable
- **Qué se hace:** ampliar `SyntheticCsv` con un generador por tramos (intervalo en segundos hasta una hora de corte, otro intervalo después), con un contrato presente en todos los snapshots y otro con un hueco definido.
- **Cubre:** RNF-10, RNF-08.
- **Prueba primero:** `Generador_6sHasta11_1minDespues_ProduceLosSnapshotsEsperados` (cuenta y primeras y últimas horas de cada tramo).
- **Depende de:** T-2.3
- **Hecho cuando:** `dotnet test` pasa la prueba y el cálculo esperado de snapshots del generador está en la propia prueba.
- **Tamaño:** S
- **Commit:** `Add cadence-configurable synthetic CSV generator`

### T-5.2 Verificar el procesamiento con cadencia variable
- **Qué se hace:** prueba de extremo a extremo (carga, filtro, strikes y serie) con un CSV sintético de un snapshot cada 6 s hasta las 11:00 y uno por minuto después; se repite con otras cadencias para demostrar que no hay ningún intervalo fijo en el código.
- **Cubre:** CA-13, RNF-10, RF-06.
- **Prueba primero:** `Cadencia6sY1min_CargaIgualQueLaReferencia` (mismo filtro y conteos esperados), `Cadencia6sY1min_SinCortesFalsosEnLaLinea`, `Cadencia6sY1min_SecondsOfDayCoincideConLaHoraReal`, y como `Theory`: `OtrasCadencias_MismoComportamiento` (4 s, 5 s, 15 s antes de las 11:00).
- **Depende de:** T-5.1, T-4.5
- **Hecho cuando:** `dotnet test --filter "CA=CA-13"` pasa todas las pruebas.
- **Tamaño:** M
- **Commit:** `Test variable cadence end to end (CA-13)`

### T-5.3 Comprobar con una prueba que CA-11 se cumple
- **Qué se hace:** una prueba en Tests que, por reflexión, recorre las pruebas del ensamblado y comprueba que existen pruebas con `Trait("CA", ...)` para CA-01, CA-03, CA-04, CA-05, CA-09 y CA-13.
- **Cubre:** CA-11.
- **Prueba primero:** `CriteriosObligatorios_TienenPruebasAutomaticas` (falla si falta alguno de los seis; se ve fallar quitando temporalmente un `Trait`).
- **Depende de:** T-3.3, T-4.1, T-4.4, T-4.5, T-3.1, T-5.2
- **Hecho cuando:** `dotnet test` pasa completo y sin interfaz.
- **Tamaño:** S
- **Commit:** `Add guard test for CA-11 required criteria`

---

## Etapa 6 — Interfaz y gráfica

Las pruebas de esta etapa solo cubren la lógica pura que vive en Core; la ventana en sí no tiene pruebas automáticas (el proyecto de pruebas no referencia WinForms) y se verifica con los pasos manuales de la etapa 7. Las tareas T-6.1 y T-6.2 añaden dos ayudantes puros a Core que el diseño no nombra (ver duda 2).

### T-6.1 Convertir la serie en arreglos para la gráfica
- **Qué se hace:** en Core, `ChartSeriesData.From(ContractSeries)` devuelve arreglos `double[]` de X (`SecondsOfDay`), Ask, Bid, PnL $ y PnL %, con `double.NaN` donde `Value == null` y en todo el PnL % si `PercentAvailable` es falso.
- **Cubre:** RF-05, RF-06, CA-05 (lado interfaz).
- **Prueba primero:** `From_PuntoConHueco_YNaN`, `From_X_EsSecondsOfDay`, `From_AskCero_PnlPercentTodoNaN`, `From_ValoresPorContratoSinRedondear`.
- **Depende de:** T-4.6
- **Hecho cuando:** `dotnet test` pasa las 4 pruebas.
- **Tamaño:** S
- **Commit:** `Add chart data mapping for valuation series`

### T-6.2 Encontrar el punto más cercano en el eje X
- **Qué se hace:** en Core, `SeriesLookup.FindNearestIndex(double[] xs, double x)` con búsqueda binaria en el arreglo ordenado; devuelve el índice del snapshot más cercano, sea hueco o no.
- **Cubre:** RF-07, D8.
- **Prueba primero:** `FindNearestIndex_AntesDelPrimero_DevuelveCero`, `FindNearestIndex_DespuesDelUltimo_DevuelveUltimo`, `FindNearestIndex_EntreDosPuntos_ElMasCercano`, `FindNearestIndex_EmpateExacto_ElAnterior`, `FindNearestIndex_ArregloVacio_DevuelveMenosUno`, `FindNearestIndex_PuntoEnHueco_DevuelveElSnapshotDelHueco`.
- **Depende de:** T-6.1
- **Hecho cuando:** `dotnet test` pasa las 6 pruebas.
- **Tamaño:** S
- **Commit:** `Add nearest-point lookup for tooltip`

### T-6.3 Crear la ventana principal con sus controles
- **Qué se hace:** en WinForms, `MainForm` con el botón **Abrir CSV…**, etiquetas de estado, `ComboBox` de strike, `ComboBox` de T0, banner de aviso y dos `FormsPlot` (arriba precio, abajo PnL, según el resultado de T-1.9). `Program.cs` hace la composición. Sin lógica de negocio.
- **Cubre:** RNF-04, D3, D4.
- **Prueba primero:** sin prueba automática: solo disposición de controles; el tamaño y la disposición se verifican abriendo la app (paso 1 de T-7.2).
- **Depende de:** T-1.9
- **Hecho cuando:** `dotnet run --project src/OptionsViewer.WinForms` abre la ventana con todos los controles y sin errores.
- **Tamaño:** M
- **Commit:** `Add main form layout and composition root`

### T-6.4 Cargar el archivo en segundo plano y mostrar el resultado
- **Qué se hace:** al pulsar **Abrir CSV…**, elegir archivo, obtener el tipo con `ChainFileName` (si es `null`, preguntar Call o Put, D5), ejecutar `await Task.Run(() => source.Load(token))`, deshabilitar controles con "Cargando…", cancelar la carga anterior si se abre otro archivo y descartar resultados viejos. Con `CsvError`: mostrar el mensaje con la línea y limpiar gráficas. Con éxito: `MarketHours.Apply`, mostrar snapshots dentro y descartados y, si `NoSnapshotsInWindow`, el aviso de zona horaria. Las excepciones de entorno se muestran como mensaje.
- **Cubre:** RF-01, RF-02, RF-10, CA-01 (visible), CA-02 (interfaz), CA-12 (interfaz), RNF-07, CA-10 (implementación), D1, D5.
- **Prueba primero:** sin prueba automática: orquestación de la interfaz (hilos y controles), no lógica del núcleo; se verifica manualmente en T-7.1 y T-7.2.
- **Depende de:** T-6.3, T-3.3, T-2.2
- **Hecho cuando:** al abrir el CSV de Calls de referencia la ventana muestra "1.613 dentro / 2 descartados"; un CSV al que falta una columna muestra el error con su línea y deja las gráficas vacías.
- **Tamaño:** M
- **Commit:** `Load CSV asynchronously and show load results`

### T-6.5 Llenar strikes y T0, y construir la serie al elegir
- **Qué se hace:** llenar el combo de strikes con `StrikeCatalog.List` y el de T0 con las `Time` de todos los snapshots de la sesión (RF-04 actualizado). Cuando ambos están elegidos, llamar a `ValuationSeries.Build`; con `SeriesError` mostrar el aviso en el banner y limpiar las gráficas, con éxito entregar la serie a las gráficas.
- **Cubre:** RF-03, RF-04, CA-06 (interfaz), D3.
- **Prueba primero:** sin prueba automática: enlace de controles; la lógica ya está probada en T-4.1 a T-4.6. Se verifica en T-7.2.
- **Depende de:** T-6.4, T-4.3
- **Hecho cuando:** con Calls cargado, elegir strike 340 y T0 = 09:30:00 produce la serie; elegir un T0 donde el contrato no existe muestra el aviso y no dibuja.
- **Tamaño:** M
- **Commit:** `Wire strike and T0 selection to series builder`

### T-6.6 Dibujar la gráfica de precio con eje fijo
- **Qué se hace:** en el panel superior, dibujar Ask(t) y Bid(t) con los arreglos de `ChartSeriesData` y la línea horizontal en `EntryAskPerContract`; fijar el eje X de 09:30 a 16:00 (`Plot.Axes.SetLimitsX(34200, 57600)`), con rótulos mediante `NumericManual` cada 30 minutos de 09:30 a 16:00 (14 rótulos `HH:mm`) y bloquear el zoom y el desplazamiento con `FormsPlot.UserInputProcessor.Disable()` (según `docs/specs/spike-scottplot.md`); un refresco por cambio.
- **Cubre:** RF-05, RF-06, RNF-06, CA-05 (interfaz), CA-13 (eje).
- **Prueba primero:** sin prueba automática: renderizado; los datos que recibe ya están probados en T-6.1. Se verifica en T-7.2.
- **Depende de:** T-6.5, T-6.1, T-1.9
- **Hecho cuando:** con Calls 337,5 y T0 = 09:44:49 se ve la línea cortada en el hueco, el eje va de 09:30 a 16:00 y la rueda del ratón no cambia el eje.
- **Tamaño:** M
- **Commit:** `Draw price chart with fixed time axis`

### T-6.7 Dibujar la gráfica de PnL en $ y en %
- **Qué se hace:** en el panel inferior, dibujar el PnL en $ y el PnL en % con un eje derecho (`Plot.Axes.AddRightAxis()`), cuyos límites se derivan de los del eje izquierdo (`límite_$ × 100 / entrada`); con `PercentAvailable` falso, ocultar el % y mostrar "n/d".
- **Cubre:** RF-05, D4, regla n/d.
- **Prueba primero:** sin prueba automática: renderizado; la conversión está probada en T-6.1 y T-4.6.
- **Depende de:** T-6.6
- **Hecho cuando:** con strike 340 y T0 = 09:30:00 el primer punto muestra PnL −11 $ y −3,67 %, y un CSV sintético con Ask(T0) = 0 muestra "n/d".
- **Tamaño:** M
- **Commit:** `Draw PnL chart in dollars and percent`

### T-6.8 Mostrar el tooltip al pasar el cursor
- **Qué se hace:** suscribirse a `FormsPlot.MouseMove` de cada gráfica, convertir el píxel a X con `Plot.GetCoordinates(Pixel)` (API confirmada en el spike), usar `SeriesLookup.FindNearestIndex` y mostrar hora, Bid, Ask, PnL $ y PnL % (o "n/d"); sobre un hueco muestra "hora — sin dato" (D8). Marcar el punto y refrescar solo cuando cambia el punto más cercano.
- **Cubre:** RF-07, D8.
- **Prueba primero:** sin prueba automática: eventos del ratón; la búsqueda del punto está probada en T-6.2.
- **Depende de:** T-6.7, T-6.2
- **Hecho cuando:** pasar el cursor por el punto de las 09:30:00 en Calls 340 muestra Bid 289 $, Ask 300 $ y −11 $ (−3,67 %), y sobre un hueco muestra "sin dato".
- **Tamaño:** M
- **Commit:** `Add chart tooltip with nearest snapshot`

### T-6.9 Cambiar contrato, T0 o archivo sin reiniciar
- **Qué se hace:** al cargar otro archivo, limpiar combos, banner y gráficas; al cambiar strike o T0, reconstruir la serie sin recargar. Comprobar que cargas consecutivas (incluida una cancelada a medias) no dejan estado mezclado.
- **Cubre:** RF-09.
- **Prueba primero:** sin prueba automática: estado de la interfaz; se verifica en T-7.2.
- **Depende de:** T-6.8
- **Hecho cuando:** se abren Calls, luego Puts, luego Calls otra vez y se cambian strike y T0 varias veces sin reiniciar y sin que queden datos del archivo anterior.
- **Tamaño:** M
- **Commit:** `Reset state when switching contract, T0 or file`

---

## Etapa 7 — Revisión final

### T-7.1 Verificar CA-10 manualmente
- **Qué se hace:** añadir una prueba explícita (`[Fact(Explicit = true)]`) que genera en `artifacts/` un CSV sintético grande y válido (una snapshot por segundo en horario, unas 23.400 snapshots), y crear `docs/specs/verification-v1.md` con los pasos y su resultado. El archivo de referencia carga en menos de un segundo, por lo que el CSV grande hace observable la carga.
  1. `dotnet build -c Release` y ejecutar la app.
  2. Abrir el CSV de Calls de referencia y comprobar que la ventana muestra "Cargando…" y luego el resultado.
  3. Abrir el CSV grande y, mientras carga, mover la ventana, abrir el desplegable de strike y hacer clic en los controles deshabilitados.
  4. Pulsar **Abrir CSV…** otra vez durante la carga y comprobar que la primera se cancela y la segunda termina.
  5. Anotar la fecha, si la ventana respondió en cada paso y los tiempos de carga.
- **Cubre:** CA-10, RNF-07.
- **Prueba primero:** sin prueba automática: la capacidad de respuesta de la ventana no se puede comprobar sin interfaz. La prueba explícita solo genera el archivo.
- **Depende de:** T-6.9
- **Hecho cuando:** `docs/specs/verification-v1.md` tiene el resultado de los 5 pasos con "responde" en cada uno y los tiempos anotados.
- **Tamaño:** M
- **Commit:** `Add CA-10 manual verification and large CSV generator`

### T-7.2 Verificar manualmente lo que depende de la ventana
- **Qué se hace:** completar `docs/specs/verification-v1.md` con un guion de verificación de la interfaz: (1) la ventana abre con todos los controles; (2) CA-02: un CSV sin una columna muestra el error con la línea y no dibuja; (3) CA-06: elegir un T0 sin el contrato muestra el aviso y no dibuja; (4) CA-12: un CSV con horas 08:00 a 08:59 muestra el aviso de zona horaria; (5) CA-04 y RF-07: Calls 340 a las 09:30:00 y tooltip; (6) CA-05: Calls 337,5 desde 09:44:49 con la línea cortada; (7) RF-09: cambiar contrato, T0 y archivo sin reiniciar; (8) eje fijo sin zoom; (9) con el escalado de pantalla de Windows al 125 % y al 150 %, la gráfica, el tooltip y el eje siguen correctos; (10) el clic derecho y el teclado no alteran el eje.
- **Cubre:** CA-02, CA-06, CA-12 (interfaz), RF-05, RF-07, RF-09, RF-10.
- **Prueba primero:** sin prueba automática: verificación de interfaz.
- **Depende de:** T-7.1
- **Hecho cuando:** los 10 pasos del guion están marcados como correctos, con fecha, en `docs/specs/verification-v1.md`.
- **Tamaño:** M
- **Commit:** `Record manual UI verification for v1.0`

### T-7.3 Cerrar el MVP contra la trazabilidad
- **Qué se hace:** ejecutar `dotnet build -c Release` y `dotnet test`, comprobar fila por fila la tabla de trazabilidad de este documento y marcar el estado final de cada requisito.
- **Cubre:** todos los requisitos de la v1.0 (revisión).
- **Prueba primero:** sin prueba automática: es una revisión; su evidencia es la salida de `dotnet test` y la tabla.
- **Depende de:** T-7.2, T-5.3
- **Hecho cuando:** `dotnet build -c Release` sin advertencias, `dotnet test` en verde y la columna "Estado" de la tabla dice "cerrado" en cada fila de la v1.0.
- **Tamaño:** S
- **Commit:** `Close v1.0 traceability review`

---

## Resumen por etapa

| Etapa | Tareas | Tamaños |
| --- | --- | --- |
| 1. Esqueleto y spike | 9 (T-1.1 a T-1.9) | 5 S, 4 M |
| 2. Lector CSV | 7 (T-2.1 a T-2.7) | 2 S, 5 M |
| 3. Snapshots y horario | 3 (T-3.1 a T-3.3) | 3 S |
| 4. Strikes y serie | 6 (T-4.1 a T-4.6) | 4 S, 2 M |
| 5. Cadencia variable | 3 (T-5.1 a T-5.3) | 2 S, 1 M |
| 6. Interfaz y gráfica | 9 (T-6.1 a T-6.9) | 2 S, 7 M |
| 7. Revisión final | 3 (T-7.1 a T-7.3) | 1 S, 2 M |
| **Total** | **40** | 19 S, 21 M |

## Trazabilidad

"Cierra" = las tareas cuya conclusión deja el requisito cumplido; "También" = tareas que aportan pero no lo cierran por sí solas.

### Requisitos funcionales

| Requisito | Cierra | También | Estado |
| --- | --- | --- | --- |
| RF-01 | T-2.7, T-6.4 | T-2.4, T-2.5 | pendiente |
| RF-02 | T-3.2, T-3.3 | T-2.6, T-6.4 | pendiente |
| RF-03 | T-4.1, T-6.5 | — | pendiente |
| RF-04 | T-4.3, T-6.5 | — | pendiente |
| RF-05 | T-4.4, T-6.6, T-6.7 | T-4.2, T-4.6, T-6.1 | pendiente |
| RF-06 | T-4.5, T-6.6 | T-1.6, T-4.2, T-6.1 | pendiente |
| RF-07 | T-6.8 | T-1.8, T-6.2 | pendiente |
| RF-08 | — (v1.1, fuera de la v1.0) | — | fuera de alcance |
| RF-09 | T-6.9 | T-6.5 | pendiente |
| RF-10 | T-3.2, T-6.4 | — | pendiente |

### Requisitos no funcionales

| Requisito | Cierra | También | Estado |
| --- | --- | --- | --- |
| RNF-01 | T-2.7 | — | pendiente |
| RNF-02 | — (v1.1, fuera de la v1.0) | — | fuera de alcance |
| RNF-03 | T-3.1 | T-1.2, T-2.1 | pendiente |
| RNF-04 | T-1.4, T-6.3 | T-1.1 | pendiente |
| RNF-05 | T-1.4, T-1.5 | T-1.2, T-2.1 | pendiente |
| RNF-06 | T-1.9, T-6.6 | T-1.3, T-1.6 a T-1.8 | pendiente |
| RNF-07 | T-6.4, T-7.1 | — | pendiente |
| RNF-08 | T-2.7, T-3.3, T-5.3 | T-1.5, T-2.3 | pendiente |
| RNF-09 | T-2.5, T-2.7 | — | pendiente |
| RNF-10 | T-4.5, T-5.2 | T-1.2, T-2.6, T-5.1 | pendiente |

### Criterios de aceptación de la v1.0

| Criterio | Cierra | También | Estado |
| --- | --- | --- | --- |
| CA-01 | T-3.3 | T-2.5, T-2.7, T-6.4 | pendiente |
| CA-02 | T-2.7, T-7.2 | T-2.4, T-2.5, T-6.4 | pendiente |
| CA-03 | T-4.1 | — | pendiente |
| CA-04 | T-4.4, T-7.2 | T-6.7, T-6.8 | pendiente |
| CA-05 | T-4.5, T-7.2 | T-6.6 | pendiente |
| CA-06 | T-4.3, T-7.2 | T-6.5 | pendiente |
| CA-09 | T-3.1 | T-3.2 | pendiente |
| CA-10 | T-7.1 | T-6.4 | pendiente (manual) |
| CA-11 | T-5.3 | T-3.3, T-4.1, T-4.4, T-4.5, T-3.1, T-5.2 | pendiente |
| CA-12 | T-3.2, T-7.2 | T-6.4 | pendiente |
| CA-13 | T-5.2 | T-5.1, T-6.6 | pendiente |

**Sin tarea:** ninguno de los requisitos de la v1.0. RF-08, RNF-02, CA-07 y CA-08 quedan fuera de este documento a propósito (v1.1).

## Dudas que necesitan decisión

1. **Versión de xUnit v3:** la propuesta aprobada citaba 3.2.2, pero hoy la última estable de `xunit.v3` en NuGet es 4.0.2 (9-oct-2026). Recomiendo fijar 4.0.2 con `xunit.runner.visualstudio` 4.0.1. ¿De acuerdo, o se mantiene 3.2.2?
2. **Dos ayudantes que el diseño no nombra:** `ChartSeriesData` (T-6.1) y `SeriesLookup` (T-6.2) viven en Core para poder probarlos sin interfaz y reutilizarlos en un frontend web. El diseño dejaba la búsqueda del punto "en los datos del núcleo" sin decir dónde. ¿Se aprueban en Core?
3. **Eje X fijo sin alternativa:** el diseño da alternativa si falla NaN o el eje secundario, pero no si ScottPlot no permite bloquear el zoom. Si T-1.8 falla, ¿prefieres reajustar los límites en cada evento, o ignorar los eventos de ratón del control?
4. **Spike:** propongo borrar `spikes/` en T-1.9 y conservar solo `docs/specs/spike-scottplot.md`. ¿O prefieres conservar el proyecto?
5. **Verificación de CA-10:** como el archivo de referencia carga en menos de un segundo, uso un CSV sintético grande generado por una prueba explícita. ¿Aceptas esa forma de hacerlo observable?
