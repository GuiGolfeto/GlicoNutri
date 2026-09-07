using System.ComponentModel.DataAnnotations;

namespace GlicoNutri.Api.Dtos;

// ── UC010 — Configurar Alertas e Lembretes ──────────────────────────────────

public record CriarAlertaRequest(
    [Required] long TipoId,
    [StringLength(500)] string? Mensagem,
    [Required] TimeOnly Horario1,
    TimeOnly? Horario2,
    /// <summary>Máscara de 7 posições começando no domingo; "1111111" é todo dia.</summary>
    [RegularExpression("^[01]{7}$", ErrorMessage = "Dias da semana deve ter 7 posições de 0 ou 1, iniciando no domingo.")]
    string? DiasSemana);

public record AlertaResponse(
    long Id,
    long PacienteId,
    long TipoId,
    string Tipo,
    string? Mensagem,
    TimeOnly? Horario1,
    TimeOnly? Horario2,
    string? DiasSemana,
    bool Ativo,
    int DisparosPendentes);

// ── Histórico de disparos (RN27) ────────────────────────────────────────────

public record RegistrarDisparoRequest(
    [Required] long AlertaId,
    DateTime? DataHoraDisparo,
    /// <summary>Código de StatusEnvio: PENDENTE, ATENDIDO, REAGENDADO ou FALHOU.</summary>
    [Required] string Status,
    [StringLength(500)] string? MensagemErro);

public record HistoricoAlertaResponse(
    long Id,
    long AlertaId,
    string TipoAlerta,
    DateTime DataHoraDisparo,
    DateTime? DataHoraAtendimento,
    string Status,
    int Tentativas,
    string? MensagemErro);

/// <summary>RN35 — resultado da varredura de alertas expirados.</summary>
public record ResultadoExpiracao(int Expirados, int PendentesRestantes);
