# Spec v1 — Visor de valorización de opciones

5 de noviembre de 2026

## Objetivo y contexto

La v1 es una aplicación de escritorio en C# (WinForms) que lee los CSV de opciones generados por otra app del trader y grafica cómo se valoriza un contrato desde un momento de entrada elegido.

- **Función:** apoyo a la app existente; solo lee los CSV, nunca los modifica ni interfiere con su escritura.
- **Primera gráfica:** evolución del Ask (y del Bid) de un contrato desde un momento T0, con la ganancia o pérdida resultante.
- **Más adelante:** otras gráficas de valorización, aún sin definir.
- **Datos de referencia:** `AAPL_Call_20261104_20261104.csv` y `AAPL_Put_20261104_20261104.csv`, un día completo con 1.615 snapshots cada uno.
- **Uso previsto:** personal, en la misma máquina donde corre la app que escribe los archivos (confirmado).

## Alcance de la v1

La v1 entrega una sola gráfica, la valorización desde T0, alimentada por un archivo cargado o por un archivo en vivo.

**Dentro de la v1**

- Carga de un CSV de Calls o de Puts en modo offline.
- Lectura en vivo de un CSV que la otra app sigue escribiendo.
- Selección del contrato (strike y tipo) y del momento T0.
- Gráfica 1: Ask(t), Bid(t), Ask de entrada y ganancia o pérdida.
- Filtro de horario de mercado, de 09:30:00 a 16:00:00 (ambos incluidos), sobre la hora tal como viene en el archivo.

**Fuera de la v1**

- Detección automática de condiciones en tiempo real para proponer T0, basada en zonas de demanda y oferta (feature posterior que definirá el trader).
- Otras gráficas de valorización.
- Interfaz web (Angular) y acceso desde otros equipos.
- Cualquier escritura o modificación de los CSV.

## Datos de entrada

Cada CSV es una serie de snapshots de la cadena de opciones de AAPL, con 12 columnas y cadencia variable: varios snapshots por minuto hasta las 11:00 y uno por minuto después.

- **Archivos:** uno de Calls y uno de Puts por día, con el mismo esquema; el nombre sigue el patrón `<id>_AAPL_<Call|Put>_<fecha>_<fecha>.csv`, y el `<id>` inicial puede no estar.
- **Formato:** UTF-8, cabecera en la primera línea, separador coma, punto decimal y saltos de línea CRLF.
- **Columnas:** `Time`, `SpotPrice`, `StrikePrice`, `Bid`, `Ask`, `IntValue`, `ExtValue`, `Delta`, `Gamma`, `Theta`, `Vega`, `IV`.
- **`Time`:** `HH:MM:SS` sin fecha ni zona. Es la hora del reloj de la PC que graba el archivo y se asume en zona Eastern (hora de Nueva York), igual que hace la otra app; no se convierte. La fecha se toma del nombre del archivo.
- **Snapshot:** todas las filas con la misma `Time`. Cada uno trae entre 5 y 11 strikes, una ventana alrededor del spot que se desplaza durante el día (17 strikes distintos en el archivo y 16 dentro del horario).
- **Cadencia:** variable. De 09:30 a 11:00 la otra app guarda varios snapshots por minuto (hoy cada 4 segundos, podría ser cada 6) y desde las 11:00 uno por minuto; la cantidad por minuto puede cambiar de un archivo a otro, así que la app no la da por fija.
- **Orden:** las filas vienen ordenadas por hora.

**Día de referencia (04-nov-2026)**

- 1.615 snapshots por archivo, de 08:24:27 a 18:24:35; 1.613 caen entre 09:30:00 y 15:59:06.
- Cadencia dentro del horario: 1.316 snapshots hasta las 11:00 (unos 15 por minuto, normalmente a 4 segundos) y 297 desde las 11:00 (uno por minuto). El último intervalo de 4 segundos cae en las 11:00:00.
- Después de las 11:00 hay 28 saltos mayores de 60 segundos, con un máximo de 181 segundos (unos 3 minutos sin datos). El hueco largo (15:59:06 a 18:24:35, unos 145 min) cae después del cierre.
- Sin nulos y sin filas con Ask menor que Bid; Ask y Bid nunca valen 0 (mínimos 0,02 y 0,01).
- `IV` vale -999 en una fila de cada archivo, la de las 18:24:35, fuera de horario.
- Algunos strikes tienen spreads muy anchos (por ejemplo, Bid 0,1 y Ask 0,5), lo que vuelve ruidosos el Ask y la IV de esos contratos.
- La ventana de strikes se desplaza con el spot, así que un contrato puede salir y volver a entrar: en Calls, el strike 340 aparece en 677 de los 1.613 snapshots y el 342,5 en todos.
- Los strikes de Calls y de Puts no coinciden snapshot a snapshot: dentro del horario, Calls cubre de 330 a 367,5 y Puts de 327,5 a 365 (16 strikes cada uno).

## Modos de operación

La app tiene dos modos que comparten el mismo núcleo de procesamiento; solo cambia la fuente de los datos.

**Offline (archivo cargado)**

- El trader carga un CSV completo, elige el contrato y T0, y obtiene la gráfica.
- T0 lo decide el trader.
- Sirve para analizar días ya terminados.

**En vivo (archivo que crece)**

- La app sigue un solo CSV a la vez (Calls o Puts) que la otra app todavía escribe y agrega snapshots a medida que aparecen.
- Abre el archivo en solo lectura permitiendo que otro proceso lo siga escribiendo (en C#, `FileShare.ReadWrite`), recuerda hasta qué byte leyó y procesa solo lo nuevo.
- Una última línea incompleta se descarta hasta que llegue completa.
- En la v1 el trader también elige contrato y T0 en este modo; proponer T0 automáticamente queda para una versión posterior.

**Núcleo común**

- Parseo de filas, agrupación en snapshots, filtro de horario y cálculo de la serie de un contrato.
- Una abstracción de fuente de snapshots con dos implementaciones: archivo completo y archivo en seguimiento.

## Gráfica 1: valorización desde T0

Para un contrato y un momento T0, la gráfica muestra cómo evoluciona el contrato después de comprarlo al Ask de T0 y cuánto se ganaría o perdería al venderlo al Bid.

**Entradas**

- Tipo (Call o Put) y strike del contrato.
- T0: la hora de un snapshot entre 09:30 y 16:00 en el que el contrato exista.

**Series**

- Ask(t) y Bid(t) del contrato en cada snapshot desde T0 hasta el último disponible, en $ por contrato (precio cotizado × 100).
- Línea horizontal en Ask(T0), el precio de entrada por contrato.
- Ganancia o pérdida de 1 contrato, en $ y en %; el subíndice c indica precio por contrato (precio cotizado × 100):

```latex
\mathrm{PnL}(t) = \mathrm{Bid}_c(t) - \mathrm{Ask}_c(T_0)
\mathrm{PnL}\%(t) = \frac{\mathrm{Bid}_c(t) - \mathrm{Ask}_c(T_0)}{\mathrm{Ask}_c(T_0)} \times 100
```

**Reglas de dibujo**

- Si el contrato no aparece en un snapshot que sí existe (salió de la ventana de strikes), la línea se corta en ese tramo; no se interpola.
- El tiempo transcurrido entre snapshots nunca corta la línea: si el contrato está en dos snapshots consecutivos, se unen aunque los separen 4, 60 o 181 segundos.
- Eje horizontal fijo, de 09:30 a 16:00, en tiempo real (según la `Time` de cada snapshot, no su número de orden); la serie empieza en T0. El eje no se amplía ni se desplaza: sin zoom en la v1.
- Si Ask(T0) es 0, la ganancia o pérdida en % no se calcula y se muestra como n/d.
- En modo en vivo la gráfica se actualiza cuando llegan snapshots nuevos.

## Requisitos funcionales

La v1 cumple diez requisitos, numerados para poder referirlos en las tareas y en las pruebas.

1. **RF-01:** cargar un CSV de Calls o de Puts desde disco, leyendo las columnas por nombre (sin distinguir mayúsculas, en cualquier orden e ignorando columnas de más), y validar que estén las 12 esperadas; si falta alguna, el archivo está vacío o una fila tiene un valor no numérico, mostrar un error claro con la línea afectada.
2. **RF-02:** agrupar las filas por `Time` en snapshots y descartar los que caen fuera de 09:30:00 a 16:00:00, ambos incluidos, comparando la hora tal como viene en el archivo.
3. **RF-03:** listar los strikes que aparecen dentro del horario, ordenados de menor a mayor, y permitir elegir uno.
4. **RF-04:** permitir elegir T0 entre todos los snapshots de la sesión; si el contrato elegido no existe en ese T0, avisar, no dibujar y pedir otro T0 u otro contrato.
5. **RF-05:** dibujar Ask(t), Bid(t), la línea de Ask(T0) y la ganancia o pérdida en $ y en %, todo por 1 contrato (precio cotizado × 100), según la sección Gráfica 1.
6. **RF-06:** cortar la línea solo en los snapshots donde el contrato no tiene datos, sin interpolar; el tiempo transcurrido entre snapshots nunca corta la línea, y el eje horizontal es de tiempo real.
7. **RF-07:** al pasar el cursor sobre la gráfica, mostrar hora, Bid, Ask y ganancia o pérdida del snapshot más cercano en el eje horizontal.
8. **RF-08:** en modo en vivo, seguir un CSV en crecimiento, uno solo a la vez (Calls o Puts), y actualizar la gráfica sin que el trader recargue el archivo.
9. **RF-09:** permitir cambiar de contrato, de T0 o de archivo sin reiniciar la app.
10. **RF-10:** si ningún snapshot del archivo cae entre 09:30:00 y 16:00:00, avisar de que puede haberse grabado en otra zona horaria.

## Requisitos no funcionales y restricciones

La restricción central es no interferir con la app que escribe los CSV; el resto fija tecnología, arquitectura y rendimiento.

- **RNF-01, solo lectura:** abrir los CSV con acceso de lectura y `FileShare.ReadWrite`, sin bloquear ni modificar el archivo mientras la otra app escribe.
- **RNF-02, lectura incremental:** en vivo, leer solo desde el último byte procesado y descartar una última línea incompleta.
- **RNF-03, hora:** `Time` es la hora del reloj de la PC que graba y se asume en zona Eastern, igual que hace la otra app; se compara tal como viene, sin convertir ni usar zonas horarias. Si el archivo se grabó en una PC de otra zona, el filtro se desfasa (ver RF-10).
- **RNF-04, tecnología:** C# con WinForms para la interfaz.
- **RNF-05, arquitectura:** Clean Architecture; el parseo, la lectura en vivo y el cálculo viven en una librería sin interfaz, para poder añadir otro frontend (por ejemplo web) sin reescribirlos.
- **RNF-06, gráficas:** una librería para WinForms que maneje series de algunos miles de puntos con actualización en vivo; se usa ScottPlot 5 (decidido en la propuesta).
- **RNF-07, rendimiento:** un archivo de un día (unas 17.000 filas) se carga y se grafica sin congelar la interfaz; la carga corre fuera del hilo de la interfaz.
- **RNF-08, pruebas:** el núcleo se puede probar con los CSV de referencia sin abrir la interfaz.
- **RNF-09, saltos de línea:** el parseo acepta CRLF y LF y no deja caracteres de retorno de carro pegados al último campo.
- **RNF-10, cadencia variable:** no se supone un intervalo fijo entre snapshots (ni 4 segundos, ni 6, ni unos 15 por minuto antes de las 11:00, ni el cambio a uno por minuto a las 11:00); todo se calcula con la `Time` de cada snapshot, de modo que un archivo con otra cantidad por minuto se procesa igual.

## Criterios de aceptación

La v1 se da por buena cuando se cumplen estos trece criterios, comprobables con los CSV de referencia del 04-nov-2026.

1. **CA-01, carga:** al cargar el CSV de Calls de referencia, la app cuenta 1.613 snapshots dentro de 09:30:00 a 16:00:00 y descarta 2 (los de 08:24:27 y 18:24:35). El CSV de Puts da el mismo resultado, y la `IV` de la última columna se lee como número aunque los archivos usen saltos de línea CRLF.
2. **CA-02, validación:** un CSV al que le falta una de las 12 columnas, está vacío o tiene un valor no numérico muestra un error con la línea afectada y no dibuja nada; el mismo CSV con las columnas en otro orden o con una columna de más se carga bien.
3. **CA-03, strikes:** la lista de strikes tiene 16 valores en ambos archivos, ordenados de menor a mayor: de 330 a 367,5 en Calls y de 327,5 a 365 en Puts.
4. **CA-04, valores de entrada:** en Calls, strike 340 y T0 = 09:30:00, la línea de entrada está en 300 $ por contrato, Bid(T0) es 289 $ y la ganancia o pérdida en T0 es −11 $ (−3,67 %).
5. **CA-05, huecos:** si el contrato sale de la ventana de strikes y vuelve a entrar, la línea queda cortada en los snapshots sin dato y no se dibuja ningún tramo que los una. Caso real: en Calls, el strike 337,5 sale tras las 09:44:49 y vuelve a las 09:45:29, con 9 snapshots sin dato. En cambio, el tiempo entre snapshots no corta la línea: si el contrato está en dos snapshots consecutivos, se unen aunque los separen 60 o 181 segundos.
6. **CA-06, contrato ausente en T0:** elegir un T0 en el que el contrato no existe muestra un aviso y no dibuja la gráfica.
7. **CA-07, solo lectura:** mientras otro proceso añade líneas a un CSV, la app lo lee sin error, el otro proceso sigue escribiendo sin excepciones y la app no cambia el contenido ni el tamaño del archivo.
8. **CA-08, en vivo:** al añadir snapshots a un CSV de prueba, la gráfica los muestra sin recargar; una línea final incompleta no genera errores ni puntos falsos y se incorpora cuando llega completa.
9. **CA-09, horario:** un snapshot a las 09:29:59 queda excluido, uno a las 09:30:00 queda incluido, uno a las 16:00:00 queda incluido y uno a las 16:00:01 queda excluido.
10. **CA-10, interfaz:** durante la carga del archivo de referencia la ventana sigue respondiendo a clics.
11. **CA-11, pruebas:** hay pruebas automatizadas que corren sin interfaz y cubren al menos CA-01, CA-03, CA-04, CA-05, CA-09 y CA-13.
12. **CA-12, zona horaria sospechosa:** un CSV sin ningún snapshot entre 09:30:00 y 16:00:00 muestra el aviso de que puede haberse grabado en otra zona horaria.
13. **CA-13, cadencia variable:** un CSV sintético con un snapshot cada 6 segundos hasta las 11:00 y uno por minuto después se carga igual que los de referencia: mismo filtro de horario, sin cortes falsos en las líneas y con el eje en tiempo real.

## Decisiones, supuestos y respuestas

Hay 8 decisiones del trader, 4 supuestos que él ya confirmó, 3 preguntas cerradas y 9 propuestas que él aceptó; para la v1 no queda nada abierto.

**Decididas por el trader**

- Interfaz de escritorio en WinForms (C#), no web.
- Dos modos, archivo cargado y archivo en vivo, sobre un mismo núcleo.
- La lectura en vivo no debe interferir con la escritura de la otra app.
- Se procesa de 09:30:00 a 16:00:00, ambos incluidos, hora de mercado de Nueva York.
- `Time` se toma como hora de pared de Nueva York, como hace la otra app: la PC que graba está en zona Eastern y no se convierte.
- Los huecos de un contrato no se interpolan.
- La gráfica 1 muestra Ask, Bid y la ganancia o pérdida frente al Ask de entrada.
- En modo offline, T0 lo elige el trader.

**Supuestos confirmados por el trader**

- La salida se valora al Bid (lo que se cobraría al vender), no al punto medio.
- En modo en vivo, contrato y T0 también los elige el trader en la v1; la detección automática queda fuera.
- Uso personal en una sola máquina, y la app lee los archivos directamente, sin pasar por una API del backend.
- La validación de columnas (RF-01) y el detalle al pasar el cursor (RF-07) las propuso Claude y el trader las aceptó.

**Preguntas cerradas**

- Todo se muestra en base a 1 contrato (precio cotizado × 100); no se muestra por acción.
- Las condiciones que detectará el programa (zonas de demanda y oferta, cómo se ven en el gráfico y cómo se procesan) las explicará el trader más adelante; quedan fuera de la v1.
- En modo en vivo se sigue un solo archivo a la vez.

**Propuestas aceptadas por el trader**

- Todo el cálculo en $ por contrato (precio cotizado × 100), también la fórmula de ganancia o pérdida.
- Si Ask(T0) es 0, el % no se calcula y se muestra n/d.
- El eje horizontal es fijo, de 09:30 a 16:00, y la serie empieza en T0.
- Al pasar el cursor se muestra el snapshot más cercano en el eje horizontal.
- La lista de strikes muestra solo los del horario, ordenada, y la lectura del CSV es por nombre de columna, tolerando otro orden y columnas de más.
- Aviso si ningún snapshot cae dentro del horario (RF-10).
- No se supone una cadencia fija entre snapshots (4 s, 6 s, unos 15 por minuto, cambio a las 11:00): todo se basa en la `Time` de cada snapshot.
- La línea se corta solo cuando el contrato falta en un snapshot que existe, nunca por el tiempo transcurrido, y el eje horizontal es de tiempo real.
- En vivo, un snapshot se da por completo cuando aparece una `Time` posterior, no por un temporizador.

## Pendiente para la v1.1

Estas decisiones del modo en vivo se toman antes de construir la v1.1.

- Cómo se cierra el último snapshot de la sesión, que ya no tendrá una `Time` posterior. La regla general quedó aceptada: un snapshot está completo cuando aparece una `Time` posterior, porque la otra app escribe fila a fila y un snapshot son de 5 a 11 filas.
- Qué hace la app si el archivo se trunca o se reinicia (mínimo: detectar que el tamaño bajó y recargar).
- Cómo abre el archivo la otra app: si no comparte lectura, la apertura falla.
- Llevar la posición de lectura en bytes y no en caracteres.
- Un solo refresco de la gráfica por lote de filas nuevas.

## Entrega por etapas

El MVP entrega el modo offline completo; el modo en vivo llega como segunda etapa sobre el mismo núcleo.

| Etapa | Incluye | Criterios de aceptación |
| --- | --- | --- |
| MVP (v1.0), offline | RF-01 a RF-07, RF-09 y RF-10; librería núcleo y WinForms con la gráfica 1 | CA-01 a CA-06 y CA-09 a CA-13 |
| v1.1, en vivo | RF-08; lectura incremental en solo lectura sobre el mismo núcleo | CA-07 y CA-08 |
