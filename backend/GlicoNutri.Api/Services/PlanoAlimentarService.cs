using GlicoNutri.Api.Data;
using GlicoNutri.Api.Dtos;
using GlicoNutri.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GlicoNutri.Api.Services;

public interface IPlanoAlimentarService
{
    Task<Resultado<PlanoAlimentarResponse>> CriarAsync(
        long pacienteId, long nutricionistaId, CriarPlanoRequest pedido, CancellationToken ct = default);

    Task<PlanoAlimentarResponse?> ObterAtivoAsync(long pacienteId, CancellationToken ct = default);
    Task<PlanoAlimentarResponse?> ObterAsync(long planoId, CancellationToken ct = default);
    Task<IReadOnlyList<PlanoAlimentarResponse>> HistoricoAsync(long pacienteId, CancellationToken ct = default);

    Task<Resultado<DistribuicaoResponse>> SugerirDistribuicaoAsync(
        long pacienteId, DistribuicaoRequest? ajuste, CancellationToken ct = default);

    Task<Resultado<PlanoAlimentarResponse>> DuplicarAsync(
        long planoId, long nutricionistaId, DateOnly dataInicio, CancellationToken ct = default);

    Task<Resultado<PlanoAlimentarResponse>> ReativarAsync(long planoId, CancellationToken ct = default);
    Task<Resultado<bool>> InativarAsync(long planoId, CancellationToken ct = default);
}

/// <summary>
/// UC007 — Elaborar Plano Alimentar. Encadeia as regras que sustentam a
/// prescrição: VET obrigatório (RN13), macros somando 100% (RN15), plano ativo
/// único (RN16) e histórico preservado (RN17).
/// </summary>
public class PlanoAlimentarService(GlicoNutriDbContext db) : IPlanoAlimentarService
{
    /// <summary>UC007, passo 7 — margem aceitável entre o montado e o VET prescrito.</summary>
    private const double MargemVet = 5.0;

    public async Task<Resultado<PlanoAlimentarResponse>> CriarAsync(
        long pacienteId, long nutricionistaId, CriarPlanoRequest pedido, CancellationToken ct = default)
    {
        var paciente = await db.Pacientes
            .FirstOrDefaultAsync(p => p.Id == pacienteId, ct);

        if (paciente is null)
            return Resultado<PlanoAlimentarResponse>.Erro("Paciente não encontrado.");

        // RN13 — sem cálculo energético não há plano: a prescrição precisa de
        // embasamento clínico, e não de um valor calórico arbitrário.
        var vet = await UltimoVetAsync(pacienteId, ct);
        if (vet is null)
            return Resultado<PlanoAlimentarResponse>.Erro(
                "Este paciente não possui cálculo de necessidade energética. " +
                "Calcule o VET antes de elaborar o plano alimentar.");

        if (pedido.DataFim is { } fim && fim < pedido.DataInicio)
            return Resultado<PlanoAlimentarResponse>.Erro(
                "A data de término não pode ser anterior à data de início.");

        // Pela RN04 o Administrador herda as permissões do Nutricionista, mas ele
        // não é um: o DER exige que nutricionista_id aponte para a tabela de
        // nutricionistas. Quando quem elabora é o Administrador, o plano fica
        // atribuído ao profissional responsável pelo paciente (RN07) — que é de
        // quem a prescrição é, clinicamente.
        if (!await db.Nutricionistas.AnyAsync(n => n.Id == nutricionistaId, ct))
        {
            var responsavel = await db.NutricionistaPaciente
                .Where(v => v.PacienteId == pacienteId && v.Ativo)
                .Select(v => (long?)v.NutricionistaId)
                .FirstOrDefaultAsync(ct);

            if (responsavel is null)
                return Resultado<PlanoAlimentarResponse>.Erro(
                    "Este paciente não possui nutricionista responsável ativo.");

            nutricionistaId = responsavel.Value;
        }

        // UC007 A2 — sempre executado. Sem ajuste explícito, aplica a distribuição
        // automática; com ajuste, é o RF03.2 sobrepondo o cálculo.
        var pct = pedido.Distribuicao ?? new DistribuicaoRequest(
            CalculadoraNutricional.PadraoCarboidratos,
            CalculadoraNutricional.PadraoProteinas,
            CalculadoraNutricional.PadraoLipidios);

        // RN15 — a soma tem de fechar em 100%, senão o plano não é confirmado.
        if (!CalculadoraNutricional.SomaCemPorCento(
                pct.CarboidratosPercentual, pct.ProteinasPercentual, pct.LipidiosPercentual))
        {
            var soma = pct.CarboidratosPercentual + pct.ProteinasPercentual + pct.LipidiosPercentual;
            return Resultado<PlanoAlimentarResponse>.Erro(
                $"A soma dos percentuais de macronutrientes deve ser 100%. Soma informada: {soma:0.##}%.");
        }

        var itens = pedido.Itens ?? [];
        if (itens.Count > 0)
        {
            var ids = itens.Select(i => i.AlimentoId).Distinct().ToList();
            var encontrados = await db.Alimentos.Where(a => ids.Contains(a.Id)).Select(a => a.Id).ToListAsync(ct);
            var faltando = ids.Except(encontrados).ToList();

            if (faltando.Count > 0)
                return Resultado<PlanoAlimentarResponse>.Erro(
                    $"Alimento não encontrado na base: {string.Join(", ", faltando)}.");
        }

        var (gCho, gPtn, gLip) = CalculadoraNutricional.MacrosEmGramas(
            vet.Value, pct.CarboidratosPercentual, pct.ProteinasPercentual, pct.LipidiosPercentual);

        // RN16 e UC007 A4 — o plano anterior é inativado por soft delete.
        //
        // A desativação vai num SaveChanges próprio, antes de inserir o novo. Num
        // único SaveChanges o EF não garante que o UPDATE que desativa saia antes
        // do INSERT que ativa, e o índice único parcial do banco rejeitaria o
        // lote. A transação explícita mantém tudo atômico mesmo assim.
        await using var transacao = await db.Database.BeginTransactionAsync(ct);

        var anteriores = await db.PlanosAlimentares
            .Where(p => p.PacienteId == pacienteId && p.Ativo)
            .ToListAsync(ct);

        if (anteriores.Count > 0)
        {
            foreach (var anterior in anteriores) anterior.Ativo = false;
            await db.SaveChangesAsync(ct);
        }

        var plano = new PlanoAlimentar
        {
            PacienteId = pacienteId,
            NutricionistaId = nutricionistaId,
            Objetivo = pedido.Objetivo.Trim(),
            DataInicio = pedido.DataInicio,
            DataFim = pedido.DataFim,
            Observacoes = pedido.Observacoes?.Trim(),
            TotalCaloriasPrescritas = vet,
            Ativo = true,
            Distribuicao = new DistribuicaoMacronutrientes
            {
                CarboidratosPercentual = pct.CarboidratosPercentual,
                ProteinasPercentual = pct.ProteinasPercentual,
                LipidiosPercentual = pct.LipidiosPercentual,
                TotalCaloriasPrescritas = vet,
                CarboidratosGramas = gCho,
                ProteinasGramas = gPtn,
                LipidiosGramas = gLip,
            },
        };

        foreach (var item in itens)
        {
            plano.Itens.Add(new ItemPlanoAlimentar
            {
                AlimentoId = item.AlimentoId,
                Dia = item.Dia,
                Horario = item.Horario,
                Refeicao = item.Refeicao?.Trim(),
                Quantidade = item.Quantidade,
                Unidade = string.IsNullOrWhiteSpace(item.Unidade) ? "g" : item.Unidade.Trim(),
                Ativo = true,
            });
        }

        db.PlanosAlimentares.Add(plano);

        await db.SaveChangesAsync(ct);
        await transacao.CommitAsync(ct);

        return Resultado<PlanoAlimentarResponse>.Ok((await ObterAsync(plano.Id, ct))!);
    }

    /// <summary>
    /// UC007 A2 — prévia da distribuição sobre o VET vigente, para a tela mostrar
    /// percentuais e gramas antes de o plano existir.
    /// </summary>
    public async Task<Resultado<DistribuicaoResponse>> SugerirDistribuicaoAsync(
        long pacienteId, DistribuicaoRequest? ajuste, CancellationToken ct = default)
    {
        var vet = await UltimoVetAsync(pacienteId, ct);
        if (vet is null)
            return Resultado<DistribuicaoResponse>.Erro(
                "Este paciente não possui cálculo de necessidade energética.");

        var pct = ajuste ?? new DistribuicaoRequest(
            CalculadoraNutricional.PadraoCarboidratos,
            CalculadoraNutricional.PadraoProteinas,
            CalculadoraNutricional.PadraoLipidios);

        if (!CalculadoraNutricional.SomaCemPorCento(
                pct.CarboidratosPercentual, pct.ProteinasPercentual, pct.LipidiosPercentual))
        {
            var soma = pct.CarboidratosPercentual + pct.ProteinasPercentual + pct.LipidiosPercentual;
            return Resultado<DistribuicaoResponse>.Erro(
                $"A soma dos percentuais deve ser 100%. Soma informada: {soma:0.##}%.");
        }

        var (gCho, gPtn, gLip) = CalculadoraNutricional.MacrosEmGramas(
            vet.Value, pct.CarboidratosPercentual, pct.ProteinasPercentual, pct.LipidiosPercentual);

        return Resultado<DistribuicaoResponse>.Ok(new DistribuicaoResponse(
            pct.CarboidratosPercentual, pct.ProteinasPercentual, pct.LipidiosPercentual,
            gCho, gPtn, gLip, vet.Value));
    }

    public Task<PlanoAlimentarResponse?> ObterAsync(long planoId, CancellationToken ct = default) =>
        MontarAsync(db.PlanosAlimentares.IgnoreQueryFilters().Where(p => p.Id == planoId), ct);

    public Task<PlanoAlimentarResponse?> ObterAtivoAsync(long pacienteId, CancellationToken ct = default) =>
        MontarAsync(db.PlanosAlimentares.Where(p => p.PacienteId == pacienteId && p.Ativo), ct);

    /// <summary>RN17 — histórico completo, inclusive os planos já inativados.</summary>
    public async Task<IReadOnlyList<PlanoAlimentarResponse>> HistoricoAsync(
        long pacienteId, CancellationToken ct = default)
    {
        var ids = await db.PlanosAlimentares
            .IgnoreQueryFilters()
            .Where(p => p.PacienteId == pacienteId)
            .OrderByDescending(p => p.Ativo).ThenByDescending(p => p.DataInicio).ThenByDescending(p => p.Id)
            .Select(p => p.Id)
            .ToListAsync(ct);

        var planos = new List<PlanoAlimentarResponse>(ids.Count);
        foreach (var id in ids)
        {
            if (await ObterAsync(id, ct) is { } plano) planos.Add(plano);
        }

        return planos;
    }

    /// <summary>
    /// UC007 A3 — duplica um plano existente como base do novo, para o
    /// nutricionista fazer só ajustes pontuais. O plano de origem permanece no
    /// histórico; o novo nasce ativo e inativa o vigente pela regra da RN16.
    /// </summary>
    public async Task<Resultado<PlanoAlimentarResponse>> DuplicarAsync(
        long planoId, long nutricionistaId, DateOnly dataInicio, CancellationToken ct = default)
    {
        var origem = await db.PlanosAlimentares
            .IgnoreQueryFilters()
            .Include(p => p.Distribuicao)
            .Include(p => p.Itens.Where(i => i.Ativo))
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == planoId, ct);

        if (origem is null) return Resultado<PlanoAlimentarResponse>.Erro("Plano de origem não encontrado.");

        var pedido = new CriarPlanoRequest(
            origem.Objetivo ?? "Plano alimentar",
            dataInicio,
            null,
            origem.Observacoes,
            origem.Distribuicao is { } d
                ? new DistribuicaoRequest(d.CarboidratosPercentual, d.ProteinasPercentual, d.LipidiosPercentual)
                : null,
            origem.Itens
                .Select(i => new ItemPlanoRequest(
                    i.AlimentoId, i.Dia, i.Horario, i.Refeicao, i.Quantidade, i.Unidade))
                .ToList());

        // Passa pelo fluxo normal de criação, para que RN13, RN15 e RN16 sejam
        // reavaliadas — o VET pode ter mudado desde o plano de origem.
        return await CriarAsync(origem.PacienteId, nutricionistaId, pedido, ct);
    }

    /// <summary>
    /// RN17 — o Nutricionista pode reativar um plano anterior. Reativar exige
    /// inativar o vigente, para não violar a RN16.
    /// </summary>
    public async Task<Resultado<PlanoAlimentarResponse>> ReativarAsync(
        long planoId, CancellationToken ct = default)
    {
        var plano = await db.PlanosAlimentares
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == planoId, ct);

        if (plano is null) return Resultado<PlanoAlimentarResponse>.Erro("Plano não encontrado.");
        if (plano.Ativo) return Resultado<PlanoAlimentarResponse>.Erro("Este plano já está ativo.");

        // Mesma ordem obrigatória da criação: liberar o slot antes de ocupá-lo,
        // senão o índice único parcial da RN16 recusa o lote.
        await using var transacao = await db.Database.BeginTransactionAsync(ct);

        var vigentes = await db.PlanosAlimentares
            .Where(p => p.PacienteId == plano.PacienteId && p.Ativo)
            .ToListAsync(ct);

        if (vigentes.Count > 0)
        {
            foreach (var vigente in vigentes) vigente.Ativo = false;
            await db.SaveChangesAsync(ct);
        }

        plano.Ativo = true;
        await db.SaveChangesAsync(ct);
        await transacao.CommitAsync(ct);

        return Resultado<PlanoAlimentarResponse>.Ok((await ObterAsync(planoId, ct))!);
    }

    public async Task<Resultado<bool>> InativarAsync(long planoId, CancellationToken ct = default)
    {
        var plano = await db.PlanosAlimentares.FirstOrDefaultAsync(p => p.Id == planoId, ct);
        if (plano is null) return Resultado<bool>.Erro("Plano não encontrado.");

        plano.Ativo = false;

        await db.SaveChangesAsync(ct);
        return Resultado<bool>.Ok(true);
    }

    /// <summary>RN13 e RN03 do UC008 — o cálculo mais recente é a base do plano.</summary>
    private async Task<double?> UltimoVetAsync(long pacienteId, CancellationToken ct) =>
        await db.NecessidadesEnergeticas
            .Where(n => n.PacienteId == pacienteId)
            .OrderByDescending(n => n.DataCalculo).ThenByDescending(n => n.Id)
            .Select(n => (double?)n.ValorKcal)
            .FirstOrDefaultAsync(ct);

    private async Task<PlanoAlimentarResponse?> MontarAsync(
        IQueryable<PlanoAlimentar> consulta, CancellationToken ct)
    {
        var plano = await consulta
            .Include(p => p.Paciente)
            .Include(p => p.Distribuicao)
            .Include(p => p.Itens.Where(i => i.Ativo)).ThenInclude(i => i.Alimento)
            .AsSplitQuery()
            .FirstOrDefaultAsync(ct);

        if (plano is null) return null;

        var itens = plano.Itens.Select(i => new ItemPlanoResponse(
            i.Id, i.AlimentoId, i.Alimento.Nome, i.Dia, i.Horario, i.Refeicao,
            i.Quantidade, i.Unidade,
            CalculadoraNutricional.PorPorcao(i.Alimento.CaloriasPor100g, i.Quantidade),
            CalculadoraNutricional.PorPorcao(i.Alimento.CarboidratosPor100g, i.Quantidade),
            CalculadoraNutricional.PorPorcao(i.Alimento.ProteinasPor100g, i.Quantidade),
            CalculadoraNutricional.PorPorcao(i.Alimento.LipidiosPor100g, i.Quantidade)))
            .OrderBy(i => i.Dia).ThenBy(i => i.Horario).ThenBy(i => i.AlimentoNome)
            .ToList();

        // UC007, passo 6 — subtotais por refeição.
        var totaisPorRefeicao = itens
            .GroupBy(i => i.Refeicao ?? "Sem refeição")
            .Select(g => new TotaisRefeicao(
                g.Key,
                Math.Round(g.Sum(i => i.Calorias ?? 0), 2),
                Math.Round(g.Sum(i => i.Carboidratos ?? 0), 2),
                Math.Round(g.Sum(i => i.Proteinas ?? 0), 2),
                Math.Round(g.Sum(i => i.Lipidios ?? 0), 2)))
            .OrderBy(t => t.Refeicao)
            .ToList();

        var montadas = Math.Round(itens.Sum(i => i.Calorias ?? 0), 2);
        var prescritas = plano.TotalCaloriasPrescritas;

        // UC007, passo 7 — o desvio é informado, não bloqueia: um nutricionista
        // pode prescrever fora da margem com intenção clínica. A tela sinaliza.
        var desvio = prescritas is > 0 && itens.Count > 0
            ? Math.Round((montadas - prescritas.Value) / prescritas.Value * 100, 1)
            : (double?)null;

        var dist = plano.Distribuicao is { } d
            ? new DistribuicaoResponse(
                d.CarboidratosPercentual, d.ProteinasPercentual, d.LipidiosPercentual,
                d.CarboidratosGramas ?? 0, d.ProteinasGramas ?? 0, d.LipidiosGramas ?? 0,
                d.TotalCaloriasPrescritas ?? 0)
            : null;

        return new PlanoAlimentarResponse(
            plano.Id, plano.PacienteId, plano.Paciente.Nome, plano.NutricionistaId,
            plano.Objetivo, plano.DataInicio, plano.DataFim, plano.Observacoes,
            prescritas, plano.Ativo, dist, itens, totaisPorRefeicao, montadas,
            desvio, desvio is null || Math.Abs(desvio.Value) <= MargemVet);
    }
}
