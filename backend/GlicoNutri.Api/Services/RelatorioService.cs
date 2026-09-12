using System.Globalization;
using GlicoNutri.Api.Data;
using GlicoNutri.Api.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GlicoNutri.Api.Services;

public record Relatorio(byte[] Conteudo, string NomeArquivo);

public interface IRelatorioService
{
    Task<Resultado<Relatorio>> GerarRelatorioGlicemicoAsync(
        long pacienteId, int dias, CancellationToken ct = default);

    Task<Resultado<Relatorio>> GerarRelatorioAntropometricoAsync(
        long pacienteId, int dias, CancellationToken ct = default);

    Task<Resultado<Relatorio>> GerarRelatorioCompletoAsync(
        long pacienteId, int dias, CancellationToken ct = default);
}

/// <summary>
/// RelatorioService do Diagrama de Classes V5.0 e do C4. Ao contrário do
/// DashboardService, este lê das tabelas de escrita — o C4 é explícito: consolida
/// o histórico clínico completo, e isso só para relatórios exportáveis.
/// </summary>
public class RelatorioService(GlicoNutriDbContext db) : IRelatorioService
{
    private static readonly CultureInfo Br = new("pt-BR");

    private const string CorPrimaria = "#00685D";
    private const string CorTexto = "#0F172A";
    private const string CorSuave = "#475569";
    private const string CorBorda = "#E2E8F0";
    private const string CorAlerta = "#DC2626";

    static RelatorioService() => QuestPDF.Settings.License = LicenseType.Community;

    public Task<Resultado<Relatorio>> GerarRelatorioGlicemicoAsync(
        long pacienteId, int dias, CancellationToken ct = default) =>
        GerarAsync(pacienteId, dias, glicemia: true, antropometria: false, plano: false,
            "relatorio-glicemico", ct);

    public Task<Resultado<Relatorio>> GerarRelatorioAntropometricoAsync(
        long pacienteId, int dias, CancellationToken ct = default) =>
        GerarAsync(pacienteId, dias, glicemia: false, antropometria: true, plano: false,
            "relatorio-antropometrico", ct);

    public Task<Resultado<Relatorio>> GerarRelatorioCompletoAsync(
        long pacienteId, int dias, CancellationToken ct = default) =>
        GerarAsync(pacienteId, dias, glicemia: true, antropometria: true, plano: true,
            "relatorio-completo", ct);

    private async Task<Resultado<Relatorio>> GerarAsync(
        long pacienteId, int dias, bool glicemia, bool antropometria, bool plano,
        string prefixo, CancellationToken ct)
    {
        var paciente = await db.Pacientes
            .Include(p => p.Sexo)
            .Include(p => p.TipoDiabetes)
            .Include(p => p.Vinculos.Where(v => v.Ativo)).ThenInclude(v => v.Nutricionista)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == pacienteId, ct);

        if (paciente is null) return Resultado<Relatorio>.Erro("Paciente não encontrado.");

        var inicio = DateTime.UtcNow.AddDays(-dias);

        var glicemias = glicemia
            ? await db.RegistrosGlicemia
                .Include(r => r.Contexto)
                .Where(r => r.PacienteId == pacienteId && r.DataHora >= inicio)
                .OrderByDescending(r => r.DataHora)
                .ToListAsync(ct)
            : [];

        var medidas = antropometria
            ? await db.RegistrosAntropometricos
                .Where(r => r.PacienteId == pacienteId && r.DataHora >= inicio)
                .OrderByDescending(r => r.DataHora)
                .ToListAsync(ct)
            : [];

        var planoAtivo = plano
            ? await db.PlanosAlimentares
                .Include(p => p.Distribuicao)
                .FirstOrDefaultAsync(p => p.PacienteId == pacienteId && p.Ativo, ct)
            : null;

        var vet = plano
            ? await db.NecessidadesEnergeticas
                .Include(n => n.Formula)
                .Include(n => n.NivelAtividade)
                .Where(n => n.PacienteId == pacienteId)
                .OrderByDescending(n => n.DataCalculo).ThenByDescending(n => n.Id)
                .FirstOrDefaultAsync(ct)
            : null;

        var responsavel = paciente.Vinculos.FirstOrDefault()?.Nutricionista.Nome;

        var pdf = Documento(paciente, responsavel, dias, glicemias, medidas, planoAtivo, vet)
            .GeneratePdf();

        var arquivo = $"{prefixo}-{paciente.Nome.Split(' ')[0].ToLowerInvariant()}-" +
                      $"{DateTime.UtcNow:yyyyMMdd}.pdf";

        return Resultado<Relatorio>.Ok(new Relatorio(pdf, arquivo));
    }

    private static IDocument Documento(
        Paciente paciente, string? responsavel, int dias,
        List<RegistroGlicemia> glicemias, List<RegistroAntropometrico> medidas,
        PlanoAlimentar? plano, NecessidadeEnergetica? vet) =>
        Document.Create(doc =>
        {
            doc.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(2, Unit.Centimetre);
                pagina.DefaultTextStyle(t => t.FontSize(10).FontColor(CorTexto).FontFamily("Helvetica"));

                pagina.Header().Element(e => Cabecalho(e, paciente, responsavel, dias));

                pagina.Content().PaddingVertical(14).Column(col =>
                {
                    col.Spacing(18);

                    if (glicemias.Count > 0 || medidas.Count == 0)
                        col.Item().Element(e => SecaoGlicemia(e, paciente, glicemias, dias));

                    if (medidas.Count > 0)
                        col.Item().Element(e => SecaoAntropometria(e, medidas));

                    if (vet is not null || plano is not null)
                        col.Item().Element(e => SecaoPlano(e, vet, plano));
                });

                pagina.Footer().Element(Rodape);
            });
        });

    /// <summary>
    /// Marca do projeto: gota com check. O mesmo desenho do favicon e das telas —
    /// o relatório é o documento que sai do sistema e vai para a mão do paciente.
    /// </summary>
    private const string MarcaSvg = """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 40 40" fill="none">
          <path d="M20 38C27.732 38 34 31.732 34 24C34 16.268 20 2 20 2C20 2 6 16.268 6 24C6 31.732 12.268 38 20 38Z" fill="#00897B"/>
          <path d="M15 24L18.5 27.5L25.5 20.5" stroke="white" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>
        """;

    private static void Cabecalho(IContainer e, Paciente paciente, string? responsavel, int dias) =>
        e.Column(col =>
        {
            col.Item().Row(linha =>
            {
                linha.ConstantItem(30).PaddingTop(2).Height(26).Svg(MarcaSvg);
                linha.ConstantItem(8);

                linha.RelativeItem().Column(c =>
                {
                    c.Item().Text("GlicoNutri").FontSize(17).Bold().FontColor(CorPrimaria);
                    c.Item().Text("Relatório de acompanhamento nutricional")
                        .FontSize(9).FontColor(CorSuave);
                });

                linha.ConstantItem(170).AlignRight().Column(c =>
                {
                    c.Item().Text($"Emitido em {DateTime.Now.ToString("dd/MM/yyyy 'às' HH:mm", Br)}")
                        .FontSize(8).FontColor(CorSuave);
                    c.Item().Text($"Período analisado: {dias} dias")
                        .FontSize(8).FontColor(CorSuave);
                });
            });

            col.Item().PaddingTop(10).BorderBottom(1).BorderColor(CorBorda);

            col.Item().PaddingTop(10).Row(linha =>
            {
                var idade = CalculadoraNutricional.CalcularIdade(
                    paciente.DataNascimento, DateOnly.FromDateTime(DateTime.UtcNow));

                linha.RelativeItem().Column(c =>
                {
                    c.Item().Text(paciente.Nome).FontSize(13).Bold();
                    c.Item().Text($"{idade} anos · {paciente.Sexo.Descricao} · {paciente.TipoDiabetes.Descricao}")
                        .FontSize(9).FontColor(CorSuave);
                });

                linha.ConstantItem(200).AlignRight().Column(c =>
                {
                    if (responsavel is not null)
                        c.Item().Text($"Nutricionista: {responsavel}").FontSize(9).FontColor(CorSuave);

                    var personalizada = paciente.GlicemiaMinAlvo is not null
                                     && paciente.GlicemiaMaxAlvo is not null;

                    var min = paciente.GlicemiaMinAlvo ?? CalculadoraNutricional.GlicemiaMinPadrao;
                    var max = paciente.GlicemiaMaxAlvo ?? CalculadoraNutricional.GlicemiaMaxPadrao;

                    // RN20 — o relatório precisa deixar claro se a faixa é a do
                    // paciente ou o padrão clínico ainda não personalizado.
                    c.Item().Text($"Faixa alvo: {min:0}–{max:0} mg/dL"
                                  + (personalizada ? string.Empty : " (padrão)"))
                        .FontSize(9).FontColor(CorSuave);
                });
            });
        });

    private static void SecaoGlicemia(
        IContainer e, Paciente paciente, List<RegistroGlicemia> registros, int dias) =>
        e.Column(col =>
        {
            col.Item().Element(c => Titulo(c, "Monitoramento glicêmico"));

            if (registros.Count == 0)
            {
                col.Item().PaddingTop(6).Text($"Nenhum registro de glicemia nos últimos {dias} dias.")
                    .FontSize(9).FontColor(CorSuave);
                return;
            }

            var noAlvo = registros.Count(r => !r.ForaDoAlvo) * 100.0 / registros.Count;

            col.Item().PaddingTop(8).Row(linha =>
            {
                void Indicador(string rotulo, string valor, string? cor = null) =>
                    linha.RelativeItem().Column(c =>
                    {
                        c.Item().Text(rotulo.ToUpperInvariant()).FontSize(7).FontColor(CorSuave);
                        c.Item().Text(valor).FontSize(14).Bold().FontColor(cor ?? CorTexto);
                    });

                Indicador("Registros", registros.Count.ToString());
                Indicador("Média", $"{registros.Average(r => r.Valor):0} mg/dL");
                Indicador("Mínima", $"{registros.Min(r => r.Valor):0}");
                Indicador("Máxima", $"{registros.Max(r => r.Valor):0}");
                Indicador("No alvo", $"{noAlvo:0.#}%", noAlvo < 70 ? CorAlerta : CorPrimaria);
            });

            col.Item().PaddingTop(12).Table(tabela =>
            {
                tabela.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2.2f);
                    c.RelativeColumn(1.4f);
                    c.RelativeColumn(2.4f);
                    c.RelativeColumn(1.6f);
                    c.RelativeColumn(3f);
                });

                CabecalhoTabela(tabela, "Data e hora", "Valor", "Contexto", "Situação", "Observação");

                foreach (var r in registros.Take(60))
                {
                    var situacao = CalculadoraNutricional.ClassificarGlicemia(r.Valor) switch
                    {
                        "HIPOGLICEMIA" => "Hipoglicemia",
                        "HIPERGLICEMIA" => "Hiperglicemia",
                        _ => r.ForaDoAlvo ? "Fora do alvo" : "No alvo",
                    };

                    var cor = r.ForaDoAlvo ? CorAlerta : CorTexto;

                    Celula(tabela, r.DataHora.ToLocalTime().ToString("dd/MM/yy HH:mm", Br));
                    Celula(tabela, $"{r.Valor:0} mg/dL", cor, negrito: true);
                    Celula(tabela, r.Contexto.Descricao);
                    Celula(tabela, situacao, cor);
                    Celula(tabela, r.Observacao ?? "—");
                }
            });

            if (registros.Count > 60)
                col.Item().PaddingTop(6)
                    .Text($"Exibindo os 60 registros mais recentes de {registros.Count} no período.")
                    .FontSize(8).Italic().FontColor(CorSuave);
        });

    private static void SecaoAntropometria(IContainer e, List<RegistroAntropometrico> medidas) =>
        e.Column(col =>
        {
            col.Item().Element(c => Titulo(c, "Evolução antropométrica"));

            var atual = medidas.First();
            var primeira = medidas.Last();

            col.Item().PaddingTop(8).Row(linha =>
            {
                void Indicador(string rotulo, string valor) =>
                    linha.RelativeItem().Column(c =>
                    {
                        c.Item().Text(rotulo.ToUpperInvariant()).FontSize(7).FontColor(CorSuave);
                        c.Item().Text(valor).FontSize(14).Bold();
                    });

                Indicador("Peso atual", $"{atual.Peso:0.#} kg");
                Indicador("IMC", $"{atual.Imc:0.##}");
                Indicador("Classificação", atual.ClassificacaoImc ?? "—");

                if (medidas.Count > 1)
                {
                    var delta = atual.Peso - primeira.Peso;
                    Indicador("Variação", $"{(delta > 0 ? "+" : string.Empty)}{delta:0.#} kg");
                }
            });

            col.Item().PaddingTop(12).Table(tabela =>
            {
                tabela.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2f);
                    c.RelativeColumn(1.4f);
                    c.RelativeColumn(1.4f);
                    c.RelativeColumn(1.2f);
                    c.RelativeColumn(2.2f);
                    c.RelativeColumn(1.2f);
                });

                CabecalhoTabela(tabela, "Data", "Peso", "Altura", "IMC", "Classificação", "RCQ");

                foreach (var m in medidas.Take(40))
                {
                    Celula(tabela, m.DataHora.ToLocalTime().ToString("dd/MM/yyyy", Br));
                    Celula(tabela, $"{m.Peso:0.#} kg", negrito: true);
                    Celula(tabela, $"{m.Altura:0.#} cm");
                    Celula(tabela, m.Imc?.ToString("0.##") ?? "—");
                    Celula(tabela, m.ClassificacaoImc ?? "—");
                    Celula(tabela, m.Rcq?.ToString("0.##") ?? "—");
                }
            });
        });

    private static void SecaoPlano(IContainer e, NecessidadeEnergetica? vet, PlanoAlimentar? plano) =>
        e.Column(col =>
        {
            col.Item().Element(c => Titulo(c, "Prescrição nutricional"));

            if (vet is null && plano is null)
            {
                col.Item().PaddingTop(6)
                    .Text("Sem cálculo energético ou plano alimentar registrado.")
                    .FontSize(9).FontColor(CorSuave);
                return;
            }

            if (vet is not null)
            {
                col.Item().PaddingTop(8).Text(t =>
                {
                    t.Span("Necessidade energética: ").FontColor(CorSuave);
                    t.Span($"{vet.ValorKcal:0} kcal/dia").Bold();
                    t.Span($"  ·  {vet.Formula.Descricao}  ·  {vet.NivelAtividade.Descricao}")
                        .FontColor(CorSuave);
                    t.Span($"  ·  calculado em {vet.DataCalculo.ToString("dd/MM/yyyy", Br)}")
                        .FontColor(CorSuave);
                });
            }

            if (plano?.Distribuicao is { } d)
            {
                col.Item().PaddingTop(4).Text(t =>
                {
                    t.Span("Plano vigente: ").FontColor(CorSuave);
                    t.Span(plano.Objetivo ?? "—").Bold();
                    t.Span($"  ·  desde {plano.DataInicio.ToString("dd/MM/yyyy", Br)}")
                        .FontColor(CorSuave);
                });

                col.Item().PaddingTop(10).Table(tabela =>
                {
                    tabela.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2f);
                        c.RelativeColumn(1.2f);
                        c.RelativeColumn(1.2f);
                    });

                    CabecalhoTabela(tabela, "Macronutriente", "Percentual", "Gramas/dia");

                    void Linha(string nome, double pct, double? g)
                    {
                        Celula(tabela, nome);
                        Celula(tabela, $"{pct:0.#}%");
                        Celula(tabela, g is { } v ? $"{v:0.#} g" : "—");
                    }

                    Linha("Carboidratos", d.CarboidratosPercentual, d.CarboidratosGramas);
                    Linha("Proteínas", d.ProteinasPercentual, d.ProteinasGramas);
                    Linha("Lipídios", d.LipidiosPercentual, d.LipidiosGramas);
                });
            }
        });

    // ── Elementos reutilizados ──────────────────────────────────────────────

    private static void Titulo(IContainer e, string texto) =>
        e.BorderBottom(1).BorderColor(CorBorda).PaddingBottom(4)
            .Text(texto).FontSize(12).Bold().FontColor(CorPrimaria);

    private static void CabecalhoTabela(TableDescriptor tabela, params string[] colunas)
    {
        tabela.Header(cab =>
        {
            foreach (var titulo in colunas)
            {
                cab.Cell().BorderBottom(1).BorderColor(CorBorda).PaddingVertical(4)
                    .Text(titulo.ToUpperInvariant()).FontSize(7).FontColor(CorSuave);
            }
        });
    }

    private static void Celula(TableDescriptor tabela, string texto, string? cor = null, bool negrito = false)
    {
        var celula = tabela.Cell().BorderBottom(1).BorderColor(CorBorda).PaddingVertical(4);
        var span = celula.Text(texto).FontSize(9).FontColor(cor ?? CorTexto);
        if (negrito) span.Bold();
    }

    private static void Rodape(IContainer e) =>
        e.BorderTop(1).BorderColor(CorBorda).PaddingTop(6).Row(linha =>
        {
            linha.RelativeItem().Text(
                "GlicoNutri · ADJ — Associação de Diabetes Juvenil de Birigui\n" +
                "Documento de apoio clínico; não substitui a avaliação do profissional de saúde.")
                .FontSize(7).FontColor(CorSuave);

            linha.ConstantItem(70).AlignRight().AlignBottom().Text(t =>
            {
                t.DefaultTextStyle(s => s.FontSize(7).FontColor(CorSuave));
                t.CurrentPageNumber();
                t.Span(" / ");
                t.TotalPages();
            });
        });
}
