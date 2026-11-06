# Visor de valorización de opciones

App de escritorio C# (WinForms, `net10.0-windows`) que lee los CSV de opciones de otra app y grafica la valorización de un contrato desde un T0. Documentos de referencia: `docs/specs/` (spec, propuesta, diseño y tareas). Se trabaja por tareas de `docs/specs/tasks-v1.md`: una tarea = un commit.

## Reglas del proyecto

- **Núcleo sin interfaz:** `OptionsViewer.Core` no depende de WinForms ni de ScottPlot (ni de ningún paquete). `Infrastructure` tampoco. Las dependencias apuntan hacia Core.
- **Hora sin zona:** `Time` es `TimeOnly` tal como viene en el CSV. No se convierte ni se usan zonas horarias.
- **Precios en `decimal`:** nunca `double` en el núcleo. El redondeo es solo al mostrar.
- **Sin cadencia fija:** no se supone ningún intervalo entre snapshots (ni 4 s, ni 6 s, ni el cambio a las 11:00). Todo se calcula con la `Time` de cada snapshot; un hueco es un contrato ausente de un snapshot existente, nunca un salto de tiempo.
- **Errores de contenido como resultado tipado** (`LoadResult` / `CsvError`, `SeriesResult` / `SeriesError`), no excepciones. Las excepciones son para fallos de entorno (archivo inexistente, permisos).
- **CSV de `samples/` son de solo lectura:** no se modifican. Los archivos se abren con `FileAccess.Read` y `FileShare.ReadWrite`.

## Pruebas

- xUnit v3. Primero la prueba (rojo), luego el código (verde).
- Nombres: `Metodo_Condicion_Resultado`. Cada prueba de un criterio lleva `[Trait("CA", "CA-xx")]` (y `[Trait("RF", "RF-xx")]` si aplica).
- Comando: `dotnet test` en la raíz de la solución.
- xUnit v3 corre sobre Microsoft.Testing.Platform (MTP). El SDK 10 exige activarlo en `global.json` (`"test": { "runner": "Microsoft.Testing.Platform" }`); sin ese bloque `dotnet test` falla con un error de VSTest.
- Filtrar por criterio: `dotnet test --filter "CA=CA-09"` (equivale a `--filter-trait "CA=CA-09"`). Si ninguna prueba coincide, el código de salida es 8 y `dotnet test` se considera fallido.
