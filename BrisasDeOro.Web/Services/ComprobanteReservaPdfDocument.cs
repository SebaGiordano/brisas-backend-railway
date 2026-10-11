using BrisasDeOro.Web.Helpers;
using BrisasDeOro.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BrisasDeOro.Web.Services;

public class ComprobanteReservaPdfDocument : IDocument
{
    private readonly Reserva _reserva;
    private readonly Dictionary<int, string>? _apartComposicion;
    private readonly Dictionary<int, int>? _diasAirePorCabana;

    private ComprobanteReservaPdfDocument(
        Reserva reserva,
        Dictionary<int, string>? apartComposicion,
        Dictionary<int, int>? diasAirePorCabana)
    {
        _reserva            = reserva;
        _apartComposicion   = apartComposicion;
        _diasAirePorCabana  = diasAirePorCabana;
    }

    public static ComprobanteReservaPdfDocument Crear(
        Reserva reserva,
        Dictionary<int, string>? apartComposicion,
        Dictionary<int, int>? diasAirePorCabana)
        => new(reserva, apartComposicion, diasAirePorCabana);

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo.png");

        // ── Cálculos (misma lógica exacta que Views/Reservas/Details.cshtml) ──
        var noches = (_reserva.FechaSalida - _reserva.FechaIngreso).Days;

        var diasAireTotalGlobal = _diasAirePorCabana?.Values.Sum() ?? 0;
        var subtotalAireGlobal  = _reserva.PrecioAireDiario.HasValue
            ? diasAireTotalGlobal * _reserva.PrecioAireDiario.Value : 0;

        var tarifaDia = (!_reserva.EsInvitacion && noches > 0)
            ? (decimal?)((_reserva.MontoTotal - subtotalAireGlobal) / noches) : null;

        var totalCobrado   = _reserva.Pagos.Sum(p => p.Monto);
        var saldoPendiente = _reserva.MontoTotal - totalCobrado;

        var estadoTexto = _reserva.Estado switch
        {
            EstadoReserva.Confirmada => "Confirmada",
            EstadoReserva.Finalizada => "Finalizada",
            EstadoReserva.Cancelada  => "Cancelada",
            _                        => _reserva.Estado.ToString()
        };
        var estadoColor = _reserva.Estado switch
        {
            EstadoReserva.Confirmada => "#198754",
            EstadoReserva.Finalizada => "#6c757d",
            EstadoReserva.Cancelada  => "#dc3545",
            _                        => "#000000"
        };

        var estadoPagoTexto = _reserva.EsInvitacion ? "Invitación"
            : totalCobrado == 0                      ? "Sin seña"
            : saldoPendiente > 0                     ? "Señado"
                                                      : "Pagado";
        var estadoPagoColor = _reserva.EsInvitacion ? "#333333"
            : totalCobrado == 0                      ? "#7A0000"
            : saldoPendiente > 0                     ? "#7A5F00"
                                                      : "#1A5C1A";

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(0.7f, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(9));

            page.Content().Column(col =>
            {
                // ── Header (mismo esqueleto que FacturacionPdfDocument) ──
                col.Item().AlignCenter().Height(90).Image(logoPath).FitHeight();

                col.Item().PaddingTop(24).AlignCenter().Element(e =>
                    e.Border(1.5f).BorderColor("#c19a44")
                     .Background(Colors.White)
                     .CornerRadius(20)
                     .PaddingVertical(6).PaddingHorizontal(20)
                     .Text("COMPROBANTE DE RESERVA").FontSize(14).Bold().FontColor("#c19a44")
                );

                col.Item().PaddingTop(2).AlignCenter().Text(t =>
                {
                    t.Span($"Reserva #{_reserva.Id}   ·   Estado: ").FontSize(9).FontColor(Colors.Grey.Darken1);
                    t.Span(estadoTexto).FontSize(9).Bold().FontColor(estadoColor);
                });

                // ── Datos del titular ──
                col.Item().PaddingTop(16).Text("DATOS DEL TITULAR").FontSize(10).Bold().FontColor(Colors.Grey.Darken3);

                FilaCampos(col,
                    ("TITULAR", _reserva.NombreHuesped),
                    ("TELÉFONO", string.IsNullOrEmpty(_reserva.Telefono) ? "No registrado" : _reserva.Telefono),
                    ("¿INCLUYE DESAYUNO?", _reserva.IncluyeDesayuno ? "Sí" : "No"));

                // ── Alojamiento ──
                col.Item().PaddingTop(12).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                col.Item().PaddingTop(16).Text(_reserva.EsGrupal ? $"UNIDADES DEL GRUPO ({_reserva.UnidadesGrupales.Count})" : "DATOS DEL ALOJAMIENTO")
                    .FontSize(10).Bold().FontColor(Colors.Grey.Darken3);

                if (!_reserva.EsGrupal)
                {
                    FilaCampos(col,
                        ("ALOJAMIENTO", _reserva.Alojamiento.Nombre),
                        ("TIPO", _reserva.Alojamiento.Tipo.ToString()),
                        ("CAPACIDAD MÁXIMA", $"{_reserva.Alojamiento.Capacidad} personas"),
                        ("HUÉSPEDES REGISTRADOS", $"{_reserva.CantidadHuespedes} personas"));
                }
                else
                {
                    foreach (var u in _reserva.UnidadesGrupales)
                    {
                        col.Item().PaddingTop(6).BorderLeft(2).BorderColor(Colors.Grey.Lighten1).PaddingLeft(8)
                            .Column(c =>
                        {
                            c.Item().Text($"{u.Alojamiento.Nombre}  (Cap. máx: {u.Alojamiento.Capacidad})").SemiBold();

                            if (u.Alojamiento.Tipo == TipoAlojamiento.Apart
                                && _apartComposicion != null
                                && _apartComposicion.TryGetValue(u.AlojamientoId, out var comp))
                                c.Item().Text($"Compuesto por: {comp}").FontSize(8).FontColor(Colors.Grey.Darken1);

                            c.Item().Text($"Personas asignadas: {u.CantidadHuespedes}").FontSize(8).FontColor(Colors.Grey.Darken1);

                            if (_diasAirePorCabana != null
                                && _diasAirePorCabana.TryGetValue(u.AlojamientoId, out var dias) && dias > 0)
                            {
                                var montoAire = dias * _reserva.PrecioAireDiario!.Value;
                                c.Item().Text($"Aire acond. adicional: {dias} día{(dias != 1 ? "s" : "")} × ${_reserva.PrecioAireDiario.Value:N0} = ${montoAire:N0}")
                                    .FontSize(8).FontColor(Colors.Blue.Darken1);
                            }
                        });
                    }

                    var capacidadTotal = _reserva.UnidadesGrupales.Sum(u => u.Alojamiento.Capacidad);
                    col.Item().PaddingTop(8)
                        .Text($"Capacidad máx. total: {capacidadTotal} personas   ·   Huéspedes registrados: {_reserva.CantidadHuespedes} personas")
                        .FontSize(8.5f).SemiBold();
                }

                // ── Fechas y estadía ──
                col.Item().PaddingTop(12).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                col.Item().PaddingTop(16).Text("FECHAS Y ESTADÍA").FontSize(10).Bold().FontColor(Colors.Grey.Darken3);

                var camposFechas = new List<(string, string)>
                {
                    ("INGRESO", _reserva.FechaIngreso.ToString("dd/MM/yyyy")),
                    ("EGRESO", _reserva.FechaSalida.ToString("dd/MM/yyyy")),
                    ("NOCHES", noches.ToString()),
                };
                if (tarifaDia.HasValue)
                    camposFechas.Add(("TARIFA POR DÍA", $"${Math.Round(tarifaDia.Value, 0):N0}"));

                FilaCampos(col, camposFechas.ToArray());

                if (diasAireTotalGlobal > 0)
                {
                    col.Item().PaddingTop(6).Column(c =>
                    {
                        c.Item().Text("AIRE ACONDICIONADO ADICIONAL")
                            .FontSize(7.5f).FontColor(Colors.Grey.Darken1).SemiBold();
                        c.Item().PaddingTop(1)
                            .Text($"{diasAireTotalGlobal} día{(diasAireTotalGlobal != 1 ? "s" : "")} × ${_reserva.PrecioAireDiario!.Value:N0} = ${subtotalAireGlobal:N0}")
                            .FontSize(9.5f).FontColor(Colors.Blue.Darken1);
                    });
                }

                // ── Resumen de montos ──
                col.Item().PaddingTop(12).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                col.Item().PaddingTop(16).Text("RESUMEN").FontSize(10).Bold().FontColor(Colors.Grey.Darken3);

                if (diasAireTotalGlobal > 0)
                {
                    col.Item().PaddingTop(6).Row(row =>
                    {
                        row.RelativeItem().Text("Subtotal alojamiento:").FontSize(9);
                        row.RelativeItem().AlignRight().Text($"${(_reserva.MontoTotal - subtotalAireGlobal):N0}").SemiBold();
                    });
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Subtotal aire acondicionado adicional:").FontSize(9);
                        row.RelativeItem().AlignRight().Text($"${subtotalAireGlobal:N0}").SemiBold();
                    });
                }

                // Bloque azul — aparece siempre, con o sin aire
                col.Item().PaddingTop(6).Background(Colors.Blue.Medium).Padding(8).Row(row =>
                {
                    row.RelativeItem().AlignMiddle().Text("TOTAL FINAL A COBRAR").FontColor(Colors.White).Bold();
                    row.RelativeItem().AlignRight().AlignMiddle()
                        .Text($"${_reserva.MontoTotal:N0}").FontColor(Colors.White).Bold().FontSize(12);
                });

                col.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Text("Estado de pago:").FontSize(9);
                    row.RelativeItem().AlignRight().Text(estadoPagoTexto).Bold().FontColor(estadoPagoColor);
                });
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text("Total cobrado:").FontSize(9);
                    row.RelativeItem().AlignRight().Text($"${totalCobrado:N0}").Bold().FontColor(Colors.Green.Darken1);
                });
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text("Saldo pendiente:").FontSize(9);
                    row.RelativeItem().AlignRight().Column(c =>
                    {
                        c.Item().AlignRight().Text($"${saldoPendiente:N0}").Bold()
                            .FontColor(saldoPendiente > 0 ? Colors.Red.Medium : Colors.Green.Medium);
                        if (saldoPendiente < 0)
                            c.Item().AlignRight().Text("(A devolver al cliente)").FontSize(7).FontColor(Colors.Grey.Darken1);
                    });
                });

                col.Item().PaddingTop(6)
                    .Text($"Fecha de registro: {_reserva.FechaCarga:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken1);

                // ── Tabla de pagos ──
                col.Item().PaddingTop(12).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                col.Item().PaddingTop(16).Text("PAGOS REGISTRADOS").FontSize(10).Bold().FontColor(Colors.Grey.Darken3);

                if (!_reserva.Pagos.Any())
                {
                    col.Item().PaddingTop(6).Text("Sin pagos registrados").FontSize(9).FontColor(Colors.Grey.Darken1).Italic();
                }
                else
                {
                    col.Item().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.9f);  // Fecha
                            columns.RelativeColumn(1.6f);  // Tipo
                            columns.RelativeColumn(1.6f);  // Método
                            columns.RelativeColumn(1.3f);  // Monto
                            columns.RelativeColumn(2.2f);  // Observaciones
                        });

                        table.Header(header =>
                        {
                            void CeldaHeader(string texto) =>
                                header.Cell().Background(Colors.Grey.Darken3).Padding(4)
                                    .Text(texto).FontColor(Colors.White).FontSize(9).SemiBold();

                            CeldaHeader("Fecha / Hora");
                            CeldaHeader("Tipo");
                            CeldaHeader("Método");
                            CeldaHeader("Monto");
                            CeldaHeader("Observaciones");
                        });

                        // Misma técnica que FacturacionPdfDocument: altura uniforme por fila
                        // + ClampLines, para que ninguna fila se parta entre páginas si algún
                        // día un comprobante con muchos pagos cruza un salto de página.
                        const float alturaFilaPago = 24f;

                        foreach (var pago in _reserva.Pagos)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).MinHeight(alturaFilaPago)
                                .AlignMiddle().Text(pago.Fecha.ToString("dd/MM/yy | HH:mm"));

                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).MinHeight(alturaFilaPago)
                                .AlignMiddle().Text(EnumDisplayHelper.NombreTipoPago(pago.TipoPago));

                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).MinHeight(alturaFilaPago)
                                .AlignMiddle().Text(EnumDisplayHelper.NombreMetodoPago(pago.MetodoPago));

                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).MinHeight(alturaFilaPago)
                                .AlignCenter().AlignMiddle().Text($"${pago.Monto:N0}")
                                .SemiBold().FontColor(pago.Monto < 0 ? Colors.Red.Medium : Colors.Black);

                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).MinHeight(alturaFilaPago)
                                .AlignMiddle().Text(pago.Observaciones ?? "—").FontColor(Colors.Grey.Darken1).ClampLines(1, "…");
                        }
                    });

                    // ── Totales al pie — misma estructura que la tabla de pagos ──────────────
                    // Usa EXACTAMENTE la misma definición de 5 columnas que la tabla de pagos
                    // (no una proporción aproximada): así el límite de columna coincide pixel
                    // a pixel, sin importar el contenido. La etiqueta ocupa las primeras 3
                    // columnas (Fecha+Tipo+Método) vía ColumnSpan(3), el monto va en la columna
                    // Monto, y la columna Observaciones queda vacía para no romper la grilla.
                    col.Item().Table(totales =>
                    {
                        totales.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1.9f);  // Fecha
                            c.RelativeColumn(1.6f);  // Tipo
                            c.RelativeColumn(1.6f);  // Método
                            c.RelativeColumn(1.3f);  // Monto
                            c.RelativeColumn(2.2f);  // Observaciones
                        });

                        // Fila 1: Total cobrado
                        totales.Cell().ColumnSpan(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                            .AlignRight().Text("Total cobrado:").FontSize(9).SemiBold();
                        totales.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                            .AlignCenter().Text($"${totalCobrado:N0}").FontSize(9).Bold().FontColor(Colors.Green.Darken1);
                        totales.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text("");

                        // Fila 2: TOTAL ESTADÍA — con fondo gris clarito para destacarla
                        totales.Cell().ColumnSpan(3).Background("#f8f9fa").Padding(4)
                            .AlignRight().Text("TOTAL ESTADÍA:").FontSize(9).Bold();
                        totales.Cell().Background("#f8f9fa").Padding(4)
                            .AlignCenter().Text($"${_reserva.MontoTotal:N0}").FontSize(9).Bold();
                        totales.Cell().Background("#f8f9fa").Padding(4).Text("");

                        // Fila 3: Saldo pendiente
                        totales.Cell().ColumnSpan(3).BorderTop(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                            .AlignRight().Text("Saldo pendiente:").FontSize(9).SemiBold();
                        totales.Cell().BorderTop(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                            .AlignCenter().Text($"${saldoPendiente:N0}").FontSize(9).Bold()
                            .FontColor(saldoPendiente > 0 ? Colors.Red.Medium : Colors.Green.Medium);
                        totales.Cell().BorderTop(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text("");
                    });
                }
            });

            page.Footer().AlignRight()
                .Text($"Generado el {DateTime.Now:dd/MM/yyyy} a las {DateTime.Now:HH:mm}")
                .FontSize(8).FontColor(Colors.Grey.Darken1);
        });
    }

    private static void FilaCampos(ColumnDescriptor col, params (string Label, string Valor)[] campos)
    {
        col.Item().PaddingTop(6).Row(row =>
        {
            foreach (var (label, valor) in campos)
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(label).FontSize(7.5f).FontColor(Colors.Grey.Darken1).SemiBold();
                    c.Item().PaddingTop(1).Text(valor).FontSize(9.5f);
                });
            }
        });
    }
}
