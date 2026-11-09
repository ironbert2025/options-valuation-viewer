using System.Text;
using OptionsViewer.Core;
using OptionsViewer.Infrastructure;

namespace OptionsViewer.Tests;

public class CsvFileSnapshotSourceTests
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    // Dos filas de ejemplo (strikes 340 y 342,5) para el snapshot de la hora indicada
    private static string[] RowFor(TimeOnly time, string strike) =>
        [time.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture), "341.53", strike, "2.89", "3.0", "1.53", "1.47", "0.625", "0.193", "-0.325", "0.028", "42.582"];

    private static SyntheticCsv CsvWithSnapshots(int count, TimeOnly start)
    {
        var csv = new SyntheticCsv();
        for (int i = 0; i < count; i++)
        {
            var t = start.Add(TimeSpan.FromSeconds(i));
            csv.AddRow(RowFor(t, "340.0")).AddRow(RowFor(t, "342.5"));
        }
        return csv;
    }

    private static LoadResult Load(string path, OptionType type = OptionType.Call, CancellationToken? ct = null) =>
        new CsvFileSnapshotSource(path, type).Load(ct ?? TestContext.Current.CancellationToken);

    // ---------- Propiedades de ISnapshotSource

    [Fact]
    public void Source_ExponeTipoYNombreDeArchivo()
    {
        ISnapshotSource source = new CsvFileSnapshotSource(SamplePaths.Puts, OptionType.Put);

        Assert.Equal(OptionType.Put, source.OptionType);
        Assert.Equal("AAPL_Put_20261104_20261104.csv", source.DisplayName);
    }

    // ---------- Archivo vacío

    [Fact]
    [Trait("RF", "RF-01")]
    [Trait("CA", "CA-02")]
    public void Load_ArchivoDe0Bytes_EmptyFile()
    {
        using var file = TempFile.WithBytes([]);

        var result = Load(file.Path);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Equal(CsvErrorKind.EmptyFile, result.Error!.Kind);
        Assert.Null(result.Error.Line);
    }

    [Fact]
    [Trait("RF", "RF-01")]
    [Trait("CA", "CA-02")]
    public void Load_SoloCabecera_EmptyFileLinea1()
    {
        using var file = TempFile.WithContent(new SyntheticCsv().Build());

        var result = Load(file.Path);

        Assert.False(result.IsSuccess);
        Assert.Equal(CsvErrorKind.EmptyFile, result.Error!.Kind);
        Assert.Equal(1, result.Error.Line);
    }

    // ---------- Validación de columnas y valores (CA-02)

    [Fact]
    [Trait("RF", "RF-01")]
    [Trait("CA", "CA-02")]
    public void Load_ColumnasReordenadasYExtra_Exito()
    {
        // Mismas dos filas con el orden de columnas cambiado y una columna de más al principio
        var csv = new SyntheticCsv()
            .WithColumns("Extra", "ask", "TIME", "Bid", "StrikePrice", "SpotPrice", "IntValue", "ExtValue", "Delta", "Gamma", "Theta", "Vega", "IV")
            .AddRow("x", "3.0", "09:30:00", "2.89", "340.0", "341.53", "1.53", "1.47", "0.625", "0.193", "-0.325", "0.028", "42.582")
            .AddRow("x", "0.55", "09:30:00", "0.5", "342.5", "341.53", "-4.73", "5.23", "0.183", "0.048", "-0.55", "0.027", "55.031")
            .Build();
        using var file = TempFile.WithContent(csv);

        var result = Load(file.Path);

        Assert.True(result.IsSuccess, result.Error?.Message);
        var snapshot = Assert.Single(result.Value!.Snapshots);
        Assert.Equal(new TimeOnly(9, 30, 0), snapshot.Time);
        Assert.Equal(3.0m, snapshot.QuotesByStrike[340.0m].Ask);
        Assert.Equal(2.89m, snapshot.QuotesByStrike[340.0m].Bid);
        Assert.Equal(0.55m, snapshot.QuotesByStrike[342.5m].Ask);
    }

    [Fact]
    [Trait("RF", "RF-01")]
    [Trait("CA", "CA-02")]
    public void Load_FaltaUnaColumna_ErrorYSinValor()
    {
        var csv = new SyntheticCsv()
            .WithColumns(SyntheticCsv.StandardColumns.Where(c => c != "Ask").ToArray())
            .AddRow("09:30:00", "341.53", "340.0", "2.89", "1.53", "1.47", "0.625", "0.193", "-0.325", "0.028", "42.582")
            .Build();
        using var file = TempFile.WithContent(csv);

        var result = Load(file.Path);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Equal(CsvErrorKind.MissingColumns, result.Error!.Kind);
        Assert.Equal(["Ask"], result.Error.MissingColumns);
    }

    [Fact]
    [Trait("RF", "RF-01")]
    [Trait("CA", "CA-02")]
    public void Load_ValorNoNumerico_ErrorConLineaDelArchivo()
    {
        var bad = RowFor(new TimeOnly(9, 30, 4), "340.0");
        bad[4] = "abc";   // Ask
        var csv = new SyntheticCsv()
            .AddRow(RowFor(new TimeOnly(9, 30, 0), "340.0"))     // línea 2
            .AddRow(bad)                                          // línea 3
            .Build();
        using var file = TempFile.WithContent(csv);

        var result = Load(file.Path);

        Assert.False(result.IsSuccess);
        Assert.Equal(CsvErrorKind.InvalidValue, result.Error!.Kind);
        Assert.Equal(3, result.Error.Line);
        Assert.Equal("Ask", result.Error.Column);
        Assert.Equal("abc", result.Error.RawValue);
    }

    [Fact]
    [Trait("RF", "RF-01")]
    [Trait("CA", "CA-02")]
    public void Load_HoraQueRetrocede_OutOfOrderConLineaDelArchivo()
    {
        var csv = new SyntheticCsv()
            .AddRow(RowFor(new TimeOnly(9, 30, 4), "340.0"))     // línea 2
            .AddRow(RowFor(new TimeOnly(9, 30, 8), "340.0"))     // línea 3
            .AddRow(RowFor(new TimeOnly(9, 30, 0), "340.0"))     // línea 4
            .Build();
        using var file = TempFile.WithContent(csv);

        var result = Load(file.Path);

        Assert.Equal(CsvErrorKind.OutOfOrder, result.Error!.Kind);
        Assert.Equal(4, result.Error.Line);
    }

    [Fact]
    public void Load_LineasEnBlanco_SeIgnoranYLaNumeracionSigueSiendoFisica()
    {
        var bad = RowFor(new TimeOnly(9, 30, 4), "340.0");
        bad[3] = "x";   // Bid
        var content = new SyntheticCsv().AddRow(RowFor(new TimeOnly(9, 30, 0), "340.0")).Build()
                      + "\r\n"                                  // línea 3, en blanco
                      + string.Join(",", bad) + "\r\n";         // línea 4
        using var file = TempFile.WithContent(content);

        var result = Load(file.Path);

        Assert.Equal(CsvErrorKind.InvalidValue, result.Error!.Kind);
        Assert.Equal(4, result.Error.Line);
    }

    // ---------- Saltos de línea (RNF-09)

    [Fact]
    [Trait("RNF", "RNF-09")]
    [Trait("CA", "CA-01")]
    public void Load_CrLfYLf_MismoResultado()
    {
        SyntheticCsv Build(string eol) => CsvWithSnapshots(5, new TimeOnly(9, 30, 0)).WithLineEnding(eol);
        using var crlf = TempFile.WithContent(Build("\r\n").Build());
        using var lf = TempFile.WithContent(Build("\n").Build());

        var a = Load(crlf.Path);
        var b = Load(lf.Path);

        Assert.True(a.IsSuccess, a.Error?.Message);
        Assert.True(b.IsSuccess, b.Error?.Message);
        Assert.Equal(Flatten(a.Value!), Flatten(b.Value!));
        Assert.Equal(5, a.Value!.Snapshots.Count);
        Assert.Equal(42.582m, a.Value.Snapshots[0].QuotesByStrike[340.0m].IV);   // IV es la última columna: sin \r pegado
    }

    private static List<string> Flatten(SnapshotSet set) =>
        set.Snapshots.SelectMany(s => s.QuotesByStrike.Values.Select(q => $"{s.Time:HH:mm:ss}|{q}")).ToList();

    // ---------- Cancelación y errores de entorno

    [Fact]
    public void Load_TokenCancelado_LanzaOperationCanceled()
    {
        using var file = TempFile.WithContent(CsvWithSnapshots(10, new TimeOnly(9, 30, 0)).Build());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() => Load(file.Path, ct: cts.Token));
    }

    [Fact]
    public void Load_ArchivoInexistente_LanzaFileNotFound()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"no-existe-{Guid.NewGuid():N}.csv");

        // Los errores de entorno son excepciones, no un CsvError (design-v1 §2.1)
        Assert.Throws<FileNotFoundException>(() => Load(missing));
    }

    // ---------- Archivos de referencia (sin filtrar)

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("RNF", "RNF-08")]
    public void Load_CallsReferencia_Devuelve1615SnapshotsSinFiltrar()
    {
        var result = Load(SamplePaths.Calls, OptionType.Call);

        Assert.True(result.IsSuccess, result.Error?.Message);
        var set = result.Value!;
        Assert.Equal(OptionType.Call, set.Type);
        Assert.Equal(1615, set.Snapshots.Count);
        Assert.Equal(16864, set.Snapshots.Sum(s => s.QuotesByStrike.Count));
        Assert.Equal(new TimeOnly(8, 24, 27), set.Snapshots[0].Time);
        var last = set.Snapshots[^1];
        Assert.Equal(new TimeOnly(18, 24, 35), last.Time);
        Assert.Equal(142.675m, last.QuotesByStrike[340.0m].IV);   // IV de la última fila, leída como número con CRLF
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("RNF", "RNF-08")]
    public void Load_PutsReferencia_Devuelve1615SnapshotsSinFiltrar()
    {
        var result = Load(SamplePaths.Puts, OptionType.Put);

        Assert.True(result.IsSuccess, result.Error?.Message);
        var set = result.Value!;
        Assert.Equal(1615, set.Snapshots.Count);
        Assert.Equal(17366, set.Snapshots.Sum(s => s.QuotesByStrike.Count));
        var last = set.Snapshots[^1];
        Assert.Equal(new TimeOnly(18, 24, 35), last.Time);
        Assert.Equal(-999.0m, last.QuotesByStrike[350.0m].IV);
    }

    // ---------- Convivencia con la otra app que escribe el archivo (RNF-01)

    // La otra app añade líneas a un CSV que mantiene abierto. Se simula con el modo de apertura que usa por
    // defecto .NET para escribir (StreamWriter(path, append: true), File.AppendAllText): FileMode.Append,
    // FileAccess.Write y FileShare.Read. Es decir: acceso de escritura, handle abierto durante toda la sesión, y solo
    // permite a los demás LEER. Con ese modo un segundo proceso solo puede abrir el archivo pidiendo acceso de
    // lectura y declarando que tolera a un escritor (FileShare.ReadWrite); si pidiera FileShare.Read, Windows
    // rechazaría la apertura con una violación de uso compartido (lo comprueban las dos pruebas de control).
    // No se ha verificado con qué modo abre el archivo la app real; el diseño lo deja pendiente para la v1.1.
    private const int InitialSnapshots = 3000;
    private const int MaxAppendedRows = 60_000;

    [Fact]
    [Trait("RNF", "RNF-01")]
    public void Load_OtroProcesoEscribiendo_NoFallaNiBloquea()
    {
        var start = new TimeOnly(9, 30, 0);
        var initial = CsvWithSnapshots(InitialSnapshots, start).Build();
        using var file = TempFile.WithContent(initial);
        byte[] initialBytes = File.ReadAllBytes(file.Path);

        long bytesAppended = 0;
        int rowsAppended = 0;
        var appendedText = new StringBuilder();
        Exception? writerError = null;
        bool stop = false;

        // Escritor: mantiene el archivo abierto con FileAccess.Write + FileShare.Read y añade snapshots completos
        // (una escritura por snapshot, con CRLF) en un hilo, sin parar, mientras se ejecuta Load.
        var writerStream = new FileStream(file.Path, FileMode.Append, FileAccess.Write, FileShare.Read);
        var writerThread = new Thread(() =>
        {
            try
            {
                int k = InitialSnapshots;
                while (!Volatile.Read(ref stop) && Volatile.Read(ref rowsAppended) < MaxAppendedRows)
                {
                    var t = start.Add(TimeSpan.FromSeconds(k++));
                    var text = string.Join("\r\n", string.Join(",", RowFor(t, "340.0")), string.Join(",", RowFor(t, "342.5"))) + "\r\n";
                    var bytes = Utf8NoBom.GetBytes(text);
                    writerStream.Write(bytes, 0, bytes.Length);
                    writerStream.Flush();
                    appendedText.Append(text);
                    Interlocked.Add(ref bytesAppended, bytes.Length);
                    Interlocked.Add(ref rowsAppended, 2);
                    Thread.Yield();
                }
            }
            catch (Exception ex)
            {
                writerError = ex;
            }
        });
        writerThread.Start();

        try
        {
            // El escritor ya está escribiendo antes de que empiece la lectura
            Assert.True(SpinWait.SpinUntil(() => Volatile.Read(ref rowsAppended) >= 20, TimeSpan.FromSeconds(10)), "El escritor no arrancó.");
            int rowsAtLoadStart = Volatile.Read(ref rowsAppended);

            LoadResult result = Load(file.Path);   // no debe lanzar ninguna excepción

            int rowsAtLoadEnd = Volatile.Read(ref rowsAppended);
            Volatile.Write(ref stop, true);
            writerThread.Join();

            // 1. Load terminó bien y leyó al menos el contenido que ya había
            Assert.True(result.IsSuccess, result.Error?.Message);
            Assert.True(result.Value!.Snapshots.Count >= InitialSnapshots);
            for (int i = 0; i < InitialSnapshots; i++)
                Assert.Equal(start.Add(TimeSpan.FromSeconds(i)), result.Value.Snapshots[i].Time);

            // 2. La lectura no bloqueó al escritor: no hubo IOException y siguió añadiendo filas durante Load
            Assert.Null(writerError);
            Assert.True(rowsAtLoadEnd > rowsAtLoadStart, $"El escritor no avanzó durante Load ({rowsAtLoadStart} -> {rowsAtLoadEnd} filas).");
        }
        finally
        {
            Volatile.Write(ref stop, true);
            writerThread.Join();
            writerStream.Dispose();
        }

        // 3. La fuente no modificó el archivo: el contenido que ya había sigue idéntico al principio, y todo lo
        //    demás es exactamente lo que añadió el escritor (ni un byte más ni menos).
        byte[] finalBytes = File.ReadAllBytes(file.Path);
        Assert.Equal(initialBytes.Length + bytesAppended, finalBytes.Length);
        Assert.True(finalBytes.AsSpan(0, initialBytes.Length).SequenceEqual(initialBytes), "El contenido original cambió.");
        Assert.Equal(appendedText.ToString(), Utf8NoBom.GetString(finalBytes, initialBytes.Length, finalBytes.Length - initialBytes.Length));
    }

    [Fact]
    [Trait("RNF", "RNF-01")]
    public void Simulacion_EscritorConFileShareRead_RechazaAUnLectorQueNoAdmiteEscritores()
    {
        // Control: demuestra que el escritor simulado es realmente restrictivo. Un lector con FileShare.Read
        // (que no admite a un escritor ya abierto) no puede abrir el archivo; por eso el lector usa FileShare.ReadWrite.
        using var file = TempFile.WithContent(new SyntheticCsv().AddRow(RowFor(new TimeOnly(9, 30, 0), "340.0")).Build());
        using var writer = new FileStream(file.Path, FileMode.Append, FileAccess.Write, FileShare.Read);

        Assert.Throws<IOException>(() => new FileStream(file.Path, FileMode.Open, FileAccess.Read, FileShare.Read));
        using var reader = new FileStream(file.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);   // este sí abre
        Assert.True(reader.Length > 0);
    }

    [Fact]
    [Trait("RNF", "RNF-01")]
    public void Simulacion_StreamWriterAppendPorDefecto_EsFileShareRead()
    {
        // Control: respalda el comentario de arriba. El modo por defecto de StreamWriter(path, append: true) se
        // comporta como FileAccess.Write + FileShare.Read: rechaza FileShare.Read y admite FileShare.ReadWrite.
        using var file = TempFile.WithContent(new SyntheticCsv().AddRow(RowFor(new TimeOnly(9, 30, 0), "340.0")).Build());
        using var writer = new StreamWriter(file.Path, append: true);

        Assert.Throws<IOException>(() => new FileStream(file.Path, FileMode.Open, FileAccess.Read, FileShare.Read));
        using var reader = new FileStream(file.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        Assert.True(reader.Length > 0);
    }
}
