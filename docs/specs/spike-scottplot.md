# Spike de ScottPlot (T-1.6 a T-1.9)

Registro de las comprobaciones desechables sobre `ScottPlot.WinForms` 5.1.59 con `net10.0-windows`. El proyecto del spike vive en `spikes/ScottPlotSpike` (fuera de la solución) y se elimina en T-1.9; este documento se conserva.

## Resumen

| Comprobación | Tarea | Resultado | Decisión |
| --- | --- | --- | --- |
| `NaN` corta la línea | T-1.6 | **Pasa** | Se usa `double.NaN` para los huecos; no hace falta partir la serie en segmentos |
| `FormsPlot` se crea y se dibuja en un `Form` real (valida NU1701 en ejecución) | T-1.6 | **Pasa** | `<NoWarn>NU1701</NoWarn>` en `OptionsViewer.WinForms.csproj` se mantiene |
| Eje secundario | T-1.7 | **Pasa** | Se usa un eje Y derecho (`Plot.Axes.AddRightAxis()`) para el PnL %; no hace falta la disposición de tres paneles (D4, opción B) |
| Eje X fijo de 09:30 a 16:00 sin zoom, y conversión píxel → coordenadas | T-1.8 | **Pasa** | Se bloquea con `FormsPlot.UserInputProcessor.Disable()`; no hace falta la alternativa de ignorar los eventos del ratón |

## T-1.6 — NaN corta la línea

**Prueba:** `Spike_NaN_CortaLaLinea` (desechable). Se dibuja un `Scatter` rojo, grosor 4, sin marcadores, con y = 5 en x = 0..10 y un `NaN` en x = 5. Se renderiza sin ventana (`plot.GetImageBytes(600, 300, ScottPlot.ImageFormat.Png)`), se carga el PNG como `Bitmap` y, con `plot.GetPixel(new Coordinates(x, 5))`, se cuentan las columnas de píxeles que tienen algún píxel rojo (en una banda de ±6 px alrededor de la fila de y = 5) en dos tramos:

- tramo con datos, de x = 1,2 a 2,8;
- tramo del `NaN`, de x = 4,2 a 5,8 (margen de 0,2 para no tocar los puntos vecinos de x = 4 y x = 6).

Como control, el mismo trazado sin `NaN` debe dar línea en ese tramo, para que la medida no sea vacía.

**Resultado** (89 columnas medidas por tramo):

| Serie | Tramo | Columnas con línea roja |
| --- | --- | --- |
| con `NaN` | con datos (x 1,2 a 2,8) | 88 |
| con `NaN` | del `NaN` (x 4,2 a 5,8) | **0** |
| sin `NaN` (control) | x 4,2 a 5,8 | 89 |

**Conclusión:** con `Plot.Add.Scatter` y la configuración por defecto, un `NaN` aislado corta la línea por completo en ambos lados (el segmento de x = 4 a 5 y el de 5 a 6 no se dibujan) y no se interpola. Sirve para representar el hueco del núcleo (`Value == null` → `double.NaN`, ver design-v1.md §6).

**Límites de la prueba:** solo se probó un `NaN` aislado en medio de la serie. No se probaron varios `NaN` consecutivos, un `NaN` al inicio o al final, ni el `Scatter` con marcadores. Los huecos reales (CA-05) son de 9 puntos consecutivos; el comportamiento con varios `NaN` seguidos se comprobará con el caso real en T-6.6.

## T-1.6 — FormsPlot en un Form real (validación de NU1701 en ejecución)

**Contexto:** `ScottPlot.WinForms` 5.1.59 arrastra `OpenTK` 3.1.0, `OpenTK.GLControl` 3.1.0 y `SkiaSharp.Views.WindowsForms` 3.119.0, que solo traen assets de .NET Framework. NuGet los restaura en modo compatibilidad y emite NU1701, silenciado solo en `OptionsViewer.WinForms.csproj`. Esta prueba comprueba que, aun así, el control funciona en ejecución sobre `net10.0-windows`.

**Prueba:** `Spike_FormsPlot_SeCreaYSeDibujaEnUnFormReal` (desechable). En un hilo STA se crea un `Form` con un `FormsPlot` (`Dock = Fill`) con una serie con un `NaN`, se muestra el formulario (`form.Show()`), se llama a `formsPlot.Refresh()` y se bombean los mensajes (`Application.DoEvents()`). Se comprueba que no hay excepciones, que el control creó su handle y que `formsPlot.Plot.RenderManager.RenderCount` aumenta tras `Refresh()`.

**Resultado:**

| Medida | Valor |
| --- | --- |
| Excepciones | ninguna |
| Handle del control creado | sí |
| Formulario visible | sí |
| `RenderCount` antes / después de `Refresh()` | 2 / 3 |

**Conclusión:** `FormsPlot` se instancia dentro de un `Form` real y se dibuja a través del control (`SkiaSharp.Views.WindowsForms` 3.119.0) sin excepciones, pese al aviso NU1701. El aviso queda silenciado con fundamento.

**Límites de la prueba:** el dibujo se comprobó con el contador de renders del control, no leyendo los píxeles de la ventana. La apariencia visual se verificará al construir la interfaz (T-6.3 y siguientes). No se probaron otros equipos ni otros escalados de pantalla.

## T-1.7 — Eje secundario

**Prueba:** `Spike_EjeSecundario_EscalaPropia` (desechable). Se dibujan dos series horizontales sin ventana:

- serie roja en el eje izquierdo ("PnL $"), constante 15, límites −30..30;
- serie azul en un eje derecho ("PnL %"), constante 2,5, límites −10..10, asignada con `scatter.Axes.YAxis = right`.

Las dos escalas dan alturas distintas para el mismo valor, así que se comprueba en el PNG renderizado que cada serie está en la fila de píxeles que marca **su propio** eje y que la azul **no** está donde la dibujaría la escala del eje izquierdo.

**Resultado:**

| Medida | Valor |
| --- | --- |
| Fila esperada de la serie roja (eje izquierdo, 15) | 103,8 → línea roja encontrada: sí |
| Fila esperada de la serie azul (eje derecho, 2,5) | 148,1 → línea azul encontrada: sí |
| Fila de la azul si usara la escala izquierda | 177,7 → línea azul encontrada: **no** |
| Límites de cada eje tras fijarlos | izquierdo −30..30; derecho −10..10 (se conservan) |

**API real usada (ScottPlot 5.1.59):** `Plot.Axes.AddRightAxis()` (devuelve un `IYAxis`, concretamente `RightAxis`), `Scatter.Axes.YAxis = right`, `Plot.Axes.SetLimitsY(bottom, top, axis)`, `Plot.GetPixel(Coordinates, xAxis, yAxis)`, `right.LabelText` y `Plot.Axes.Left.Label.Text` (en `AxisBase`, `Label` está marcado obsoleto: usar `LabelText`).

**Conclusión:** existe un eje secundario real con escala propia e independiente del izquierdo. Sirve para el PnL % de la interfaz (D4, opción A): el % es el PnL en $ reescalado (`× 100 / entrada`), así que sus límites se calculan a partir de los del eje izquierdo.

**Límites de la prueba:**
- Los límites de ambos ejes se fijaron a mano; no se probó el autoescalado ni cómo mantener sincronizados los dos ejes cuando cambian los datos. En la interfaz los límites del derecho se derivarán de los del izquierdo (`límite_$ × 100 / entrada`).
- Se comprobó la posición de las series, no el aspecto de las marcas ni el rótulo del eje derecho.
- Cómo se comportan los dos ejes con la interacción del usuario (zoom, desplazamiento) se trata en T-1.8.
- Con `Ask(T0) = 0` el % no está disponible; ocultar el eje derecho en ese caso se resolverá en T-6.7.

## T-1.8 — Eje X fijo de 09:30 a 16:00 sin zoom, y conversión píxel → coordenada

**Prueba:** `Spike_EjeX_FijoSinZoom` (desechable), con un `FormsPlot` dentro de un `Form` real (hilo STA, `Show()`, `Refresh()`), 391 puntos con X en segundos desde medianoche (34.200 a 57.600) y rótulos `HH:mm` mediante `NumericAutomatic.LabelFormatter`.

1. **Control positivo (interacción activa):** se simulan una rueda y un arrastre con el botón izquierdo. Deben cambiar los límites; si no, la comprobación sería vacía.
2. **Bloqueo:** `FormsPlot.UserInputProcessor.Disable()`, se vuelven a fijar los límites (`Plot.Axes.SetLimitsX(34200, 57600)` y `SetLimitsY(0, 10)`) y se simulan 15 ruedas (arriba, abajo y cerca del borde), 2 arrastres con el botón izquierdo, 1 con el derecho, 1 con el central, un clic central y un doble clic.
3. **Conversión píxel → coordenada:** con la zona de datos del último render (`Plot.RenderManager.LastRender.DataRect`), se convierten píxeles conocidos con `Plot.GetCoordinates(Pixel)` y se comparan con la X esperada. Tolerancia: un píxel (≈ 31,8 s).

**Resultado:**

| Medida | Valor |
| --- | --- |
| Interacción activa, límites iniciales | X 34.200..57.600, Y 0..10 |
| Interacción activa, tras la rueda simulada | X 35.688,9..56.036,7, Y 0,63..9,32 (**cambian**) |
| Interacción activa, tras el arrastre simulado | X 38.014,9..61.414,9 (**cambian**) |
| Interacción desactivada, límites fijados | X 34.200..57.600, Y 0..10 |
| Interacción desactivada, tras todos los eventos | X 34.200..57.600, Y 0..10 (**sin cambios**) |
| Píxel izquierdo de la zona de datos → X | 34.200,0 (09:30:00) |
| Píxel derecho de la zona de datos → X | 57.600,0 (16:00:00) |
| Píxel central → X | 45.900,0 (12:45:00; esperado 12:45:00) |
| A 1/4 del ancho → X | 40.050,0 (11:07:30; esperado 11:07:30) |
| Inversa: `GetPixel(09:30:00).X` / `GetPixel(16:00:00).X` | 32,9 / 769,0 (= `DataRect.Left` / `Right`) |
| Zona de datos | 736,1 px de ancho (≈ 31,8 s por píxel); `DisplayScale` = 1 |

**API real usada (ScottPlot 5.1.59):**
- Límites: `Plot.Axes.SetLimitsX(left, right)`, `SetLimitsY(bottom, top)`, `Plot.Axes.GetLimits()`.
- Bloqueo del ratón: `FormsPlot.UserInputProcessor.Disable()` (también existen `Enable()`, `IsEnabled`).
- Conversión: `Plot.GetCoordinates(Pixel)` y `Plot.GetPixel(Coordinates)`; la zona de datos sale de `Plot.RenderManager.LastRender.DataRect`.
- Rótulos `HH:mm`: `Plot.Axes.Bottom.TickGenerator = new NumericAutomatic { LabelFormatter = v => ... }` o `new NumericManual(double[] posiciones, string[] rótulos)`.

**Rótulos del eje inferior:**
- Con `NumericAutomatic` y el formateador, los rótulos son `HH:mm` pero caen en horas poco redondas y no incluyen los extremos: `10:00 10:33 11:06 11:40 12:13 12:46 13:20 13:53 14:26 15:00 15:33`.
- Con `NumericManual` cada 30 minutos salen exactamente `09:30 10:00 ... 15:30 16:00` (14 rótulos). Es lo que conviene usar en T-6.6.

**Hallazgos útiles para la etapa 6:**
- `FormsPlot` aloja un control hijo (`TransparentSKControl`, expuesto como la propiedad `SKControl`) que es quien recibe el ratón. Los eventos simulados sobre el `FormsPlot` externo no llegan a ScottPlot; los simulados sobre el hijo sí, y el `FormsPlot` externo recibe también sus eventos `MouseMove` (se contaron 73 de 73 disparados). Por tanto el tooltip de T-6.8 puede suscribirse a `FormsPlot.MouseMove`.
- Las coordenadas de pantalla y las de ScottPlot coinciden aquí porque `DisplayScale` fue 1. Con escalado de pantalla distinto de 100 % la conversión puede necesitar la escala (`FormsPlot.DisplayScale`); sin verificar.

**Límites de la prueba:**
- Los eventos de ratón se simularon invocando `OnMouseMove`, `OnMouseDown`, `OnMouseUp`, `OnMouseWheel` y `OnMouseDoubleClick` por reflexión sobre el control hijo, no con entrada real del sistema operativo. Es la misma ruta de código que usan los eventos reales, pero no se probó con un ratón físico.
- Se probó la interacción del ratón. No se probaron el teclado, el menú contextual del clic derecho (`FormsPlot.Menu`), el comportamiento al redimensionar la ventana, ni el escalado de pantalla distinto de 100 %.
- Se fijó también el eje Y a mano (0..10); en la interfaz el Y se ajustará a los datos de cada serie (T-6.6).
- No se comprobó que `Disable()` desactive también el zoom con el eje secundario de T-1.7; el bloqueo es del procesador de entrada, no de un eje concreto.

## Notas técnicas

- **Resultados del spike:** `dotnet test` con Microsoft.Testing.Platform no muestra la salida de consola; las mediciones se escribieron en un archivo (`spike-output.txt`) junto al ejecutable.
- **API de ScottPlot 5.1.59 usada y confirmada:** `Plot.Add.Scatter(double[], double[])`, `Scatter.Color`, `Scatter.LineWidth`, `Scatter.MarkerSize`, `Plot.Axes.SetLimits(left, right, bottom, top)`, `Plot.GetImageBytes(int, int, ScottPlot.ImageFormat)`, `Plot.GetPixel(Coordinates)`, `FormsPlot` (`ScottPlot.WinForms`), `FormsPlot.Refresh()` y `Plot.RenderManager.RenderCount`.
- **Ambigüedades de nombres:** en un proyecto con `UseWindowsForms` hay que calificar `System.Drawing.Color` y `ScottPlot.ImageFormat`, porque colisionan con `ScottPlot.Color` y `System.Drawing.Imaging.ImageFormat`.
