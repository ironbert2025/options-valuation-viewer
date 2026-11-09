using System.Text;

namespace OptionsViewer.Tests;

/// <summary>Archivo temporal para las pruebas; se borra al liberarlo.</summary>
public sealed class TempFile : IDisposable
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"ovv-test-{Guid.NewGuid():N}.csv");

    public static TempFile WithContent(string content)
    {
        var file = new TempFile();
        File.WriteAllText(file.Path, content, Utf8NoBom);
        return file;
    }

    public static TempFile WithBytes(byte[] bytes)
    {
        var file = new TempFile();
        File.WriteAllBytes(file.Path, bytes);
        return file;
    }

    public void Dispose()
    {
        try { File.Delete(Path); } catch (IOException) { /* se limpia el directorio temporal más tarde */ }
    }
}
