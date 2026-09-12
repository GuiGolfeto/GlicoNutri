using GlicoNutri.Api.Models.Referencia;

namespace GlicoNutri.Api.Models;

/// <summary>
/// Projecao de leitura dos dashboards. E uma VIEW, nao uma tabela: cada coluna e
/// calculada na hora a partir das tabelas transacionais. Guardar esses valores
/// era a violacao de 3FN que a orientacao vetou, e tambem o que exigia que todo
/// Service de escrita se lembrasse de atualizar o resumo depois de gravar.
/// Somente leitura: gravar aqui e erro, e o Postgres recusa.
/// </summary>
public class ResumoClinicoPaciente
{
    /// <summary>PK da view; corresponde a usuarios.id do paciente.</summary>
    public long PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;

    public double? UltimaGlicemiaValor { get; set; }

    public long? UltimaGlicemiaContextoId { get; set; }
    public ContextoGlicemia? UltimaGlicemiaContexto { get; set; }

    public DateTime? UltimaGlicemiaData { get; set; }
    public double? MediaGlicemia7Dias { get; set; }
    public double? PercentualNoAlvo7Dias { get; set; }

    public double? UltimoImc { get; set; }
    public string? UltimaClassificacaoImc { get; set; }
    public DateTime? UltimaDataAntropometria { get; set; }

    public bool PlanoAtivo { get; set; }

    /// <summary>Disparos ainda pendentes, contados na leitura (RN35).</summary>
    public int AlertasPendentesCount { get; set; }

    public int? DiasSemRegistroGlicemia { get; set; }
    public DateTime DataAtualizacao { get; set; }
}
