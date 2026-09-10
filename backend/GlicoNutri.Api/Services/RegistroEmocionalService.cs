using GlicoNutri.Api.Data;
using GlicoNutri.Api.Dtos;
using GlicoNutri.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GlicoNutri.Api.Services;

public interface IRegistroEmocionalService
{
    Task<Resultado<IReadOnlyList<RegistroEmocionalResponse>>> RegistrarAsync(
        long pacienteId, CriarRegistroEmocionalRequest pedido, CancellationToken ct = default);

    Task<Resultado<DiarioEmocionalResponse>> DiarioAsync(
        long pacienteId, int dias, CancellationToken ct = default);

    Task<Resultado<CorrelacaoEmocionalResponse>> CorrelacaoAsync(
        long pacienteId, int dias, CancellationToken ct = default);

    Task<Resultado<bool>> RemoverAsync(long registroId, CancellationToken ct = default);
}

/// <summary>
/// UC012 — Registrar Emoção, e a análise de correlação do RF06.2.
/// </summary>
public class RegistroEmocionalService(GlicoNutriDbContext db) : IRegistroEmocionalService
{
    private static readonly int[] PeriodosAceitos = [7, 15, 30, 60, 90];

    /// <summary>
    /// Janela para associar uma emoção a uma medição de glicemia.
    ///
    /// O DER V2.0 não liga registros_emocionais a registros_glicemia por chave
    /// estrangeira — a RN22 fala em vínculo de contexto, não de referência. Sem
    /// FK, a proximidade no tempo é o único vínculo disponível, e duas horas
    /// cobrem o intervalo entre uma refeição e a medição pós-prandial.
    /// </summary>
    private const int JanelaHoras = 2;

    public async Task<Resultado<IReadOnlyList<RegistroEmocionalResponse>>> RegistrarAsync(
        long pacienteId, CriarRegistroEmocionalRequest pedido, CancellationToken ct = default)
    {
        if (!await db.Pacientes.AnyAsync(p => p.Id == pacienteId, ct))
            return Resultado<IReadOnlyList<RegistroEmocionalResponse>>.Erro("Paciente não encontrado.");

        // RN01 do UC012 — ao menos uma emoção.
        var ids = pedido.EstadosEmocionaisIds.Distinct().ToList();
        if (ids.Count == 0)
            return Resultado<IReadOnlyList<RegistroEmocionalResponse>>.Erro(
                "Selecione ao menos uma emoção.");

        var estados = await db.EstadosEmocionais
            .Where(e => ids.Contains(e.Id) && e.Ativo)
            .ToListAsync(ct);

        if (estados.Count != ids.Count)
            return Resultado<IReadOnlyList<RegistroEmocionalResponse>>.Erro(
                "Estado emocional inválido.");

        var dataHora = pedido.DataHora ?? DateTime.UtcNow;
        if (dataHora > DateTime.UtcNow.AddMinutes(5))
            return Resultado<IReadOnlyList<RegistroEmocionalResponse>>.Erro(
                "A data do registro não pode estar no futuro.");

        var novos = estados.Select(e => new RegistroEmocional
        {
            PacienteId = pacienteId,
            EstadoEmocionalId = e.Id,
            Intensidade = pedido.Intensidade,
            DataHora = dataHora,
            Descricao = pedido.Descricao?.Trim(),
            Ativo = true,
        }).ToList();

        db.RegistrosEmocionais.AddRange(novos);
        await db.SaveChangesAsync(ct);

        var resposta = novos.Select(r => new RegistroEmocionalResponse(
            r.Id, r.PacienteId, r.EstadoEmocionalId,
            estados.First(e => e.Id == r.EstadoEmocionalId).Descricao,
            r.Intensidade, r.DataHora, r.Descricao)).ToList();

        return Resultado<IReadOnlyList<RegistroEmocionalResponse>>.Ok(resposta);
    }

    /// <summary>Diário emocional do período. RN04 do UC012 — visível ao Nutricionista.</summary>
    public async Task<Resultado<DiarioEmocionalResponse>> DiarioAsync(
        long pacienteId, int dias, CancellationToken ct = default)
    {
        if (!PeriodosAceitos.Contains(dias))
            return Resultado<DiarioEmocionalResponse>.Erro(
                $"Período inválido. Use um destes: {string.Join(", ", PeriodosAceitos)} dias.");

        var inicio = DateTime.UtcNow.AddDays(-dias);

        var registros = await db.RegistrosEmocionais
            .Include(r => r.EstadoEmocional)
            .Where(r => r.PacienteId == pacienteId && r.DataHora >= inicio)
            .OrderByDescending(r => r.DataHora)
            .Select(r => new RegistroEmocionalResponse(
                r.Id, r.PacienteId, r.EstadoEmocionalId, r.EstadoEmocional.Descricao,
                r.Intensidade, r.DataHora, r.Descricao))
            .ToListAsync(ct);

        return Resultado<DiarioEmocionalResponse>.Ok(
            new DiarioEmocionalResponse(dias, inicio, registros.Count, registros));
    }

    /// <summary>
    /// RF06.2 — correlação entre estado emocional e variação glicêmica. Para cada
    /// emoção, agrega as medições feitas dentro da janela em torno dos registros
    /// daquele estado.
    /// </summary>
    public async Task<Resultado<CorrelacaoEmocionalResponse>> CorrelacaoAsync(
        long pacienteId, int dias, CancellationToken ct = default)
    {
        if (!PeriodosAceitos.Contains(dias))
            return Resultado<CorrelacaoEmocionalResponse>.Erro(
                $"Período inválido. Use um destes: {string.Join(", ", PeriodosAceitos)} dias.");

        var inicio = DateTime.UtcNow.AddDays(-dias);

        var emocionais = await db.RegistrosEmocionais
            .Include(r => r.EstadoEmocional)
            .Where(r => r.PacienteId == pacienteId && r.DataHora >= inicio)
            .Select(r => new
            {
                r.EstadoEmocionalId,
                Emocao = r.EstadoEmocional.Descricao,
                r.Intensidade,
                r.DataHora,
            })
            .ToListAsync(ct);

        var glicemias = await db.RegistrosGlicemia
            .Where(r => r.PacienteId == pacienteId && r.DataHora >= inicio)
            .Select(r => new { r.Valor, r.DataHora, r.ForaDoAlvo })
            .ToListAsync(ct);

        var janela = TimeSpan.FromHours(JanelaHoras);

        var porEmocao = emocionais
            .GroupBy(e => new { e.EstadoEmocionalId, e.Emocao })
            .Select(grupo =>
            {
                // Uma medição pode cair na janela de mais de um registro emocional;
                // contada uma vez só por emoção, para não inflar a média.
                var associadas = glicemias
                    .Where(g => grupo.Any(e =>
                        (g.DataHora - e.DataHora).Duration() <= janela))
                    .ToList();

                return new CorrelacaoEmocao(
                    grupo.Key.Emocao,
                    grupo.Count(),
                    associadas.Count,
                    associadas.Count > 0 ? Math.Round(associadas.Average(g => g.Valor), 1) : null,
                    associadas.Count > 0 ? associadas.Min(g => g.Valor) : null,
                    associadas.Count > 0 ? associadas.Max(g => g.Valor) : null,
                    associadas.Count > 0
                        ? Math.Round(associadas.Count(g => g.ForaDoAlvo) * 100.0 / associadas.Count, 1)
                        : null,
                    Math.Round(grupo.Average(e => (double)e.Intensidade), 1));
            })
            .OrderByDescending(c => c.RegistrosEmocionais)
            .ToList();

        return Resultado<CorrelacaoEmocionalResponse>.Ok(new CorrelacaoEmocionalResponse(
            dias, JanelaHoras, emocionais.Count, glicemias.Count,
            glicemias.Count > 0 ? Math.Round(glicemias.Average(g => g.Valor), 1) : null,
            porEmocao));
    }

    public async Task<Resultado<bool>> RemoverAsync(long registroId, CancellationToken ct = default)
    {
        var registro = await db.RegistrosEmocionais.FirstOrDefaultAsync(r => r.Id == registroId, ct);
        if (registro is null) return Resultado<bool>.Erro("Registro não encontrado.");

        registro.Ativo = false;
        await db.SaveChangesAsync(ct);
        return Resultado<bool>.Ok(true);
    }
}
