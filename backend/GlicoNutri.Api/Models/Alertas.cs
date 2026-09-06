using GlicoNutri.Api.Models.Referencia;

namespace GlicoNutri.Api.Models;

/// <summary>
/// Alerta configurado pelo Nutricionista ou Administrador (RN23). No cadastro do
/// paciente o sistema pre-configura automaticamente 8h e 20h (RN24). O paciente
/// so pode desativar, nunca criar ou editar.
/// </summary>
public class Alerta
{
    public long Id { get; set; }

    public long PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;

    public long TipoId { get; set; }
    public TipoAlerta Tipo { get; set; } = null!;

    /// <summary>Mensagem personalizada definida no cadastro; nunca substituida por texto generico (RN25).</summary>
    public string? Mensagem { get; set; }

    public TimeOnly? Horario1 { get; set; }
    public TimeOnly? Horario2 { get; set; }

    /// <summary>Dias da semana em que o alerta dispara.</summary>
    public string? DiasSemana { get; set; }

    public bool Ativo { get; set; } = true;

    public ICollection<HistoricoAlerta> Historico { get; set; } = new List<HistoricoAlerta>();
}

/// <summary>
/// Registro de cada tentativa de disparo (RN27). PENDENTE vira FALHOU
/// automaticamente apos 2 horas sem resposta do paciente (RN35).
/// </summary>
public class HistoricoAlerta
{
    public long Id { get; set; }

    public long AlertaId { get; set; }
    public Alerta Alerta { get; set; } = null!;

    /// <summary>Quando a notificacao foi agendada/disparada.</summary>
    public DateTime DataHoraDisparo { get; set; }

    /// <summary>Quando o paciente realizou a acao pedida. Null enquanto PENDENTE.</summary>
    public DateTime? DataHoraAtendimento { get; set; }

    public long StatusEnvioId { get; set; }
    public StatusEnvio StatusEnvio { get; set; } = null!;

    public int Tentativas { get; set; }
    public string? MensagemErro { get; set; }
}
