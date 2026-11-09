namespace OptionsViewer.WinForms;

partial class MainForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        rootLayout = new TableLayoutPanel();
        toolbarLayout = new TableLayoutPanel();
        openButton = new Button();
        strikeLabel = new Label();
        strikeCombo = new ComboBox();
        t0Label = new Label();
        t0Combo = new ComboBox();
        statusPanel = new FlowLayoutPanel();
        fileLabel = new Label();
        typeLabel = new Label();
        snapshotsLabel = new Label();
        discardedLabel = new Label();
        noticeBanner = new Label();
        chartsLayout = new TableLayoutPanel();
        priceChart = new ScottPlot.WinForms.FormsPlot();
        pnlChart = new ScottPlot.WinForms.FormsPlot();
        openFileDialog = new OpenFileDialog();
        rootLayout.SuspendLayout();
        toolbarLayout.SuspendLayout();
        statusPanel.SuspendLayout();
        chartsLayout.SuspendLayout();
        SuspendLayout();
        //
        // rootLayout: barra de herramientas, estado, aviso y gráficas, de arriba abajo
        //
        rootLayout.ColumnCount = 1;
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.Controls.Add(toolbarLayout, 0, 0);
        rootLayout.Controls.Add(statusPanel, 0, 1);
        rootLayout.Controls.Add(noticeBanner, 0, 2);
        rootLayout.Controls.Add(chartsLayout, 0, 3);
        rootLayout.Dock = DockStyle.Fill;
        rootLayout.Location = new Point(0, 0);
        rootLayout.Name = "rootLayout";
        rootLayout.Padding = new Padding(8);
        rootLayout.RowCount = 4;
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayout.TabIndex = 0;
        //
        // toolbarLayout: Abrir CSV, strike y T0
        //
        toolbarLayout.AutoSize = true;
        toolbarLayout.ColumnCount = 6;
        toolbarLayout.ColumnStyles.Add(new ColumnStyle());
        toolbarLayout.ColumnStyles.Add(new ColumnStyle());
        toolbarLayout.ColumnStyles.Add(new ColumnStyle());
        toolbarLayout.ColumnStyles.Add(new ColumnStyle());
        toolbarLayout.ColumnStyles.Add(new ColumnStyle());
        toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        toolbarLayout.Controls.Add(openButton, 0, 0);
        toolbarLayout.Controls.Add(strikeLabel, 1, 0);
        toolbarLayout.Controls.Add(strikeCombo, 2, 0);
        toolbarLayout.Controls.Add(t0Label, 3, 0);
        toolbarLayout.Controls.Add(t0Combo, 4, 0);
        toolbarLayout.Dock = DockStyle.Fill;
        toolbarLayout.Name = "toolbarLayout";
        toolbarLayout.RowCount = 1;
        toolbarLayout.RowStyles.Add(new RowStyle());
        toolbarLayout.TabIndex = 0;
        //
        // openButton
        //
        openButton.Anchor = AnchorStyles.Left;
        openButton.AutoSize = true;
        openButton.Margin = new Padding(0, 3, 20, 3);
        openButton.MinimumSize = new Size(110, 30);
        openButton.Name = "openButton";
        openButton.TabIndex = 0;
        openButton.Text = "Abrir CSV…";
        openButton.UseVisualStyleBackColor = true;
        //
        // strikeLabel
        //
        strikeLabel.Anchor = AnchorStyles.Left;
        strikeLabel.AutoSize = true;
        strikeLabel.Margin = new Padding(0, 0, 4, 0);
        strikeLabel.Name = "strikeLabel";
        strikeLabel.Text = "Strike:";
        //
        // strikeCombo
        //
        strikeCombo.Anchor = AnchorStyles.Left;
        strikeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        strikeCombo.Enabled = false;
        strikeCombo.FormattingEnabled = true;
        strikeCombo.Margin = new Padding(0, 3, 20, 3);
        strikeCombo.Name = "strikeCombo";
        strikeCombo.Size = new Size(130, 28);
        strikeCombo.TabIndex = 1;
        //
        // t0Label
        //
        t0Label.Anchor = AnchorStyles.Left;
        t0Label.AutoSize = true;
        t0Label.Margin = new Padding(0, 0, 4, 0);
        t0Label.Name = "t0Label";
        t0Label.Text = "T0:";
        //
        // t0Combo
        //
        t0Combo.Anchor = AnchorStyles.Left;
        t0Combo.DropDownStyle = ComboBoxStyle.DropDownList;
        t0Combo.Enabled = false;
        t0Combo.FormattingEnabled = true;
        t0Combo.Margin = new Padding(0, 3, 20, 3);
        t0Combo.Name = "t0Combo";
        t0Combo.Size = new Size(130, 28);
        t0Combo.TabIndex = 2;
        //
        // statusPanel: archivo, tipo, snapshots dentro de horario y descartados
        //
        statusPanel.AutoSize = true;
        statusPanel.Controls.Add(fileLabel);
        statusPanel.Controls.Add(typeLabel);
        statusPanel.Controls.Add(snapshotsLabel);
        statusPanel.Controls.Add(discardedLabel);
        statusPanel.Dock = DockStyle.Fill;
        statusPanel.Name = "statusPanel";
        statusPanel.TabIndex = 1;
        //
        // fileLabel
        //
        fileLabel.AutoSize = true;
        fileLabel.Margin = new Padding(0, 4, 24, 4);
        fileLabel.Name = "fileLabel";
        fileLabel.Text = "Archivo: (ninguno)";
        //
        // typeLabel
        //
        typeLabel.AutoSize = true;
        typeLabel.Margin = new Padding(0, 4, 24, 4);
        typeLabel.Name = "typeLabel";
        typeLabel.Text = "Tipo: —";
        //
        // snapshotsLabel
        //
        snapshotsLabel.AutoSize = true;
        snapshotsLabel.Margin = new Padding(0, 4, 24, 4);
        snapshotsLabel.Name = "snapshotsLabel";
        snapshotsLabel.Text = "Snapshots en horario: —";
        //
        // discardedLabel
        //
        discardedLabel.AutoSize = true;
        discardedLabel.Margin = new Padding(0, 4, 24, 4);
        discardedLabel.Name = "discardedLabel";
        discardedLabel.Text = "Descartados: —";
        //
        // noticeBanner: aviso o error (oculto hasta que haya algo que avisar)
        //
        noticeBanner.AutoSize = true;
        noticeBanner.BackColor = Color.FromArgb(255, 244, 206);
        noticeBanner.Dock = DockStyle.Fill;
        noticeBanner.ForeColor = Color.FromArgb(102, 77, 3);
        noticeBanner.Margin = new Padding(0, 4, 0, 4);
        noticeBanner.Name = "noticeBanner";
        noticeBanner.Padding = new Padding(8);
        noticeBanner.TabIndex = 2;
        noticeBanner.Visible = false;
        //
        // chartsLayout: precio arriba y ganancia o pérdida abajo
        //
        chartsLayout.ColumnCount = 1;
        chartsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        chartsLayout.Controls.Add(priceChart, 0, 0);
        chartsLayout.Controls.Add(pnlChart, 0, 1);
        chartsLayout.Dock = DockStyle.Fill;
        chartsLayout.Name = "chartsLayout";
        chartsLayout.RowCount = 2;
        chartsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
        chartsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
        chartsLayout.TabIndex = 3;
        //
        // priceChart
        //
        priceChart.DisplayScale = 1F;
        priceChart.Dock = DockStyle.Fill;
        priceChart.Name = "priceChart";
        priceChart.TabIndex = 0;
        //
        // pnlChart
        //
        pnlChart.DisplayScale = 1F;
        pnlChart.Dock = DockStyle.Fill;
        pnlChart.Name = "pnlChart";
        pnlChart.TabIndex = 1;
        //
        // openFileDialog: solo archivos CSV
        //
        openFileDialog.CheckFileExists = true;
        openFileDialog.DefaultExt = "csv";
        openFileDialog.Filter = "Archivos CSV (*.csv)|*.csv|Todos los archivos (*.*)|*.*";
        openFileDialog.FilterIndex = 1;
        openFileDialog.Multiselect = false;
        openFileDialog.Title = "Abrir CSV de opciones";
        //
        // MainForm
        //
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1100, 720);
        Controls.Add(rootLayout);
        MinimumSize = new Size(860, 560);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Visor de valorización de opciones";
        rootLayout.ResumeLayout(false);
        rootLayout.PerformLayout();
        toolbarLayout.ResumeLayout(false);
        toolbarLayout.PerformLayout();
        statusPanel.ResumeLayout(false);
        statusPanel.PerformLayout();
        chartsLayout.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel rootLayout;
    private TableLayoutPanel toolbarLayout;
    private Button openButton;
    private Label strikeLabel;
    private ComboBox strikeCombo;
    private Label t0Label;
    private ComboBox t0Combo;
    private FlowLayoutPanel statusPanel;
    private Label fileLabel;
    private Label typeLabel;
    private Label snapshotsLabel;
    private Label discardedLabel;
    private Label noticeBanner;
    private TableLayoutPanel chartsLayout;
    private ScottPlot.WinForms.FormsPlot priceChart;
    private ScottPlot.WinForms.FormsPlot pnlChart;
    private OpenFileDialog openFileDialog;
}
