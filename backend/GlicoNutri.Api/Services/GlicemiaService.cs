using GlicoNutri.Api.Data;
using GlicoNutri.Api.Dtos;
using GlicoNutri.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GlicoNutri.Api.Services;

public interface IGlicemiaService
{
    Task<Resultado<RegistroGlicemiaResponse>> RegistrarAsync(
        long pacienteId, CriarRegistroGlicemiaRequest pedido, CancellationToken ct = default);

    Task<Resultado<HistoricoGlicemicoResponse>> HistoricoAsync(
        long pacienteId, int dias, CancellationToken ct = default);

    Task<Resultado<SerieGlicemicaResponse>> SerieAsync(
        long pacienteId, int dias, CancellationToken ct = default);

    Task<Resultado<bool>> RemoverAsync(long registroId, CancellationToken ct = default);

    /// <summary>
    /// Reclassifica todos os registros do paciente contra a faixa alvo vigente.
    /// Chamado quando o Nutricionista altera os limites (RN20).
    /// </summary>
    Task ReclassificarAsync(long pacienteId, CancellationToken ct = default);
}

/// <summary>
/// UC004 — Registrar Glicemia e UC005 — Visualizar Histórico Glicêmico.
/// Classifica cada registro contra a faixa alvo do paciente (RN19/RN20) e mantém
/// o read model do dashboard atualizado.
/// </summary>
public class GlicemiaService(GlicoNutriDbContext db) : IGlicemiaService
{
    /// <summary>Períodos oferecidos pelo UC005; 7 dias é o padrão da RN04.</summary>
    private static readonly int[] PeriodosAceitos = [7, 15, 30, 60, 90];

    public async Task<Resultado<RegistroGlicemiaResponse>> RegistrarAsync(
        long pacienteId, CriarRegistroGlicemiaRequest pedido, CancellationToken ct = default)
    {
        var paciente = await db.Pacientes
            .Include(p => p.Resumo)
            .FirstOrDefaultAsync(p => p.Id == pacienteId, ct);

        if (paciente is null)
            return Resultado<RegistroGlicemiaResponse>.Erro("Paciente não encontrado.");

        // RN18 — o contexto é obrigatório e precisa existir na tabela de referência.
        var contexto = await db.ContextosGlicemia
            .FirstOrDefaultAsync(c => c.Id == pedido.ContextoId && c.Ativo, ct);

        if (contexto is null)
            return Resultado<RegistroGlicemiaResponse>.Erro("Contexto da medição inválido.");

        var dataHora = pedido.DataHora ?? DateTime.UtcNow;

        // RN04 do UC004 permite registro retroativo, mas não futuro.
        if (dataHora > DateTime.UtcNow.AddMinutes(5))
            return Resultado<RegistroGlicemiaResponse>.Erro(
                "A data da medição não pode estar no futuro.");

        var registro = new RegistroGlicemia
        {
            PacienteId = pacienteId,
            Valor = pedido.Valor,
            ContextoId = contexto.Id,
            DataHora = dataHora,
            Observacao = pedido.Observacao?.Trim(),
            // RN19 — classificação automática contra a faixa individual.
            ForaDoAlvo = CalculadoraNutricional.ForaDoAlvo(
                pedido.Valor, paciente.GlicemiaMinAlvo, paciente.GlicemiaMaxAlvo),
            Ativo = true,
        };

        db.RegistrosGlicemia.Add(registro);
        await db.SaveChangesAsync(ct);

        await AtualizarResumoAsync(pacienteId, ct);

        return Resultado<RegistroGlicemiaResponse>.Ok(Mapear(registro, contexto.Descricao, paciente));
    }

    /// <summary>
    /// UC005 — lista do período com o painel de estatísticas. RN04 do UC005:
    /// 7 dias por padrão, ordenado da medição mais recente para a mais antiga.
    /// </summary>
    public async Task<Resultado<HistoricoGlicemicoResponse>> HistoricoAsync(
        long pacienteId, int dias, CancellationToken ct = default)
    {
        var paciente = await db.Pacientes.FirstOrDefaultAsync(p => p.Id == pacienteId, ct);
        if (paciente is null)
            return Resultado<HistoricoGlicemicoResponse>.Erro("Paciente não encontrado.");

        if (!PeriodosAceitos.Contains(dias))
            return Resultado<HistoricoGlicemicoResponse>.Erro(
                $"Período inválido. Use um destes: {string.Join(", ", PeriodosAceitos)} dias.");

        var inicio = DateTime.UtcNow.AddDays(-dias);

        var registros = await db.RegistrosGlicemia
            .Include(r => r.Contexto)
            .Where(r => r.PacienteId == pacienteId && r.DataHora >= inicio)
            .OrderByDescending(r => r.DataHora)
            .ToListAsync(ct);

        var min = paciente.GlicemiaMinAlvo ?? CalculadoraNutricional.GlicemiaMinPadrao;
        var max = paciente.GlicemiaMaxAlvo ?? CalculadoraNutricional.GlicemiaMaxPadrao;
        var personalizada = paciente.GlicemiaMinAlvo is not null && paciente.GlicemiaMaxAlvo is not null;

        var resumo = new ResumoGlicemico(
            registros.Count,
            registros.Count > 0 ? Math.Round(registros.Average(r => r.Valor), 1) : null,
            registros.Count > 0 ? registros.Min(r => r.Valor) : null,
            registros.Count > 0 ? registros.Max(r => r.Valor) : null,
            registros.Count > 0
                ? Math.Round(registros.Count(r => !r.ForaDoAlvo) * 100.0 / registros.Count, 1)
                : null,
            registros.Count(r => r.ForaDoAlvo),
            registros.Count(r => r.Valor < CalculadoraNutricional.GlicemiaMinPadrao),
            registros.Count(r => r.Valor > CalculadoraNutricional.GlicemiaMaxPadrao),
            min, max, personalizada);

        var lista = registros.Select(r => Mapear(r, r.Contexto.Descricao, paciente)).ToList();

        return Resultado<HistoricoGlicemicoResponse>.Ok(
            new HistoricoGlicemicoResponse(dias, inicio, resumo, lista));
    }

    /// <summary>RF08.1 — série para o gráfico de evolução, em ordem cronológica.</summary>
    public async Task<Resultado<SerieGlicemicaResponse>> SerieAsync(
        long pacienteId, int dias, CancellationToken ct = default)
    {
        var paciente = await db.Pacientes.FirstOrDefaultAsync(p => p.Id == pacienteId, ct);
        if (paciente is null)
            return Resultado<SerieGlicemicaResponse>.Erro("Paciente não encontrado.");

        if (!PeriodosAceitos.Contains(dias))
            return Resultado<SerieGlicemicaResponse>.Erro(
                $"Período inválido. Use um destes: {string.Join(", ", PeriodosAceitos)} dias.");

        var inicio = DateTime.UtcNow.AddDays(-dias);

        var pontos = await db.RegistrosGlicemia
            .Include(r => r.Contexto)
            .Where(r => r.PacienteId == pacienteId && r.DataHora >= inicio)
            .OrderBy(r => r.DataHora)
            .Select(r => new PontoSerieGlicemia(r.DataHora, r.Valor, r.Contexto.Descricao, r.ForaDoAlvo))
            .ToListAsync(ct);

        return Resultado<SerieGlicemicaResponse>.Ok(new SerieGlicemicaResponse(
            dias,
            paciente.GlicemiaMinAlvo ?? CalculadoraNutricional.GlicemiaMinPadrao,
            paciente.GlicemiaMaxAlvo ?? CalculadoraNutricional.GlicemiaMaxPadrao,
            pontos));
    }

    public async Task<Resultado<bool>> RemoverAsync(long registroId, CancellationToken ct = default)
    {
        var registro = await db.RegistrosGlicemia.FirstOrDefaultAsync(r => r.Id == registroId, ct);
        if (registro is null) return Resultado<bool>.Erro("Registro não encontrado.");

        registro.Ativo = false;
        await db.SaveChangesAsync(ct);

        await AtualizarResumoAsync(registro.PacienteId, ct);
        return Resultado<bool>.Ok(true);
    }

    /// <summary>
    /// RN19 — a marcação de fora do alvo é valor derivado da faixa do paciente,
    /// não um fato histórico. Quando o Nutricionista aperta ou afrouxa os limites
    /// (RN20), os registros anteriores precisam ser reclassificados: senão o
    /// percentual no alvo passaria a misturar dois critérios diferentes e o
    /// dashboard mostraria a faixa nova com a contagem antiga.
    /// </summary>
    public async Task ReclassificarAsync(long pacienteId, CancellationToken ct = default)
    {
        var paciente = await db.Pacientes.FirstOrDefaultAsync(p => p.Id == pacienteId, ct);
        if (paciente is null) return;

        var registros = await db.RegistrosGlicemia
            .Where(r => r.PacienteId == pacienteId)
            .ToListAsync(ct);

        var mudou = false;
        foreach (var registro in registros)
        {
            var fora = CalculadoraNutricional.ForaDoAlvo(
                registro.Valor, paciente.GlicemiaMinAlvo, paciente.GlicemiaMaxAlvo);

            if (registro.ForaDoAlvo == fora) continue;

            registro.ForaDoAlvo = fora;
            mudou = true;
        }

        if (mudou) await db.SaveChangesAsync(ct);

        await AtualizarResumoAsync(pacienteId, ct);
    }

    /// <summary>
    /// Recalcula os campos glicêmicos do read model a partir dos registros
    /// vigentes, como manda a nota do DER V2.0.
    ///
    /// Recalcular tudo — em vez de somar incrementalmente — é o que mantém o
    /// resumo correto depois de um registro retroativo ou de uma remoção, que
    /// mudam a média e o percentual sem serem a medição mais recente.
    /// </summary>
    private async Task AtualizarResumoAsync(long pacienteId, CancellationToken ct)
    {
        var resumo = await db.ResumoClinicoPaciente
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.PacienteId == pacienteId, ct);

        if (resumo is null) return;

        var ultimo = await db.RegistrosGlicemia
            .Where(r => r.PacienteId == pacienteId)
            .OrderByDescending(r => r.DataHora)
            .Select(r => new { r.Valor, r.ContextoId, r.DataHora })
            .FirstOrDefaultAsync(ct);

        var seteDias = DateTime.UtcNow.AddDays(-7);
        var recentes = await db.RegistrosGlicemia
            .Where(r => r.PacienteId == pacienteId && r.DataHora >= seteDias)
            .Select(r => new { r.Valor, r.ForaDoAlvo })
            .ToListAsync(ct);

        resumo.UltimaGlicemiaValor = ultimo?.Valor;
        resumo.UltimaGlicemiaContextoId = ultimo?.ContextoId;
        resumo.UltimaGlicemiaData = ultimo?.DataHora;

        resumo.MediaGlicemia7Dias = recentes.Count > 0
            ? Math.Round(recentes.Average(r => r.Valor), 1)
            : null;

        resumo.PercentualNoAlvo7Dias = recentes.Count > 0
            ? Math.Round(recentes.Count(r => !r.ForaDoAlvo) * 100.0 / recentes.Count, 1)
            : null;

        // Alimenta o painel "pacientes sem registro recente" do UC011.
        resumo.DiasSemRegistroGlicemia = ultimo is not null
            ? (int)Math.Floor((DateTime.UtcNow - ultimo.DataHora).TotalDays)
            : null;

        resumo.DataAtualizacao = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private static RegistroGlicemiaResponse Mapear(
        RegistroGlicemia r, string contexto, Paciente paciente)
    {
        var min = paciente.GlicemiaMinAlvo ?? CalculadoraNutricional.GlicemiaMinPadrao;
        var max = paciente.GlicemiaMaxAlvo ?? CalculadoraNutricional.GlicemiaMaxPadrao;

        return new RegistroGlicemiaResponse(
            r.Id, r.PacienteId, r.Valor, contexto, r.ContextoId, r.DataHora, r.Observacao,
            r.ForaDoAlvo,
            CalculadoraNutricional.ClassificarGlicemia(r.Valor),
            min, max,
            paciente.GlicemiaMinAlvo is not null && paciente.GlicemiaMaxAlvo is not null);
    }
}
