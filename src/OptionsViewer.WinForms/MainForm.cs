namespace OptionsViewer.WinForms;

/// <summary>
/// Ventana principal. Por ahora solo la disposición de los controles (T-6.3): sin lógica de negocio. La carga del
/// archivo, la selección de strike y T0 y el dibujo de las gráficas se enlazan en las tareas siguientes de la etapa 6.
/// </summary>
public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();

        // Las gráficas empiezan vacías, solo con su título
        priceChart.Plot.Title("Ask, Bid y precio de entrada ($ por contrato)");
        pnlChart.Plot.Title("Ganancia o pérdida ($ y %)");
    }
}
