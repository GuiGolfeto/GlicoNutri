using GlicoNutri.Api.Data;
using GlicoNutri.Api.Dtos;
using GlicoNutri.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GlicoNutri.Api.Services;

public interface IAlertaService
{
    Task<Resultado<AlertaResponse>> CriarAsync(
        long pacienteId, CriarAlertaRequest pedido, CancellationToken ct = default);

    Task<IReadOnlyList<AlertaResponse>> ListarAsync(
        long pacienteId, bool incluirInativos, CancellationToken ct = default);

    Task<Resultado<AlertaResponse>> AtualizarAsync(
        long alertaId, CriarAlertaRequest pedido, CancellationToken ct = default);

    Task<Resultado<bool>> DesativarAsync(long alertaId, CancellationToken ct = default);
    Task<Resultado<bool>> ReativarAsync(long alertaId, CancellationToken ct = default);

    Task<Resultado<HistoricoAlertaResponse>> RegistrarDisparoAsync(
        long pacienteId, RegistrarDisparoRequest pedido, CancellationToken ct = default);

    Task<IReadOnlyList<HistoricoAlertaResponse>> HistoricoAsync(
        long pacienteId, int dias, CancellationToken ct = default);

    Task<ResultadoExpiracao> VerificarAlertasExpiradosAsync(
        long pacienteId, CancellationToken ct = default);
}

/// <summary>
/// UC010 — Configurar Alertas e Lembretes. A configuração é exclusiva do
/// Nutricionista ou do Administrador (RN23); o paciente apenas desativa.
/// </summary>
public class AlertaService(GlicoNutriDbContext db, ILogger<AlertaService> log) : IAlertaService
{
    /// <summary>RN03 do UC010 — teto de alertas ativos simultâneos por paciente.</summary>
    private const int LimiteAlertasAtivos = 10;

    /// <summary>RN35 — janela de oportunidade do alerta antes de expirar.</summary>
    private const int JanelaExpiracaoHoras = 2;

    private static readonly int[] PeriodosAceitos = [7, 15, 30, 60, 90];

    public async Task<Resultado<AlertaResponse>> CriarAsync(
        long pacienteId, CriarAlertaRequest pedido, CancellationToken ct = default)
    {
        if (!await db.Pacientes.AnyAsync(p => p.Id == pacienteId, ct))
            return Resultado<AlertaResponse>.Erro("Paciente não encontrado.");

        var tipo = await db.TiposAlerta.FirstOrDefaultAsync(t => t.Id == pedido.TipoId && t.Ativo, ct);
        if (tipo is null) return Resultado<AlertaResponse>.Erro("Tipo de alerta inválido.");

        var ativos = await db.Alertas.CountAsync(a => a.PacienteId == pacienteId, ct);
        if (ativos >= LimiteAlertasAtivos)
            return Resultado<AlertaResponse>.Erro(
                $"O paciente já possui {LimiteAlertasAtivos} alertas ativos, que é o limite. " +
                "Desative um alerta antes de criar outro.");

        if (pedido.Horario2 is { } h2 && h2 == pedido.Horario1)
            return Resultado<AlertaResponse>.Erro("Os dois horários do alerta devem ser diferentes.");

        // UC010, passo 5 — conflito com alerta já configurado no mesmo horário.
        if (await ConflitaAsync(pacienteId, null, pedido, ct))
            return Resultado<AlertaResponse>.Erro(
                "Já existe um alerta ativo configurado para este horário.");

        var alerta = new Alerta
        {
            PacienteId = pacienteId,
            TipoId = tipo.Id,
            Mensagem = pedido.Mensagem?.Trim(),
            Horario1 = pedido.Horario1,
            Horario2 = pedido.Horario2,
            DiasSemana = pedido.DiasSemana ?? "1111111",
            Ativo = true,
        };

        db.Alertas.Add(alerta);
        await db.SaveChangesAsync(ct);

        return Resultado<AlertaResponse>.Ok(Mapear(alerta, tipo.Descricao, 0));
    }

    public async Task<IReadOnlyList<AlertaResponse>> ListarAsync(
        long pacienteId, bool incluirInativos, CancellationToken ct = default)
    {
        var consulta = incluirInativos ? db.Alertas.IgnoreQueryFilters() : db.Alertas;

        return await consulta
            .Include(a => a.Tipo)
            .Where(a => a.PacienteId == pacienteId)
            .OrderBy(a => a.Horario1)
            .Select(a => new AlertaResponse(
                a.Id, a.PacienteId, a.TipoId, a.Tipo.Descricao, a.Mensagem,
                a.Horario1, a.Horario2, a.DiasSemana, a.Ativo,
                a.Historico.Count(h => h.StatusEnvio.Codigo == Codigos.Status.Pendente)))
            .ToListAsync(ct);
    }

    public async Task<Resultado<AlertaResponse>> AtualizarAsync(
        long alertaId, CriarAlertaRequest pedido, CancellationToken ct = default)
    {
        var alerta = await db.Alertas.Include(a => a.Tipo).FirstOrDefaultAsync(a => a.Id == alertaId, ct);
        if (alerta is null) return Resultado<AlertaResponse>.Erro("Alerta não encontrado.");

        var tipo = await db.TiposAlerta.FirstOrDefaultAsync(t => t.Id == pedido.TipoId && t.Ativo, ct);
        if (tipo is null) return Resultado<AlertaResponse>.Erro("Tipo de alerta inválido.");

        if (await ConflitaAsync(alerta.PacienteId, alertaId, pedido, ct))
            return Resultado<AlertaResponse>.Erro(
                "Já existe outro alerta ativo configurado para este horário.");

        alerta.TipoId = tipo.Id;
        alerta.Mensagem = pedido.Mensagem?.Trim();
        alerta.Horario1 = pedido.Horario1;
        alerta.Horario2 = pedido.Horario2;
        alerta.DiasSemana = pedido.DiasSemana ?? alerta.DiasSemana;

        await db.SaveChangesAsync(ct);

        var pendentes = await db.HistoricoAlertas.CountAsync(
            h => h.AlertaId == alertaId && h.StatusEnvio.Codigo == Codigos.Status.Pendente, ct);

        return Resultado<AlertaResponse>.Ok(Mapear(alerta, tipo.Descricao, pendentes));
    }

    /// <summary>
    /// RN23 — desativar é a única ação permitida ao paciente neste módulo.
    /// Soft delete, para preservar o histórico de disparos exigido pela RN27.
    /// </summary>
    public async Task<Resultado<bool>> DesativarAsync(long alertaId, CancellationToken ct = default)
    {
        var alerta = await db.Alertas.FirstOrDefaultAsync(a => a.Id == alertaId, ct);
        if (alerta is null) return Resultado<bool>.Erro("Alerta não encontrado.");

        alerta.Ativo = false;
        await db.SaveChangesAsync(ct);
        await RecalcularPendentesAsync(alerta.PacienteId, ct);

        return Resultado<bool>.Ok(true);
    }

    public async Task<Resultado<bool>> ReativarAsync(long alertaId, CancellationToken ct = default)
    {
        var alerta = await db.Alertas.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == alertaId, ct);
        if (alerta is null) return Resultado<bool>.Erro("Alerta não encontrado.");

        var ativos = await db.Alertas.CountAsync(a => a.PacienteId == alerta.PacienteId, ct);
        if (ativos >= LimiteAlertasAtivos)
            return Resultado<bool>.Erro(
                $"O paciente já possui {LimiteAlertasAtivos} alertas ativos, que é o limite.");

        alerta.Ativo = true;
        await db.SaveChangesAsync(ct);
        return Resultado<bool>.Ok(true);
    }

    /// <summary>
    /// RN27 — toda tentativa de envio é registrada com data, status e número de
    /// tentativas. Chamado pelo aplicativo, que é quem agenda e dispara as
    /// notificações locais no dispositivo.
    /// </summary>
    public async Task<Resultado<HistoricoAlertaResponse>> RegistrarDisparoAsync(
        long pacienteId, RegistrarDisparoRequest pedido, CancellationToken ct = default)
    {
        var alerta = await db.Alertas
            .IgnoreQueryFilters()
            .Include(a => a.Tipo)
            .FirstOrDefaultAsync(a => a.Id == pedido.AlertaId && a.PacienteId == pacienteId, ct);

        if (alerta is null) return Resultado<HistoricoAlertaResponse>.Erro("Alerta não encontrado.");

        var status = await db.StatusEnvio
            .FirstOrDefaultAsync(s => s.Codigo == pedido.Status.ToUpperInvariant(), ct);

        if (status is null) return Resultado<HistoricoAlertaResponse>.Erro("Status de envio inválido.");

        var disparo = pedido.DataHoraDisparo ?? DateTime.UtcNow;

        // Um alerta atendido ou reagendado encerra o disparo pendente daquele
        // ciclo, em vez de abrir outro registro (RN26).
        var pendente = await db.HistoricoAlertas
            .Where(h => h.AlertaId == alerta.Id
                     && h.StatusEnvio.Codigo == Codigos.Status.Pendente
                     && h.DataHoraDisparo <= disparo)
            .OrderByDescending(h => h.DataHoraDisparo)
            .FirstOrDefaultAsync(ct);

        HistoricoAlerta registro;

        if (pendente is not null && status.Codigo != Codigos.Status.Pendente)
        {
            pendente.StatusEnvioId = status.Id;
            pendente.Tentativas++;
            pendente.MensagemErro = pedido.MensagemErro;
            pendente.DataHoraAtendimento =
                status.Codigo == Codigos.Status.Atendido ? disparo : null;
            registro = pendente;
        }
        else
        {
            registro = new HistoricoAlerta
            {
                AlertaId = alerta.Id,
                DataHoraDisparo = disparo,
                StatusEnvioId = status.Id,
                Tentativas = 1,
                MensagemErro = pedido.MensagemErro,
                DataHoraAtendimento =
                    status.Codigo == Codigos.Status.Atendido ? disparo : null,
            };
            db.HistoricoAlertas.Add(registro);
        }

        await db.SaveChangesAsync(ct);
        await RecalcularPendentesAsync(pacienteId, ct);

        return Resultado<HistoricoAlertaResponse>.Ok(new HistoricoAlertaResponse(
            registro.Id, alerta.Id, alerta.Tipo.Descricao, registro.DataHoraDisparo,
            registro.DataHoraAtendimento, status.Codigo, registro.Tentativas, registro.MensagemErro));
    }

    public async Task<IReadOnlyList<HistoricoAlertaResponse>> HistoricoAsync(
        long pacienteId, int dias, CancellationToken ct = default)
    {
        if (!PeriodosAceitos.Contains(dias)) dias = 30;
        var inicio = DateTime.UtcNow.AddDays(-dias);

        return await db.HistoricoAlertas
            .IgnoreQueryFilters()
            .Include(h => h.Alerta).ThenInclude(a => a.Tipo)
            .Include(h => h.StatusEnvio)
            .Where(h => h.Alerta.PacienteId == pacienteId && h.DataHoraDisparo >= inicio)
            .OrderByDescending(h => h.DataHoraDisparo)
            .Select(h => new HistoricoAlertaResponse(
                h.Id, h.AlertaId, h.Alerta.Tipo.Descricao, h.DataHoraDisparo,
                h.DataHoraAtendimento, h.StatusEnvio.Codigo, h.Tentativas, h.MensagemErro))
            .ToListAsync(ct);
    }

    /// <summary>
    /// RN35 — ao abrir sessão no aplicativo, todo disparo PENDENTE com mais de
    /// duas horas é marcado como FALHOU: a janela de oportunidade encerrou sem
    /// resposta do paciente. Impede o acúmulo de pendentes sem resolução.
    /// </summary>
    public async Task<ResultadoExpiracao> VerificarAlertasExpiradosAsync(
        long pacienteId, CancellationToken ct = default)
    {
        var limite = DateTime.UtcNow.AddHours(-JanelaExpiracaoHoras);

        var pendente = await db.StatusEnvio.FirstAsync(s => s.Codigo == Codigos.Status.Pendente, ct);
        var falhou = await db.StatusEnvio.FirstAsync(s => s.Codigo == Codigos.Status.Falhou, ct);

        var expirados = await db.HistoricoAlertas
            .IgnoreQueryFilters()
            .Include(h => h.Alerta)
            .Where(h => h.Alerta.PacienteId == pacienteId
                     && h.StatusEnvioId == pendente.Id
                     && h.DataHoraDisparo < limite)
            .ToListAsync(ct);

        foreach (var registro in expirados)
        {
            registro.StatusEnvioId = falhou.Id;
            registro.MensagemErro =
                $"Alerta expirado: janela de {JanelaExpiracaoHoras} horas encerrada sem registro pelo paciente.";
        }

        if (expirados.Count > 0)
        {
            await db.SaveChangesAsync(ct);
            log.LogInformation(
                "RN35: {Total} alerta(s) do paciente {Paciente} expirados por inatividade.",
                expirados.Count, pacienteId);
        }

        var restantes = await RecalcularPendentesAsync(pacienteId, ct);
        return new ResultadoExpiracao(expirados.Count, restantes);
    }

    /// <summary>
    /// Mantém alertas_pendentes_count do read model coerente, como manda a nota
    /// do DER V2.0 e o RN35.
    /// </summary>
    private async Task<int> RecalcularPendentesAsync(long pacienteId, CancellationToken ct)
    {
        var pendentes = await db.HistoricoAlertas
            .IgnoreQueryFilters()
            .CountAsync(h => h.Alerta.PacienteId == pacienteId
                          && h.Alerta.Ativo
                          && h.StatusEnvio.Codigo == Codigos.Status.Pendente, ct);

        var resumo = await db.ResumoClinicoPaciente
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.PacienteId == pacienteId, ct);

        if (resumo is not null)
        {
            resumo.AlertasPendentesCount = pendentes;
            resumo.DataAtualizacao = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return pendentes;
    }

    /// <summary>UC010, passo 5 — dois alertas ativos não podem ocupar o mesmo horário.</summary>
    private async Task<bool> ConflitaAsync(
        long pacienteId, long? ignorarId, CriarAlertaRequest pedido, CancellationToken ct)
    {
        var horarios = new List<TimeOnly> { pedido.Horario1 };
        if (pedido.Horario2 is { } h2) horarios.Add(h2);

        return await db.Alertas.AnyAsync(a =>
            a.PacienteId == pacienteId
            && (ignorarId == null || a.Id != ignorarId)
            && ((a.Horario1 != null && horarios.Contains(a.Horario1.Value))
             || (a.Horario2 != null && horarios.Contains(a.Horario2.Value))), ct);
    }

    private static AlertaResponse Mapear(Alerta a, string tipo, int pendentes) =>
        new(a.Id, a.PacienteId, a.TipoId, tipo, a.Mensagem,
            a.Horario1, a.Horario2, a.DiasSemana, a.Ativo, pendentes);
}
