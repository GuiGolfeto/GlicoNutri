using GlicoNutri.Api.Data;
using GlicoNutri.Api.Dtos;
using GlicoNutri.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GlicoNutri.Api.Services;

public interface IAntropometriaService
{
    Task<Resultado<RegistroAntropometricoResponse>> RegistrarAsync(
        long pacienteId, CriarRegistroAntropometricoRequest pedido, CancellationToken ct = default);

    Task<IReadOnlyList<RegistroAntropometricoResponse>> HistoricoAsync(
        long pacienteId, CancellationToken ct = default);

    Task<Resultado<bool>> RemoverAsync(long registroId, CancellationToken ct = default);

    /// <summary>RF08.2 — série de peso e IMC para o gráfico de evolução.</summary>
    Task<Resultado<SerieAntropometricaResponse>> SerieAsync(
        long pacienteId, int dias, CancellationToken ct = default);
}

/// <summary>
/// UC008 — Registrar Dados Antropométricos. Calcula e classifica o IMC (RN21),
/// deriva a RCQ e mantém o read model do dashboard em dia.
/// </summary>
public class AntropometriaService(GlicoNutriDbContext db) : IAntropometriaService
{
    public async Task<Resultado<RegistroAntropometricoResponse>> RegistrarAsync(
        long pacienteId, CriarRegistroAntropometricoRequest pedido, CancellationToken ct = default)
    {
        var paciente = await db.Pacientes
            .Include(p => p.Resumo)
            .FirstOrDefaultAsync(p => p.Id == pacienteId, ct);

        if (paciente is null)
            return Resultado<RegistroAntropometricoResponse>.Erro("Paciente não encontrado.");

        var dataHora = pedido.DataHora ?? DateTime.UtcNow;
        if (dataHora > DateTime.UtcNow.AddMinutes(5))
            return Resultado<RegistroAntropometricoResponse>.Erro(
                "A data da medição não pode estar no futuro.");

        // UC008 A3 — o comparativo é contra a medição imediatamente anterior no
        // tempo, e não contra a mais recente do paciente. A distinção só aparece
        // num lançamento retroativo, e sem ela o mesmo registro mostraria uma
        // variação no momento do cadastro e outra no histórico.
        var anterior = await db.RegistrosAntropometricos
            .Where(r => r.PacienteId == pacienteId && r.DataHora < dataHora)
            .OrderByDescending(r => r.DataHora)
            .FirstOrDefaultAsync(ct);

        // UC008 A2 — sempre executado (RN21): o IMC nunca é informado à mão.
        var (imc, classificacao) = CalculadoraNutricional.CalcularImc(pedido.Peso, pedido.Altura);

        var registro = new RegistroAntropometrico
        {
            PacienteId = pacienteId,
            Peso = pedido.Peso,
            Altura = pedido.Altura,
            CircunferenciaAbdominal = pedido.CircunferenciaAbdominal,
            CircunferenciaCintura = pedido.CircunferenciaCintura,
            CircunferenciaQuadril = pedido.CircunferenciaQuadril,
            CircunferenciaBraco = pedido.CircunferenciaBraco,
            Rcq = CalculadoraNutricional.CalcularRcq(pedido.CircunferenciaCintura, pedido.CircunferenciaQuadril),
            Imc = imc,
            ClassificacaoImc = classificacao,
            DataHora = dataHora,
            Ativo = true,
        };

        db.RegistrosAntropometricos.Add(registro);

        AtualizarResumo(paciente, registro);

        await db.SaveChangesAsync(ct);

        return Resultado<RegistroAntropometricoResponse>.Ok(Mapear(registro, anterior));
    }

    public async Task<IReadOnlyList<RegistroAntropometricoResponse>> HistoricoAsync(
        long pacienteId, CancellationToken ct = default)
    {
        // RF04.2 — histórico cronológico. Vem do mais recente para o mais antigo,
        // e cada item carrega a variação em relação ao anterior no tempo.
        var registros = await db.RegistrosAntropometricos
            .Where(r => r.PacienteId == pacienteId)
            .OrderByDescending(r => r.DataHora)
            .ToListAsync(ct);

        return registros
            .Select((r, i) => Mapear(r, i + 1 < registros.Count ? registros[i + 1] : null))
            .ToList();
    }

    /// <summary>
    /// RF08.2 — série cronológica de peso e IMC. O caso de uso oferece 30, 60 e
    /// 90 dias como períodos de filtro.
    /// </summary>
    public async Task<Resultado<SerieAntropometricaResponse>> SerieAsync(
        long pacienteId, int dias, CancellationToken ct = default)
    {
        int[] periodosAceitos = [7, 15, 30, 60, 90, 180, 365];
        if (!periodosAceitos.Contains(dias))
            return Resultado<SerieAntropometricaResponse>.Erro(
                $"Período inválido. Use um destes: {string.Join(", ", periodosAceitos)} dias.");

        var inicio = DateTime.UtcNow.AddDays(-dias);

        var pontos = await db.RegistrosAntropometricos
            .Where(r => r.PacienteId == pacienteId && r.DataHora >= inicio)
            .OrderBy(r => r.DataHora)
            .Select(r => new PontoSerieAntropometrica(r.DataHora, r.Peso, r.Imc, r.ClassificacaoImc))
            .ToListAsync(ct);

        return Resultado<SerieAntropometricaResponse>.Ok(
            new SerieAntropometricaResponse(dias, pontos));
    }

    /// <summary>RN05 e o soft delete do DER: remover é inativar, nunca apagar.</summary>
    public async Task<Resultado<bool>> RemoverAsync(long registroId, CancellationToken ct = default)
    {
        var registro = await db.RegistrosAntropometricos
            .FirstOrDefaultAsync(r => r.Id == registroId, ct);

        if (registro is null) return Resultado<bool>.Erro("Registro não encontrado.");

        registro.Ativo = false;

        // O resumo pode estar apontando para o registro que acabou de sair.
        var paciente = await db.Pacientes
            .Include(p => p.Resumo)
            .FirstAsync(p => p.Id == registro.PacienteId, ct);

        var maisRecente = await db.RegistrosAntropometricos
            .Where(r => r.PacienteId == registro.PacienteId && r.Id != registroId)
            .OrderByDescending(r => r.DataHora)
            .FirstOrDefaultAsync(ct);

        if (paciente.Resumo is { } resumo)
        {
            resumo.UltimoImc = maisRecente?.Imc;
            resumo.UltimaClassificacaoImc = maisRecente?.ClassificacaoImc;
            resumo.UltimaDataAntropometria = maisRecente?.DataHora;
            resumo.DataAtualizacao = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        return Resultado<bool>.Ok(true);
    }

    /// <summary>
    /// O read model é atualizado pela camada de serviço após cada persistência,
    /// como manda a nota do DER V2.0. Só avança se este for o registro mais
    /// recente — um lançamento retroativo não deve sobrescrever o atual.
    /// </summary>
    private static void AtualizarResumo(Paciente paciente, RegistroAntropometrico registro)
    {
        if (paciente.Resumo is not { } resumo) return;

        if (resumo.UltimaDataAntropometria is { } ultima && registro.DataHora < ultima) return;

        resumo.UltimoImc = registro.Imc;
        resumo.UltimaClassificacaoImc = registro.ClassificacaoImc;
        resumo.UltimaDataAntropometria = registro.DataHora;
        resumo.DataAtualizacao = DateTime.UtcNow;
    }

    private static RegistroAntropometricoResponse Mapear(
        RegistroAntropometrico r, RegistroAntropometrico? anterior)
    {
        ComparativoMedida? comparar(double? antes, double? agora)
        {
            if (agora is not { } atual) return null;
            var delta = antes is { } a ? Math.Round(atual - a, 2) : (double?)null;
            return new ComparativoMedida(
                antes, atual, delta, CalculadoraNutricional.VariacaoPercentual(antes, atual));
        }

        return new RegistroAntropometricoResponse(
            r.Id, r.PacienteId, r.Peso, r.Altura,
            r.CircunferenciaAbdominal, r.CircunferenciaCintura,
            r.CircunferenciaQuadril, r.CircunferenciaBraco,
            r.Rcq, r.Imc, r.ClassificacaoImc, r.DataHora,
            comparar(anterior?.Peso, r.Peso),
            comparar(anterior?.Imc, r.Imc));
    }
}
