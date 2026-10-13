using KawaSys.Desktop.Data;
using KawaSys.Desktop.Models;

namespace KawaSys.Desktop.Forms;

public class MotosForm : Form
{
    private readonly MotoRepository _repo = new();
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoGenerateColumns = true, AllowUserToAddRows = false, MultiSelect = false };
    private readonly TextBox _txtBuscar = new() { Width = 200 };
    private readonly TextBox _txtModelo = new(), _txtMarca = new(), _txtColor = new();
    private readonly NumericUpDown _numPrecio = new() { Maximum = 1_000_000_000, DecimalPlaces = 2, ThousandsSeparator = true };
    private readonly NumericUpDown _numStock = new() { Maximum = 100000 };
    private readonly NumericUpDown _numCil = new() { Minimum = 1, Maximum = 5000, Value = 150 };
    private Moto? _seleccion;

    public MotosForm()
    {
        Text = "Motos";
        Size = new Size(900, 520);
        StartPosition = FormStartPosition.CenterParent;

        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(6) };
        var btnBuscar = new Button { Text = "Buscar" };
        btnBuscar.Click += (_, _) => Cargar();
        top.Controls.AddRange([new Label { Text = "Modelo/Marca:", AutoSize = true, Margin = new Padding(0, 6, 0, 0) }, _txtBuscar, btnBuscar]);

        var form = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 110, ColumnCount = 6, Padding = new Padding(6) };
        void Campo(string t, Control c) { form.Controls.Add(new Label { Text = t, AutoSize = true, Anchor = AnchorStyles.Left }); c.Anchor = AnchorStyles.Left | AnchorStyles.Right; form.Controls.Add(c); }
        Campo("Modelo", _txtModelo); Campo("Marca", _txtMarca); Campo("Color", _txtColor);
        Campo("Precio", _numPrecio); Campo("Stock", _numStock); Campo("Cilindrada", _numCil);

        var btns = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(6) };
        var btnNuevo = new Button { Text = "Nuevo" };
        var btnGuardar = new Button { Text = "Guardar" };
        var btnBaja = new Button { Text = "Dar de baja" };
        btnNuevo.Click += (_, _) => Limpiar();
        btnGuardar.Click += (_, _) => Guardar();
        btnBaja.Click += (_, _) => Baja();
        btns.Controls.AddRange([btnNuevo, btnGuardar, btnBaja]);

        _grid.SelectionChanged += (_, _) => Seleccionar();
        Controls.Add(_grid);
        Controls.Add(btns);
        Controls.Add(form);
        Controls.Add(top);
        Load += (_, _) => Cargar();
    }

    private void Cargar()
    {
        try { _grid.DataSource = _repo.Buscar(_txtBuscar.Text); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Error de base de datos"); }
    }

    private void Seleccionar()
    {
        if (_grid.CurrentRow?.DataBoundItem is not Moto m) return;
        _seleccion = m;
        _txtModelo.Text = m.Modelo; _txtMarca.Text = m.Marca; _txtColor.Text = m.Color;
        _numPrecio.Value = m.Precio; _numStock.Value = m.Stock; _numCil.Value = m.Cilindrada;
    }

    private void Limpiar()
    {
        _seleccion = null;
        _grid.ClearSelection();
        _txtModelo.Clear(); _txtMarca.Clear(); _txtColor.Clear();
        _numPrecio.Value = 0; _numStock.Value = 0; _numCil.Value = 150;
        _txtModelo.Focus();
    }

    private void Guardar()
    {
        if (string.IsNullOrWhiteSpace(_txtModelo.Text) || string.IsNullOrWhiteSpace(_txtMarca.Text) || string.IsNullOrWhiteSpace(_txtColor.Text))
        {
            MessageBox.Show("Modelo, marca y color son obligatorios.");
            return;
        }
        var m = new Moto
        {
            IdMoto = _seleccion?.IdMoto ?? 0,
            Modelo = _txtModelo.Text.Trim(), Marca = _txtMarca.Text.Trim(), Color = _txtColor.Text.Trim(),
            Precio = _numPrecio.Value, Stock = (int)_numStock.Value, Cilindrada = (int)_numCil.Value
        };
        try
        {
            if (_seleccion == null) _repo.Alta(m); else _repo.Modificar(m);
            Cargar();
            Limpiar();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Error al guardar"); }
    }

    private void Baja()
    {
        if (_seleccion == null) { MessageBox.Show("Seleccione una moto."); return; }
        if (MessageBox.Show($"¿Dar de baja {_seleccion.Marca} {_seleccion.Modelo}?", "Confirmar", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        try { _repo.Baja(_seleccion.IdMoto); Cargar(); Limpiar(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Error al dar de baja"); }
    }
}
