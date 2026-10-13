using KawaSys.Desktop.Data;
using KawaSys.Desktop.Models;

namespace KawaSys.Desktop.Forms;

public class VentaForm : Form
{
    private readonly VentaService _service = new();
    private readonly MotoRepository _motos = new();
    private readonly ComboBox _cmbCliente = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    private readonly ComboBox _cmbSucursal = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
    private readonly ComboBox _cmbPago = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
    private readonly ComboBox _cmbMoto = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
    private readonly NumericUpDown _numCant = new() { Minimum = 1, Maximum = 1000, Value = 1, Width = 60 };
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
    private readonly Label _lblTotal = new() { Text = "Total: $0,00", AutoSize = true, Font = new Font("Segoe UI", 12, FontStyle.Bold) };
    private readonly BindingSource _items = new() { DataSource = new List<ItemVenta>() };
    private List<(int Id, string Nombre)> _clientes = [];
    private List<(int Id, string Nombre)> _sucursales = [];
    private List<Moto> _catalogo = [];

    private List<ItemVenta> Items => (List<ItemVenta>)_items.DataSource;

    public VentaForm()
    {
        Text = "Registrar venta";
        Size = new Size(900, 540);
        StartPosition = FormStartPosition.CenterParent;

        var cab = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 70, Padding = new Padding(6), WrapContents = true };
        cab.Controls.AddRange([Lbl("Cliente"), _cmbCliente, Lbl("Sucursal"), _cmbSucursal, Lbl("Pago"), _cmbPago]);

        var agregar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(6) };
        var btnAgregar = new Button { Text = "Agregar" };
        var btnQuitar = new Button { Text = "Quitar" };
        btnAgregar.Click += (_, _) => Agregar();
        btnQuitar.Click += (_, _) => Quitar();
        agregar.Controls.AddRange([Lbl("Moto"), _cmbMoto, Lbl("Cant."), _numCant, btnAgregar, btnQuitar]);

        var pie = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(6) };
        var btnConfirmar = new Button { Text = "Confirmar venta", Width = 140 };
        btnConfirmar.Click += (_, _) => Confirmar();
        pie.Controls.AddRange([btnConfirmar, _lblTotal]);

        _grid.DataSource = _items;
        Controls.Add(_grid);
        Controls.Add(pie);
        Controls.Add(agregar);
        Controls.Add(cab);
        Load += (_, _) => CargarCombos();
    }

    private static Label Lbl(string t) => new() { Text = t, AutoSize = true, Margin = new Padding(8, 6, 0, 0) };

    private void CargarCombos()
    {
        try
        {
            _clientes = _service.Clientes();
            _sucursales = _service.Sucursales();
            _cmbCliente.DataSource = _clientes.Select(c => c.Nombre).ToList();
            _cmbSucursal.DataSource = _sucursales.Select(s => s.Nombre).ToList();
            _cmbPago.DataSource = FacturaService.MediosDePago.Append(VentaService.CuentaCorriente).ToArray();
            RecargarMotos();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Error de base de datos"); }
    }

    private void RecargarMotos()
    {
        _catalogo = _motos.Buscar().Where(m => m.Stock > 0).ToList();
        _cmbMoto.DataSource = _catalogo.Select(m => $"{m.Marca} {m.Modelo} {m.Color} - ${m.Precio:N2} (stock {m.Stock})").ToList();
    }

    private void Agregar()
    {
        if (_cmbMoto.SelectedIndex < 0) return;
        var m = _catalogo[_cmbMoto.SelectedIndex];
        int cant = (int)_numCant.Value;
        int yaEnVenta = Items.Where(i => i.IdMoto == m.IdMoto).Sum(i => i.Cantidad);
        if (yaEnVenta + cant > m.Stock)
        {
            MessageBox.Show($"Stock insuficiente: quedan {m.Stock} unidades.");
            return;
        }
        Items.Add(new ItemVenta(m.IdMoto, $"{m.Marca} {m.Modelo}", cant, m.Precio));
        Refrescar();
    }

    private void Quitar()
    {
        if (_grid.CurrentRow?.DataBoundItem is ItemVenta it) { Items.Remove(it); Refrescar(); }
    }

    private void Refrescar()
    {
        _items.ResetBindings(false);
        _lblTotal.Text = $"Total: ${Items.Sum(i => i.SubTotal):N2}";
    }

    private void Confirmar()
    {
        if (_cmbCliente.SelectedIndex < 0 || _cmbSucursal.SelectedIndex < 0) return;
        try
        {
            var r = _service.Registrar(
                _clientes[_cmbCliente.SelectedIndex].Id,
                _sucursales[_cmbSucursal.SelectedIndex].Id,
                (string)_cmbPago.SelectedItem!,
                Items.ToList());
            MessageBox.Show($"Venta #{r.IdVenta} registrada. Factura N° {r.NroFactura}. Total ${r.Total:N2}", "Venta confirmada");
            Items.Clear();
            Refrescar();
            RecargarMotos();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "No se pudo registrar la venta");
            RecargarMotos();
        }
    }
}
