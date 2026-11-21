namespace Dsw2025Tpi.Tests.Unit;

/// <summary>
/// CI-02 — Prueba TEMPORAL que falla a proposito.
/// Sirve para demostrar que el job "test" corta el pipeline y que GitHub
/// no ejecuta ni "build" ni "deploy" cuando una prueba esta en rojo.
/// Se elimina en el commit siguiente de esta misma rama.
/// </summary>
public class CI02_PruebaDeBloqueo
{
    [Fact]
    public void EstaPruebaFallaAProposito_ParaVerificarElBloqueoDelPipeline()
    {
        Assert.Equal(1, 2);
    }
}
