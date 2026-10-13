namespace KawaSys.Desktop.Models;

public class Moto
{
    public int IdMoto { get; set; }
    public string Modelo { get; set; } = "";
    public string Marca { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public int Cilindrada { get; set; }
    public string Color { get; set; } = "";
}
