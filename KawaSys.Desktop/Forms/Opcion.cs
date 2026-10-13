using KawaSys.Desktop.Data;

namespace KawaSys.Desktop.Forms;

/// <summary>Item de combo con opción "Todos" (Id nulo).</summary>
public record Opcion(int? Id, string Nombre)
{
    public override string ToString() => Nombre;

    public static void Llenar(ComboBox cmb, IEnumerable<(int Id, string Nombre)> datos)
    {
        cmb.DropDownStyle = ComboBoxStyle.DropDownList;
        cmb.Items.Clear();
        cmb.Items.Add(new Opcion(null, "(Todos)"));
        foreach (var (id, nombre) in datos) cmb.Items.Add(new Opcion(id, nombre));
        cmb.SelectedIndex = 0;
    }

    public static int? Seleccionado(ComboBox cmb) => (cmb.SelectedItem as Opcion)?.Id;
}
