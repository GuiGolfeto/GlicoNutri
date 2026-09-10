using GlicoNutri.Api.Models.Referencia;

namespace GlicoNutri.Api.Models;

/// <summary>Medicao de glicemia registrada pelo paciente (UC004 / RN18).</summary>
public class RegistroGlicemia
{
    public long Id { get; set; }

    public long PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;

    /// <summary>Valor em mg/dL. Faixa aceita: 0 a 600 (UC004, RN01 do caso de uso).</summary>
    public double Valor { get; set; }

    public long ContextoId { get; set; }
    public ContextoGlicemia Contexto { get; set; } = null!;

    public DateTime DataHora { get; set; }
    public string? Observacao { get; set; }

    /// <summary>
    /// Classificacao automatica contra a faixa alvo do paciente (RN19).
    /// Sem faixa personalizada, aplica-se o padrao clinico 70-180 mg/dL (RN20).
    /// </summary>
    public bool ForaDoAlvo { get; set; }

    public bool Ativo { get; set; } = true;
}

/// <summary>Medidas corporais do paciente. O IMC e sempre calculado, nunca informado (RN21).</summary>
public class RegistroAntropometrico
{
    public long Id { get; set; }

    public long PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;

    public double Peso { get; set; }
    public double Altura { get; set; }

    public double? CircunferenciaAbdominal { get; set; }
    public double? CircunferenciaCintura { get; set; }
    public double? CircunferenciaQuadril { get; set; }
    public double? CircunferenciaBraco { get; set; }

    /// <summary>Relacao cintura-quadril; calculada quando ambas as circunferencias existem.</summary>
    public double? Rcq { get; set; }

    /// <summary>IMC = peso(kg) / altura(m)^2, calculado pelo sistema (RN21).</summary>
    public double? Imc { get; set; }

    /// <summary>Classificacao do IMC segundo os criterios da OMS.</summary>
    public string? ClassificacaoImc { get; set; }

    public DateTime DataHora { get; set; }
    public bool Ativo { get; set; } = true;
}

/// <summary>Registro emocional opcional, vinculado a uma refeicao ou glicemia (RN22 / UC012).</summary>
public class RegistroEmocional
{
    public long Id { get; set; }

    public long PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;

    public long EstadoEmocionalId { get; set; }
    public EstadoEmocional EstadoEmocional { get; set; } = null!;

    /// <summary>Escala de 1 (leve) a 5 (muito intenso). Obrigatoria conforme UC012.</summary>
    public int Intensidade { get; set; }

    public DateTime DataHora { get; set; }
    public string? Descricao { get; set; }

    public bool Ativo { get; set; } = true;
}
