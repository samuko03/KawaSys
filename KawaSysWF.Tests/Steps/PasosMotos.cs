using KawaSysWF.Database;
using KawaSysWF.Tests.Support;
using Reqnroll;
using Xunit;

namespace KawaSysWF.Tests.Steps
{
    /// <summary>Pasos de Gestión de modelos de motos: registrar, modificar, baja, consulta y stock.</summary>
    [Binding]
    public class PasosMotos
    {
        private const int SucursalNorte = 2;
        private readonly Contexto _ctx;
        public PasosMotos(Contexto ctx) => _ctx = ctx;

        private static Moto Moto(string marca, string modelo) =>
            MotosRepository.Buscar().FirstOrDefault(m => m.Marca == marca && m.Modelo == modelo)
            ?? throw new InvalidOperationException($"No existe el modelo {marca} {modelo} en el catálogo.");

        [Given(@"^que el catálogo tiene los modelos iniciales de KawaSys$")]
        public void CatalogoInicial()
        {
            Assert.Equal(16, MotosRepository.Buscar().Count);
            Assert.Equal(15, Repository.GetVentas().Count);
        }

        // ── Registrar ───────────────────────────────────────────────────────
        [When(@"^registro el modelo ""([^""]*)"" ""([^""]*)"" de (\d+) cc color ""([^""]*)"" a (-?\d+) con stock (-?\d+)$")]
        public void Registrar(string marca, string modelo, int cc, string color, decimal precio, int stock) =>
            _ctx.Intentar(() => MotosRepository.Registrar(new Moto
            {
                Marca = marca, Modelo = modelo, Cilindrada = cc, Color = color, Precio = precio, Stock = stock
            }));

        // ── Modificar ───────────────────────────────────────────────────────
        [When(@"^modifico el modelo ""([^""]*)"" ""([^""]*)"" con precio (\d+) y color ""([^""]*)""$")]
        public void ModificarPrecioYColor(string marca, string modelo, decimal precio, string color)
        {
            var m = Moto(marca, modelo);
            m.Precio = precio; m.Color = color;
            _ctx.Intentar(() => MotosRepository.Modificar(m));
        }

        [When(@"^cambio el nombre del modelo ""([^""]*)"" ""([^""]*)"" a ""([^""]*)""$")]
        public void CambiarModelo(string marca, string modelo, string nuevo)
        {
            var m = Moto(marca, modelo);
            m.Modelo = nuevo;
            _ctx.Intentar(() => MotosRepository.Modificar(m));
        }

        // ── Baja ────────────────────────────────────────────────────────────
        [Given(@"^que el modelo ""([^""]*)"" ""([^""]*)"" está dado de baja$")]
        [When(@"^doy de baja el modelo ""([^""]*)"" ""([^""]*)""$")]
        public void DarDeBaja(string marca, string modelo) => _ctx.Intentar(() => MotosRepository.DarDeBaja(Moto(marca, modelo).Id));

        [When(@"^reactivo el modelo ""([^""]*)"" ""([^""]*)""$")]
        public void Reactivar(string marca, string modelo)
        {
            var m = Moto(marca, modelo);
            m.Activa = true;
            _ctx.Intentar(() => MotosRepository.Modificar(m));
        }

        [Then(@"^el modelo ""([^""]*)"" ""([^""]*)"" figura en el catálogo como (activo|dado de baja)$")]
        public void FiguraComo(string marca, string modelo, string estado) =>
            Assert.Equal(estado == "activo", Moto(marca, modelo).Activa);

        [Then(@"^el modelo ""([^""]*)"" ""([^""]*)"" no figura entre los modelos activos$")]
        public void NoFiguraEntreActivos(string marca, string modelo) =>
            Assert.DoesNotContain(MotosRepository.Buscar(soloActivas: true), m => m.Marca == marca && m.Modelo == modelo);

        // ── Consultar ───────────────────────────────────────────────────────
        [When(@"^busco modelos con el texto ""([^""]*)""$")]
        public void Buscar(string texto) => _ctx.Motos = MotosRepository.Buscar(texto, soloActivas: true);

        [Then(@"^obtengo (\d+) modelos$")]
        public void CantidadModelos(int cantidad) => Assert.Equal(cantidad, _ctx.Motos.Count);

        [Then(@"^todos los modelos encontrados contienen ""([^""]*)""$")]
        public void TodosContienen(string texto) =>
            Assert.All(_ctx.Motos, m => Assert.Contains(texto, m.Descripcion, StringComparison.OrdinalIgnoreCase));

        // ── Stock ───────────────────────────────────────────────────────────
        [When(@"^(ingreso|resto) (\d+) unidades del modelo ""([^""]*)"" ""([^""]*)""$")]
        public void ActualizarStock(string accion, int unidades, string marca, string modelo) =>
            _ctx.Intentar(() => MotosRepository.ActualizarStock(Moto(marca, modelo).Id, accion == "ingreso" ? unidades : -unidades));

        [When(@"^intento vender (\d+) ""([^""]*)"" ""([^""]*)"" a (\d+)$")]
        public void Vender(int cantidad, string marca, string modelo, decimal precio) =>
            _ctx.Intentar(() => Repository.SaveVenta(new Venta
            {
                Fecha = DateTime.Today.ToString("yyyy-MM-dd"), SucursalId = SucursalNorte, Marca = marca, Modelo = modelo,
                Cantidad = cantidad, PrecioUnitario = precio, Total = cantidad * precio, FormaPago = "Efectivo"
            }));

        [Then(@"^el modelo ""([^""]*)"" ""([^""]*)"" tiene stock (\d+)$")]
        public void TieneStock(string marca, string modelo, int stock) => Assert.Equal(stock, Moto(marca, modelo).Stock);

        [Then(@"^el modelo ""([^""]*)"" ""([^""]*)"" tiene precio (\d+) y stock (\d+)$")]
        public void TienePrecioYStock(string marca, string modelo, decimal precio, int stock)
        {
            var m = Moto(marca, modelo);
            Assert.Equal(precio, m.Precio);
            Assert.Equal(stock, m.Stock);
        }

        [Then(@"^el modelo ""([^""]*)"" ""([^""]*)"" es de color ""([^""]*)""$")]
        public void EsDeColor(string marca, string modelo, string color) => Assert.Equal(color, Moto(marca, modelo).Color);

        [Then(@"^siguen registradas (\d+) ventas$")]
        public void VentasRegistradas(int cantidad) => Assert.Equal(cantidad, Repository.GetVentas().Count);
    }
}
