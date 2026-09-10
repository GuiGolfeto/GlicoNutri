using GlicoNutri.Api.Data;
using GlicoNutri.Api.Dtos;
using GlicoNutri.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GlicoNutri.Api.Services;

public interface INecessidadeEnergeticaService
{
    Task<Resultado<NecessidadeEnergeticaResponse>> SimularAsync(
        long pacienteId, CalcularVetRequest pedido, CancellationToken ct = default);

    Task<Resultado<NecessidadeEnergeticaResponse>> CalcularESalvarAsync(
        long pacienteId, CalcularVetRequest pedido, CancellationToken ct = default);

    Task<IReadOnlyList<NecessidadeEnergeticaResponse>> HistoricoAsync(
        long pacienteId, CancellationToken ct = default);
}

/// <summary>
/// UC006 — Calcular Necessidade Energética. Só fórmulas validadas (RN14), com a
/// fórmula usada registrada em cada cálculo para rastreabilidade clínica.
/// </summary>
public class NecessidadeEnergeticaService(GlicoNutriDbContext db) : INecessidadeEnergeticaService
{
    /// <summary>Dados de entrada resolvidos a partir do paciente e da última medição.</summary>
    private record Base(
        Paciente Paciente,
        RegistroAntropometrico Medicao,
        int Idade,
        bool Masculino);

    public async Task<Resultado<NecessidadeEnergeticaResponse>> SimularAsync(
        long pacienteId, CalcularVetRequest pedido, CancellationToken ct = default) =>
        await CalcularAsync(pacienteId, pedido, persistir: false, ct);

    public async Task<Resultado<NecessidadeEnergeticaResponse>> CalcularESalvarAsync(
        long pacienteId, CalcularVetRequest pedido, CancellationToken ct = default) =>
        await CalcularAsync(pacienteId, pedido, persistir: true, ct);

    private async Task<Resultado<NecessidadeEnergeticaResponse>> CalcularAsync(
        long pacienteId, CalcularVetRequest pedido, bool persistir, CancellationToken ct)
    {
        var resolucao = await ResolverBaseAsync(pacienteId, ct);
        if (!resolucao.Sucesso) return Resultado<NecessidadeEnergeticaResponse>.Erro(resolucao.Mensagem!);
        var dados = resolucao.Dados!;

        var formula = await db.FormulasEnergeticas
            .FirstOrDefaultAsync(f => f.Id == pedido.FormulaId && f.Ativo, ct);
        if (formula is null)
            return Resultado<NecessidadeEnergeticaResponse>.Erro("Fórmula de cálculo inválida.");

        var nivel = await db.NiveisAtividade
            .FirstOrDefaultAsync(n => n.Id == pedido.NivelAtividadeId && n.Ativo, ct);
        if (nivel is null)
            return Resultado<NecessidadeEnergeticaResponse>.Erro("Nível de atividade inválido.");

        double tmb;
        try
        {
            // RN14 — uma fórmula não implementada falha aqui, em vez de produzir
            // um valor calórico sem respaldo.
            tmb = CalculadoraNutricional.CalcularTmb(
                formula.Codigo, dados.Medicao.Peso, dados.Medicao.Altura, dados.Idade, dados.Masculino);
        }
        catch (ArgumentOutOfRangeException e)
        {
            return Resultado<NecessidadeEnergeticaResponse>.Erro(e.Message);
        }

        var vet = CalculadoraNutricional.CalcularVet(tmb, nivel.Fator);

        // UC006 A3 — o cálculo anterior entra no resultado para comparação.
        var anterior = await db.NecessidadesEnergeticas
            .Where(n => n.PacienteId == pacienteId)
            .OrderByDescending(n => n.DataCalculo).ThenByDescending(n => n.Id)
            .FirstOrDefaultAsync(ct);

        long? id = null;
        var dataCalculo = DateOnly.FromDateTime(DateTime.UtcNow);

        if (persistir)
        {
            // RN04 do UC006 — cada cálculo é versionado e o histórico completo é
            // preservado: o anterior continua ativo, nada é sobrescrito.
            var registro = new NecessidadeEnergetica
            {
                PacienteId = pacienteId,
                FormulaId = formula.Id,
                NivelAtividadeId = nivel.Id,
                Objetivo = pedido.Objetivo?.Trim(),
                DataCalculo = dataCalculo,
                ValorKcal = vet,
                Ativo = true,
            };

            db.NecessidadesEnergeticas.Add(registro);
            await db.SaveChangesAsync(ct);
            id = registro.Id;
        }

        return Resultado<NecessidadeEnergeticaResponse>.Ok(new NecessidadeEnergeticaResponse(
            id, pacienteId, formula.Descricao, nivel.Descricao, nivel.Fator,
            Math.Round(tmb, 0), vet, pedido.Objetivo, dataCalculo,
            dados.Medicao.Peso, dados.Medicao.Altura, dados.Idade,
            dados.Paciente.Sexo.Descricao, dados.Medicao.DataHora,
            anterior?.ValorKcal,
            CalculadoraNutricional.VariacaoPercentual(anterior?.ValorKcal, vet)));
    }

    public async Task<IReadOnlyList<NecessidadeEnergeticaResponse>> HistoricoAsync(
        long pacienteId, CancellationToken ct = default)
    {
        var registros = await db.NecessidadesEnergeticas
            .Include(n => n.Formula)
            .Include(n => n.NivelAtividade)
            .Where(n => n.PacienteId == pacienteId)
            .OrderByDescending(n => n.DataCalculo).ThenByDescending(n => n.Id)
            .ToListAsync(ct);

        return registros.Select((n, i) =>
        {
            var anterior = i + 1 < registros.Count ? registros[i + 1] : null;
            return new NecessidadeEnergeticaResponse(
                n.Id, n.PacienteId, n.Formula.Descricao, n.NivelAtividade.Descricao,
                n.NivelAtividade.Fator,
                // A TMB fica nula no histórico. O DER V2.0 persiste apenas o VET,
                // e reconstituí-la dividindo o valor já arredondado devolveria um
                // número que não reproduz o do cálculo original — inaceitável num
                // dado clínico. O que importa para a rastreabilidade da RN04 está
                // preservado: fórmula, fator, VET e data.
                null,
                n.ValorKcal, n.Objetivo, n.DataCalculo,
                null, null, null, null, null,
                anterior?.ValorKcal,
                CalculadoraNutricional.VariacaoPercentual(anterior?.ValorKcal, n.ValorKcal));
        }).ToList();
    }

    /// <summary>
    /// UC006 A2 — sempre executado: sem peso, altura, idade e sexo o cálculo é
    /// bloqueado e o nutricionista é mandado ao UC008.
    /// </summary>
    private async Task<Resultado<Base>> ResolverBaseAsync(long pacienteId, CancellationToken ct)
    {
        var paciente = await db.Pacientes
            .Include(p => p.Sexo)
            .FirstOrDefaultAsync(p => p.Id == pacienteId, ct);

        if (paciente is null) return Resultado<Base>.Erro("Paciente não encontrado.");

        // RN03 do UC008 — a medição mais recente é a referência do VET.
        var medicao = await db.RegistrosAntropometricos
            .Where(r => r.PacienteId == pacienteId)
            .OrderByDescending(r => r.DataHora)
            .FirstOrDefaultAsync(ct);

        if (medicao is null)
            return Resultado<Base>.Erro(
                "Este paciente ainda não possui registro antropométrico. " +
                "Registre peso e altura antes de calcular a necessidade energética.");

        var idade = CalculadoraNutricional.CalcularIdade(
            paciente.DataNascimento, DateOnly.FromDateTime(DateTime.UtcNow));

        if (idade <= 0) return Resultado<Base>.Erro("Data de nascimento do paciente inválida.");

        var masculino = paciente.Sexo.Codigo == Codigos.Sexo.Masculino;

        return Resultado<Base>.Ok(new Base(paciente, medicao, idade, masculino));
    }
}
