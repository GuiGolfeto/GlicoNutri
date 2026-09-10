using System.ComponentModel.DataAnnotations;

namespace GlicoNutri.Api.Dtos;

// ── UC008 — Registrar Dados Antropométricos ─────────────────────────────────

public record CriarRegistroAntropometricoRequest(
    [Range(1, 500)] double Peso,
    // Em centímetros, como o UC008 coleta. A faixa recusa altura digitada em
    // metros (1,75), erro que passaria despercebido e falsearia todo o IMC.
    [Range(50, 250)] double Altura,
    [Range(20, 250)] double? CircunferenciaAbdominal,
    [Range(20, 250)] double? CircunferenciaCintura,
    [Range(20, 250)] double? CircunferenciaQuadril,
    [Range(10, 100)] double? CircunferenciaBraco,
    /// <summary>Opcional; assume o momento atual quando ausente (UC008, passo 1).</summary>
    DateTime? DataHora);

public record ComparativoMedida(double? Anterior, double Atual, double? Delta, double? DeltaPercentual);

public record RegistroAntropometricoResponse(
    long Id,
    long PacienteId,
    double Peso,
    double Altura,
    double? CircunferenciaAbdominal,
    double? CircunferenciaCintura,
    double? CircunferenciaQuadril,
    double? CircunferenciaBraco,
    double? Rcq,
    double? Imc,
    string? ClassificacaoImc,
    DateTime DataHora,
    /// <summary>UC008 A3 — variação em relação ao registro anterior, quando existir.</summary>
    ComparativoMedida? ComparativoPeso,
    ComparativoMedida? ComparativoImc);

// ── UC006 — Calcular Necessidade Energética ─────────────────────────────────

public record CalcularVetRequest(
    [Required] long FormulaId,
    [Required] long NivelAtividadeId,
    [StringLength(100)] string? Objetivo);

public record NecessidadeEnergeticaResponse(
    long? Id,
    long PacienteId,
    string Formula,
    string NivelAtividade,
    double FatorAtividade,
    /// <summary>UC006 A4 — o resultado expõe TMB, fator e VET, não só o número final.</summary>
    double? Tmb,
    double ValorKcal,
    string? Objetivo,
    DateOnly DataCalculo,
    // Dados de origem, para o nutricionista conferir sobre o que o cálculo foi
    // feito. Só vêm preenchidos no cálculo em si: o DER V2.0 persiste apenas o
    // VET, então no histórico não há como recuperá-los sem inventar valor.
    double? PesoUtilizado,
    double? AlturaUtilizada,
    int? IdadeUtilizada,
    string? SexoUtilizado,
    DateTime? DataMedicaoUtilizada,
    /// <summary>UC006 A3 — VET anterior e variação percentual, quando houver.</summary>
    double? VetAnterior,
    double? VariacaoPercentual);
