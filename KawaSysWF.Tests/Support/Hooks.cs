using KawaSysWF.Database;
using Microsoft.Data.Sqlite;
using Reqnroll;
using Reqnroll.Bindings;

namespace KawaSysWF.Tests.Support
{
    /// <summary>
    /// Cada escenario corre sobre una copia nueva de la base con los datos iniciales de KawaSys,
    /// así ningún escenario depende de lo que hizo otro.
    /// </summary>
    [Binding]
    public class Hooks
    {
        private static readonly string Carpeta = Path.Combine(Path.GetTempPath(), "kawasys-tests");
        private static readonly string Plantilla = Path.Combine(Carpeta, "plantilla.db");
        private readonly Contexto _ctx;
        private readonly ScenarioContext _escenario;
        private string _archivo = "";

        public Hooks(Contexto ctx, ScenarioContext escenario) { _ctx = ctx; _escenario = escenario; }

        [BeforeTestRun]
        public static void CrearPlantilla()
        {
            Directory.CreateDirectory(Carpeta);
            if (File.Exists(Plantilla)) File.Delete(Plantilla);
            DBHelper.UsarBaseDeDatos(Plantilla);
            DBHelper.Initialize();
            SqliteConnection.ClearAllPools();
        }

        [BeforeScenario]
        public void CopiarBase()
        {
            _archivo = Path.Combine(Carpeta, $"escenario-{Guid.NewGuid():N}.db");
            File.Copy(Plantilla, _archivo);
            DBHelper.UsarBaseDeDatos(_archivo);
        }

        // Un "Entonces" que verifica estado no puede pasar si el "Cuando" falló sin que nadie lo esperara.
        [BeforeStep]
        public void ExigirSinErroresInesperados()
        {
            var paso = _escenario.StepContext.StepInfo;
            if (paso.StepDefinitionType != StepDefinitionType.Then || paso.Text.StartsWith("el sistema rechaza")) return;
            if (_ctx.Error != null && !_ctx.ErrorVerificado)
                throw new Xunit.Sdk.XunitException($"El sistema devolvió un error inesperado: {_ctx.Error.Message}");
        }

        [AfterScenario]
        public void BorrarBase()
        {
            SqliteConnection.ClearAllPools();
            try { File.Delete(_archivo); } catch (IOException) { }
            if (_escenario.TestError == null && _ctx.Error != null && !_ctx.ErrorVerificado)
                throw new Xunit.Sdk.XunitException($"El sistema devolvió un error que ningún paso verificó: {_ctx.Error.Message}");
        }
    }
}
