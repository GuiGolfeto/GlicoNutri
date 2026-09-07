using System.ComponentModel.DataAnnotations;

namespace GlicoNutri.Api.Dtos;

// ── RF01.1 — Cadastro manual ────────────────────────────────────────────────

public record CriarAlimentoRequest(
    [Required, StringLength(200, MinimumLength = 2)] string Nome,
    [StringLength(100)] string? GrupoAlimentar,
    [Range(0, 900)] double? CaloriasPor100g,
    [Range(0, 100)] double? CarboidratosPor100g,
    [Range(0, 100)] double? ProteinasPor100g,
    [Range(0, 100)] double? LipidiosPor100g,
    [Range(0, 100)] double? FibrasPor100g,
    /// <summary>
    /// A TACO não publica índice glicêmico; quando informado, vem de outra fonte
    /// ou do julgamento clínico do nutricionista.
    /// </summary>
    [Range(0, 200)] int? IndiceGlicemico);

public record AlimentoResponse(
    long Id,
    string Nome,
    string? GrupoAlimentar,
    double? CaloriasPor100g,
    double? CarboidratosPor100g,
    double? ProteinasPor100g,
    double? LipidiosPor100g,
    double? FibrasPor100g,
    int? IndiceGlicemico,
    string Fonte,
    bool Ativo);

// ── RF01.2 / RN10 — Importação em lote ──────────────────────────────────────

/// <summary>Uma linha recusada, com o motivo — o relatório de erros da RN10.</summary>
public record LinhaRejeitada(int Linha, string Conteudo, string Motivo);

public record ResultadoImportacao(
    int LinhasLidas,
    int Importados,
    int IgnoradosPorDuplicidade,
    int Rejeitados,
    IReadOnlyList<LinhaRejeitada> Erros);
