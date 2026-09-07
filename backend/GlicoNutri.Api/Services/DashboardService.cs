using GlicoNutri.Api.Data;
using GlicoNutri.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace GlicoNutri.Api.Services;

public interface IDashboardService
{
    Task<DashboardResponse> ObterAsync(
        long? nutricionistaId, CancellationToken ct = default);
}

/// <summary>
/// UC011 — Dashboard do Nutricionista.
///
/// Todos os indicadores clínicos vêm de resumo_clinico_paciente, o Read Model,
/// como determinam a nota do DER V2.0 e o C4: nenhuma agregação é calculada
/// sobre as tabelas de escrita. As únicas leituras fora do Read Model são o nome
/// do paciente e o vínculo com o nutricionista — identificação e permissão, não
/// indicador clínico, e a RN01 do UC011 exige o vínculo para filtrar o painel.
/// </summary>
public class DashboardService(GlicoNutriDbContext db) : IDashboardService
{
    /// <summary>Dias sem registro a partir dos quais o paciente entra no painel de pendências.</summary>
    private const int DiasSemRegistroParaAlerta = 3;

    /// <summary>Faixas de controle glicêmico, pelo percentual de medições no alvo.</summary>
    private const double ControleBom = 70;
    private const double ControleRegular = 40;

    private const string Critico = "CRITICO";
    private const string Atencao = "ATENCAO";
    private const string Informativo = "INFORMATIVO";

    private static readonly Dictionary<string, int> OrdemSeveridade = new()
    {
        [Critico] = 0,
        [Atencao] = 1,
        [Informativo] = 2,
    };

    public async Task<DashboardResponse> ObterAsync(
        long? nutricionistaId, CancellationToken ct = default)
    {
        var consulta = db.ResumoClinicoPaciente
            .Include(r => r.UltimaGlicemiaContexto)
            .AsQueryable();

        // RN01 do UC011 — o Nutricionista vê apenas os pacientes vinculados a ele.
        // O Administrador, que herda o perfil, vê todos.
        if (nutricionistaId is { } id)
        {
            consulta = consulta.Where(r => r.Paciente.Vinculos.Any(
                v => v.NutricionistaId == id && v.Ativo));
        }

        var linhas = await consulta
            .Select(r => new
            {
                r.PacienteId,
                Nome = r.Paciente.Nome,
                r.UltimaGlicemiaValor,
                Contexto = r.UltimaGlicemiaContexto != null ? r.UltimaGlicemiaContexto.Descricao : null,
                r.UltimaGlicemiaData,
                r.MediaGlicemia7Dias,
                r.PercentualNoAlvo7Dias,
                r.UltimoImc,
                r.UltimaClassificacaoImc,
                r.UltimaDataAntropometria,
                r.PlanoAtivo,
                r.AlertasPendentesCount,
                r.DiasSemRegistroGlicemia,
                // A faixa alvo mora no paciente, não no resumo; é o que permite
                // sinalizar a pendência da RN20 no painel.
                r.Paciente.GlicemiaMinAlvo,
                r.Paciente.GlicemiaMaxAlvo,
            })
            .ToListAsync(ct);

        var hoje = DateTime.UtcNow.Date;
        var alertas = new List<AlertaDashboard>();
        var pacientes = new List<PacienteDashboard>();

        foreach (var l in linhas)
        {
            var doPaciente = new List<AlertaDashboard>();

            void Registrar(string tipo, string severidade, string descricao, DateTime? quando) =>
                doPaciente.Add(new AlertaDashboard(l.PacienteId, l.Nome, tipo, severidade, descricao, quando));

            var min = l.GlicemiaMinAlvo ?? CalculadoraNutricional.GlicemiaMinPadrao;
            var max = l.GlicemiaMaxAlvo ?? CalculadoraNutricional.GlicemiaMaxPadrao;

            if (l.UltimaGlicemiaValor is { } valor)
            {
                // Hipoglicemia vem antes da hiperglicemia na ordem de criticidade:
                // o risco agudo é maior e demanda ação imediata.
                if (valor < CalculadoraNutricional.GlicemiaMinPadrao)
                    Registrar("HIPOGLICEMIA", Critico,
                        $"Última glicemia em {valor:0} mg/dL.", l.UltimaGlicemiaData);
                else if (valor > CalculadoraNutricional.GlicemiaMaxPadrao)
                    Registrar("HIPERGLICEMIA", Critico,
                        $"Última glicemia em {valor:0} mg/dL.", l.UltimaGlicemiaData);
                else if (valor < min || valor > max)
                    Registrar("FORA_DO_ALVO", Atencao,
                        $"Última glicemia em {valor:0} mg/dL, fora da faixa alvo de {min:0}–{max:0}.",
                        l.UltimaGlicemiaData);
            }

            if (l.DiasSemRegistroGlicemia is { } dias && dias >= DiasSemRegistroParaAlerta)
                Registrar("SEM_REGISTRO", Atencao,
                    $"Sem registro de glicemia há {dias} dia(s).", l.UltimaGlicemiaData);

            if (l.UltimaGlicemiaData is null)
                Registrar("SEM_REGISTRO", Atencao,
                    "Nenhum registro de glicemia até o momento.", null);

            if (!l.PlanoAtivo)
                Registrar("SEM_PLANO", Atencao, "Sem plano alimentar ativo.", null);

            if (l.AlertasPendentesCount > 0)
                Registrar("ALERTAS_PENDENTES", Informativo,
                    $"{l.AlertasPendentesCount} alerta(s) aguardando resposta.", null);

            // RN20 — o sistema deve sinalizar que a faixa ainda é a padrão.
            if (l.GlicemiaMinAlvo is null || l.GlicemiaMaxAlvo is null)
                Registrar("FAIXA_PADRAO", Informativo,
                    "Faixa glicêmica ainda não personalizada; em uso o padrão de 70–180 mg/dL.", null);

            alertas.AddRange(doPaciente);

            pacientes.Add(new PacienteDashboard(
                l.PacienteId, l.Nome, l.UltimaGlicemiaValor, l.Contexto, l.UltimaGlicemiaData,
                l.MediaGlicemia7Dias, l.PercentualNoAlvo7Dias, l.UltimoImc, l.UltimaClassificacaoImc,
                l.UltimaDataAntropometria, l.PlanoAtivo, l.AlertasPendentesCount,
                l.DiasSemRegistroGlicemia,
                doPaciente.Count > 0
                    ? doPaciente.MinBy(a => OrdemSeveridade[a.Severidade])!.Severidade
                    : null));
        }

        // RN03 do UC011 — criticidade primeiro, data de ocorrência depois.
        var alertasOrdenados = alertas
            .OrderBy(a => OrdemSeveridade[a.Severidade])
            .ThenByDescending(a => a.Ocorrencia ?? DateTime.MinValue)
            .ThenBy(a => a.PacienteNome)
            .ToList();

        var comMedia = linhas.Where(l => l.MediaGlicemia7Dias is not null).ToList();
        var comAlvo = linhas.Where(l => l.PercentualNoAlvo7Dias is not null).ToList();

        var indicadores = new IndicadoresDashboard(
            linhas.Count,
            linhas.Count(l => l.UltimaGlicemiaData is { } d && d.Date == hoje),
            pacientes.Count(p => p.Severidade is Critico or Atencao),
            linhas.Count(l => l.PlanoAtivo),
            comMedia.Count > 0 ? Math.Round(comMedia.Average(l => l.MediaGlicemia7Dias!.Value), 1) : null,
            comAlvo.Count > 0 ? Math.Round(comAlvo.Average(l => l.PercentualNoAlvo7Dias!.Value), 1) : null);

        // A4 do UC011 — distribuições consolidadas dos pacientes do painel.
        var controle = new List<FatiaDistribuicao>
        {
            new($"Bom (≥{ControleBom:0}% no alvo)",
                comAlvo.Count(l => l.PercentualNoAlvo7Dias >= ControleBom)),
            new($"Regular ({ControleRegular:0}–{ControleBom:0}%)",
                comAlvo.Count(l => l.PercentualNoAlvo7Dias >= ControleRegular
                                && l.PercentualNoAlvo7Dias < ControleBom)),
            new($"Ruim (<{ControleRegular:0}%)",
                comAlvo.Count(l => l.PercentualNoAlvo7Dias < ControleRegular)),
            new("Sem dados no período", linhas.Count - comAlvo.Count),
        };

        var imc = linhas
            .GroupBy(l => l.UltimaClassificacaoImc ?? "Sem medição")
            .Select(g => new FatiaDistribuicao(g.Key, g.Count()))
            .OrderByDescending(f => f.Quantidade)
            .ToList();

        return new DashboardResponse(
            DateTime.UtcNow, indicadores, alertasOrdenados,
            pacientes.OrderBy(p => p.Severidade is null)
                     .ThenBy(p => p.Severidade is null ? 9 : OrdemSeveridade[p.Severidade])
                     .ThenBy(p => p.Nome)
                     .ToList(),
            controle, imc);
    }
}
