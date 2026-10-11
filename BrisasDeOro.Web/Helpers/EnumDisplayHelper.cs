using BrisasDeOro.Web.Models;

namespace BrisasDeOro.Web.Helpers;

public static class EnumDisplayHelper
{
    public static string NombreTipoPago(TipoPago t) => t switch
    {
        TipoPago.Sena        => "Seña",
        TipoPago.PagoParcial => "Pago parcial",
        TipoPago.Cancelacion => "Cancelación",
        TipoPago.Ajuste      => "Ajuste / Corrección",
        _                    => t.ToString()
    };

    public static string NombreMetodoPago(MetodoPago m) => m switch
    {
        MetodoPago.Transferencia  => "Transferencia",
        MetodoPago.Efectivo       => "Efectivo",
        MetodoPago.TarjetaCredito => "Tarjeta crédito",
        MetodoPago.TarjetaDebito  => "Tarjeta débito",
        _                         => m.ToString()
    };
}
