using BrisasDeOro.Web.Helpers;
using BrisasDeOro.Web.Models.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BrisasDeOro.Web.Services;

public class FacturacionPdfDocument : IDocument
{
    // Altura mínima reservada, uniforme entre TODAS las celdas de una misma fila.
    // Necesaria para que QuestPDF decida la paginación de forma consistente entre
    // celdas: si cada celda mide su altura de forma independiente (algunas de 1
    // línea, otras de 2), la fila puede terminar partida entre dos páginas — con
    // una altura explícita e idéntica en todas las celdas de la fila, eso no pasa
    // (verificado empíricamente con nombres largos exactamente en el borde de página).
    // El valor varía de fila a fila (ver AlturaFila), pero dentro de una misma fila
    // las 7-8 celdas siempre usan el mismo valor entre sí.
    private const float RowHeightCorta = 24f;  // fila de 1 línea
    private const float RowHeightLarga = 40f;  // fila de 2 líneas

    // Umbral aproximado de caracteres antes de que una columna angosta necesite
    // ajustar a 2 líneas. Confirmado empíricamente con el test de estrés (150 filas
    // sintéticas + "Carina Beatriz Ortega" en el borde de página).
    private const int UmbralCaracteres = 22;

    private static float AlturaFila(string? vinculadaConNombre, params string[] textosColumna)
    {
        if (!string.IsNullOrEmpty(vinculadaConNombre)) return RowHeightLarga; // el badge siempre agrega una línea
        return textosColumna.Any(t => (t?.Length ?? 0) > UmbralCaracteres) ? RowHeightLarga : RowHeightCorta;
    }

    private readonly string _modo;
    private readonly Dictionary<string, string> _filtros;
    private readonly List<MovimientoViewModel>? _movimientos;
    private readonly decimal _totalPeriodo;
    private readonly List<SaldoPendienteViewModel>? _saldos;

    private FacturacionPdfDocument(
        string modo, Dictionary<string, string> filtros,
        List<MovimientoViewModel>? movimientos, decimal totalPeriodo,
        List<SaldoPendienteViewModel>? saldos)
    {
        _modo         = modo;
        _filtros      = filtros;
        _movimientos  = movimientos;
        _totalPeriodo = totalPeriodo;
        _saldos       = saldos;
    }

    public static FacturacionPdfDocument ParaMovimientos(
        Dictionary<string, string> filtros, List<MovimientoViewModel> movimientos, decimal totalPeriodo)
        => new("Movimientos", filtros, movimientos, totalPeriodo, null);

    public static FacturacionPdfDocument ParaSaldos(
        Dictionary<string, string> filtros, List<SaldoPendienteViewModel> saldos)
        => new("Saldos", filtros, null, 0, saldos);

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        var logoPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "images", "logo.png");
        if (!File.Exists(logoPath))
            logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo.png");
        var titulo = _modo == "Saldos" ? "INFORME DE SALDOS PENDIENTES" : "INFORME DE MOVIMIENTOS";

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(0.7f, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(9));

            page.Content().Column(col =>
            {
                col.Item().AlignCenter().Height(90).Image(logoPath).FitHeight();

                col.Item().PaddingTop(24).AlignCenter().Element(e =>
                    e.Border(1.5f).BorderColor("#c19a44")
                     .Background(Colors.White)
                     .CornerRadius(20)
                     .PaddingVertical(6).PaddingHorizontal(20)
                     .Text(titulo).FontSize(14).Bold().FontColor("#c19a44")
                );

                foreach (var filtro in _filtros)
                    col.Item().PaddingTop(2)
                        .Text($"{filtro.Key}: {filtro.Value}").FontSize(9).FontColor(Colors.Grey.Darken1);

                if (_modo == "Saldos")
                    ComponerTablaSaldos(col);
                else
                    ComponerTablaMovimientos(col);
            });

            page.Footer().AlignRight()
                .Text($"Generado el {DateTime.Now:dd/MM/yyyy} a las {DateTime.Now:HH:mm}")
                .FontSize(8).FontColor(Colors.Grey.Darken1);
        });
    }

    private void ComponerTablaMovimientos(ColumnDescriptor col)
    {
        col.Item().PaddingTop(6).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1.9f);   // Fecha
                columns.RelativeColumn(2.2f);   // Titular
                columns.RelativeColumn(1.8f);   // Alojamiento
                columns.RelativeColumn(1.4f);   // Concepto
                columns.RelativeColumn(1.35f);  // Método
                columns.RelativeColumn(1.05f);  // Monto
                columns.RelativeColumn(1.6f);   // Observaciones
            });

            table.Header(header =>
            {
                void CeldaHeader(string texto) =>
                    header.Cell().Background(Colors.Grey.Darken3).Padding(4)
                        .Text(texto).FontColor(Colors.White).FontSize(9).SemiBold();

                CeldaHeader("Fecha");
                CeldaHeader("Titular");
                CeldaHeader("Alojamiento");
                CeldaHeader("Concepto");
                CeldaHeader("Método");
                CeldaHeader("Monto");
                CeldaHeader("Observaciones");
            });

            foreach (var mov in _movimientos!)
            {
                try
                {
                    var alojamiento = mov.EsGrupal
                        ? $"Grupo ({mov.Unidades.Count} unidades)"
                        : mov.NombreAlojamiento;

                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                        .Text(mov.Fecha.ToString("dd/MM/yy | HH:mm"));

                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                        .Column(c =>
                    {
                        c.Item().Text(mov.NombreHuesped).SemiBold().ClampLines(2, "…");
                        if (mov.VinculadaConNombre != null)
                            c.Item().Text($"Vinculada con: {mov.VinculadaConNombre}")
                                .FontSize(7).FontColor(Colors.Blue.Darken1).ClampLines(1, "…");
                    });

                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                        .Text(alojamiento).ClampLines(2, "…");

                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                        .Text(EnumDisplayHelper.NombreTipoPago(mov.TipoPago));

                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                        .Text(EnumDisplayHelper.NombreMetodoPago(mov.MetodoPago));

                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                        .AlignRight().Text($"${mov.Monto:N0}").FontSize(8.5f).SemiBold();

                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                        .Text(mov.Observaciones ?? "—").FontColor(Colors.Grey.Darken1).ClampLines(2, "…");
                }
                catch (Exception ex)
                {
                    // Fila de error visible para diagnóstico
                    table.Cell().ColumnSpan(7).Background(Colors.Red.Lighten3).Padding(4)
                        .Text($"ERROR en fila: {ex.Message}").FontSize(8).FontColor(Colors.Red.Darken3);
                }
            }
        });

        // Misma técnica que ComprobanteReservaPdfDocument: usa EXACTAMENTE la misma
        // definición de columnas que la tabla de arriba (no una proporción aproximada),
        // así el total queda alineado pixel a pixel bajo la columna Monto.
        col.Item().Table(totalRow =>
        {
            totalRow.ColumnsDefinition(c =>
            {
                c.RelativeColumn(1.9f);   // Fecha
                c.RelativeColumn(2.2f);   // Titular
                c.RelativeColumn(1.8f);   // Alojamiento
                c.RelativeColumn(1.4f);   // Concepto
                c.RelativeColumn(1.35f);  // Método
                c.RelativeColumn(1.05f);  // Monto
                c.RelativeColumn(1.6f);   // Observaciones
            });

            totalRow.Cell().ColumnSpan(5).Background("#d1e7dd").Padding(6)
                .AlignRight().Text("TOTAL DEL PERÍODO:").FontSize(9).Bold();

            // Padding(4) y FontSize(8.5) — mismos valores que ya usan las celdas de datos
            // de Monto en esta tabla (confirmado con el motor real: la columna de 51.6pt
            // no entra con Padding(6)/FontSize(10), ni siquiera para "$360.000").
            totalRow.Cell().Background("#d1e7dd").Padding(4)
                .AlignRight().AlignMiddle().Text($"${_totalPeriodo:N0}").FontSize(8.5f).Bold().FontColor("#0a3622");

            totalRow.Cell().Background("#d1e7dd").Padding(6).Text("");
        });
    }

    private void ComponerTablaSaldos(ColumnDescriptor col)
    {
        col.Item().PaddingTop(6).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2.0f);  // Titular
                columns.RelativeColumn(1.5f);  // Alojamiento
                columns.RelativeColumn(1.1f);  // Ingreso (fecha corta, formato fijo dd/mm/aaaa)
                columns.RelativeColumn(1.1f);  // Egreso (ídem)
                columns.RelativeColumn(1.4f);  // Total estadía
                columns.RelativeColumn(1.0f);  // Cobrado
                columns.RelativeColumn(1.4f);  // Saldo pendiente
                columns.RelativeColumn(1.3f);  // Estado de pago
            });

            table.Header(header =>
            {
                void CeldaHeader(string texto, HorizontalAlignment align = HorizontalAlignment.Left)
                {
                    var celda = header.Cell().Background(Colors.Grey.Darken3).Padding(4);
                    var texto2 = align == HorizontalAlignment.Right ? celda.AlignRight() : celda;
                    texto2.Text(texto).FontColor(Colors.White).FontSize(9).SemiBold();
                }

                CeldaHeader("Titular");
                CeldaHeader("Alojamiento");
                CeldaHeader("Ingreso");
                CeldaHeader("Egreso");
                CeldaHeader("Total estadía");
                CeldaHeader("Cobrado");
                CeldaHeader("Saldo pend.");
                CeldaHeader("Estado pago", HorizontalAlignment.Right);
            });

            foreach (var s in _saldos!)
            {
                var alojamiento = s.EsGrupal
                    ? $"Grupo ({s.Unidades.Count} unidades)"
                    : s.NombreAlojamiento;

                var colorSaldo = s.SaldoPendiente > 0 ? Colors.Red.Medium : Colors.Green.Medium;

                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                    .Column(c =>
                {
                    c.Item().Text(s.NombreHuesped).SemiBold().ClampLines(2, "…");
                    if (s.VinculadaConNombre != null)
                        c.Item().Text($"Vinculada con: {s.VinculadaConNombre}")
                            .FontSize(7).FontColor(Colors.Blue.Darken1).ClampLines(1, "…");
                });

                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                    .Text(alojamiento).ClampLines(2, "…");

                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                    .Text(s.FechaIngreso.ToString("dd/MM/yy"));

                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                    .Text(s.FechaSalida.ToString("dd/MM/yy"));

                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                    .AlignRight().Text($"${s.MontoTotal:N0}");

                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                    .AlignRight().Text($"${s.TotalCobrado:N0}").FontColor(Colors.Green.Darken1);

                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                    .AlignRight().Text($"${s.SaldoPendiente:N0}").SemiBold().FontColor(colorSaldo);

                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                    .AlignRight().Text(s.EstadoPagoTexto);
            }
        });

        // Misma técnica que ComprobanteReservaPdfDocument: usa EXACTAMENTE la misma
        // definición de columnas que la tabla de arriba (no una proporción aproximada),
        // así el total queda alineado pixel a pixel bajo la columna Saldo pendiente.
        col.Item().Table(totalRow =>
        {
            totalRow.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2.0f);  // Titular
                c.RelativeColumn(1.5f);  // Alojamiento
                c.RelativeColumn(1.1f);  // Ingreso
                c.RelativeColumn(1.1f);  // Egreso
                c.RelativeColumn(1.4f);  // Total estadía
                c.RelativeColumn(1.0f);  // Cobrado
                c.RelativeColumn(1.4f);  // Saldo pendiente
                c.RelativeColumn(1.3f);  // Estado pago
            });

            totalRow.Cell().ColumnSpan(6).Background("#f8d7da").Padding(6)
                .AlignRight().Text("TOTAL SALDO PENDIENTE:").FontSize(9).Bold();

            totalRow.Cell().Background("#f8d7da").PaddingVertical(6).PaddingRight(4)
                .AlignRight().AlignMiddle().Text($"${_saldos!.Sum(s => s.SaldoPendiente):N0}").FontSize(10f).Bold().FontColor("#842029");

            totalRow.Cell().Background("#f8d7da").Padding(6).Text("");
        });
    }
}
