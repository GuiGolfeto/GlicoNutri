using System.ComponentModel.DataAnnotations;

namespace GlicoNutri.Api.Dtos;

// ── UC007 — Elaborar Plano Alimentar ────────────────────────────────────────

public record DistribuicaoRequest(
    [Range(0, 100)] double CarboidratosPercentual,
    [Range(0, 100)] double ProteinasPercentual,
    [Range(0, 100)] double LipidiosPercentual);

public record CriarPlanoRequest(
    /// <summary>
    /// O UC007 chama de "nome do plano"; no DER V2.0 o campo equivalente é
    /// objetivo, e é onde o texto fica gravado.
    /// </summary>
    [Required, StringLength(255)] string Objetivo,
    [Required] DateOnly DataInicio,
    DateOnly? DataFim,
    string? Observacoes,
    /// <summary>Ausente, o sistema aplica a distribuição automática do UC007 A2.</summary>
    DistribuicaoRequest? Distribuicao,
    IReadOnlyList<ItemPlanoRequest>? Itens);

public record ItemPlanoRequest(
    [Required] long AlimentoId,
    [Range(1, 7)] int Dia,
    TimeOnly? Horario,
    [StringLength(50)] string? Refeicao,
    /// <summary>Em gramas — os valores nutricionais da base são por 100 g.</summary>
    [Range(0.1, 5000)] double Quantidade,
    [StringLength(20)] string? Unidade);

/// <param name="Padrao">Sugestão do sistema: o centro da faixa.</param>
public record FaixaMacronutriente(double Minimo, double Maximo, double Padrao);

public record FaixasMacronutrientesResponse(
    FaixaMacronutriente Carboidratos,
    FaixaMacronutriente Proteinas,
    FaixaMacronutriente Lipidios,
    string Referencia);

public record DistribuicaoResponse(
    double CarboidratosPercentual,
    double ProteinasPercentual,
    double LipidiosPercentual,
    double CarboidratosGramas,
    double ProteinasGramas,
    double LipidiosGramas,
    double TotalCaloriasPrescritas);

public record ItemPlanoResponse(
    long Id,
    long AlimentoId,
    string AlimentoNome,
    int Dia,
    TimeOnly? Horario,
    string? Refeicao,
    double Quantidade,
    string? Unidade,
    double? Calorias,
    double? Carboidratos,
    double? Proteinas,
    double? Lipidios);

public record TotaisRefeicao(
    string Refeicao,
    double Calorias,
    double Carboidratos,
    double Proteinas,
    double Lipidios);

public record PlanoAlimentarResponse(
    long Id,
    long PacienteId,
    string PacienteNome,
    long NutricionistaId,
    string? Objetivo,
    DateOnly DataInicio,
    DateOnly? DataFim,
    string? Observacoes,
    double? TotalCaloriasPrescritas,
    bool Ativo,
    DistribuicaoResponse? Distribuicao,
    IReadOnlyList<ItemPlanoResponse> Itens,
    /// <summary>Subtotais por refeição, exigidos no passo 6 do UC007.</summary>
    IReadOnlyList<TotaisRefeicao> TotaisPorRefeicao,
    double CaloriasMontadas,
    /// <summary>UC007, passo 7 — desvio do montado em relação ao VET prescrito.</summary>
    double? DesvioPercentualDoVet,
    bool DentroDaMargem);
