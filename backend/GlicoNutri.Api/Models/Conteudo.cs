using GlicoNutri.Api.Models.Referencia;

namespace GlicoNutri.Api.Models;

/// <summary>
/// Conteudo do repositorio educativo. Publicacao restrita a Nutricionista e
/// Administrador (RN28); despublicacao e exclusiva do Administrador, por
/// inativacao logica (RN29).
/// </summary>
public class ConteudoEducativo
{
    public long Id { get; set; }

    public long AutorId { get; set; }
    public Usuario Autor { get; set; } = null!;

    public string Titulo { get; set; } = null!;

    public long TipoId { get; set; }
    public TipoConteudo Tipo { get; set; } = null!;

    public string? Corpo { get; set; }

    /// <summary>
    /// RF09.2 — midia do conteudo, por link externo. Hospedar arquivo exigiria um
    /// servico de armazenamento que a arquitetura nao preve; o link nao abre frente
    /// de infraestrutura e aproveita o material que a associacao ja publica.
    /// Em troca, o conteudo depende de terceiro e pode sair do ar: a tela avisa
    /// quando o link nao carrega. Decisao do Mateus em 11/09/2026.
    /// </summary>
    public string? UrlMidia { get; set; }

    public DateTime? DataPublicacao { get; set; }

    public bool Ativo { get; set; } = true;
}

/// <summary>
/// Receita publicada pelo nutricionista. Tratada pela RN30 como especializacao de
/// conteudo educativo, mas modelada como tabela propria no DER V2.0, por exigir
/// a lista de ingredientes vinculada a alimentos cadastrados.
/// </summary>
public class Receita
{
    public long Id { get; set; }

    public long NutricionistaId { get; set; }
    public Nutricionista Nutricionista { get; set; } = null!;

    public string Nome { get; set; } = null!;
    public string? Descricao { get; set; }

    /// <summary>Tempo de preparo em minutos.</summary>
    public int? TempoPreparo { get; set; }
    public int? Porcoes { get; set; }
    public string? Instrucoes { get; set; }

    public DateTime? DataPublicacao { get; set; }
    public bool Ativo { get; set; } = true;

    public ICollection<IngredienteReceita> Ingredientes { get; set; } = new List<IngredienteReceita>();
}

/// <summary>Ingrediente da receita, obrigatoriamente vinculado a um alimento da base (RN30).</summary>
public class IngredienteReceita
{
    public long Id { get; set; }

    public long ReceitaId { get; set; }
    public Receita Receita { get; set; } = null!;

    public long AlimentoId { get; set; }
    public Alimento Alimento { get; set; } = null!;

    public double Quantidade { get; set; }
    public string? Unidade { get; set; }
}

/// <summary>
/// RF09.3 — conteudo marcado como favorito pelo paciente. O DER V2.0 nao previa
/// a tabela, e o requisito ficava registrado como limitacao; o Mateus optou por
/// implementar em 11/09/2026. Duas tabelas de vinculo, uma por tipo de material,
/// porque conteudo e receita sao entidades distintas no DER e a chave estrangeira
/// so garante integridade apontando para uma delas.
/// </summary>
public class FavoritoConteudo
{
    public long PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;

    public long ConteudoId { get; set; }
    public ConteudoEducativo Conteudo { get; set; } = null!;

    public DateTime DataFavoritado { get; set; }
}

/// <summary>RF09.3 — receita marcada como favorita pelo paciente.</summary>
public class FavoritoReceita
{
    public long PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;

    public long ReceitaId { get; set; }
    public Receita Receita { get; set; } = null!;

    public DateTime DataFavoritado { get; set; }
}
