namespace GlicoNutri.Api.Models.Referencia;

/// <summary>
/// Contrato das tabelas de referencia do dominio.
/// O Diagrama de Classes V5.0 (C1) converteu os enums estaticos em entidades
/// com CRUD, para permitir mudanca de dominio sem recompilar a aplicacao.
/// Todas seguem o mesmo formato: id + codigo + descricao + ativo.
/// </summary>
public interface IEntidadeReferencia
{
    long Id { get; set; }
    string Codigo { get; set; }
    string Descricao { get; set; }
    bool Ativo { get; set; }
}

public class SexoBiologico : IEntidadeReferencia
{
    public long Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Descricao { get; set; } = null!;
    public bool Ativo { get; set; } = true;
}

public class TipoDiabetes : IEntidadeReferencia
{
    public long Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Descricao { get; set; } = null!;
    public bool Ativo { get; set; } = true;
}

/// <summary>
/// De onde veio o indice glicemico de um alimento. A TACO nao publica o dado em
/// nenhum dos 597 alimentos, entao ele entra por outra fonte — e precisa dizer
/// qual, para o nutricionista saber o que esta lendo.
/// </summary>
public class FonteIndiceGlicemico : IEntidadeReferencia
{
    public long Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Descricao { get; set; } = null!;
    public bool Ativo { get; set; } = true;
}

public class PerfilUsuario : IEntidadeReferencia
{
    public long Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Descricao { get; set; } = null!;
    public bool Ativo { get; set; } = true;
}

public class ContextoGlicemia : IEntidadeReferencia
{
    public long Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Descricao { get; set; } = null!;
    public bool Ativo { get; set; } = true;
}

public class EstadoEmocional : IEntidadeReferencia
{
    public long Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Descricao { get; set; } = null!;
    public bool Ativo { get; set; } = true;
}

/// <summary>Formulas aceitas pela RN14: Harris-Benedict e Mifflin-St Jeor.</summary>
public class FormulaEnergetica : IEntidadeReferencia
{
    public long Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Descricao { get; set; } = null!;
    public bool Ativo { get; set; } = true;
}

/// <summary>
/// Nivel de atividade fisica. O campo Fator e o multiplicador aplicado sobre a
/// TMB para obter o VET (UC006, passo 5) — dado de dominio que so existe porque
/// esta deixou de ser um enum estatico.
/// </summary>
public class NivelAtividade : IEntidadeReferencia
{
    public long Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Descricao { get; set; } = null!;
    public double Fator { get; set; }
    public bool Ativo { get; set; } = true;
}

public class TipoConteudo : IEntidadeReferencia
{
    public long Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Descricao { get; set; } = null!;
    public bool Ativo { get; set; } = true;
}

public class TipoAlerta : IEntidadeReferencia
{
    public long Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Descricao { get; set; } = null!;
    public bool Ativo { get; set; } = true;
}

public class StatusEnvio : IEntidadeReferencia
{
    public long Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Descricao { get; set; } = null!;
    public bool Ativo { get; set; } = true;
}

public class FonteAlimento : IEntidadeReferencia
{
    public long Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Descricao { get; set; } = null!;
    public bool Ativo { get; set; } = true;
}
