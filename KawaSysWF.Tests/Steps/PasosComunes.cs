using KawaSysWF.Database;
using KawaSysWF.Tests.Support;
using Reqnroll;
using Xunit;

namespace KawaSysWF.Tests.Steps
{
    [Binding]
    public class PasosComunes
    {
        private readonly Contexto _ctx;
        public PasosComunes(Contexto ctx) => _ctx = ctx;

        public static Cliente Cliente(string nombre) =>
            Repository.GetClientes().SingleOrDefault(c => c.Nombre == nombre)
            ?? throw new InvalidOperationException($"No existe el cliente \"{nombre}\" en los datos de prueba.");

        [Given(@"^que el sistema tiene los datos iniciales de KawaSys$")]
        public void DatosIniciales()
        {
            Assert.Equal(4, Repository.GetClientes().Count);
            Assert.Equal(15, Repository.GetVentas().Count);
            Assert.Empty(FacturacionRepository.Listar());
        }

        [Given(@"^que se facturaron las ventas históricas$")]
        public void FacturarHistoricas() => Assert.Equal(15, FacturacionRepository.GenerarPendientes());

        [Given(@"^que la cuenta corriente de ""([^""]*)"" tiene saldo (-?\d+)$")]
        [Then(@"^el saldo de la cuenta corriente de ""([^""]*)"" es (-?\d+)$")]
        public void SaldoCuentaCorriente(string cliente, decimal saldo) => Assert.Equal(saldo, Cliente(cliente).Saldo);

        [Then(@"^el sistema rechaza la operación con el mensaje ""([^""]*)""$")]
        public void Rechaza(string mensaje)
        {
            Assert.True(_ctx.Error != null, "Se esperaba que el sistema rechazara la operación, pero la aceptó.");
            Assert.Contains(mensaje, _ctx.Error!.Message);
            _ctx.ErrorVerificado = true;
        }

        [Then(@"^la operación se realiza sin errores$")]
        public void SinErrores() => Assert.Null(_ctx.Error);
    }
}
