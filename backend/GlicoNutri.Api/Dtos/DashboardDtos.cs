namespace GlicoNutri.Api.Dtos;

// ── UC011 — Dashboard do Nutricionista ──────────────────────────────────────

/// <summary>Cartões do topo do painel.</summary>
public record IndicadoresDashboard(
    int TotalPacientes,
    /// <summary>
    /// Pacientes cuja última glicemia é de hoje. O protótipo rotula este cartão
    /// como "Registros de Hoje", mas contar registros exigiria ler as tabelas de
    /// escrita, o que a nota do DER V2.0 veda ao dashboard.
    /// </summary>
    int PacientesComRegistroHoje,
    int PacientesComAlerta,
    int PlanosAtivos,
    double? MediaGlicemicaGeral,
    double? MediaPercentualNoAlvo);

/// <summary>RN03 do UC011 — pendências ordenadas por criticidade e data.</summary>
public record AlertaDashboard(
    long PacienteId,
    string PacienteNome,
    string Tipo,
    /// <summary>CRITICO, ATENCAO ou INFORMATIVO.</summary>
    string Severidade,
    string Descricao,
    DateTime? Ocorrencia);

public record PacienteDashboard(
    long PacienteId,
    string Nome,
    double? UltimaGlicemia,
    string? UltimaGlicemiaContexto,
    DateTime? UltimaGlicemiaData,
    double? MediaGlicemia7Dias,
    double? PercentualNoAlvo7Dias,
    double? UltimoImc,
    string? ClassificacaoImc,
    DateTime? UltimaDataAntropometria,
    bool PlanoAtivo,
    int AlertasPendentes,
    int? DiasSemRegistroGlicemia,
    /// <summary>Maior severidade entre as pendências do paciente, ou null se não houver.</summary>
    string? Severidade);

/// <summary>Distribuição usada nos gráficos consolidados do painel (A4 do UC011).</summary>
public record FatiaDistribuicao(string Rotulo, int Quantidade);

public record DashboardResponse(
    DateTime AtualizadoEm,
    IndicadoresDashboard Indicadores,
    IReadOnlyList<AlertaDashboard> Alertas,
    IReadOnlyList<PacienteDashboard> Pacientes,
    IReadOnlyList<FatiaDistribuicao> ControleGlicemico,
    IReadOnlyList<FatiaDistribuicao> ClassificacaoImc);

// ── RF08.2 — Série de peso e IMC ────────────────────────────────────────────

public record PontoSerieAntropometrica(DateTime DataHora, double Peso, double? Imc, string? ClassificacaoImc);

public record SerieAntropometricaResponse(int Dias, IReadOnlyList<PontoSerieAntropometrica> Pontos);
