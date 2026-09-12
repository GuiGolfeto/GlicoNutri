using GlicoNutri.Api.Models.Referencia;

namespace GlicoNutri.Api.Models;

/// <summary>
/// Raiz da hierarquia de identidade. Mapeada como TPH (Table-Per-Hierarchy) por
/// especificacao do professor orientador: uma unica tabela usuarios, com perfil_id
/// dizendo de que tipo e cada linha. O DER V2.0 descreve o TPT anterior.
/// </summary>
public abstract class Usuario
{
    public long Id { get; set; }
    public string Nome { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string SenhaHash { get; set; } = null!;

    /// <summary>
    /// Discriminador da hierarquia TPH, alem de FK para perfis_usuario: e o EF que
    /// o grava, a partir do tipo da entidade. Nao atribua na mao.
    /// </summary>
    public long PerfilId { get; set; }
    public PerfilUsuario Perfil { get; set; } = null!;

    /// <summary>Soft delete (RN05): desativar uma conta nunca apaga historico clinico.</summary>
    public bool Ativo { get; set; } = true;

    public DateTime DataCadastro { get; set; }
    public DateTime? UltimoAcesso { get; set; }

    // --- Estado de autenticacao ---------------------------------------------
    // Nao consta do DER V2.0, mas e exigido pelas RN02 e RN03 e ja previsto no C4
    // ("UsuarioRepository ... contador de tentativas de login").

    /// <summary>Tentativas invalidas consecutivas no ciclo atual (RN02).</summary>
    public int TentativasInvalidas { get; set; }

    /// <summary>Bloqueios ja aplicados: 0 nenhum, 1 = 5min, 2 = 30min, 3+ = 24h (RN02).</summary>
    public int NivelBloqueio { get; set; }

    /// <summary>Instante em que o bloqueio automatico expira; null se liberado (RN02).</summary>
    public DateTime? BloqueadoAte { get; set; }

    /// <summary>Senha ainda e a provisoria gerada pelo sistema; troca obrigatoria (RN03).</summary>
    public bool SenhaProvisoria { get; set; }

    public ICollection<ConteudoEducativo> ConteudosPublicados { get; set; } = new List<ConteudoEducativo>();
}

public class Nutricionista : Usuario
{
    public string Crn { get; set; } = null!;
    public string? Especialidade { get; set; }
    public string? Telefone { get; set; }

    public ICollection<NutricionistaPaciente> Vinculos { get; set; } = new List<NutricionistaPaciente>();
    public ICollection<PlanoAlimentar> PlanosElaborados { get; set; } = new List<PlanoAlimentar>();
    public ICollection<Receita> Receitas { get; set; } = new List<Receita>();
}

public class Paciente : Usuario
{
    public string Cpf { get; set; } = null!;

    public long SexoId { get; set; }
    public SexoBiologico Sexo { get; set; } = null!;

    public DateOnly DataNascimento { get; set; }
    public string? Telefone { get; set; }

    public long TipoDiabetesId { get; set; }
    public TipoDiabetes TipoDiabetes { get; set; } = null!;

    public string? MedicacaoEmUso { get; set; }
    public string? ObservacoesClinicas { get; set; }

    /// <summary>
    /// Faixa glicemica alvo definida individualmente pelo Nutricionista (RN20).
    /// Null significa "ainda nao personalizada": o sistema aplica o padrao clinico
    /// de 70-180 mg/dL e sinaliza a pendencia ao Nutricionista.
    /// </summary>
    public double? GlicemiaMinAlvo { get; set; }
    public double? GlicemiaMaxAlvo { get; set; }

    public ICollection<NutricionistaPaciente> Vinculos { get; set; } = new List<NutricionistaPaciente>();
    public ICollection<PlanoAlimentar> Planos { get; set; } = new List<PlanoAlimentar>();
    public ICollection<RegistroGlicemia> RegistrosGlicemia { get; set; } = new List<RegistroGlicemia>();
    public ICollection<RegistroAntropometrico> RegistrosAntropometricos { get; set; } = new List<RegistroAntropometrico>();
    public ICollection<RegistroEmocional> RegistrosEmocionais { get; set; } = new List<RegistroEmocional>();
    public ICollection<NecessidadeEnergetica> NecessidadesEnergeticas { get; set; } = new List<NecessidadeEnergetica>();
    public ICollection<Alerta> Alertas { get; set; } = new List<Alerta>();
    public ResumoClinicoPaciente? Resumo { get; set; }
}

public class Administrador : Usuario
{
    /// <summary>Administrador herda todas as permissoes do Nutricionista (RN04).</summary>
    public int NivelAcesso { get; set; } = 1;
}

/// <summary>
/// Vinculo obrigatorio entre paciente e nutricionista responsavel (RN07),
/// com chave primaria composta conforme o DER V2.0.
/// </summary>
public class NutricionistaPaciente
{
    public long NutricionistaId { get; set; }
    public Nutricionista Nutricionista { get; set; } = null!;

    public long PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;

    public DateTime DataVinculo { get; set; }
    public bool Ativo { get; set; } = true;
}
