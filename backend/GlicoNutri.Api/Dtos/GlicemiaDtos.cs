using System.ComponentModel.DataAnnotations;

namespace GlicoNutri.Api.Dtos;

// ── UC004 — Registrar Glicemia ──────────────────────────────────────────────

public record CriarRegistroGlicemiaRequest(
    /// <summary>Em mg/dL. RN18 e UC004 A1 — obrigatório e entre 0 e 600.</summary>
    [Range(0, 600)] double Valor,
    /// <summary>RN18 — contexto da medição é obrigatório.</summary>
    [Required] long ContextoId,
    /// <summary>Ausente, assume o momento atual; ajustável para registro retroativo (RN04 do UC004).</summary>
    DateTime? DataHora,
    /// <summary>UC004 A3 — nota livre opcional.</summary>
    [StringLength(500)] string? Observacao);

public record RegistroGlicemiaResponse(
    long Id,
    long PacienteId,
    double Valor,
    string Contexto,
    long ContextoId,
    DateTime DataHora,
    string? Observacao,
    /// <summary>RN19 — contra a faixa alvo individual do paciente.</summary>
    bool ForaDoAlvo,
    /// <summary>RN02 do UC004 — NORMAL, HIPOGLICEMIA ou HIPERGLICEMIA, por limiar clínico absoluto.</summary>
    string Classificacao,
    double MinAlvoAplicado,
    double MaxAlvoAplicado,
    /// <summary>RN20 — falso quando os limites ainda são o padrão, e não os do paciente.</summary>
    bool FaixaPersonalizada);

// ── UC005 — Visualizar Histórico Glicêmico ──────────────────────────────────

/// <summary>Painel de estatísticas do período, exibido no passo 2 do UC005.</summary>
public record ResumoGlicemico(
    int TotalRegistros,
    double? Media,
    double? Minimo,
    double? Maximo,
    /// <summary>Percentual de registros dentro da faixa alvo do paciente.</summary>
    double? PercentualNoAlvo,
    int RegistrosForaDoAlvo,
    int Hipoglicemias,
    int Hiperglicemias,
    double MinAlvoAplicado,
    double MaxAlvoAplicado,
    bool FaixaPersonalizada);

public record HistoricoGlicemicoResponse(
    int Dias,
    DateTime InicioPeriodo,
    ResumoGlicemico Resumo,
    IReadOnlyList<RegistroGlicemiaResponse> Registros);

/// <summary>Ponto da série do gráfico de evolução glicêmica (RF08.1).</summary>
public record PontoSerieGlicemia(DateTime DataHora, double Valor, string Contexto, bool ForaDoAlvo);

public record SerieGlicemicaResponse(
    int Dias,
    double MinAlvo,
    double MaxAlvo,
    IReadOnlyList<PontoSerieGlicemia> Pontos);
