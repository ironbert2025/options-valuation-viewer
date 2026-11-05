# Propuesta v1.0 — Visor de valorización de opciones

5 de noviembre de 2026 · Fase PROPOSE · Base: `docs/specs/spec-v1.md`

## 1. Enfoque

Se construye el MVP v1.0 (solo offline) de dentro hacia fuera: primero un núcleo sin interfaz, probado con los CSV de referencia, y después la ventana WinForms como capa fina encima.

- **Clean Architecture (RNF-05):** las dependencias apuntan al núcleo. El dominio y los casos de uso no conocen WinForms ni el sistema de archivos; la lectura del CSV entra por una abstracción de fuente de snapshots (`ISnapshotSource`), que en v1.1 tendrá una segunda implementación en vivo sin tocar el cálculo.
- **WinForms (RNF-04):** la ventana solo pide datos al núcleo y los pinta. No contiene reglas de negocio (filtro, huecos, PnL).
- **Tiempo (RNF-03, RNF-10):** `Time` se guarda como `TimeOnly` tal como viene, sin zonas ni conversión. Todo el cálculo y el eje usan la `Time` de cada snapshot; nunca el número de orden ni un intervalo fijo.
- **Carga fuera del hilo de la UI (RNF-07):** `async` con `Task.Run` en la capa WinForms; el núcleo es síncrono y simple.

## 2. Estructura propuesta

| Proyecto | Responsabilidad |
| --- | --- |
| `OptionsViewer.Core` | Dominio y casos de uso: snapshot, contrato, filtro 09:30:00–16:00:00, serie de un contrato, PnL, huecos, n/d con Ask(T0)=0, aviso RF-10. Define `ISnapshotSource`. Sin dependencias. |
| `OptionsViewer.Infrastructure` | Lectura del CSV por nombre de columna, validación (RF-01), CRLF/LF, errores con número de línea; implementa `ISnapshotSource` para archivo completo. |
| `OptionsViewer.WinForms` | Ventana: selector de archivo, strikes, T0, gráfica y tooltip; hace de punto de composición. |
| `OptionsViewer.Tests` | Pruebas sin interfaz del núcleo y del lector con los CSV de `samples/` y CSV sintéticos. |

Dependencias: `WinForms → Core, Infrastructure`; `Infrastructure → Core`; `Tests → Core, Infrastructure`.

## 3. Orden de construcción

1. **Esqueleto:** solución, 4 proyectos, versión de SDK fijada, pruebas que corren vacías. Sin RF/CA.
2. **Lector CSV:** columnas por nombre, orden libre, CRLF, errores con línea. Cubre RF-01, CA-02 y la parte CRLF de CA-01.
3. **Snapshots y horario:** agrupar por `Time`, filtro inclusivo, aviso de zona horaria. Cubre RF-02, RF-10, CA-01, CA-09, CA-12.
4. **Strikes y serie del contrato:** lista ordenada, T0 válido, huecos, PnL, n/d. Cubre RF-03 a RF-06, CA-03 a CA-06 (incluye el caso real del 337,5 en Calls).
5. **Cadencia variable:** prueba con CSV sintético a 6 s y 1 min. Cubre RNF-10, CA-13; con esto queda cerrado CA-11.
6. **Interfaz y gráfica:** carga asíncrona, selección, gráfica con eje fijo en tiempo real, tooltip, cambio de contrato/T0/archivo. Cubre RF-07, RF-09, CA-10.
7. **Revisión final** contra CA-01 a CA-06 y CA-09 a CA-13.

## 4. Decisiones técnicas abiertas

**4.1 Librería de gráficas**

- **ScottPlot 5:** rápida con miles de puntos, huecos con `NaN`, eje de fechas y líneas horizontales nativas; tooltip por eventos del ratón (hay que programarlo). Última versión que encontré: `ScottPlot.WinForms` 5.1.59 (22-jun-2026), con net10.0-windows y net8.0-windows. *Fuentes en conflicto: nuget.org en caché mostraba 5.0.56; verificar antes de fijar.*
- **LiveCharts2:** tooltips y animaciones integrados; el WinForms es la plataforma menos pulida y la versión estable es 2.0.0, con prerelease 2.0.0-rc6.1 más nueva (sin verificar el soporte de net10 ni cuál es la vigente).
- **Recomendación: ScottPlot 5.** Encaja mejor con huecos, eje de tiempo real y un solo refresco por lote (v1.1).

**4.2 Versión de .NET**

- **Instalado (`dotnet --list-sdks`):** 9.0.312, 9.0.313 y 10.0.202; runtimes WindowsDesktop 8, 9 y 10.
- **net10.0-windows:** LTS actual, es el SDK por defecto y ScottPlot lo lista. Contra: poca historia de uso (LiveCharts2 sin verificar).
- **net8.0-windows:** soporte más probado; soporte de .NET 8 (sin verificar) termina cerca de noviembre de 2026.
- **Recomendación: net10.0-windows,** fijado con `global.json` al SDK 10.0.202.

**4.3 Framework de pruebas**

- **xUnit v3:** 3.2.2 estable (ene-2026), soporta .NET 8+; ejecuta en paralelo por defecto y crea una instancia por prueba. 4.0.0 existe solo como prerelease.
- **NUnit:** 4.6.1 vigente; el soporte de net10 lo vi en una sola fuente sin verificar, con un error de adaptador reportado.
- **Recomendación: xUnit v3 (3.2.2).** Las pruebas del núcleo son puras y no comparten estado.

## 5. Riesgos

1. **Puntos y huecos mal distinguidos.** Cortar por tiempo en vez de por ausencia rompería CA-05/CA-13. *Mitigación:* la regla vive en el núcleo (un contrato ausente de un snapshot existente) y se prueba con el 337,5 de Calls y con el CSV sintético.
2. **Datos reales con valores raros** (IV = −999 fuera de horario, spreads anchos, Ask muy bajo). *Mitigación:* el filtro de horario va antes del cálculo y n/d se prueba con Ask 0 sintético.
3. **Rendimiento de la UI al cargar.** *Mitigación:* carga asíncrona y una sola actualización de gráfica por carga; medir con el archivo de ~17.000 filas.
4. **Dependencia de versión de librerías** (ScottPlot/net10 sin verificar en nuget). *Mitigación:* confirmar versión y target en la etapa 1 y fijar versiones exactas.

## 6. Fuera de alcance

- Modo en vivo (RF-08, CA-07, CA-08): v1.1. Aquí solo se deja la abstracción `ISnapshotSource` para no rehacer el núcleo.
- Decisiones de v1.1: último snapshot de la sesión, truncado del archivo, apertura compartida, posición en bytes.
- Zonas de demanda y oferta y la propuesta automática de T0.
- Otras gráficas, interfaz web y escritura de los CSV.
