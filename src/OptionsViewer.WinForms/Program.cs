namespace OptionsViewer.WinForms;

static class Program
{
    /// <summary>
    ///  Punto de entrada y de composición de la aplicación: aquí se crean las dependencias y se pasan a la ventana.
    ///  Por ahora la ventana no recibe ninguna (T-6.3); la fuente de datos y los casos de uso se enlazan en T-6.4.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // Configuración de la aplicación (DPI, fuente por defecto): https://aka.ms/applicationconfiguration
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
