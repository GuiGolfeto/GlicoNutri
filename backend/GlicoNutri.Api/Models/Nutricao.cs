using GlicoNutri.Api.Models.Referencia;

namespace GlicoNutri.Api.Models;

/// <summary>Banco de alimentos, alimentado manualmente ou pela importacao TACO (RF01).</summary>
public class Alimento
{
    public long Id { get; set; }
    public string Nome { get; set; } = null!;
    public string? GrupoAlimentar { get; set; }

    public double? CaloriasPor100g { get; set; }
    public double? CarboidratosPor100g { get; set; }
    public double? ProteinasPor100g { get; set; }
    public double? LipidiosPor100g { get; set; }
    public double? FibrasPor100g { get; set; }
    public int? IndiceGlicemico { get; set; }

    public long FonteId { get; set; }
    public FonteAlimento Fonte { get; set; } = null!;

    /// <summary>Inativacao logica (RN12): nunca exclusao fisica, para nao quebrar planos que referenciam o alimento.</summary>
    public bool Ativo { get; set; } = true;

    public ICollection<ItemPlanoAlimentar> ItensPlano { get; set; } = new List<ItemPlanoAlimentar>();
    public ICollection<IngredienteReceita> Ingredientes { get; set; } = new List<IngredienteReceita>();
}

/// <summary>
/// Plano alimentar do paciente. No maximo um ativo por paciente (RN16); ao criar
/// um novo, o anterior e desativado por soft delete preservando o historico (RN17).
/// </summary>
public class PlanoAlimentar
{
    public long Id { get; set; }

    public long PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;

    public long NutricionistaId { get; set; }
    public Nutricionista Nutricionista { get; set; } = null!;

    public DateOnly DataInicio { get; set; }
    public DateOnly? DataFim { get; set; }
    public string? Objetivo { get; set; }
    public string? Observacoes { get; set; }

    /// <summary>VCT que fundamenta o plano; exige calculo energetico previo (RN13).</summary>
    public double? TotalCaloriasPrescritas { get; set; }

    public bool Ativo { get; set; } = true;

    public DistribuicaoMacronutrientes? Distribuicao { get; set; }
    public ICollection<ItemPlanoAlimentar> Itens { get; set; } = new List<ItemPlanoAlimentar>();
}

/// <summary>
/// Distribuicao de macronutrientes do plano (1:1). A soma dos percentuais deve
/// ser exatamente 100% (RN15) — validado na camada de servico.
/// </summary>
public class DistribuicaoMacronutrientes
{
    public long Id { get; set; }

    public long PlanoAlimentarId { get; set; }
    public PlanoAlimentar PlanoAlimentar { get; set; } = null!;

    public double CarboidratosPercentual { get; set; }
    public double ProteinasPercentual { get; set; }
    public double LipidiosPercentual { get; set; }

    public double? TotalCaloriasPrescritas { get; set; }
    public double? CarboidratosGramas { get; set; }
    public double? ProteinasGramas { get; set; }
    public double? LipidiosGramas { get; set; }
}

/// <summary>Item de refeicao dentro do plano alimentar.</summary>
public class ItemPlanoAlimentar
{
    public long Id { get; set; }

    public long PlanoAlimentarId { get; set; }
    public PlanoAlimentar PlanoAlimentar { get; set; } = null!;

    public long AlimentoId { get; set; }
    public Alimento Alimento { get; set; } = null!;

    public int Dia { get; set; }
    public TimeOnly? Horario { get; set; }
    public string? Refeicao { get; set; }
    public double Quantidade { get; set; }
    public string? Unidade { get; set; }

    public bool Ativo { get; set; } = true;
}

/// <summary>
/// Resultado do calculo de necessidade energetica (UC006). A formula usada fica
/// registrada para rastreabilidade clinica (RN14).
/// </summary>
public class NecessidadeEnergetica
{
    public long Id { get; set; }

    public long PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;

    public long FormulaId { get; set; }
    public FormulaEnergetica Formula { get; set; } = null!;

    public long NivelAtividadeId { get; set; }
    public NivelAtividade NivelAtividade { get; set; } = null!;

    public string? Objetivo { get; set; }
    public DateOnly DataCalculo { get; set; }

    /// <summary>VET em kcal/dia.</summary>
    public double ValorKcal { get; set; }

    public bool Ativo { get; set; } = true;
}
