using System.ComponentModel.DataAnnotations;

namespace GlicoNutri.Api.Dtos;

// ── RF09.2 — Conteúdo educativo ─────────────────────────────────────────────

public record CriarConteudoRequest(
    [Required, StringLength(200, MinimumLength = 3)] string Titulo,
    [Required] long TipoId,
    /// <summary>Texto do conteúdo. Aceita Markdown para a formatação do RF09.2.</summary>
    [Required, MinLength(10)] string Corpo);

public record ConteudoEducativoResponse(
    long Id,
    string Titulo,
    long TipoId,
    string Tipo,
    string? Corpo,
    long AutorId,
    string AutorNome,
    DateTime? DataPublicacao,
    bool Ativo);

// ── RF09.1 — Receitas ───────────────────────────────────────────────────────

public record IngredienteRequest(
    [Required] long AlimentoId,
    /// <summary>Em gramas — os valores nutricionais da base são por 100 g.</summary>
    [Range(0.1, 5000)] double Quantidade,
    [StringLength(20)] string? Unidade);

public record CriarReceitaRequest(
    [Required, StringLength(200, MinimumLength = 3)] string Nome,
    [StringLength(1000)] string? Descricao,
    /// <summary>Em minutos.</summary>
    [Range(1, 1440)] int? TempoPreparo,
    [Range(1, 100)] int? Porcoes,
    [Required, MinLength(10)] string Instrucoes,
    /// <summary>
    /// RN30 — toda receita precisa da lista de ingredientes vinculada a alimentos
    /// cadastrados, senão as informações nutricionais não podem ser calculadas.
    /// </summary>
    [Required, MinLength(1)] IReadOnlyList<IngredienteRequest> Ingredientes);

public record IngredienteResponse(
    long Id,
    long AlimentoId,
    string AlimentoNome,
    double Quantidade,
    string? Unidade,
    double? Calorias,
    double? Carboidratos,
    double? Proteinas,
    double? Lipidios);

/// <summary>Totais calculados a partir dos ingredientes (RN30).</summary>
public record InformacaoNutricional(
    double Calorias,
    double Carboidratos,
    double Proteinas,
    double Lipidios,
    double Fibras,
    double? CaloriasPorPorcao,
    double? CarboidratosPorPorcao,
    /// <summary>Falso quando algum ingrediente não tem o valor na base, e o total sai subestimado.</summary>
    bool Completa);

public record ReceitaResponse(
    long Id,
    string Nome,
    string? Descricao,
    int? TempoPreparo,
    int? Porcoes,
    string? Instrucoes,
    long NutricionistaId,
    string NutricionistaNome,
    DateTime? DataPublicacao,
    bool Ativo,
    IReadOnlyList<IngredienteResponse> Ingredientes,
    InformacaoNutricional Nutricional);
