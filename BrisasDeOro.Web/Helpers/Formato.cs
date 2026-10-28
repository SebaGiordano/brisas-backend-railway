using System.Globalization;

namespace BrisasDeOro.Web.Helpers;

// Formato único de montos para mostrar en pantalla y PDFs (estilo argentino:
// punto de miles, sin decimales). Solo para presentación: no usar para parsear.
public static class Formato
{
    private static readonly CultureInfo Ar = CultureInfo.GetCultureInfo("es-AR");

    // 682500 → "682.500"
    public static string Numero(decimal valor) => valor.ToString("N0", Ar);

    // 682500 → "$682.500"  |  -5000 → "-$5.000"
    public static string Moneda(decimal valor) =>
        valor < 0 ? "-$" + Numero(Math.Abs(valor)) : "$" + Numero(valor);
}
